using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace VoxelTerrain
{
    public class ChunkManager : MonoBehaviour
    {
        Camera _camera;
        DensityFieldBuilder _densityFieldBuilder;
        MeshBuilder _meshBuilder;
        BrushTools _brushTools;
        ModelTools _modelTools;

        [SerializeField]
        ChunkSetting _chunkSetting;
        [SerializeField]
        LodSetting _lodSetting;

        [SerializeField]
        TextMeshProUGUI _text;
        [SerializeField]
        TextMeshProUGUI _pathInputField;

        GameObject _terrainGO;
        Dictionary<Vector3Int, GameObject> _cachedGOs;
        Queue<Vector3Int> _creatingGOs;
        Queue<Vector3Int> _releasingGOs;
        Dictionary<Vector3Int, float[]> _cachedFields;
        Dictionary<Vector3Int, float[]> _editedFields;

        Dictionary<Vector3Int, ChunkNode> _visibleNodes;
        ChunkNode _root;

        Queue<Vector3Int> _savingFields;
        Queue<Vector3Int> _loadingFields;

        [SerializeField]
        bool _isTriplane = false;
        [SerializeField]
        Material _triplane;
        [SerializeField]
        bool _isNoise = false;

        void Awake()
        {
            _camera = Camera.main;

            _densityFieldBuilder = GetComponent<DensityFieldBuilder>();
            _meshBuilder = GetComponent<MeshBuilder>();
            _brushTools = GetComponent<BrushTools>();
            _modelTools = GetComponent<ModelTools>();

            var numVoxelsPerAxisForChunk = _chunkSetting.NumVoxelsPerAxis;
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var levelOfDetail = _lodSetting.LevelOfDetail;
            var lodThresholds = _lodSetting.LodThresholds;
            var numVoxelsPerAxisForTerrain = numVoxelsPerAxisForChunk << levelOfDetail;

            _root = new ChunkNode(Vector3Int.zero, numVoxelsPerAxisForTerrain, levelOfDetail);

            _terrainGO = new GameObject();
            _terrainGO.name = "Terrain";
            _terrainGO.transform.position = Vector3.zero;

            _cachedGOs = new Dictionary<Vector3Int, GameObject>();
            _creatingGOs = new Queue<Vector3Int>();
            _releasingGOs = new Queue<Vector3Int>();
            _cachedFields = new Dictionary<Vector3Int, float[]>();
            _editedFields = new Dictionary<Vector3Int, float[]>();

            _savingFields = new Queue<Vector3Int>();
            _loadingFields = new Queue<Vector3Int>();

            UpdateByCamera();
        }

        void Update()
        {
            int trisCount = 0;
            foreach (var pair in _cachedGOs)
            {
                var chunkGO = pair.Value;
                var mesh = chunkGO.GetComponent<MeshFilter>().sharedMesh;
                if (mesh == null)
                {
                    continue;
                }
                trisCount += mesh.triangles.Count();
            }

            _text?.SetText(@"path: " + _pathInputField.text + @"

4: load
5: save

triangles: " + trisCount + @"
cachedGOs: " + _cachedGOs.Count + @"
creatingGos: " + _creatingGOs.Count + @"
releasingGOs: " + _releasingGOs.Count + @"
cachedFields: " + _cachedFields.Count + @"
editedFields: " + _editedFields.Count + @"
savingFields: " + _savingFields.Count + @"
loadingFields: " + _loadingFields.Count + @"
");
            LoadChunks();
            SaveChunks();
        }

        Dictionary<Vector3Int, ChunkNode> UpdateOctree(Camera camera)
        {
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var lodThresholds = _lodSetting.LodThresholds;

            var visibleNodes = new Dictionary<Vector3Int, ChunkNode>();

            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            var cameraPos = camera.transform.position;

            Stack<ChunkNode> queue = new Stack<ChunkNode>();
            queue.Push(_root);

            while (queue.Count != 0)
            {
                var node = queue.Pop();

                Vector3 center = (Vector3)node.CenterCoord * voxelSizePerAxis;

                Vector3 size = Vector3.one * node.NumVoxelsPerAxis * voxelSizePerAxis;
                var bounds = new Bounds(center, size);
                if(!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
                {
                    MergeNode(node);
                    node.IsRender = false;
                    continue;
                }

                var closestPoint = bounds.ClosestPoint(cameraPos);

                var levelOfDetail = node.LevelOfDetail; 

                var distance = Vector3.Distance(cameraPos, closestPoint);

                if (lodThresholds[levelOfDetail] <= distance)
                {
                    MergeNode(node);
                    visibleNodes.Add(node.CenterCoord, node);
                    node.IsRender = true;
                    continue;
                }

                SubdivideNode(node);

                var children = node.Children;
                if (children == null)
                {
                    visibleNodes.Add(node.CenterCoord, node);
                    node.IsRender = true;
                    continue;
                }

                node.IsRender = false;
                for (var i = 0; i < 8; ++i)
                {
                    queue.Push(children[i]);
                }

            }

            foreach (var pair in visibleNodes)
            {
                var node = pair.Value;
                node.LodMask = GetLodMask(node, visibleNodes);
            }

            return visibleNodes;
        }

        Queue<ChunkNode> GetIntersertOctreeNodes(Vector3Int coord, int numVoxels)
        {
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;

            var center = (Vector3)coord;
            var size = Vector3.one * numVoxels * voxelSizePerAxis;
            var bounds = new Bounds(coord, size);

            var intersertNodes = new Queue<ChunkNode>();

            var queue = new Queue<ChunkNode>();
            queue.Enqueue(_root);

            while (queue.Count != 0)
            {
                var node = queue.Dequeue();

                Vector3 centerCoord = (Vector3)node.CenterCoord
                    * voxelSizePerAxis;

                Vector3 boundsSize = Vector3.one
                    * node.NumVoxelsPerAxis
                    * voxelSizePerAxis;
                var nodeBounds = new Bounds(centerCoord, boundsSize);

                if (!bounds.Intersects(nodeBounds))
                {
                    continue;
                }
                intersertNodes.Enqueue(node);

                SubdivideNode(node);

                var children = node.Children;
                if (children == null)
                {
                    continue;
                }

                for (var i = 0; i < 8; ++i)
                {
                    queue.Enqueue(children[i]);
                }

            }

            return intersertNodes;
        }

        public void UpdateByCamera()
        {
            if (_creatingGOs.Count > 0)
            {
                return;
            }

            _visibleNodes = UpdateOctree(_camera);
            foreach (var pair in _visibleNodes)
            {
                var centerCrood = pair.Key;
                if (_cachedGOs.ContainsKey(centerCrood))
                {
                    continue;
                }
                _creatingGOs.Enqueue(centerCrood);
            }

            foreach (var pair in _cachedGOs)
            {
                var centerCoord = pair.Key;
                if (_visibleNodes.ContainsKey(centerCoord))
                {
                    continue;
                }
                _releasingGOs.Enqueue(centerCoord);
            }
            
            StartCoroutine(CreateGOs());
            StartCoroutine(ReleaseGOs());
        }

        void MergeNode(ChunkNode node)
        {
            if (node.Children == null)
            {
                return;
            }
            node.Children = null;
        }

        void SubdivideNode(ChunkNode node)
        {
            if (node.Children != null)
            {
                return;
            }

            if (node.LevelOfDetail == 0)
            {
                return;
            }

            node.Children = new ChunkNode[8];
            for (var i = 0; i < 8; ++i)
            {
                var centerCoord = node.CenterCoord;
                var numVoxelsPerAxis = node.NumVoxelsPerAxis
                    >> 1;
                var halfNumVoxelsPerAxis = numVoxelsPerAxis
                    >> 1;

                var octreeMask = (OctreeMask)i;
                if (octreeMask.HasFlag(
                    OctreeMask.PositiveX))
                {
                    centerCoord.x
                        += halfNumVoxelsPerAxis;
                }
                else
                {
                    centerCoord.x
                        -= halfNumVoxelsPerAxis;
                }

                if (octreeMask.HasFlag(
                    OctreeMask.PositiveY))
                {
                    centerCoord.y
                        += halfNumVoxelsPerAxis;
                }
                else
                {
                    centerCoord.y
                        -= halfNumVoxelsPerAxis;
                }

                if (octreeMask.HasFlag(
                    OctreeMask.PositiveZ))
                {
                    centerCoord.z
                        += halfNumVoxelsPerAxis;
                }
                else
                {
                    centerCoord.z
                        -= halfNumVoxelsPerAxis;
                }

                var levelOfDetail = node.LevelOfDetail - 1;
                node.Children[i] = new ChunkNode(centerCoord,
                    numVoxelsPerAxis, levelOfDetail);

            }

        }

        bool IsCreatingGOs()
        {
            return _creatingGOs.Count > 0;
        }

        bool IsReleasingGOs()
        {
            return _releasingGOs.Count > 0;
        }

        IEnumerator CreateGOs()
        {
            yield return new WaitUntil(IsCreatingGOs);

            /*
            System.Diagnostics.Stopwatch sw =  new System.Diagnostics.Stopwatch();
            sw.Start();//开启计时器。
            */

            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var lodMaterials =  _lodSetting.LodMaterials;

            while (_creatingGOs.Count != 0)
            {
                var centerCoord = _creatingGOs.Dequeue();
                if (_cachedGOs.ContainsKey(centerCoord))
                {
                    continue;
                }

                if (!_visibleNodes.ContainsKey(centerCoord))
                {
                    continue;
                }
                var node = _visibleNodes[centerCoord];

                var mesh = CreateMeshInCreateGOs(node);

                GameObject chunkGO = null;
                if (_cachedGOs.ContainsKey(centerCoord))
                {
                    chunkGO = _cachedGOs[centerCoord];
                }
                else
                {
                    chunkGO = new GameObject();
                    chunkGO.name = "Chunk " + centerCoord;
                    chunkGO.transform.position = (Vector3)centerCoord * voxelSizePerAxis;
                    chunkGO.transform.parent = _terrainGO.transform;
                    chunkGO.AddComponent<MeshFilter>();
                    if (_isTriplane)
                    {
                        chunkGO.AddComponent<MeshRenderer>().sharedMaterial = _triplane;
                    }
                    else
                    {
                        chunkGO.AddComponent<MeshRenderer>().sharedMaterial = lodMaterials[node.LevelOfDetail];
                    }
                    chunkGO.AddComponent<MeshCollider>();
                    _cachedGOs[centerCoord] = chunkGO;
                }

                chunkGO.GetComponent<MeshFilter>().sharedMesh = mesh;
                chunkGO.GetComponent<MeshCollider>().sharedMesh = mesh;
                yield return null;
            }

            /*
            sw.Stop();//关闭计时器。
            UnityEngine.Debug.Log( string .Format( "total: {0} ms" , sw.ElapsedMilliseconds));
            */
        }

        IEnumerator ReleaseGOs()
        {
            yield return new WaitUntil(IsReleasingGOs);

            while (_releasingGOs.Count != 0)
            {
                var centerCoord = _releasingGOs.Dequeue();
                if (!_cachedGOs.ContainsKey(centerCoord))
                {
                    continue;
                }

                var chunkGO = _cachedGOs[centerCoord];

                _cachedGOs.Remove(centerCoord);
                Destroy(chunkGO);

                if (_cachedFields.ContainsKey(centerCoord))
                {
                    _cachedFields.Remove(centerCoord);
                }

                yield return null;
            }
        }

        bool IsLoadingFields()
        {
            return _loadingFields.Count != 0;
        }

        public void LoadChunks()
        {
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                string dir = _pathInputField.text;
                if (!Directory.Exists(dir))
                {
                    return;
                }
                if (IsLoadingFields())
                {
                    return;
                }
                foreach (var pair in _visibleNodes)
                {
                    var centerCoord = pair.Key;
                    _loadingFields.Enqueue(centerCoord);
                }
                _editedFields.Clear();
                _cachedFields.Clear();
                StartCoroutine(LoadDensityField(dir));
            }
        }

        IEnumerator LoadDensityField(string dir)
        { 
            yield return new WaitUntil(IsLoadingFields);

            while (_loadingFields.Count != 0)
            {
                var centerCoord = _loadingFields.Dequeue();
                if (!_visibleNodes.ContainsKey(centerCoord))
                {
                    continue;
                }

                if (!_densityFieldBuilder.IsDensityFieldExist(dir, centerCoord))
                {
                    continue;
                }

                float[] densityField = _densityFieldBuilder.LoadDensityField(dir, centerCoord);
                if (densityField == null)
                {
                    continue;
                }

                if (_editedFields.ContainsKey(centerCoord))
                {
                    _editedFields.Remove(centerCoord);
                }
                _cachedFields[centerCoord] = densityField;

                var node = _visibleNodes[centerCoord];
                if (_cachedGOs.ContainsKey(centerCoord))
                {
                    var chunkGO = _cachedGOs[centerCoord];

                    var trisBuffer = _meshBuilder.InitTrisBuffer();
                    var chunkLodMask = LodMask.None;
                    _meshBuilder.GenerateRegularCell(trisBuffer, densityField, centerCoord, node.LevelOfDetail, chunkLodMask);
                    var mesh = _meshBuilder.TrisbufferToMesh(trisBuffer);
                    trisBuffer.Release();

                    chunkGO.GetComponent<MeshFilter>().sharedMesh = mesh;
                    chunkGO.GetComponent<MeshCollider>().sharedMesh = mesh;
                }

                yield return null;
            }
        }


        public void SaveChunks()
        {
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                string dir = _pathInputField.text;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(_pathInputField.text);
                }
                if (IsSavingFields())
                {
                    return;
                }
                foreach (var pair in _editedFields)
                {
                    var centerCoord = pair.Key;
                    _savingFields.Enqueue(centerCoord);
                }

                foreach (var pair in _cachedFields)
                {
                    var centerCoord = pair.Key;
                    _savingFields.Enqueue(centerCoord);
                }
                StartCoroutine(SaveDensityField(dir));
            }
        }

        bool IsSavingFields()
        {
            return _savingFields.Count != 0;
        }

        IEnumerator SaveDensityField(string dir)
        { 
            yield return new WaitUntil(IsSavingFields);

            while (_savingFields.Count != 0)
            {
                var centerCoord = _savingFields.Dequeue();
                float[] densityField = null;
                if (_editedFields.ContainsKey(centerCoord))
                {
                    densityField = _editedFields[centerCoord];
                }
                else if (_cachedFields.ContainsKey(centerCoord))
                {
                    densityField = _cachedFields[centerCoord];
                }

                if (densityField == null)
                {
                    continue;
                }

                _densityFieldBuilder.SaveDensityField(dir, densityField, centerCoord);
                yield return null;
            }
        }

        public void UseBrush(Brush brush, Vector3 hitPoint, float weight, float delta)
        {
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var lodMaterials = _lodSetting.LodMaterials;

            Vector3Int hitCoord = Vector3Int.zero;
            hitCoord.x = Mathf.FloorToInt(hitPoint.x / voxelSizePerAxis);
            hitCoord.y = Mathf.FloorToInt(hitPoint.y / voxelSizePerAxis);
            hitCoord.z = Mathf.FloorToInt(hitPoint.z / voxelSizePerAxis);

            var intersertNodes = GetIntersertOctreeNodes(hitCoord, brush.NumVoxelsPerAxis);

            var reRenderNodes = new Queue<ChunkNode>();
            while (intersertNodes.Count != 0)
            {
                var node = intersertNodes.Dequeue();
                var centerCoord = node.CenterCoord;
                var levelOfDetail = node.LevelOfDetail;

                float[] densityField = null;
                if (_editedFields.ContainsKey(centerCoord))
                {
                    densityField = _editedFields[centerCoord];
                    _editedFields.Remove(centerCoord);
                }
                else if (_cachedFields.ContainsKey(centerCoord))
                {
                    densityField = _cachedFields[centerCoord];
                    _cachedFields.Remove(centerCoord);
                }
                else if (_densityFieldBuilder.IsDensityFieldExist(_pathInputField.text, centerCoord))
                {
                    densityField = _densityFieldBuilder.LoadDensityField(centerCoord);
                }
                else
                {
                    if (_isNoise)
                    {
                        densityField = _densityFieldBuilder.BuildNoiseDensityField(centerCoord, levelOfDetail);
                    }
                    else
                    {
                        densityField = _densityFieldBuilder.BuildDensityField(centerCoord, levelOfDetail);
                    }
                }

                _brushTools.UseBrush(densityField, centerCoord, levelOfDetail, brush, hitPoint, weight, delta);
                _editedFields[node.CenterCoord] = densityField;

                if (!node.IsRender)
                {
                    continue;
                }
                reRenderNodes.Enqueue(node);
            }

            while (reRenderNodes.Count != 0)
            {
                var node = reRenderNodes.Dequeue();
                var centerCoord = node.CenterCoord;

                var mesh = CreateMeshInUseBrush(node);

                GameObject chunkGO = null;
                if (_cachedGOs.ContainsKey(centerCoord))
                {
                    chunkGO = _cachedGOs[centerCoord];
                }
                else
                {
                    chunkGO = new GameObject();
                    chunkGO.name = "Chunk " + centerCoord;
                    chunkGO.transform.position = (Vector3)centerCoord * voxelSizePerAxis;
                    chunkGO.transform.parent = _terrainGO.transform;
                    chunkGO.AddComponent<MeshFilter>();
                    if (_isTriplane)
                    {
                        chunkGO.AddComponent<MeshRenderer>().sharedMaterial = _triplane;
                    }
                    else
                    {
                        chunkGO.AddComponent<MeshRenderer>().sharedMaterial = lodMaterials[node.LevelOfDetail];
                    }
                    chunkGO.AddComponent<MeshCollider>();
                    _cachedGOs[node.CenterCoord] = chunkGO;
                }

                chunkGO.GetComponent<MeshFilter>().sharedMesh = mesh;
                chunkGO.GetComponent<MeshCollider>().sharedMesh = mesh;
            }
        }

        LodMask GetLodMask(ChunkNode node, Dictionary<Vector3Int, ChunkNode> visibleNodes)
        {
            if (node.LevelOfDetail == 0)
            {
                return LodMask.None;
            }

            var centerCoord = node.CenterCoord;
            var numVoxelsPerAxis = node.NumVoxelsPerAxis;
            var halfNumVoxelsPerAxis = numVoxelsPerAxis >> 1;
            var halfHalfNumVoxelsPerAxis = numVoxelsPerAxis >> 2;

            var lodMask = LodMask.None;

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x -= (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.y -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.z -= halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.NegativeX;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x -= (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.y -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.z += halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.NegativeX;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x -= (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.y += halfHalfNumVoxelsPerAxis;
                childCenterCoord.z -= halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.NegativeX;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x -= (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.y += halfHalfNumVoxelsPerAxis;
                childCenterCoord.z += halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.NegativeX;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x += (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.y -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.z -= halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.PositiveX;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x += (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.y -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.z += halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.PositiveX;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x += (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.y += halfHalfNumVoxelsPerAxis;
                childCenterCoord.z -= halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.PositiveX;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x += (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.y += halfHalfNumVoxelsPerAxis;
                childCenterCoord.z += halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.PositiveX;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.y -= (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.z -= halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.NegativeY;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.y -= (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.z += halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.NegativeY;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x += halfHalfNumVoxelsPerAxis;
                childCenterCoord.y -= (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.z -= halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.NegativeY;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x += halfHalfNumVoxelsPerAxis;
                childCenterCoord.y -= (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.z += halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.NegativeY;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.y += (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.z -= halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.PositiveY;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.y += (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.z += halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.PositiveY;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x += halfHalfNumVoxelsPerAxis;
                childCenterCoord.y += (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.z -= halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.PositiveY;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x += halfHalfNumVoxelsPerAxis;
                childCenterCoord.y += (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);
                childCenterCoord.z += halfHalfNumVoxelsPerAxis;

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.PositiveY;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.y -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.z -= (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.NegativeZ;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.y += halfHalfNumVoxelsPerAxis;
                childCenterCoord.z -= (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.NegativeZ;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x += halfHalfNumVoxelsPerAxis;
                childCenterCoord.y -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.z -= (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.NegativeZ;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x += halfHalfNumVoxelsPerAxis;
                childCenterCoord.y += halfHalfNumVoxelsPerAxis;
                childCenterCoord.z -= (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.NegativeZ;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.y -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.z += (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.PositiveZ;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.y += halfHalfNumVoxelsPerAxis;
                childCenterCoord.z += (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.PositiveZ;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x += halfHalfNumVoxelsPerAxis;
                childCenterCoord.y -= halfHalfNumVoxelsPerAxis;
                childCenterCoord.z += (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.PositiveZ;
                }
            }

            {
                var childCenterCoord = centerCoord;
                childCenterCoord.x += halfHalfNumVoxelsPerAxis;
                childCenterCoord.y += halfHalfNumVoxelsPerAxis;
                childCenterCoord.z += (halfNumVoxelsPerAxis + halfHalfNumVoxelsPerAxis);

                if (visibleNodes.ContainsKey(childCenterCoord))
                {
                    lodMask |= LodMask.PositiveZ;
                }
            }

            return lodMask;
        }

        float[] GetDensityFieldInCreateGOs(Vector3Int centerCoord, int levelOfDetail)
        {
            float[] densityField = null;
            if (_editedFields.ContainsKey(centerCoord))
            {
                densityField = _editedFields[centerCoord];
            }
            else if (_cachedFields.ContainsKey(centerCoord))
            {
                densityField = _cachedFields[centerCoord];
            }
            else if (_densityFieldBuilder.IsDensityFieldExist(_pathInputField.text, centerCoord))
            {
                densityField = _densityFieldBuilder.LoadDensityField(_pathInputField.text, centerCoord);
                _cachedFields[centerCoord] = densityField;
            }
            else
            {
                if (_isNoise)
                {
                    densityField = _densityFieldBuilder.BuildNoiseDensityField(centerCoord, levelOfDetail);
                }
                else
                {
                    densityField = _densityFieldBuilder.BuildDensityField(centerCoord, levelOfDetail);
                }
                _cachedFields[centerCoord] = densityField;
            }
            return densityField;
        }

        Mesh CreateMeshInCreateGOs(ChunkNode node)
        {
            var centerCoord = node.CenterCoord;
            var levelOfDetail = node.LevelOfDetail;
            var lodMask = node.LodMask;

            var trisBuffer = _meshBuilder.InitTrisBuffer();
            var densityField = GetDensityFieldInCreateGOs(centerCoord, levelOfDetail);
            _meshBuilder.GenerateRegularCell(trisBuffer, densityField, centerCoord, levelOfDetail, lodMask);
            if (levelOfDetail > 0)
            {
                var numVoxelsPerAxis = node.NumVoxelsPerAxis;
                var halfNumVoxelPerAxis = numVoxelsPerAxis >> 1;
                var halfNbhNumVoxelPerAxis = numVoxelsPerAxis >> 2;
                var nbhLevelOfDetail = levelOfDetail - 1;
                if (lodMask.HasFlag(LodMask.NegativeX))
                {
                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }
                }

                if (lodMask.HasFlag(LodMask.PositiveX))
                {
                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }
                }

                if (lodMask.HasFlag(LodMask.NegativeY))
                {
                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }
                }

                if (lodMask.HasFlag(LodMask.PositiveY))
                {
                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }
                }

                if (lodMask.HasFlag(LodMask.NegativeZ))
                {
                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }
                }

                if (lodMask.HasFlag(LodMask.PositiveZ))
                {
                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInCreateGOs(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }
                }
            }
            var mesh = _meshBuilder.TrisbufferToMesh(trisBuffer);
            trisBuffer.Dispose();
            return mesh;
        }

        float[] GetDensityFieldInUseBrush(Vector3Int centerCoord, int levelOfDetail)
        {
            float[] densityField = null;
            if (_editedFields.ContainsKey(centerCoord))
            {
                densityField = _editedFields[centerCoord];
            }
            else if (_cachedFields.ContainsKey(centerCoord))
            {
                densityField = _cachedFields[centerCoord];
            }
            else if (_densityFieldBuilder.IsDensityFieldExist(_pathInputField.text, centerCoord))
            {
                densityField = _densityFieldBuilder.LoadDensityField(_pathInputField.text, centerCoord);
                _cachedFields[centerCoord] = densityField;
            }
            else
            {
                if (_isNoise)
                {
                    densityField = _densityFieldBuilder.BuildNoiseDensityField(centerCoord, levelOfDetail);
                }
                else
                {
                    densityField = _densityFieldBuilder.BuildDensityField(centerCoord, levelOfDetail);
                }
                _cachedFields[centerCoord] = densityField;
            }
            return densityField;
        }

        Mesh CreateMeshInUseBrush(ChunkNode node)
        {
            var centerCoord = node.CenterCoord;
            var levelOfDetail = node.LevelOfDetail;
            var lodMask = node.LodMask;

            var trisBuffer = _meshBuilder.InitTrisBuffer();
            var densityField = GetDensityFieldInUseBrush(centerCoord, levelOfDetail);
            _meshBuilder.GenerateRegularCell(trisBuffer, densityField, centerCoord, levelOfDetail, lodMask);
            if (levelOfDetail > 0)
            {
                var numVoxelsPerAxis = node.NumVoxelsPerAxis;
                var halfNumVoxelPerAxis = numVoxelsPerAxis >> 1;
                var halfNbhNumVoxelPerAxis = numVoxelsPerAxis >> 2;
                var nbhLevelOfDetail = levelOfDetail - 1;
                if (lodMask.HasFlag(LodMask.NegativeX))
                {
                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }
                }

                if (lodMask.HasFlag(LodMask.PositiveX))
                {
                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveX(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }
                }

                if (lodMask.HasFlag(LodMask.NegativeY))
                {
                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }
                }

                if (lodMask.HasFlag(LodMask.PositiveY))
                {
                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z -= halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        nbhCenterCoord.z += halfNbhNumVoxelPerAxis;
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveY(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }
                }

                if (lodMask.HasFlag(LodMask.NegativeZ))
                {
                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z -= (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellNegativeZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }
                }

                if (lodMask.HasFlag(LodMask.PositiveZ))
                {
                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y -= halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }

                    {
                        var nbhCenterCoord = centerCoord;
                        nbhCenterCoord.x += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.y += halfNbhNumVoxelPerAxis;
                        nbhCenterCoord.z += (halfNumVoxelPerAxis + halfNbhNumVoxelPerAxis);
                        var nbhDensityField = GetDensityFieldInUseBrush(nbhCenterCoord, nbhLevelOfDetail);
                        _meshBuilder.GenerateTransitionCellPositiveZ(trisBuffer, nbhDensityField, nbhCenterCoord, nbhLevelOfDetail, lodMask, centerCoord);
                    }
                }
            }
            var mesh = _meshBuilder.TrisbufferToMesh(trisBuffer);
            trisBuffer.Dispose();
            return mesh;
        }

        public void PutModel(Vector3 hitPoint)
        {
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var lodMaterials = _lodSetting.LodMaterials;

            Vector3Int hitCoord = Vector3Int.zero;
            hitCoord.x = Mathf.FloorToInt(hitPoint.x / voxelSizePerAxis);
            hitCoord.y = Mathf.FloorToInt(hitPoint.y / voxelSizePerAxis);
            hitCoord.z = Mathf.FloorToInt(hitPoint.z / voxelSizePerAxis);

            var intersertNodes = GetIntersertOctreeNodes(hitCoord, _modelTools._voxelResolution);

            var reRenderNodes = new Queue<ChunkNode>();
            while (intersertNodes.Count != 0)
            {
                var node = intersertNodes.Dequeue();
                var centerCoord = node.CenterCoord;
                var levelOfDetail = node.LevelOfDetail;

                float[] densityField = null;
                if (_editedFields.ContainsKey(centerCoord))
                {
                    densityField = _editedFields[centerCoord];
                    _editedFields.Remove(centerCoord);
                }
                else if (_cachedFields.ContainsKey(centerCoord))
                {
                    densityField = _cachedFields[centerCoord];
                    _cachedFields.Remove(centerCoord);
                }
                else if (_densityFieldBuilder.IsDensityFieldExist(_pathInputField.text, centerCoord))
                {
                    densityField = _densityFieldBuilder.LoadDensityField(centerCoord);
                }
                else
                {
                    if (_isNoise)
                    {
                        densityField = _densityFieldBuilder.BuildNoiseDensityField(centerCoord, levelOfDetail);
                    }
                    else
                    {
                        densityField = _densityFieldBuilder.BuildDensityField(centerCoord, levelOfDetail);
                    }
                }

                var pointArray = _modelTools.BuildModel();
                _modelTools.UseModel(densityField, node.CenterCoord, node.LevelOfDetail, pointArray, hitPoint);
                _editedFields[node.CenterCoord] = densityField;

                if (!node.IsRender)
                {
                    continue;
                }
                reRenderNodes.Enqueue(node);
            }

            while (reRenderNodes.Count != 0)
            {
                var node = reRenderNodes.Dequeue();
                var centerCoord = node.CenterCoord;

                var mesh = CreateMeshInUseBrush(node);

                GameObject chunkGO = null;
                if (_cachedGOs.ContainsKey(centerCoord))
                {
                    chunkGO = _cachedGOs[centerCoord];
                }
                else
                {
                    chunkGO = new GameObject();
                    chunkGO.name = "Chunk " + centerCoord;
                    chunkGO.transform.position = (Vector3)centerCoord * voxelSizePerAxis;
                    chunkGO.transform.parent = _terrainGO.transform;
                    chunkGO.AddComponent<MeshFilter>();
                    if (_isTriplane)
                    {
                        chunkGO.AddComponent<MeshRenderer>().sharedMaterial = _triplane;
                    }
                    else
                    {
                        chunkGO.AddComponent<MeshRenderer>().sharedMaterial = lodMaterials[node.LevelOfDetail];
                    }
                    chunkGO.AddComponent<MeshCollider>();
                    _cachedGOs[node.CenterCoord] = chunkGO;
                }

                chunkGO.GetComponent<MeshFilter>().sharedMesh = mesh;
                chunkGO.GetComponent<MeshCollider>().sharedMesh = mesh;
            }
        }

    }

}
