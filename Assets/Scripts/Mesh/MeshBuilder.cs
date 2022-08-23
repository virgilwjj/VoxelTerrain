using System.Linq.Expressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelTerrain
{
    public class MeshBuilder : MonoBehaviour
    {
        [SerializeField]
        ChunkSetting _chunkSetting;
        [SerializeField]
        ComputeShader _generateRegularCell;
        [SerializeField]
        ComputeShader _generateTransitionCell;
        ComputeBuffer _transitionVertexDataBuffer;

        void Awake()
        {
            _transitionVertexDataBuffer = new ComputeBuffer(512 * 12, sizeof(uint));

            var transitionVertexDataArray = new uint[512 * 12];
            var i = 0;
            for (var j = 0; j < 512; ++j)
            {
                for (var k = 0; k < 12; ++k)
                {
                    transitionVertexDataArray[i++]
                        = Transvoxel.transitionVertexData[j][k];
                }
            }
            _transitionVertexDataBuffer.SetData(transitionVertexDataArray);
        }

        void OnDestroy()
        {
            _transitionVertexDataBuffer.Dispose();
            _transitionVertexDataBuffer = null;
        }

        public ComputeBuffer InitTrisBuffer()
        {
            var numVoxelsPerAxis = _chunkSetting.NumVoxelsPerAxis;
            var trisCapacity = 5 * numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
            var trisBuffer = new ComputeBuffer(trisCapacity, sizeof(float) * 3 * 6, ComputeBufferType.Append);
            trisBuffer.SetCounterValue(0);
            return trisBuffer;
        }

        public void GenerateRegularCell(ComputeBuffer trisBuffer, float[] densityField, Vector3Int centerCoord, int levelOfDetail, LodMask lodMask)
        {
            var numVoxelsPerAxis = _chunkSetting.NumVoxelsPerAxis;
            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var isoLevel = _chunkSetting.IsoLevel;
            var minCoord = centerCoord - Vector3Int.one * ((numVoxelsPerAxis >> 1) << levelOfDetail);
            var stridePerAxis = 1 << levelOfDetail;

            var numPointsPerChunk = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            var densityFieldBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float));
            densityFieldBuffer.SetData(densityField);

            _generateRegularCell.SetInt("numVoxelsPerAxis", numVoxelsPerAxis);
            _generateRegularCell.SetInt("numPointsPerAxis", numPointsPerAxis);
            _generateRegularCell.SetFloat("voxelSizePerAxis", voxelSizePerAxis);
            _generateRegularCell.SetFloat("isoLevel", isoLevel);
            _generateRegularCell.SetVectorInt("centerCoord", centerCoord);
            _generateRegularCell.SetVectorInt("minCoord", minCoord);
            _generateRegularCell.SetInt("stridePerAxis", stridePerAxis);
            _generateRegularCell.SetInt("lodMask", (int)lodMask);
            _generateRegularCell.SetBuffer(0, "densityField", densityFieldBuffer);
            _generateRegularCell.SetBuffer(0, "trisBuffer", trisBuffer);
            _generateRegularCell.DispatchThreads(0, numVoxelsPerAxis, numVoxelsPerAxis, numVoxelsPerAxis);

            densityFieldBuffer.Dispose();
        }

        public void GenerateTransitionCellNegativeX(ComputeBuffer trisBuffer, float[] densityField, Vector3Int centerCoord, int levelOfDetail, LodMask lodMask, Vector3Int realCenterCoord)
        {
            var numVoxelsPerAxis = _chunkSetting.NumVoxelsPerAxis;
            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var isoLevel = _chunkSetting.IsoLevel;
            var minCoord = centerCoord - Vector3Int.one * ((numVoxelsPerAxis >> 1) << levelOfDetail);
            var stridePerAxis = 1 << levelOfDetail;
            var realMinCoord = realCenterCoord - Vector3Int.one * (numVoxelsPerAxis << levelOfDetail);

            var numPointsPerChunk = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            var densityFieldBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float));
            densityFieldBuffer.SetData(densityField);

            var kernelIndex = _generateTransitionCell.FindKernel("GenerateTransitionCellNegativeX");
            _generateTransitionCell.SetBuffer(kernelIndex, "transitionVertexDataBuffer", _transitionVertexDataBuffer);
            _generateTransitionCell.SetInt("numVoxelsPerAxis", numVoxelsPerAxis);
            _generateTransitionCell.SetInt("numPointsPerAxis", numPointsPerAxis);
            _generateTransitionCell.SetFloat("voxelSizePerAxis", voxelSizePerAxis);
            _generateTransitionCell.SetFloat("isoLevel", isoLevel);
            _generateTransitionCell.SetVectorInt("minCoord", minCoord);
            _generateTransitionCell.SetInt("stridePerAxis", stridePerAxis);
            _generateTransitionCell.SetVectorInt("realMinCoord", realMinCoord);
            _generateTransitionCell.SetVectorInt("realCenterCoord", realCenterCoord);
            _generateTransitionCell.SetInt("lodMask", (int)lodMask);
            _generateTransitionCell.SetBuffer(kernelIndex, "densityField", densityFieldBuffer);
            _generateTransitionCell.SetBuffer(kernelIndex, "trisBuffer", trisBuffer);
            _generateTransitionCell.DispatchThreads(kernelIndex, 1, (numVoxelsPerAxis >> 1), (numVoxelsPerAxis >> 1));

            densityFieldBuffer.Dispose();
        }

        public void GenerateTransitionCellPositiveX(ComputeBuffer trisBuffer, float[] densityField, Vector3Int centerCoord, int levelOfDetail, LodMask lodMask, Vector3Int realCenterCoord)
        {
            var numVoxelsPerAxis = _chunkSetting.NumVoxelsPerAxis;
            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var isoLevel = _chunkSetting.IsoLevel;
            var minCoord = centerCoord - Vector3Int.one * ((numVoxelsPerAxis >> 1) << levelOfDetail);
            var stridePerAxis = 1 << levelOfDetail;

            var numPointsPerChunk = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            var densityFieldBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float));
            densityFieldBuffer.SetData(densityField);

            var kernelIndex = _generateTransitionCell.FindKernel("GenerateTransitionCellPositiveX");
            _generateTransitionCell.SetBuffer(kernelIndex, "transitionVertexDataBuffer", _transitionVertexDataBuffer);
            _generateTransitionCell.SetInt("numVoxelsPerAxis", numVoxelsPerAxis);
            _generateTransitionCell.SetInt("numPointsPerAxis", numPointsPerAxis);
            _generateTransitionCell.SetFloat("voxelSizePerAxis", voxelSizePerAxis);
            _generateTransitionCell.SetFloat("isoLevel", isoLevel);
            _generateTransitionCell.SetVectorInt("centerCoord", realCenterCoord);
            _generateTransitionCell.SetVectorInt("minCoord", minCoord);
            _generateTransitionCell.SetInt("stridePerAxis", stridePerAxis);
            _generateTransitionCell.SetInt("lodMask", (int)lodMask);
            _generateTransitionCell.SetBuffer(kernelIndex, "densityField", densityFieldBuffer);
            _generateTransitionCell.SetBuffer(kernelIndex, "trisBuffer", trisBuffer);
            _generateTransitionCell.DispatchThreads(kernelIndex, 1, numVoxelsPerAxis >> 1, numVoxelsPerAxis >> 1);

            densityFieldBuffer.Dispose();
        }

        public void GenerateTransitionCellNegativeY(ComputeBuffer trisBuffer, float[] densityField, Vector3Int centerCoord, int levelOfDetail, LodMask lodMask, Vector3Int realCenterCoord)
        {
            var numVoxelsPerAxis = _chunkSetting.NumVoxelsPerAxis;
            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var isoLevel = _chunkSetting.IsoLevel;
            var minCoord = centerCoord - Vector3Int.one * ((numVoxelsPerAxis >> 1) << levelOfDetail);
            var stridePerAxis = 1 << levelOfDetail;
            var realMinCoord = realCenterCoord - Vector3Int.one * (numVoxelsPerAxis << levelOfDetail);

            var numPointsPerChunk = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            var densityFieldBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float));
            densityFieldBuffer.SetData(densityField);

            var kernelIndex = _generateTransitionCell.FindKernel("GenerateTransitionCellNegativeY");
            _generateTransitionCell.SetBuffer(kernelIndex, "transitionVertexDataBuffer", _transitionVertexDataBuffer);
            _generateTransitionCell.SetInt("numVoxelsPerAxis", numVoxelsPerAxis);
            _generateTransitionCell.SetInt("numPointsPerAxis", numPointsPerAxis);
            _generateTransitionCell.SetFloat("voxelSizePerAxis", voxelSizePerAxis);
            _generateTransitionCell.SetFloat("isoLevel", isoLevel);
            _generateTransitionCell.SetVectorInt("minCoord", minCoord);
            _generateTransitionCell.SetInt("stridePerAxis", stridePerAxis);
            _generateTransitionCell.SetVectorInt("realMinCoord", realMinCoord);
            _generateTransitionCell.SetVectorInt("realCenterCoord", realCenterCoord);
            _generateTransitionCell.SetInt("lodMask", (int)lodMask);
            _generateTransitionCell.SetBuffer(kernelIndex, "densityField", densityFieldBuffer);
            _generateTransitionCell.SetBuffer(kernelIndex, "trisBuffer", trisBuffer);
            _generateTransitionCell.DispatchThreads(kernelIndex, 1, numVoxelsPerAxis >> 1, numVoxelsPerAxis >> 1);

            densityFieldBuffer.Dispose();
        }

        public void GenerateTransitionCellPositiveY(ComputeBuffer trisBuffer, float[] densityField, Vector3Int centerCoord, int levelOfDetail, LodMask lodMask, Vector3Int realCenterCoord)
        {
            var numVoxelsPerAxis = _chunkSetting.NumVoxelsPerAxis;
            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var isoLevel = _chunkSetting.IsoLevel;
            var minCoord = centerCoord - Vector3Int.one * ((numVoxelsPerAxis >> 1) << levelOfDetail);
            var stridePerAxis = 1 << levelOfDetail;

            var numPointsPerChunk = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            var densityFieldBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float));
            densityFieldBuffer.SetData(densityField);

            var kernelIndex = _generateTransitionCell.FindKernel("GenerateTransitionCellPositiveY");
            _generateTransitionCell.SetBuffer(kernelIndex, "transitionVertexDataBuffer", _transitionVertexDataBuffer);
            _generateTransitionCell.SetInt("numVoxelsPerAxis", numVoxelsPerAxis);
            _generateTransitionCell.SetInt("numPointsPerAxis", numPointsPerAxis);
            _generateTransitionCell.SetFloat("voxelSizePerAxis", voxelSizePerAxis);
            _generateTransitionCell.SetFloat("isoLevel", isoLevel);
            _generateTransitionCell.SetVectorInt("centerCoord", realCenterCoord);
            _generateTransitionCell.SetVectorInt("minCoord", minCoord);
            _generateTransitionCell.SetInt("stridePerAxis", stridePerAxis);
            _generateTransitionCell.SetInt("lodMask", (int)lodMask);
            _generateTransitionCell.SetBuffer(kernelIndex, "densityField", densityFieldBuffer);
            _generateTransitionCell.SetBuffer(kernelIndex, "trisBuffer", trisBuffer);
            _generateTransitionCell.DispatchThreads(kernelIndex, 1, numVoxelsPerAxis >> 1, numVoxelsPerAxis >> 1);

            densityFieldBuffer.Dispose();
        }

        public void GenerateTransitionCellNegativeZ(ComputeBuffer trisBuffer, float[] densityField, Vector3Int centerCoord, int levelOfDetail, LodMask lodMask, Vector3Int realCenterCoord)
        {
            var numVoxelsPerAxis = _chunkSetting.NumVoxelsPerAxis;
            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var isoLevel = _chunkSetting.IsoLevel;
            var minCoord = centerCoord - Vector3Int.one * ((numVoxelsPerAxis >> 1) << levelOfDetail);
            var stridePerAxis = 1 << levelOfDetail;
            var realMinCoord = realCenterCoord - Vector3Int.one * (numVoxelsPerAxis << levelOfDetail);

            var numPointsPerChunk = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            var densityFieldBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float));
            densityFieldBuffer.SetData(densityField);

            var kernelIndex = _generateTransitionCell.FindKernel("GenerateTransitionCellNegativeZ");
            _generateTransitionCell.SetBuffer(kernelIndex, "transitionVertexDataBuffer", _transitionVertexDataBuffer);
            _generateTransitionCell.SetInt("numVoxelsPerAxis", numVoxelsPerAxis);
            _generateTransitionCell.SetInt("numPointsPerAxis", numPointsPerAxis);
            _generateTransitionCell.SetFloat("voxelSizePerAxis", voxelSizePerAxis);
            _generateTransitionCell.SetFloat("isoLevel", isoLevel);
            _generateTransitionCell.SetVectorInt("minCoord", minCoord);
            _generateTransitionCell.SetInt("stridePerAxis", stridePerAxis);
            _generateTransitionCell.SetVectorInt("realMinCoord", realMinCoord);
            _generateTransitionCell.SetVectorInt("realCenterCoord", realCenterCoord);
            _generateTransitionCell.SetInt("lodMask", (int)lodMask);
            _generateTransitionCell.SetBuffer(kernelIndex, "densityField", densityFieldBuffer);
            _generateTransitionCell.SetBuffer(kernelIndex, "trisBuffer", trisBuffer);
            _generateTransitionCell.DispatchThreads(kernelIndex, 1, numVoxelsPerAxis >> 1, numVoxelsPerAxis >> 1);

            densityFieldBuffer.Dispose();
        }

        public void GenerateTransitionCellPositiveZ(ComputeBuffer trisBuffer, float[] densityField, Vector3Int centerCoord, int levelOfDetail, LodMask lodMask, Vector3Int realCenterCoord)
        {
            var numVoxelsPerAxis = _chunkSetting.NumVoxelsPerAxis;
            var numPointsPerAxis = numVoxelsPerAxis + 1;
            var voxelSizePerAxis = _chunkSetting.VoxelSizePerAxis;
            var isoLevel = _chunkSetting.IsoLevel;
            var minCoord = centerCoord - Vector3Int.one * ((numVoxelsPerAxis >> 1) << levelOfDetail);
            var stridePerAxis = 1 << levelOfDetail;

            var numPointsPerChunk = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            var densityFieldBuffer = new ComputeBuffer(numPointsPerChunk, sizeof(float));
            densityFieldBuffer.SetData(densityField);

            var kernelIndex = _generateTransitionCell.FindKernel("GenerateTransitionCellPositiveZ");
            _generateTransitionCell.SetBuffer(kernelIndex, "transitionVertexDataBuffer", _transitionVertexDataBuffer);
            _generateTransitionCell.SetInt("numVoxelsPerAxis", numVoxelsPerAxis);
            _generateTransitionCell.SetInt("numPointsPerAxis", numPointsPerAxis);
            _generateTransitionCell.SetFloat("voxelSizePerAxis", voxelSizePerAxis);
            _generateTransitionCell.SetFloat("isoLevel", isoLevel);
            _generateTransitionCell.SetVectorInt("centerCoord", realCenterCoord);
            _generateTransitionCell.SetVectorInt("minCoord", minCoord);
            _generateTransitionCell.SetInt("stridePerAxis", stridePerAxis);
            _generateTransitionCell.SetInt("lodMask", (int)lodMask);
            _generateTransitionCell.SetBuffer(kernelIndex, "densityField", densityFieldBuffer);
            _generateTransitionCell.SetBuffer(kernelIndex, "trisBuffer", trisBuffer);
            _generateTransitionCell.DispatchThreads(kernelIndex, 1, numVoxelsPerAxis >> 1, numVoxelsPerAxis >> 1);

            densityFieldBuffer.Dispose();
        }

        public Mesh TrisbufferToMesh(ComputeBuffer trisBuffer)
        {
            var trisCountArray = new int[1];

            var trisCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            ComputeBuffer.CopyCount(trisBuffer, trisCountBuffer, 0);
            trisCountBuffer.GetData(trisCountArray);
            trisCountBuffer.Dispose();

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
        
            Parallel.For(0, trisCount, (i) =>
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
            });

            var mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = meshTris;

            return mesh;
        }

    }

}
