using UnityEngine;

namespace VoxelTerrain
{
    public class CubeBrushTexBuilder : BrushTexBuilder
    {
        [SerializeField]
        ComputeShader _buildCubeBrushTex;

        public override RenderTexture BuildBrushTex(
            int numVoxelsPerAxis)
        {
            var brushTex = base.BuildBrushTex(
                numVoxelsPerAxis);

            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis
                = TerrainConfig.VoxelSizePerAxis;

            _buildCubeBrushTex.SetTexture(0, "brushTex",
                brushTex);
            _buildCubeBrushTex.SetInt("numVoxelsPerAxis",
                numVoxelsPerAxis);
            _buildCubeBrushTex.SetInt("numPointsPerAxis",
                numPointsPerAxis);
            _buildCubeBrushTex.DispatchThreads(0,
                numPointsPerAxis, numPointsPerAxis,
                numPointsPerAxis);

            return brushTex;
        }

    }

}
