using System.ComponentModel;
using UnityEngine;

namespace VoxelTerrain
{
    public class TransvoxelGenerator : ChunkMeshGenerator
    {
        [SerializeField]
        ComputeShader _generateRegularCell;
        [SerializeField]
        ComputeShader _generateTransitionCell;
        ComputeBuffer _transitionVertexDataBuffer;

        void Awake()
        {
            InitTransitionVertexDataBuffer();
        }

        void OnDestroy()
        {
            _transitionVertexDataBuffer.Release();
            _transitionVertexDataBuffer = null;
        }

        void InitTransitionVertexDataBuffer()
        {
            _transitionVertexDataBuffer
                = new ComputeBuffer(512 * 12, sizeof(uint));
            uint[] transitionVertexDataArray = new uint[512 * 12]; 
            uint i = 0;
            for (uint j = 0; j < 512; ++j)
            {
                for (uint k = 0; k < 12; ++k)
                {
                    transitionVertexDataArray[i++]
                        = Transvoxel.transitionVertexData[j][k];
                }
            }
            _transitionVertexDataBuffer.SetData(
                transitionVertexDataArray);
        }

        Mesh TrisbufferToMesh(ComputeBuffer trisBuffer)
        {
            var trisCountBuffer = new ComputeBuffer(1, sizeof(int),
                ComputeBufferType.Raw);
            ComputeBuffer.CopyCount(trisBuffer, trisCountBuffer, 0);
            int[] trisCountArray = {0};
            trisCountBuffer.GetData(trisCountArray);
            trisCountBuffer.Release();
            trisCountBuffer = null;

            var trisCount = trisCountArray[0];
            if (trisCount == 0)
            {
                return null;
            }

            var trisArray = new Triangle[trisCount];
            trisBuffer.GetData(trisArray, 0, 0, trisCount);
        
            var vertices = new Vector3[trisCount * 3];
            var normals = new Vector3[trisCount * 3];
            var meshTris = new int[trisCount * 3];
            for (var i = 0; i < trisCount; ++i)
            {
                vertices[i * 3 + 0] = trisArray[i].vertex0;
                vertices[i * 3 + 1] = trisArray[i].vertex1;
                vertices[i * 3 + 2] = trisArray[i].vertex2;
                normals[i * 3 + 0] = trisArray[i].normal0;
                normals[i * 3 + 1] = trisArray[i].normal1;
                normals[i * 3 + 2] = trisArray[i].normal2;
                meshTris[i * 3 + 0] = i * 3 + 0;
                meshTris[i * 3 + 1] = i * 3 + 1;
                meshTris[i * 3 + 2] = i * 3 + 2;
            }

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = meshTris;

            return mesh;
        }

        public override Mesh GenerateChunkMesh(
            Texture chunkTex, Vector3Int coordinate,
            int levelOfDetail, LodMask lodMask)
        {
            var numVoxelsPerAxis
                = TerrainConfig.NumVoxelsPerAxisForChunk;
            var trisCapacity = 5 * numVoxelsPerAxis
                * numVoxelsPerAxis * numVoxelsPerAxis;

            var trisBuffer = new ComputeBuffer(trisCapacity,
                sizeof(float) * 3 * 6, ComputeBufferType.Append);
            trisBuffer.SetCounterValue(0);

            GenerateRegularCell(trisBuffer, chunkTex, coordinate,
                levelOfDetail, lodMask);
            /*
            if (lodMask.HasFlag(LodMask.NegativeX))
            {
                GenerateTransitionCellNegativeX(trisBuffer,
                    chunkTex, coordinate, levelOfDetail, lodMask);
            }
            if (lodMask.HasFlag(LodMask.PositiveX))
            {
                GenerateTransitionCellPositiveX(trisBuffer,
                    chunkTex, coordinate, levelOfDetail, lodMask);
            }
            if (lodMask.HasFlag(LodMask.NegativeY))
            {
                GenerateTransitionCellNegativeY(trisBuffer,
                    chunkTex, coordinate, levelOfDetail, lodMask);
            }
            if (lodMask.HasFlag(LodMask.PositiveY))
            {
                GenerateTransitionCellPositiveY(trisBuffer,
                    chunkTex, coordinate, levelOfDetail, lodMask);
            }
            if (lodMask.HasFlag(LodMask.NegativeZ))
            {
                GenerateTransitionCellNegativeZ(trisBuffer,
                    chunkTex, coordinate, levelOfDetail, lodMask);
            }
            if (lodMask.HasFlag(LodMask.PositiveZ))
            {
                GenerateTransitionCellPositiveZ(trisBuffer,
                    chunkTex, coordinate, levelOfDetail, lodMask);
            }
            */

            var mesh = TrisbufferToMesh(trisBuffer);
            
            trisBuffer.Release();
            trisBuffer = null;

            return mesh;
        }

