using System;

namespace VoxelTerrain
{
    [Flags, Serializable]
    public enum OctreeMask
    {
        PositiveX = 1 << 0,
        PositiveZ = 1 << 1,
        PositiveY = 1 << 2,
    }

}
