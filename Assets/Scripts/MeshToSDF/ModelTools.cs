using UnityEngine;

namespace VoxelTerrain
{
    public class ModelTools : MonoBehaviour
    {
        MtV _mtv;
        TexConverter _texConverter;
        [SerializeField]
        Mesh _mesh;
        [SerializeField]
        public int _voxelResolution;
        [SerializeField]
        Vector3 _offset;
        [SerializeField]
        int _numSamplesPerTriangle;
        [SerializeField]
        float _scaleMeshBy; 
        [SerializeField]
        float _postProcessThickness;

        [SerializeField]
        ChunkSetting _chunkSetting;
        [SerializeField]
        ComputeShader _useModel;
        
        void Awake()
        {
            _mtv = GetComponent<MtV>();     
            _texConverter = GetComponent<TexConverter>();
        }

        public float[] BuildModel()
        {
            var voxels = _mtv.MeshToSDF(_mesh, _voxelResolution, _offset, _numSamplesPerTriangle, _scaleMeshBy);     
            var pointBuffer = _mtv.FloodFillToSDF(voxels, _postProcessThickness, _voxelResolution);
            voxels.Release();

            var pointArray = new float[_voxelResolution * _voxelResolution * _voxelResolution];
            pointBuffer.GetData(pointArray);
            pointBuffer.Release();

            return pointArray;
        }

        public void UseModel(float[] densityField, Vector3Int centerCoord, int levelOfDetail,
            float[] pointArray, Vector3 hitPoint)
        {
            var numVoxelsPerAxis = _chunkSetting.NumVoxelsPerAxis;
            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var isoLevel = _chunkSetting.IsoLevel;
            var minCoord = centerCoord - Vector3Int.one * ((numVoxelsPerAxis >> 1) << levelOfDetail);
            var stridePerAxis = 1 << levelOfDetail;

            var numPointsPerAxisForBrush = _voxelResolution;
            var hitCoord = Vector3Int.zero;
            hitCoord.x = Mathf.FloorToInt(hitPoint.x / voxelSizePerAxis);
            hitCoord.y = Mathf.FloorToInt(hitPoint.y / voxelSizePerAxis);
            hitCoord.z = Mathf.FloorToInt(hitPoint.z / voxelSizePerAxis);
            var minCoordForBrush = Vector3Int.zero;
            minCoordForBrush.x = hitCoord.x - (numPointsPerAxisForBrush >> 1);
            minCoordForBrush.y = hitCoord.y;
            minCoordForBrush.z = hitCoord.z - (numPointsPerAxisForBrush >> 1);

            var numPointsPerChunk = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            var densityFieldBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float));
            densityFieldBuffer.SetData(densityField);

            var numPointsPerChunkForBrush = numPointsPerAxisForBrush * numPointsPerAxisForBrush * numPointsPerAxisForBrush;
            var pointBuffer = new ComputeBuffer(numPointsPerChunkForBrush, sizeof(float));
            pointBuffer.SetData(pointArray);

            _useModel.SetBuffer(0, "densityField", densityFieldBuffer);
            _useModel.SetInt("numVoxelsPerAxis", numVoxelsPerAxis);
            _useModel.SetInt("numPointsPerAxis", numPointsPerAxis);
            _useModel.SetFloat("voxelSizePerAxis", voxelSizePerAxis);
            _useModel.SetFloat("isoLevel", isoLevel);
            _useModel.SetVectorInt("minCoord", minCoord);
            _useModel.SetInt("stridePerAxis", stridePerAxis);
            _useModel.SetBuffer(0, "pointBuffer", pointBuffer); 
            _useModel.SetInt("numPointsPerAxisForBrush", numPointsPerAxisForBrush);
            _useModel.SetVectorInt("minCoordForBrush", minCoordForBrush);
            _useModel.DispatchThreads(0, numPointsPerAxisForBrush, numPointsPerAxisForBrush, numPointsPerAxisForBrush);

            densityFieldBuffer.GetData(densityField);
            densityFieldBuffer.Dispose();
        }
    }

}