        void GenerateRegularCell(ComputeBuffer trisBuffer,
            Texture chunkTex, Vector3Int coordinate,
            int levelOfDetail, LodMask lodMask)
        {
            var numVoxelsPerAxis
                = TerrainConfig.NumVoxelsPerAxisForChunk;
            var numPointsPerAxis
                = TerrainConfig.NumPointsPerAxisForChunk;
            var voxelSizePerAxis
                = TerrainConfig.VoxelSizePerAxis;

            _generateRegularCell.SetBuffer(0, "trisBuffer",
                trisBuffer);
            _generateRegularCell.SetTexture(0, "chunkTex",
                chunkTex);
            _generateRegularCell.SetInt("numVoxelsPerAxis",
                numVoxelsPerAxis);
            _generateRegularCell.SetInt("numPointsPerAxis",
                numPointsPerAxis);
            _generateRegularCell.SetFloat("voxelSizePerAxis",
                voxelSizePerAxis);
            _generateRegularCell.SetVectorInt("coordinate",
                coordinate);
            _generateRegularCell.SetInt("levelOfDetail",
                levelOfDetail);
            _generateRegularCell.SetInt("lodMask", (int)lodMask);
            _generateRegularCell.DispatchThreads(0,
                numVoxelsPerAxis, numVoxelsPerAxis,
                numVoxelsPerAxis);
        }

        void GenerateTransitionCellNegativeX(
            ComputeBuffer trisBuffer, Texture chunkTex,
            Vector3Int coordinate, int levelOfDetail,
            LodMask lodMask)
        {
            var numVoxelsPerAxis
                = TerrainConfig.NumVoxelsPerAxisForChunk;
            var numPointsPerAxis
                = TerrainConfig.NumPointsPerAxisForChunk;
            var voxelSizePerAxis
                = TerrainConfig.VoxelSizePerAxis;

            var kernelIndex = _generateTransitionCell
                .FindKernel("GenerateTransitionCellNegativeX");
            _generateTransitionCell.SetBuffer(kernelIndex,
                "transitionVertexDataBuffer",
                _transitionVertexDataBuffer);
            _generateTransitionCell.SetBuffer(kernelIndex,
                "trisBuffer", trisBuffer);
            _generateTransitionCell.SetTexture(kernelIndex,
                "chunkTex", chunkTex);
            _generateTransitionCell.SetInt("numVoxelsPerAxis",
                numVoxelsPerAxis);
            _generateTransitionCell.SetInt("numPointsPerAxis",
                numPointsPerAxis);
            _generateTransitionCell.SetFloat("voxelSizePerAxis",
                voxelSizePerAxis);
            _generateTransitionCell.SetVectorInt("coordinate",
                coordinate);
            _generateTransitionCell.SetInt("levelOfDetail",
                levelOfDetail);
            _generateTransitionCell.SetInt("lodMask", (int)lodMask);
            _generateTransitionCell.DispatchThreads(0, 1,
                numVoxelsPerAxis, numVoxelsPerAxis);
        }

