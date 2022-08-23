using UnityEngine;

namespace VoxelTerrain
{
    public struct EditInfo
    {
        public BrushType BrushType;
        public int NumVoxelsPerAxis;

        public Vector3 EditPoint;
        public float Weight;
        public float Delta;
    }

}
