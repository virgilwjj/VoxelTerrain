using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelTerrain
{
    public class BrushTexBuilder : MonoBehaviour
    {
        [HideInInspector]
        public TerrainConfig TerrainConfig;

        public virtual RenderTexture BuildBrushTex(
            int numVoxelsPerAxis)
        {
            var numPointsPerAxis = numVoxelsPerAxis + 1;

            var brushTex = new RenderTexture(numPointsPerAxis,
                numPointsPerAxis, 0, RenderTextureFormat.RFloat);
            brushTex.volumeDepth = numPointsPerAxis;
            brushTex.dimension = TextureDimension.Tex3D;
            brushTex.enableRandomWrite = true;
            brushTex.Create();

            return brushTex;
        }

    }

}
