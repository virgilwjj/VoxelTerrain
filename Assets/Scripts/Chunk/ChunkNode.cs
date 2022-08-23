using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelTerrain
{
    public class ChunkNode
    {
        public readonly Vector3Int CenterCoord;
        public readonly int NumVoxelsPerAxis;
        public readonly int LevelOfDetail;
        public bool IsRender = false;
        public LodMask LodMask = LodMask.None;
        public int TimeToLive = 10;
        public ChunkNode[] Children = null;

        public ChunkNode(Vector3Int centerCoord,
            int numVoxelsPerAxis, int levelOfDetail)
        {
            CenterCoord = centerCoord;
            NumVoxelsPerAxis = numVoxelsPerAxis;
            LevelOfDetail = levelOfDetail;
        }

    }

}
