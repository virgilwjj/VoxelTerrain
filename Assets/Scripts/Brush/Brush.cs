using UnityEngine;

namespace VoxelTerrain
{
    public class Brush
    {
        Texture3D _brushTex;
        readonly int _numVoxelsPerAxis;
        readonly BrushType _brushType;

        public Texture3D BrushTex => _brushTex;
        public int NumVoxelsPerAxis => _numVoxelsPerAxis;
        public BrushType BrushType => _brushType;

        public Brush(Texture3D brushTex, int numVoxelsPerAxis, BrushType brushType)
        {
            _brushTex = brushTex;
            _numVoxelsPerAxis = numVoxelsPerAxis;
            _brushType = brushType;
        }

    }

}
