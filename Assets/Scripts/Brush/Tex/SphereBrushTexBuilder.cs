using UnityEngine;

namespace VoxelTerrain
{
    public class SphereBrushTexBuilder : BrushTexBuilder
    {
        [SerializeField]
        ComputeShader _buildSphereBrushTex;

        public override RenderTexture BuildBrushTex(
            int numVoxelsPerAxis)
        {
            var brushTex = base.BuildBrushTex(
                numVoxelsPerAxis);

            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis
                = TerrainConfig.VoxelSizePerAxis;
            var isoLevel = TerrainConfig.IsoLevel;
            var sphereCenter = Vector3.one
                * (numVoxelsPerAxis >> 1);
            var sphereRadius = numVoxelsPerAxis * 0.5f;

            _buildSphereBrushTex.SetTexture(0, "brushTex",
                brushTex);
            _buildSphereBrushTex.SetInt("numVoxelsPerAxis",
                numVoxelsPerAxis);
            _buildSphereBrushTex.SetInt("numPointsPerAxis",
                numPointsPerAxis);
            _buildSphereBrushTex.SetFloat("voxelSizePerAxis",
                voxelSizePerAxis);
            _buildSphereBrushTex.SetFloat("isoLevel",
                isoLevel);
            _buildSphereBrushTex.SetVector("sphereCenter",
                sphereCenter);
            _buildSphereBrushTex.SetFloat("sphereRadius",
                sphereRadius);
            _buildSphereBrushTex.DispatchThreads(0,
                numPointsPerAxis, numPointsPerAxis,
                numPointsPerAxis);

            return brushTex;
        }

    }

}