        void GenerateTransitionCellPositiveX(
            ComputeBuffer trisBuffer, Texture chunkTex,
            Vector3Int coordinate, int levelOfDetail,
            LodMask lodMask)
        {
            var numVoxelsPerAxis
                = TerrainConfig.NumVoxelsPerAxisForChunk;
            var numPointsPerAxis
                = TerrainConfig.NumPointsPerAxisForChunk;
            var voxelSizePerAxis
                = TerrainConfig.VoxelSizePerAxis;

            var kernelIndex = _generateTransitionCell
                .FindKernel("GenerateTransitionCellPositiveX");
            _generateTransitionCell.SetBuffer(kernelIndex,
                "transitionVertexDataBuffer",
                _transitionVertexDataBuffer);
            _generateTransitionCell.SetBuffer(kernelIndex,
                "trisBuffer", trisBuffer);
            _generateTransitionCell.SetTexture(kernelIndex,
                "chunkTex", chunkTex);
            _generateTransitionCell.SetInt("numVoxelsPerAxis",
                numVoxelsPerAxis);
            _generateTransitionCell.SetInt("numPointsPerAxis",
                numPointsPerAxis);
            _generateTransitionCell.SetFloat("voxelSizePerAxis",
                voxelSizePerAxis);
            _generateTransitionCell.SetVectorInt("coordinate",
                coordinate);
            _generateTransitionCell.SetInt("levelOfDetail",
                levelOfDetail);
            _generateTransitionCell.SetInt("lodMask", (int)lodMask);
            _generateTransitionCell.DispatchThreads(0, 1,
                numVoxelsPerAxis, numVoxelsPerAxis);
        }

        void GenerateTransitionCellNegativeY(
            ComputeBuffer trisBuffer, Texture chunkTex,
            Vector3Int coordinate, int levelOfDetail,
            LodMask lodMask)
        {
            var numVoxelsPerAxis
                = TerrainConfig.NumVoxelsPerAxisForChunk;
            var numPointsPerAxis
                = TerrainConfig.NumPointsPerAxisForChunk;
            var voxelSizePerAxis
                = TerrainConfig.VoxelSizePerAxis;

            var kernelIndex = _generateTransitionCell
                .FindKernel("GenerateTransitionCellNegativeY");
            _generateTransitionCell.SetBuffer(kernelIndex,
                "transitionVertexDataBuffer",
                _transitionVertexDataBuffer);
            _generateTransitionCell.SetBuffer(kernelIndex,
                "trisBuffer", trisBuffer);
            _generateTransitionCell.SetTexture(kernelIndex,
                "chunkTex", chunkTex);
            _generateTransitionCell.SetInt("numVoxelsPerAxis",
                numVoxelsPerAxis);
            _generateTransitionCell.SetInt("numPointsPerAxis",
                numPointsPerAxis);
            _generateTransitionCell.SetFloat("voxelSizePerAxis",
                voxelSizePerAxis);
            _generateTransitionCell.SetVectorInt("coordinate",
                coordinate);
            _generateTransitionCell.SetInt("levelOfDetail",
                levelOfDetail);
            _generateTransitionCell.SetInt("lodMask", (int)lodMask);
            _generateTransitionCell.DispatchThreads(0, 1,
                numVoxelsPerAxis, numVoxelsPerAxis);
        }

        void GenerateTransitionCellPositiveY(
            ComputeBuffer trisBuffer, Texture chunkTex,
            Vector3Int coordinate, int levelOfDetail,
            LodMask lodMask)
        {
            var numVoxelsPerAxis
                = TerrainConfig.NumVoxelsPerAxisForChunk;
            var numPointsPerAxis
                = TerrainConfig.NumPointsPerAxisForChunk;
            var voxelSizePerAxis
                = TerrainConfig.VoxelSizePerAxis;

            var kernelIndex = _generateTransitionCell
                .FindKernel("GenerateTransitionCellPositiveY");
            _generateTransitionCell.SetBuffer(kernelIndex,
                "transitionVertexDataBuffer",
                _transitionVertexDataBuffer);
            _generateTransitionCell.SetBuffer(kernelIndex,
                "trisBuffer", trisBuffer);
            _generateTransitionCell.SetTexture(kernelIndex,
                "chunkTex", chunkTex);
            _generateTransitionCell.SetInt("numVoxelsPerAxis",
                numVoxelsPerAxis);
            _generateTransitionCell.SetInt("numPointsPerAxis",
                numPointsPerAxis);
            _generateTransitionCell.SetFloat("voxelSizePerAxis",
                voxelSizePerAxis);
            _generateTransitionCell.SetVectorInt("coordinate",
                coordinate);
            _generateTransitionCell.SetInt("levelOfDetail",
                levelOfDetail);
            _generateTransitionCell.SetInt("lodMask", (int)lodMask);
            _generateTransitionCell.DispatchThreads(0, 1,
                numVoxelsPerAxis, numVoxelsPerAxis);
        }

