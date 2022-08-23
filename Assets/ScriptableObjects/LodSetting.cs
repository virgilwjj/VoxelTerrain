using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelTerrain
{
    [CreateAssetMenu(fileName = "LodSetting",
        menuName = "Lod Setting")]
    public class LodSetting : ScriptableObject
    {
        [SerializeField, Range(0, 10)]
        int _levelOfDetail;
        [SerializeField]
        float[] _lodThresholds;
        [SerializeField]
        Material[] _lodMaterials;

        public int LevelOfDetail => _levelOfDetail;
        public float[] LodThresholds => _lodThresholds;
        public Material[] LodMaterials => _lodMaterials;
    }

}
