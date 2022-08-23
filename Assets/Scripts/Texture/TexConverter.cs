using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelTerrain
{
    public class TexConverter : MonoBehaviour
    {
        [SerializeField]
        ComputeShader _sliceRenderTex3D;
        [SerializeField]
        ComputeShader _composeRenderTex3D;

        public RenderTexture[] RenderTex3DToRenderTex2DArray(
            RenderTexture tex3D)
        {
            var width = tex3D.width;
            var height = tex3D.height;
            var depth = tex3D.volumeDepth;

            _sliceRenderTex3D.SetTexture(0, "tex3D", tex3D);
            _sliceRenderTex3D.SetInt("width", width);
            _sliceRenderTex3D.SetInt("height", height);
            _sliceRenderTex3D.SetInt("depth", depth);

            var tex2DArray = new RenderTexture[depth];
            for (var z = 0; z < depth; ++z)
            {
                var tex2D = new RenderTexture(width, height, 0,
                    RenderTextureFormat.RFloat);
                tex2D.dimension = TextureDimension.Tex2D;
                tex2D.enableRandomWrite = true;
                tex2D.Create();

                _sliceRenderTex3D.SetTexture(0, "tex2D", tex2D);
                _sliceRenderTex3D.SetInt("z", z);

                _sliceRenderTex3D.DispatchThreads(0, width, height, 1);

                tex2DArray[z] = tex2D;
            }

            return tex2DArray;
        }

        public RenderTexture RenderTex2DArrayToRenderTex3D(
            RenderTexture[] tex2DArray)
        {
            var width = tex2DArray[0].width;
            var height = tex2DArray[0].height;
            var depth = tex2DArray.Length;

            var tex3D = new RenderTexture(width, height, 0,
                RenderTextureFormat.RFloat);
            tex3D.volumeDepth = depth;
            tex3D.dimension = TextureDimension.Tex3D;
            tex3D.enableRandomWrite = true;
            tex3D.Create();

            _composeRenderTex3D.SetTexture(0, "tex3D", tex3D);
            _composeRenderTex3D.SetInt("width", width);
            _composeRenderTex3D.SetInt("height", height);
            _composeRenderTex3D.SetInt("depth", depth);

            for (var z = 0; z < depth; ++z)
            {
                var tex2D = tex2DArray[z];

                _composeRenderTex3D.SetTexture(0, "tex2D", tex2D);
                _composeRenderTex3D.SetInt("z", z);

                _composeRenderTex3D.DispatchThreads(0, width, height, 1);
            }

            return tex3D;
        }

        public Texture2D RenderTexToTex2D(RenderTexture renderTex)
        {
            var width = renderTex.width;
            var height = renderTex.height;

            RenderTexture.active = renderTex;

            var tex2D = new Texture2D(width, height, TextureFormat.RFloat,
                false);
            tex2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex2D.Apply();

            return tex2D;
        }

        public RenderTexture Tex2DToRenderTex(Texture2D tex2D)
        {
            var width = tex2D.width;
            var height = tex2D.height;

            var renderTex = new RenderTexture(width, height, 0,
                RenderTextureFormat.RFloat);
            renderTex.dimension = TextureDimension.Tex2D;
            renderTex.enableRandomWrite = true;
            renderTex.Create();

            Graphics.Blit(tex2D, renderTex);

            return renderTex;
        }

        public Texture3D RenderTexToTex3D(RenderTexture renderTex)
        {
            var width = renderTex.width;
            var height = renderTex.height;
            var depth = renderTex.volumeDepth;

            var renderTexArray = RenderTex3DToRenderTex2DArray(renderTex);

            var tex2DArray = new Texture2D[depth];
            for (var z = 0; z < depth; ++z)
            {
                tex2DArray[z] = RenderTexToTex2D(renderTexArray[z]);
                RenderTexture.active = null;
                renderTexArray[z].Release();
                renderTexArray[z] = null;
            }

            var tex3D = new Texture3D(width, height, depth,
                TextureFormat.RFloat, false);
            tex3D.wrapMode = TextureWrapMode.Repeat;
            tex3D.filterMode = FilterMode.Trilinear;
            tex3D.anisoLevel = 0;

            var tex3DPixels = tex3D.GetPixels();
            for (var z = 0; z < depth; ++z)
            {
                var tex2DPixels = tex2DArray[z].GetPixels();
                for (var y = 0; y < height; ++y)
                {
                    for (var x = 0; x < width; ++x)
                    {
                        tex3DPixels[x + width * (y + height * z)]
                            = tex2DPixels[x + width * y];
                    }
                }
            }

            tex3D.SetPixels(tex3DPixels);
            tex3D.Apply();

            return tex3D;
        }

        public RenderTexture Tex3DToRenderTex(Texture3D tex3D)
        {
            var width = tex3D.width;
            var height = tex3D.height;
            var depth = tex3D.depth;

            var tex2DArray = new Texture2D[depth];
            var tex3DPixels = tex3D.GetPixels();
            for (var z = 0; z < depth; ++z)
            {
                var tex2D = new Texture2D(width, height,
                    TextureFormat.RFloat, false);
                var tex2DPixels = tex2D.GetPixels();
                for (var y = 0; y < height; ++y)
                {
                    for (var x = 0; x < width; ++x)
                    {
                        tex2DPixels[x + width * y]
                            = tex3DPixels[x + width * (y + height * z)];
                    }
                }
                tex2D.SetPixels(tex2DPixels);
                tex2D.Apply();
                tex2DArray[z] = tex2D;
            }

            var renderTexArray = new RenderTexture[depth];
            for (var z = 0; z < depth; ++z)
            {
                renderTexArray[z] = Tex2DToRenderTex(tex2DArray[z]);
            }

            var renderTex = RenderTex2DArrayToRenderTex3D(renderTexArray);
            for (var z = 0; z < depth; ++z)
            {
                // todo
                renderTexArray[z].Release();
                renderTexArray[z] = null;
            }

            return renderTex;
        }

    }

}
