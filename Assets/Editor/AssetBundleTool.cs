using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace VoxelTerrain
{
    public class AssetBundleTool
    {
	    [MenuItem("Tools/AutoSet AssetBundleName")]
	    public static void AutoSetBundleName()
        {
		    string path = Path.Combine(Application.dataPath,
                "BlushTexs");
		    DirectoryInfo dir_info = new DirectoryInfo(path);
		    DirectoryInfo[] dir_arr = dir_info.GetDirectories();

                Debug.Log("wjj2");
		    for (int i = 0; i < dir_arr.Length; i++)
            {
                Debug.Log("wjj");
			    DirectoryInfo current_dir = dir_arr[i];

			    string dir_name = current_dir.Name;
			    string assetbundle_name = string.Format("auto_set_{0}", dir_name.ToLower());

			    string dir_path = current_dir.FullName;
			    string asset_path = dir_path.Replace(Application.dataPath, "Assets");
			    AssetImporter ai = AssetImporter.GetAtPath(asset_path);

			    ai.assetBundleName = assetbundle_name;
			    ai.assetBundleVariant = "variant";
		    }
	    }
    }
}
