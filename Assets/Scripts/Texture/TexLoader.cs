using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxelTerrain
{
    public class TexLoader : MonoBehaviour
    {
        public void SaveTex3D(Texture3D tex3D, string dir,
            string assetName, string variant,
            string assetBundleName, string assetBundleVariant)
        {
#if UNITY_EDITOR
            string path = "Assets/" + dir + assetName + "." + variant;
            AssetDatabase.CreateAsset(tex3D, path);
            var assetImporter = AssetImporter.GetAtPath(path);
            assetImporter.assetBundleName = assetBundleName;
            assetImporter.assetBundleVariant = assetBundleVariant;
#endif
        }

        public Texture3D LoadTex3D(string dir, string assetName,
            string variant)
        {
#if UNITY_EDITOR
            string path = "Assets/" + dir + assetName + "." + variant;
            return AssetDatabase.LoadAssetAtPath(path,
                typeof(Texture3D)) as Texture3D;
#else
            return null;
#endif
        }

    }

}
