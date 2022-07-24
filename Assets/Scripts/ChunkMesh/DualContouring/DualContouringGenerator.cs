using UnityEngine;

namespace VoxelTerrain
{
    public class DualContouringGenerator : ChunkMeshGenerator
    {
        public override Mesh GenerateChunkMesh(Texture chunkTex,
            Vector3Int coordinate, int levelOfDetail,
            LodMask lodMask)
        {
            Mesh mesh = new Mesh();
            // todo
            return mesh;
        }

    }

}