        void GenerateTransitionCellNegativeZ(
            ComputeBuffer trisBuffer, Texture chunkTex,
            Vector3Int coordinate, int levelOfDetail,
            LodMask lodMask)
        {
            var numVoxelsPerAxis
                = TerrainConfig.NumVoxelsPerAxisForChunk;
            var numPointsPerAxis
                = TerrainConfig.NumPointsPerAxisForChunk;
            var voxelSizePerAxis
                = TerrainConfig.VoxelSizePerAxis;

            var kernelIndex = _generateTransitionCell
                .FindKernel("GenerateTransitionCellNegativeZ");
            _generateTransitionCell.SetBuffer(kernelIndex,
                "transitionVertexDataBuffer",
                _transitionVertexDataBuffer);
            _generateTransitionCell.SetBuffer(kernelIndex,
                "trisBuffer", trisBuffer);
            _generateTransitionCell.SetTexture(kernelIndex,
                "chunkTex", chunkTex);
            _generateTransitionCell.SetInt("numVoxelsPerAxis",
                numVoxelsPerAxis);
            _generateTransitionCell.SetInt("numPointsPerAxis",
                numPointsPerAxis);
            _generateTransitionCell.SetFloat("voxelSizePerAxis",
                voxelSizePerAxis);
            _generateTransitionCell.SetVectorInt("coordinate",
                coordinate);
            _generateTransitionCell.SetInt("levelOfDetail",
                levelOfDetail);
            _generateTransitionCell.SetInt("lodMask", (int)lodMask);
            _generateTransitionCell.DispatchThreads(0, 1,
                numVoxelsPerAxis, numVoxelsPerAxis);
        }

        void GenerateTransitionCellPositiveZ(
            ComputeBuffer trisBuffer, Texture chunkTex,
            Vector3Int coordinate, int levelOfDetail,
            LodMask lodMask)
        {
            var numVoxelsPerAxis
                = TerrainConfig.NumVoxelsPerAxisForChunk;
            var numPointsPerAxis
                = TerrainConfig.NumPointsPerAxisForChunk;
            var voxelSizePerAxis
                = TerrainConfig.VoxelSizePerAxis;

            var kernelIndex = _generateTransitionCell
                .FindKernel("GenerateTransitionCellPositiveZ");
            _generateTransitionCell.SetBuffer(kernelIndex,
                "transitionVertexDataBuffer",
                _transitionVertexDataBuffer);
            _generateTransitionCell.SetBuffer(kernelIndex,
                "trisBuffer", trisBuffer);
            _generateTransitionCell.SetTexture(kernelIndex,
                "chunkTex", chunkTex);
            _generateTransitionCell.SetInt("numVoxelsPerAxis",
                numVoxelsPerAxis);
            _generateTransitionCell.SetInt("numPointsPerAxis",
                numPointsPerAxis);
            _generateTransitionCell.SetFloat("voxelSizePerAxis",
                voxelSizePerAxis);
            _generateTransitionCell.SetVectorInt("coordinate",
                coordinate);
            _generateTransitionCell.SetInt("levelOfDetail",
                levelOfDetail);
            _generateTransitionCell.SetInt("lodMask", (int)lodMask);
            _generateTransitionCell.DispatchThreads(0, 1,
                numVoxelsPerAxis, numVoxelsPerAxis);
        }

    }

}
