using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelTerrain
{
    public class BlushTexGenerator : MonoBehaviour
    {
        [HideInInspector]
        public TerrainConfig TerrainConfig;

        public virtual RenderTexture GenerateBlushTex(
            int numVoxelsPerAxis)
        {
            var numPointsPerAxis = numVoxelsPerAxis + 1;

            var blushTex = new RenderTexture(numPointsPerAxis,
                numPointsPerAxis, 0, RenderTextureFormat.RFloat);
            blushTex.volumeDepth = numPointsPerAxis;
            blushTex.dimension = TextureDimension.Tex3D;
            blushTex.enableRandomWrite = true;
            blushTex.Create();

            return blushTex;
        }

    }

}
