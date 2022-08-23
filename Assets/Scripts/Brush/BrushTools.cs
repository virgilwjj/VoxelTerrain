using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelTerrain
{
    public class BrushTools : MonoBehaviour
    {
        [SerializeField]
        BrushSetting _brushSetting;
        [SerializeField]
        ChunkSetting _chunkSetting;

        [SerializeField]
        ComputeShader _buildSphereBrushTex;

        [SerializeField]
        ComputeShader _buildCubeBrushTex;

        [SerializeField]
        ComputeShader _useBrush;

        TexConverter _texConverter;
        TexLoader _texLoader;

        void Awake()
        {
            _texConverter = gameObject.AddComponent<TexConverter>();     
            _texLoader = gameObject.AddComponent<TexLoader>();
        }

        public Brush BuildBrush(int numVoxelsPerAxis, BrushType brushType)
        {
            if (brushType == BrushType.Sphere)
            {
                var brushTex = _brushSetting.SphereBrushTexs[numVoxelsPerAxis];
                if (brushTex != null)
                {
                    return new Brush(brushTex, numVoxelsPerAxis, brushType);
                }
                else
                {
                    return BuildSphereBrush(numVoxelsPerAxis);
                }
            }
            else if (brushType == BrushType.Cube)
            {
                var brushTex = _brushSetting.CubeBrushTexs[numVoxelsPerAxis];
                if (brushTex != null)
                {
                    return new Brush(brushTex, numVoxelsPerAxis, brushType);
                }
                else
                {
                    return BuildCubeBrush(numVoxelsPerAxis);
                }
            }
            else
            {
                return null;
            }
        }

        Brush BuildSphereBrush(int numVoxelsPerAxis)
        {
            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var isoLevel = _chunkSetting.IsoLevel;
            var sphereCenter = Vector3.one * numVoxelsPerAxis * 0.5f * voxelSizePerAxis;
            var sphereRadius = numVoxelsPerAxis * 0.5f * voxelSizePerAxis;

            var brushTex = new RenderTexture(numPointsPerAxis, numPointsPerAxis, 0, RenderTextureFormat.RFloat);
            brushTex.volumeDepth = numPointsPerAxis;
            brushTex.dimension = TextureDimension.Tex3D;
            brushTex.enableRandomWrite = true;
            brushTex.Create();

            _buildSphereBrushTex.SetTexture(0, "brushTex", brushTex);
            _buildSphereBrushTex.SetInt("numPointsPerAxisForBrush", numPointsPerAxis);
            _buildSphereBrushTex.SetFloat("voxelSizePerAxis", voxelSizePerAxis);
            _buildSphereBrushTex.SetFloat("isoLevel", isoLevel);
            _buildSphereBrushTex.SetVector("sphereCenter", sphereCenter);
            _buildSphereBrushTex.SetFloat("sphereRadius", sphereRadius);
            _buildSphereBrushTex.DispatchThreads(0, numPointsPerAxis, numPointsPerAxis, numPointsPerAxis);

            Texture3D tex3D = _texConverter.RenderTexToTex3D(brushTex);

            return new Brush(tex3D, numVoxelsPerAxis, BrushType.Sphere);
        }

        Brush BuildCubeBrush(int numVoxelsPerAxis)
        {
            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var isoLevel = _chunkSetting.IsoLevel;
            var cubeCenter = Vector3.one * numVoxelsPerAxis * 0.5f * voxelSizePerAxis;
            var cubeExtents = numVoxelsPerAxis * 0.5f * voxelSizePerAxis;

            var brushTex = new RenderTexture(numPointsPerAxis, numPointsPerAxis, 0, RenderTextureFormat.RFloat);
            brushTex.volumeDepth = numPointsPerAxis;
            brushTex.dimension = TextureDimension.Tex3D;
            brushTex.enableRandomWrite = true;
            brushTex.Create();

            _buildCubeBrushTex.SetTexture(0, "brushTex", brushTex);
            _buildCubeBrushTex.SetInt("numPointsPerAxisForBrush", numPointsPerAxis);
            _buildCubeBrushTex.SetFloat("voxelSizePerAxis", voxelSizePerAxis);
            _buildCubeBrushTex.SetFloat("isoLevel", isoLevel);
            _buildCubeBrushTex.SetVector("cubeCenter", cubeCenter);
            _buildCubeBrushTex.SetFloat("cubeExtents", cubeExtents);
            _buildCubeBrushTex.DispatchThreads(0, numPointsPerAxis, numPointsPerAxis, numPointsPerAxis);

            Texture3D tex3D = _texConverter.RenderTexToTex3D(brushTex);

            return new Brush(tex3D, numVoxelsPerAxis, BrushType.Cube);
        }

        public void SaveBrush(Brush brush, string brushName)
        {
            _texLoader.SaveTex3D(brush.BrushTex, "Textures/BrushTexs/", brushName, "asset", "brush_tex", "unity");
        }

        public Brush LoadBrush(Brush brush, string brushName, int numVoxelsPerAxis)
        {
            var tex3D = _texLoader.LoadTex3D("Textures/BrushTexs", brushName, "asset");
            return new Brush(tex3D, numVoxelsPerAxis, BrushType.Custom);
        }

        public void UseBrush(float[] densityField, Vector3Int centerCoord, int levelOfDetail, Brush brush, Vector3 hitPoint, float weight, float delta)
        {
            var numVoxelsPerAxis = _chunkSetting.NumVoxelsPerAxis;
            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var isoLevel = _chunkSetting.IsoLevel;
            var minCoord = centerCoord - Vector3Int.one * ((numVoxelsPerAxis >> 1) << levelOfDetail);
            var stridePerAxis = 1 << levelOfDetail;

            var brushTex = brush.BrushTex;
            var numVoxelsPerAxisForBlush = brush.NumVoxelsPerAxis;
            var numPointsPerAxisForBlush = numVoxelsPerAxisForBlush + 1;
            var hitCoord = Vector3Int.zero;
            hitCoord.x = Mathf.FloorToInt(hitPoint.x / voxelSizePerAxis);
            hitCoord.y = Mathf.FloorToInt(hitPoint.y / voxelSizePerAxis);
            hitCoord.z = Mathf.FloorToInt(hitPoint.z / voxelSizePerAxis);
            var minCoordForBrush = hitCoord - Vector3Int.one * (numVoxelsPerAxisForBlush >> 1);

            var numPointsPerChunk = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            var densityFieldBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float));
            densityFieldBuffer.SetData(densityField);

            _useBrush.SetBuffer(0, "densityField", densityFieldBuffer);
            _useBrush.SetInt("numVoxelsPerAxis", numVoxelsPerAxis);
            _useBrush.SetInt("numPointsPerAxis", numPointsPerAxis);
            _useBrush.SetFloat("voxelSizePerAxis", voxelSizePerAxis);
            _useBrush.SetFloat("isoLevel", isoLevel);
            _useBrush.SetVectorInt("minCoord", minCoord);
            _useBrush.SetInt("stridePerAxis", stridePerAxis);
            _useBrush.SetTexture(0, "brushTex", brushTex); 
            _useBrush.SetInt("numVoxelsPerAxisForBrush", numVoxelsPerAxisForBlush);
            _useBrush.SetInt("numPointsPerAxisForBrush", numPointsPerAxisForBlush);
            _useBrush.SetVectorInt("minCoordForBrush", minCoordForBrush);
            _useBrush.SetFloat("weight", weight);
            _useBrush.SetFloat("delta", delta);
            _useBrush.DispatchThreads(0, numPointsPerAxisForBlush, numPointsPerAxisForBlush, numPointsPerAxisForBlush);

            densityFieldBuffer.GetData(densityField);
            densityFieldBuffer.Dispose();
        }

    }

}
