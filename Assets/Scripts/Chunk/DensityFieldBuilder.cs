using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace VoxelTerrain
{
    public class DensityFieldBuilder : MonoBehaviour
    {
        [SerializeField]
        ChunkSetting _chunkSetting;
        [SerializeField]
        ComputeShader _buildDensityBuilder;
        [SerializeField]
        ComputeShader _buildNoiseDensityBuilder;

        public float[] BuildDensityField(Vector3Int centerCoord, int levelOfDetail)
        {
            var numVoxelsPerAxis = _chunkSetting.NumVoxelsPerAxis;
            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var minCoord = centerCoord - Vector3Int.one * ((numVoxelsPerAxis >> 1) << levelOfDetail);
            var stridePerAxis = 1 << levelOfDetail;

            var numPointsPerChunk = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            var densityField = new float[numPointsPerChunk];
            var densityFieldBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float));
            densityFieldBuffer.SetData(densityField);

            _buildDensityBuilder.SetInt("numVoxelsPerAxis", numVoxelsPerAxis);
            _buildDensityBuilder.SetInt("numPointsPerAxis", numPointsPerAxis);
            _buildDensityBuilder.SetFloat("voxelSizePerAxis", voxelSizePerAxis);
            _buildDensityBuilder.SetVectorInt("minCoord", minCoord);
            _buildDensityBuilder.SetInt("stridePerAxis", stridePerAxis);
            _buildDensityBuilder.SetBuffer(0, "densityField", densityFieldBuffer);
            _buildDensityBuilder.DispatchThreads(0, numPointsPerAxis, numPointsPerAxis, numPointsPerAxis);

            densityFieldBuffer.GetData(densityField);
            densityFieldBuffer.Dispose();
            return densityField;
        }

        public float[] BuildNoiseDensityField(Vector3Int centerCoord, int levelOfDetail)
        {
            const int seed = 1;
            const int numOctaves = 8; 

            // Noise parameters
            var prng = new System.Random(seed);
            var offsets = new Vector3[numOctaves];
            float offsetRange = 1000;
            for (int i = 0; i < numOctaves; i++)
            {
                offsets[i] = new Vector3((float)prng.NextDouble() * 2 - 1, (float)prng.NextDouble() * 2 - 1, (float)prng.NextDouble() * 2 - 1) * offsetRange;
            }

            var offsetsBuffer = new ComputeBuffer(offsets.Length, sizeof(float) * 3);
            offsetsBuffer.SetData(offsets);

            var numVoxelsPerAxis = _chunkSetting.NumVoxelsPerAxis;
            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var minCoord = centerCoord - Vector3Int.one * ((numVoxelsPerAxis >> 1) << levelOfDetail);
            var stridePerAxis = 1 << levelOfDetail;

            var numPointsPerChunk = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            var densityField = new float[numPointsPerChunk];
            var densityFieldBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float));
            densityFieldBuffer.SetData(densityField);

            _buildNoiseDensityBuilder.SetBuffer(0, "offsets", offsetsBuffer);
            _buildNoiseDensityBuilder.SetInt("numVoxelsPerAxis", numVoxelsPerAxis);
            _buildNoiseDensityBuilder.SetInt("numPointsPerAxis", numPointsPerAxis);
            _buildNoiseDensityBuilder.SetFloat("voxelSizePerAxis", voxelSizePerAxis);
            _buildNoiseDensityBuilder.SetVectorInt("minCoord", minCoord);
            _buildNoiseDensityBuilder.SetInt("stridePerAxis", stridePerAxis);
            _buildNoiseDensityBuilder.SetBuffer(0, "densityField", densityFieldBuffer);
            _buildNoiseDensityBuilder.DispatchThreads(0, numPointsPerAxis, numPointsPerAxis, numPointsPerAxis);

            offsetsBuffer.Release();

            densityFieldBuffer.GetData(densityField);
            densityFieldBuffer.Dispose();
            return densityField;
        }

        public void SaveDensityField(float[] densityField, Vector3Int centerCoord)
        {
            string fieldName = "density_" + centerCoord.x + "_" + centerCoord.y + "_" + centerCoord.z;
            string path = "Chunks/" + fieldName + ".asset";
            byte[] result = densityField.SelectMany(x => BitConverter.GetBytes(x)).ToArray();
            File.WriteAllBytes(path, result);
        }

        public float[] LoadDensityField(Vector3Int centerCoord)
        {
            string fieldName = "density_" + centerCoord.x + "_" + centerCoord.y + "_" + centerCoord.z;
            string path = "Chunks/" + fieldName + ".asset";
            byte[] result = File.ReadAllBytes(path);
            var densityField = new float[result.Length / 4];            
            for (var i = 0; i < (result.Length / 4); ++i)
            {
                densityField[i] = BitConverter.ToSingle(result, 4 * i);
            }
            return densityField;
        }

        public bool IsDensityFieldExist(string dir, Vector3Int centerCoord)
        {
            string fieldName = "density_" + centerCoord.x + "_" + centerCoord.y + "_" + centerCoord.z;
            string path = dir + "/" + fieldName + ".asset";
            return File.Exists(path);
        }

        public void SaveDensityField(string dir, float[] densityField, Vector3Int centerCoord)
        {
            string fieldName = "density_" + centerCoord.x + "_" + centerCoord.y + "_" + centerCoord.z;
            string path = dir + "/" + fieldName + ".asset";
            byte[] result = densityField.SelectMany(x => BitConverter.GetBytes(x)).ToArray();
            File.WriteAllBytes(path, result);
        }

        public float[] LoadDensityField(string dir, Vector3Int centerCoord)
        {
            string fieldName = "density_" + centerCoord.x + "_" + centerCoord.y + "_" + centerCoord.z;
            string path = dir + "/" + fieldName + ".asset";
            byte[] result = File.ReadAllBytes(path);
            var densityField = new float[result.Length / 4];            
            for (var i = 0; i < (result.Length / 4); ++i)
            {
                densityField[i] = BitConverter.ToSingle(result, 4 * i);
            }
            return densityField;
        }

    }

}
