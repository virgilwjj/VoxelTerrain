using UnityEngine;

namespace VoxelTerrain
{
    [CreateAssetMenu(fileName = "ChunkSetting",
        menuName = "Chunk Setting")]
    public class ChunkSetting : ScriptableObject
    {
        [SerializeField, Range(2, 9)]
        int _powerVoxelsPerAxis = 5;
        [SerializeField, Range(0, 10)]
        float _voxelSizePerAxis = 1.0f;
        [SerializeField, Range(0, 1)]
        float _isoLevel = 0.0f;
        [SerializeField, Min(1)]
        float _thickness = 16.0f;

        public int NumVoxelsPerAxis
            => 1 << _powerVoxelsPerAxis;
        public float VoxelSizePerAxis => _voxelSizePerAxis;
        public float IsoLevel => _isoLevel;
        public float Thickness => _thickness;
    }

}
