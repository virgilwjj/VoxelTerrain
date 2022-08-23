using System.Security.Cryptography;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace VoxelTerrain
{
    public class CreateNoiseChunks : MonoBehaviour
    {
        [SerializeField]
        ChunkSetting _chunkSetting;
        [SerializeField]
        LodSetting _lodSetting;
        DensityFieldBuilder _densityFieldBuilder;

        void Awake()
        {
            _densityFieldBuilder = GetComponent<DensityFieldBuilder>();
            StartCoroutine(GetAllNodes());
        }

        IEnumerator GetAllNodes()
        {
            var terrain = new Terrain();

            var queue = new Queue<Node>();
            var root = new Node();
            root.centerCoord = Vector3Int.zero;
            root.levelOfDetail = 5;
            root.numVoxelsPerAxis = _chunkSetting.NumVoxelsPerAxis << root.levelOfDetail;

            queue.Enqueue(root);
            int i = 0;
            while (queue.Count != 0)
            {
                ++i;
                Debug.Log(i);
                var node = queue.Dequeue();
                var centerCoord = node.centerCoord;
                var levelOfDetail = node.levelOfDetail;

                var densityField = _densityFieldBuilder.BuildNoiseDensityField(centerCoord, levelOfDetail);

                _densityFieldBuilder.SaveDensityField("Outputs/Noise", densityField, node.centerCoord);

                string fieldName = "density_" + centerCoord.x + "_" + centerCoord.y + "_" + centerCoord.z + ".asset";
                Debug.Log(fieldName);
                yield return null;

                if (levelOfDetail == 0)
                {
                    continue;
                }

                var numVoxelsPerAxis = node.numVoxelsPerAxis;
                numVoxelsPerAxis = numVoxelsPerAxis >> 1;
                var halfNumVoxelsPerAxis = numVoxelsPerAxis >> 1;
                --levelOfDetail;

                {
                    var childCenterCoord = centerCoord;
                    childCenterCoord.x -= halfNumVoxelsPerAxis;
                    childCenterCoord.y -= halfNumVoxelsPerAxis;
                    childCenterCoord.z -= halfNumVoxelsPerAxis;
                    var child = new Node();
                    child.centerCoord = childCenterCoord;
                    child.levelOfDetail = levelOfDetail;
                    child.numVoxelsPerAxis = numVoxelsPerAxis;
                    queue.Enqueue(child);
                } 

                {
                    var childCenterCoord = centerCoord;
                    childCenterCoord.x += halfNumVoxelsPerAxis;
                    childCenterCoord.y -= halfNumVoxelsPerAxis;
                    childCenterCoord.z -= halfNumVoxelsPerAxis;
                    var child = new Node();
                    child.centerCoord = childCenterCoord;
                    child.levelOfDetail = levelOfDetail;
                    child.numVoxelsPerAxis = numVoxelsPerAxis;
                    queue.Enqueue(child);
                } 

                {
                    var childCenterCoord = centerCoord;
                    childCenterCoord.x -= halfNumVoxelsPerAxis;
                    childCenterCoord.y += halfNumVoxelsPerAxis;
                    childCenterCoord.z -= halfNumVoxelsPerAxis;
                    var child = new Node();
                    child.centerCoord = childCenterCoord;
                    child.levelOfDetail = levelOfDetail;
                    child.numVoxelsPerAxis = numVoxelsPerAxis;
                    queue.Enqueue(child);
                } 

                {
                    var childCenterCoord = centerCoord;
                    childCenterCoord.x += halfNumVoxelsPerAxis;
                    childCenterCoord.y += halfNumVoxelsPerAxis;
                    childCenterCoord.z -= halfNumVoxelsPerAxis;
                    var child = new Node();
                    child.centerCoord = childCenterCoord;
                    child.levelOfDetail = levelOfDetail;
                    child.numVoxelsPerAxis = numVoxelsPerAxis;
                    queue.Enqueue(child);
                } 

                {
                    var childCenterCoord = centerCoord;
                    childCenterCoord.x -= halfNumVoxelsPerAxis;
                    childCenterCoord.y -= halfNumVoxelsPerAxis;
                    childCenterCoord.z += halfNumVoxelsPerAxis;
                    var child = new Node();
                    child.centerCoord = childCenterCoord;
                    child.levelOfDetail = levelOfDetail;
                    child.numVoxelsPerAxis = numVoxelsPerAxis;
                    queue.Enqueue(child);
                } 

                {
                    var childCenterCoord = centerCoord;
                    childCenterCoord.x += halfNumVoxelsPerAxis;
                    childCenterCoord.y -= halfNumVoxelsPerAxis;
                    childCenterCoord.z += halfNumVoxelsPerAxis;
                    var child = new Node();
                    child.centerCoord = childCenterCoord;
                    child.levelOfDetail = levelOfDetail;
                    child.numVoxelsPerAxis = numVoxelsPerAxis;
                    queue.Enqueue(child);
                } 

                {
                    var childCenterCoord = centerCoord;
                    childCenterCoord.x -= halfNumVoxelsPerAxis;
                    childCenterCoord.y += halfNumVoxelsPerAxis;
                    childCenterCoord.z += halfNumVoxelsPerAxis;
                    var child = new Node();
                    child.centerCoord = childCenterCoord;
                    child.levelOfDetail = levelOfDetail;
                    child.numVoxelsPerAxis = numVoxelsPerAxis;
                    queue.Enqueue(child);
                } 

                {
                    var childCenterCoord = centerCoord;
                    childCenterCoord.x += halfNumVoxelsPerAxis;
                    childCenterCoord.y += halfNumVoxelsPerAxis;
                    childCenterCoord.z += halfNumVoxelsPerAxis;
                    var child = new Node();
                    child.centerCoord = childCenterCoord;
                    child.levelOfDetail = levelOfDetail;
                    child.numVoxelsPerAxis = numVoxelsPerAxis;
                    queue.Enqueue(child);
                } 
            }

            /*
            string jsonStr = JsonConvert.SerializeObject(terrain, Formatting.Indented);
            
            byte[] byteArray = System.Text.Encoding.Default.GetBytes(jsonStr);
            File.WriteAllBytes("Chunks/ChunksInfo.json", byteArray);
            */
        }

        class Terrain
        {
            public string[] notEmptyChunks;
        }

        class Node
        {
            public Vector3Int centerCoord;
            public int levelOfDetail;
            public int numVoxelsPerAxis;
        }

    }

}
