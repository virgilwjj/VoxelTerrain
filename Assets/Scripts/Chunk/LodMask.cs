using System;

namespace VoxelTerrain
{
    [Flags, Serializable]
    public enum LodMask
    {
        None = 0,
        NegativeX = 1 << 0,
        PositiveX = 1 << 1,
        NegativeY = 1 << 2,
        PositiveY = 1 << 3,
        NegativeZ = 1 << 4,
        PositiveZ = 1 << 5,
    }

}
