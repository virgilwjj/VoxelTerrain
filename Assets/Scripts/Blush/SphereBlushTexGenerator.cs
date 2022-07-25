using UnityEngine;

namespace VoxelTerrain
{
    public class SphereBlushTexGenerator : BlushTexGenerator
    {
        [SerializeField]
        ComputeShader _generateSphereBlushTex;

        public override RenderTexture GenerateBlushTex(
            int numVoxelsPerAxis)
        {
            var blushTex = base.GenerateBlushTex(
                numVoxelsPerAxis);

            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis
                = TerrainConfig.VoxelSizePerAxis;
            var sphereCenter = Vector3.one * numVoxelsPerAxis
                * 0.5f;
            var sphereRadius = (numVoxelsPerAxis - 1) * 0.5f;

            _generateSphereBlushTex.SetTexture(0, "blushTex",
                blushTex);
            _generateSphereBlushTex.SetInt("numVoxelsPerAxis",
                numVoxelsPerAxis);
            _generateSphereBlushTex.SetInt("numPointsPerAxis",
                numPointsPerAxis);
            _generateSphereBlushTex.SetFloat("voxelSizePerAxis",
                voxelSizePerAxis);
            _generateSphereBlushTex.SetVector("sphereCenter",
                sphereCenter);
            _generateSphereBlushTex.SetFloat("sphereRadius",
                sphereRadius);
            _generateSphereBlushTex.DispatchThreads(0,
                numPointsPerAxis, numPointsPerAxis,
                numPointsPerAxis);

            return blushTex;
        }

    }

}
