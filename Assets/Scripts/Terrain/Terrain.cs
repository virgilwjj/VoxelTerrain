using System.Security.Cryptography;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelTerrain
{
    public class Terrain : MonoBehaviour
    {
        [SerializeField]
        TerrainConfig _terrainConfig;
        [SerializeField]
        Camera _camera;
        Octree _octree;
        ChunkTexGenerator _chunkTexGenerator;
        ChunkMeshGenerator _chunkMeshGenerator;

        Dictionary<Vector3Int, GameObject> goMgr;

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

            goMgr = new Dictionary<Vector3Int, GameObject>();
            _octree = new Octree(_terrainConfig);
        }

        void Update()
        {
            var visibleLeafNode = _octree.GetVisibleLeafNode(_camera);
            foreach (var node in visibleLeafNode)
            {


                    if (goMgr.TryGetValue(node.CenterCoord, out var go))
                    {
                    }
                    else
                    {
                        var chunkTex = _chunkTexGenerator.GenerateChunkTex(
                            node.CenterCoord, node.LevelOfDetail);

                        var mesh = _chunkMeshGenerator.GenerateChunkMesh(
                            chunkTex, node.CenterCoord, node.LevelOfDetail, node.LodMask);

                        chunkTex.Release();
                        chunkTex = null;

                        if (mesh != null)
                        {

                            var newGo = new GameObject("chunk");
                            newGo.AddComponent<MeshFilter>().sharedMesh = mesh;
                            newGo.AddComponent<MeshRenderer>();
                            newGo.transform.position = (Vector3)node.CenterCoord * 1.0f;

                            goMgr[node.CenterCoord] = newGo;
                        }
                    }

            }
        }

    }
}