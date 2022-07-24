using UnityEngine;

namespace VoxelTerrain
{
    public static class ComputeShaderExtensions
    {
        public static void SetVectorInt(
            this ComputeShader computeShader, string name,
            Vector3Int val)
        {
            computeShader.SetInts(name, val.x, val.y, val.z);
        }
        
        public static void DispatchThreads(
            this ComputeShader computeShader, int kernelIndex,
            int x, int y, int z)
        {
            uint threadGroupSizeX, threadGroupSizeY,
            threadGroupSizeZ;
            computeShader.GetKernelThreadGroupSizes(kernelIndex,
                out threadGroupSizeX, out threadGroupSizeY,
                out threadGroupSizeZ);

            int threadGroupsX = (x + (int)threadGroupSizeX - 1)
                / (int)threadGroupSizeX;
            int threadGroupsY = (y + (int)threadGroupSizeY - 1)
                / (int)threadGroupSizeY;
            int threadGroupsZ = (z + (int)threadGroupSizeZ - 1)
                / (int)threadGroupSizeZ;

            computeShader.Dispatch(kernelIndex, threadGroupsX,
                threadGroupsY, threadGroupsZ);
        }

    }

}
