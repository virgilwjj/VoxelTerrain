using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelTerrain
{
    [Flags, Serializable]
    public enum OctreeMask
    {
        PositiveX = 1 << 0,
        PositiveY = 1 << 1,
        PositiveZ = 1 << 2,
    }

}
