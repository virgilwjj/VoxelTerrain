using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelTerrain
{
    [CreateAssetMenu(fileName = "TerrainConfig",
        menuName = "Scriptable Objects/Terrain Config")]
    public class TerrainConfig : ScriptableObject
    {
        [SerializeField, Range(2, 10)]
        int _powerVoxelsPerAxisForChunk;
        [SerializeField, Range(0, 10)]
        int _levelOfDetail;
        [SerializeField, Range(1, 10)]
        float _voxelSizePerAxis;
        [SerializeField, Range(0, 1)]
        float _isoLevel = 0.5f;

        public int NumVoxelsPerAxisForChunk
            => 1 << _powerVoxelsPerAxisForChunk;
        public int NumPointsPerAxisForChunk
            => NumVoxelsPerAxisForChunk + 1;
        public int LevelOfDetail => _levelOfDetail;
        public int NumVoxelsPerAxisForTerrain
            => 1 << (_powerVoxelsPerAxisForChunk + _levelOfDetail);
        public int NumPointsPerAxisForTerrain
            => NumVoxelsPerAxisForTerrain + 1;
        public float VoxelSizePerAxis => _voxelSizePerAxis;
        public float IsoLevel => _isoLevel;

    }

}
