using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelTerrain
{
    public class Octree
    {
        TerrainConfig _terrainConfig;
        
        OctreeNode _root;

        public Octree(TerrainConfig terrainConfig)
        {
            _terrainConfig = terrainConfig;

            var numVoxelsPerAxis
                = _terrainConfig.NumVoxelsPerAxisForTerrain;
            var levelOfDetail
                = _terrainConfig.LevelOfDetail;

            _root = new OctreeNode(Vector3Int.zero,
                numVoxelsPerAxis, levelOfDetail, null, 0);
        } 

        public List<OctreeNode> GetVisibleLeafNode(Camera camera)
        {
            var visibleLeafNode = new List<OctreeNode>();

            var frustumPlanes
                = GeometryUtility.CalculateFrustumPlanes(
                    camera);
            var camPos = camera.transform.position;
            var voxelSizePerAxis
                = _terrainConfig.VoxelSizePerAxis;
            var lodThresholds
                = _terrainConfig.LodThresholds;

            Queue<OctreeNode> queue
                = new Queue<OctreeNode>();
            queue.Enqueue(_root);

            while (queue.Count != 0)
            {
                var node = queue.Dequeue();

                Vector3 center = (Vector3)node.CenterCoord
                    * voxelSizePerAxis;
                Vector3 size = Vector3.one
                    * node.NumVoxelsPerAxis
                    * voxelSizePerAxis;
                var bounds = new Bounds(center, size);
                if(!GeometryUtility.TestPlanesAABB(
                    frustumPlanes, bounds))
                {
                    node.Merge();
                    continue;
                }

                var levelOfDetail = node.LevelOfDetail; 
                var lodThreshold = lodThresholds[
                    levelOfDetail];
                var distance = Vector3.Distance(camPos,
                    bounds.center);

                if (distance >= lodThreshold)
                {
                    node.Merge();
                    visibleLeafNode.Add(node);
                    continue;
                }

                node.Subdivide();

                var children = node.Children;
                if (children == null)
                {
                    visibleLeafNode.Add(node);
                    continue;
                }

                for (var i = 0; i < 8; ++i)
                {
                    queue.Enqueue(children[i]);
                }
            }

            return visibleLeafNode;
        }

    }

}
