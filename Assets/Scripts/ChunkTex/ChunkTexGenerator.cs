using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelTerrain
{
    public class ChunkTexGenerator : MonoBehaviour
    {
        [HideInInspector]
        public TerrainConfig TerrainConfig;

        public virtual RenderTexture GenerateChunkTex(
            Vector3Int coordinate, int levelOfDetail)
        {
            var numPointsPerAxis
                = TerrainConfig.NumPointsPerAxisForChunk;

            var chunkTex = new RenderTexture(numPointsPerAxis,
                numPointsPerAxis, 0, RenderTextureFormat.RFloat);
            chunkTex.volumeDepth = numPointsPerAxis;
            chunkTex.dimension = TextureDimension.Tex3D;
            chunkTex.enableRandomWrite = true;
            chunkTex.Create();

            return chunkTex;
        }

    }

}
