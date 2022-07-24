using UnityEngine;

namespace VoxelTerrain
{
    public class ChunkMeshGenerator : MonoBehaviour
    {
        [HideInInspector]
        public TerrainConfig TerrainConfig;

        public virtual Mesh GenerateChunkMesh(Texture chunkTex,
            Vector3Int coordinate, int levelOfDetail,
            LodMask lodMask)
        {
            Mesh mesh = new Mesh();

            return mesh;
        }

    }

}
