using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VoxelTerrain
{
    public class TexLoader : MonoBehaviour
    {
        public void SaveTex3D(Texture3D tex3D, string assetName)
        {
            AssetDatabase.CreateAsset(tex3D, "Assets/" + assetName
                + ".asset");
        }

        public Texture3D LoadTex3D(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath("Assets/" + assetName
                + ".asset", typeof(Texture3D)) as Texture3D;
        }

    }

}
