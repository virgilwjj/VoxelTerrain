using UnityEngine;

namespace VoxelTerrain
{
    public class ChunkTexEditor : MonoBehaviour
    {
        [HideInInspector]
        public TerrainConfig TerrainConfig;

        [SerializeField]
        ComputeShader _chunkTexEditor;

        public void AddEdit(RenderTexture chunkTex,
            Vector3Int coordinate, int levelOfDetail,
            Texture blushTex, int numVoxelsPerAxisForBlush,
            Vector3 hitPoint, float delta)
        {
            var numVoxelsPerAxis
                = TerrainConfig.NumVoxelsPerAxisForChunk;
            var numPointsPerAxis
                = TerrainConfig.NumPointsPerAxisForChunk;
            var voxelSizePerAxis = TerrainConfig.VoxelSizePerAxis;
            var numPointsPerAxisForBlush
                = numVoxelsPerAxisForBlush + 1;

            Vector3Int hitCoord = Vector3Int.zero;
            hitCoord.x = Mathf.FloorToInt(hitPoint.x
                / voxelSizePerAxis);
            hitCoord.y = Mathf.FloorToInt(hitPoint.y
                / voxelSizePerAxis);
            hitCoord.z = Mathf.FloorToInt(hitPoint.z
                / voxelSizePerAxis);
            
            Vector3Int minCoord = Vector3Int.zero;
            minCoord.x = hitCoord.x 
                - (numVoxelsPerAxisForBlush >> 1);
            minCoord.y = hitCoord.y
                - (numVoxelsPerAxisForBlush >> 1);
            minCoord.z = hitCoord.z
                - (numVoxelsPerAxisForBlush >> 1);

            _chunkTexEditor.SetTexture(0, "chunkTex",
                chunkTex);
            _chunkTexEditor.SetInt("numVoxelsPerAxis",
                numVoxelsPerAxis);
            _chunkTexEditor.SetInt("numPointsPerAxis",
                numPointsPerAxis);
            _chunkTexEditor.SetFloat("voxelSizePerAxis",
                voxelSizePerAxis);
            _chunkTexEditor.SetVectorInt("coordinate",
                coordinate);
            _chunkTexEditor.SetInt("levelOfDetail",
                levelOfDetail);
            _chunkTexEditor.SetTexture(0, "blushTex",
                blushTex); 
            _chunkTexEditor.SetInt("numVoxelsPerAxisForBlush",
                numVoxelsPerAxisForBlush);
            _chunkTexEditor.SetInt("numPointsPerAxisForBlush",
                numPointsPerAxisForBlush);
            _chunkTexEditor.SetVectorInt("minCoord", minCoord);
            _chunkTexEditor.SetFloat("delta", delta);
            
            _chunkTexEditor.DispatchThreads(0,
                numPointsPerAxisForBlush, numPointsPerAxisForBlush,
                numPointsPerAxisForBlush);
        }
    }

}
 