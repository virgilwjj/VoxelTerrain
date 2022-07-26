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

        BrushTexBuilder _blushTexGenerator;

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

            _blushTexGenerator = gameObject
                .AddComponent(typeof(SphereBrushTexBuilder))
                as BrushTexBuilder;
            _blushTexGenerator.TerrainConfig = _terrainConfig;
        }

        void Start()
        {
            var coordinate = new Vector3Int(0, 0, 0);
            var levelOfDetail = 0;
            var lodMesk = LodMask.None;
            var chunkTex = _chunkTexGenerator.GenerateChunkTex(
                coordinate, levelOfDetail);    

            /*
            for (int i = 2; i <= 16; ++i)
            {
                var brushTex = _blushTexGenerator.GenerateBlushTex(i);           
                var a =  _texConverter.RenderTexToTex3D(brushTex);
                _texLoader.SaveTex3D(a, "Textures/Brushs/sphereBrush_" + i);
            }
            */
            
            var blushTex = _blushTexGenerator.BuildBrushTex(15);           
            _chunkTexEditor.AddEdit(chunkTex, coordinate, 0, blushTex, 15, new Vector3(0, 0, 0), 1.0f, 1.0f);

            var mesh = _chunkMeshGenerator.GenerateChunkMesh(
                chunkTex, coordinate, levelOfDetail, lodMesk);

            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

    }

}
