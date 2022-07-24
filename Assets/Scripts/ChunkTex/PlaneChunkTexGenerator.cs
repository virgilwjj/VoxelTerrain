using UnityEngine;

namespace VoxelTerrain
{
    public class PlaneChunkTexGenerator : ChunkTexGenerator
    {
        [SerializeField]
        ComputeShader _generatePlaneChunkTex;

        public override RenderTexture GenerateChunkTex(
            Vector3Int coordinate, int levelOfDetail)
        {
            var chunkTex = base.GenerateChunkTex(coordinate,
                levelOfDetail);

            var numVoxelsPerAxis
                = TerrainConfig.NumVoxelsPerAxisForChunk;
            var numPointsPerAxis
                = TerrainConfig.NumPointsPerAxisForChunk;
            var voxelSizePerAxis
                = TerrainConfig.VoxelSizePerAxis;

            _generatePlaneChunkTex.SetTexture(0, "chunkTex",
                chunkTex);
            _generatePlaneChunkTex.SetInt("numVoxelsPerAxis",
                numVoxelsPerAxis);
            _generatePlaneChunkTex.SetInt("numPointsPerAxis",
                numPointsPerAxis);
            _generatePlaneChunkTex.SetFloat("voxelSizePerAxis",
                voxelSizePerAxis);
            _generatePlaneChunkTex.SetVectorInt("coordinate",
                coordinate);
            _generatePlaneChunkTex.SetInt("levelOfDetail",
                levelOfDetail);
            _generatePlaneChunkTex.DispatchThreads(0,
                numPointsPerAxis, numPointsPerAxis,
                numPointsPerAxis);

            return chunkTex;
        }

    }

}
