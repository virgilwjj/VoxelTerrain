using UnityEngine;

namespace VoxelTerrain
{
    public class TerrainManager : MonoBehaviour
    {
        [SerializeField]
        TerrainConfig _terrainConfig;
        ChunkTexGenerator _chunkTexGenerator;
        ChunkMeshGenerator _chunkMeshGenerator;

        void Awake()
        {
            _chunkTexGenerator = gameObject
                .AddComponent(typeof(PlaneChunkTexGenerator))
                as ChunkTexGenerator;
            _chunkTexGenerator.TerrainConfig = _terrainConfig;

            _chunkMeshGenerator = gameObject
                .AddComponent(typeof(TransvoxelGenerator))
                as ChunkMeshGenerator;
            _chunkMeshGenerator.TerrainConfig = _terrainConfig;
        }

        void Start()
        {
            var coordinate = new Vector3Int(0, 0, 0);
            var levelOfDetail = 0;
            var lodMesk = LodMask.None;
            var chunkTex = _chunkTexGenerator.GenerateChunkTex(
                coordinate, levelOfDetail);    
            var mesh = _chunkMeshGenerator.GenerateChunkMesh(
                chunkTex, coordinate, levelOfDetail, lodMesk);

            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

    }

}
