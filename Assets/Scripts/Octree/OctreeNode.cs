using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelTerrain
{
    public class OctreeNode
    {
        public Vector3Int CenterCoord;
        public int NumVoxelsPerAxis;
        public int LevelOfDetail;
        public OctreeNode Parent; 
        public OctreeNode[] Children;
        public bool IsMerge = false;
        public OctreeMask OctreeMask;
        public LodMask LodMask = LodMask.None;

        public OctreeNode(Vector3Int centerCoord,
            int numVoxelsPerAxis, int levelOfDetail,
            OctreeNode parent, OctreeMask octreeMask)
        {
            CenterCoord = centerCoord;
            NumVoxelsPerAxis = numVoxelsPerAxis;
            LevelOfDetail = levelOfDetail;
            Parent = parent;
            OctreeMask = octreeMask;
        }

        public void Subdivide()
        {
            if (LevelOfDetail == 0)
            {
                return;
            }

            IsMerge = false;
            if (Children != null)
            {
                return;
            }

            Children = new OctreeNode[8];
            for (var i = 0; i < 8; ++i)
            {
                var centerCoord = CenterCoord;
                var numVoxelsPerAxis = NumVoxelsPerAxis
                    >> 1;

                var octreeMask = (OctreeMask)i;
                if (octreeMask.HasFlag(
                    OctreeMask.PositiveX))
                {
                    centerCoord.x
                        += (numVoxelsPerAxis >> 1);
                }
                else
                {
                    centerCoord.x
                        -= (numVoxelsPerAxis >> 1);
                }

                if (octreeMask.HasFlag(
                    OctreeMask.PositiveY))
                {
                    centerCoord.y
                        += (numVoxelsPerAxis >> 1);
                }
                else
                {
                    centerCoord.y
                        -= (numVoxelsPerAxis >> 1);
                }

                if (octreeMask.HasFlag(
                    OctreeMask.PositiveZ))
                {
                    centerCoord.z
                        += (numVoxelsPerAxis >> 1);
                }
                else
                {
                    centerCoord.z
                        -= (numVoxelsPerAxis >> 1);
                }

                var levelOfDetail = LevelOfDetail - 1;
                Children[i] = new OctreeNode(centerCoord,
                    numVoxelsPerAxis, levelOfDetail, this,
                    octreeMask);

            }
        }

        public void Merge()
        {
            IsMerge = true;
        }

    }

}
