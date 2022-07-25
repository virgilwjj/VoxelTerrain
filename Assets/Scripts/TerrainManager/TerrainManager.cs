using UnityEngine;

namespace VoxelTerrain
{
    public class TerrainManager : MonoBehaviour
    {
        [SerializeField]
        TerrainConfig _terrainConfig;
        ChunkTexGenerator _chunkTexGenerator;
        ChunkMeshGenerator _chunkMeshGenerator;
        TexConverter _texConverter;
        TexLoader _texLoader;
        ChunkTexEditor _chunkTexEditor;

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

            _texConverter = gameObject
                .AddComponent(typeof(TexConverter))
                as TexConverter;

            _texLoader = gameObject
                .AddComponent(typeof(TexLoader))
                as TexLoader;

            _chunkTexEditor = gameObject
                .AddComponent(typeof(ChunkTexEditor))
                as ChunkTexEditor;
            _chunkTexEditor.TerrainConfig = _terrainConfig;
        }

        void Start()
        {
            var coordinate = new Vector3Int(0, 0, 0);
            var levelOfDetail = 0;
            var lodMesk = LodMask.None;
            var chunkTex = _chunkTexGenerator.GenerateChunkTex(
                coordinate, levelOfDetail);    

            var blushTex = _texLoader.LoadTex3D("BlushTexs/sphere_8");
            
            _chunkTexEditor.AddEdit(chunkTex, coordinate, 0, blushTex, 8, new Vector3(3, 3, 3), 1.0f);

            var mesh = _chunkMeshGenerator.GenerateChunkMesh(
                chunkTex, coordinate, levelOfDetail, lodMesk);

            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

    }

}
