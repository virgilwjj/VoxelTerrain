using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelTerrain
{
    public class MtV : MonoBehaviour
    {
        [SerializeField]
        ComputeShader _meshToSDF;
        [SerializeField]
        ComputeShader _jumpFloodAssignment;

        public RenderTexture MeshToSDF(Mesh mesh, int voxelResolution,
            Vector3 offset, int numSamplesPerTriangle, float scaleMeshBy)
        {
            var indicies = mesh.triangles;
            var numIdxes = indicies.Length;
            var numTris = numIdxes / 3;
            var indicesBuffer = new ComputeBuffer(numIdxes,
                sizeof(uint));
            indicesBuffer.SetData(indicies);

            var vertexBuffer = new ComputeBuffer(mesh.vertexCount,
                sizeof(float) * 3);
            var verts = mesh.vertices;
            vertexBuffer.SetData(verts);

            var voxels = new RenderTexture(voxelResolution,
                voxelResolution, 0, RenderTextureFormat.ARGBFloat);
            voxels.dimension = TextureDimension.Tex3D;
            voxels.enableRandomWrite = true;
            voxels.useMipMap = false;
            voxels.volumeDepth = voxelResolution;
            voxels.Create();

            var Zero = _meshToSDF.FindKernel("Zero");
            _meshToSDF.SetBuffer(Zero, "IndexBuffer", indicesBuffer);
            _meshToSDF.SetBuffer(Zero, "VertexBuffer", vertexBuffer);
            _meshToSDF.SetTexture(Zero, "Voxels", voxels);
            _meshToSDF.DispatchThreads(Zero, voxelResolution, voxelResolution, voxelResolution);

            var MtV = _meshToSDF.FindKernel("MeshToVoxel");
            _meshToSDF.SetBuffer(MtV, "IndexBuffer", indicesBuffer);
            _meshToSDF.SetBuffer(MtV, "VertexBuffer", vertexBuffer);
            _meshToSDF.SetTexture(MtV, "Voxels", voxels);
            _meshToSDF.SetInt("tris", numTris);
            _meshToSDF.SetVector("offset", offset);
            _meshToSDF.SetInt("numSamples", numSamplesPerTriangle);
            _meshToSDF.SetFloat("scale", scaleMeshBy);
            _meshToSDF.SetInt("voxelSide", voxelResolution);
            _meshToSDF.DispatchThreads(MtV, numTris, 1, 1);

            indicesBuffer.Dispose();
            vertexBuffer.Dispose();

            return voxels;
        }

        public ComputeBuffer FloodFillToSDF(RenderTexture voxels, float postProcessThickness, int voxelResolution)
        {
            int dispatchCubeSize = voxels.width;

            var Preprocess = _jumpFloodAssignment.FindKernel("Preprocess");
            _jumpFloodAssignment.SetInt("dispatchCubeSide", dispatchCubeSize);
            _jumpFloodAssignment.SetTexture(Preprocess, "Voxels", voxels);
            _jumpFloodAssignment.DispatchThreads(Preprocess, voxels.width, voxels.height, voxels.volumeDepth);

            var JFA = _jumpFloodAssignment.FindKernel("JFA");
            _jumpFloodAssignment.SetTexture(JFA, "Voxels", voxels);
        
            for (int offset = voxels.width / 2; offset >= 1; offset /= 2)
            {
                _jumpFloodAssignment.SetInt("samplingOffset", offset);
                _jumpFloodAssignment.DispatchThreads(JFA, voxels.width, voxels.height, voxels.volumeDepth);
            }

            var pointBuffer = new ComputeBuffer(voxelResolution * voxelResolution * voxelResolution, sizeof(float));

            var Postprocess = _jumpFloodAssignment.FindKernel("Postprocess");
            _jumpFloodAssignment.SetBuffer(Postprocess, "pointBuffer", pointBuffer);
            _jumpFloodAssignment.SetFloat("postProcessThickness", postProcessThickness);
            _jumpFloodAssignment.SetTexture(Postprocess, "Voxels", voxels);
            _jumpFloodAssignment.SetInt("voxelSide", voxelResolution);
            _jumpFloodAssignment.DispatchThreads(Postprocess, voxels.width, voxels.height, voxels.volumeDepth);

            return pointBuffer;
        }

    }

}
