using UnityEngine;

namespace VoxelTerrain
{
    [CreateAssetMenu(fileName = "BrushSetting",
        menuName = "Brush Setting")]
    public class BrushSetting : ScriptableObject
    {
        [SerializeField]
        Texture3D _sphereBrushTex2;
        [SerializeField]
        Texture3D _sphereBrushTex3;
        [SerializeField]
        Texture3D _sphereBrushTex4;
        [SerializeField]
        Texture3D _sphereBrushTex5;
        [SerializeField]
        Texture3D _sphereBrushTex6;
        [SerializeField]
        Texture3D _sphereBrushTex7;
        [SerializeField]
        Texture3D _sphereBrushTex8;
        [SerializeField]
        Texture3D _sphereBrushTex9;
        [SerializeField]
        Texture3D _sphereBrushTex10;
        [SerializeField]
        Texture3D _sphereBrushTex11;
        [SerializeField]
        Texture3D _sphereBrushTex12;
        [SerializeField]
        Texture3D _sphereBrushTex13;
        [SerializeField]
        Texture3D _sphereBrushTex14;
        Texture3D[] _sphereBrushTexs;
        [SerializeField]
        Texture3D _cubeBrushTex2;
        [SerializeField]
        Texture3D _cubeBrushTex3;
        [SerializeField]
        Texture3D _cubeBrushTex4;
        [SerializeField]
        Texture3D _cubeBrushTex5;
        [SerializeField]
        Texture3D _cubeBrushTex6;
        [SerializeField]
        Texture3D _cubeBrushTex7;
        [SerializeField]
        Texture3D _cubeBrushTex8;
        [SerializeField]
        Texture3D _cubeBrushTex9;
        [SerializeField]
        Texture3D _cubeBrushTex10;
        [SerializeField]
        Texture3D _cubeBrushTex11;
        [SerializeField]
        Texture3D _cubeBrushTex12;
        [SerializeField]
        Texture3D _cubeBrushTex13;
        [SerializeField]
        Texture3D _cubeBrushTex14;
        Texture3D[] _cubeBrushTexs;

        public Texture3D[] SphereBrushTexs => _sphereBrushTexs;
        public Texture3D[] CubeBrushTexs => _cubeBrushTexs;

        void OnValidate()
        {
            _sphereBrushTexs = new Texture3D[15];           
            _sphereBrushTexs[2] = _sphereBrushTex2;
            _sphereBrushTexs[3] = _sphereBrushTex3;
            _sphereBrushTexs[4] = _sphereBrushTex4;
            _sphereBrushTexs[5] = _sphereBrushTex5;
            _sphereBrushTexs[6] = _sphereBrushTex6;
            _sphereBrushTexs[7] = _sphereBrushTex7;
            _sphereBrushTexs[8] = _sphereBrushTex8;
            _sphereBrushTexs[9] = _sphereBrushTex9;
            _sphereBrushTexs[10] = _sphereBrushTex10;
            _sphereBrushTexs[11] = _sphereBrushTex11;
            _sphereBrushTexs[12] = _sphereBrushTex12;
            _sphereBrushTexs[13] = _sphereBrushTex13;
            _sphereBrushTexs[14] = _sphereBrushTex14;

            _cubeBrushTexs = new Texture3D[15];           
            _cubeBrushTexs[2] = _cubeBrushTex2;
            _cubeBrushTexs[3] = _cubeBrushTex3;
            _cubeBrushTexs[4] = _cubeBrushTex4;
            _cubeBrushTexs[5] = _cubeBrushTex5;
            _cubeBrushTexs[6] = _cubeBrushTex6;
            _cubeBrushTexs[7] = _cubeBrushTex7;
            _cubeBrushTexs[8] = _cubeBrushTex8;
            _cubeBrushTexs[9] = _cubeBrushTex9;
            _cubeBrushTexs[10] = _cubeBrushTex10;
            _cubeBrushTexs[11] = _cubeBrushTex11;
            _cubeBrushTexs[12] = _cubeBrushTex12;
            _cubeBrushTexs[13] = _cubeBrushTex13;
            _cubeBrushTexs[14] = _cubeBrushTex14;
        }
 
    }

}
