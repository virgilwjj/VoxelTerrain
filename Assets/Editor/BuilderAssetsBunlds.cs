using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class BuilderAssetsBunlds
{
    //打包、、、、、、、、、、、、、、、、、、、、、、
    [MenuItem("Tools/打包")]
    public static void BundlerAssets()
    {
       // Debug.LogError("打包Assets");
        BuildPipeline.BuildAssetBundles(GetOutAssetsDirecotion(), BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
    }

    public static string GetOutAssetsDirecotion()
    {
        string assetBundleDirectory = Application.streamingAssetsPath;
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }
        return assetBundleDirectory;
    }

    //  设置资源名称/////////////////////////////

    [MenuItem("Tools/设置Assetbundle名字")]
    public static void SetAssetBundellabls()
    {
        CheckFileSystemInfo();
    }

    public static void CheckFileSystemInfo()  //检查目标目录下的文件系统
    {
        AssetDatabase.RemoveUnusedAssetBundleNames(); //移除没有用的assetbundlename
        Object obj = Selection.activeObject;    //选中的物体
        string path = AssetDatabase.GetAssetPath(obj);//选中的文件夹
        CoutineCheck(path);
    }

    public static void CheckFileOrDirectory(FileSystemInfo fileSystemInfo, string path) //判断是文件还是文件夹
    {
        FileInfo fileInfo = fileSystemInfo as FileInfo;
        if (fileInfo != null)
        {
            SetBundleName(path);
        }
        else
        {
            CoutineCheck(path);
        }
    }

    public static void CoutineCheck(string path)   //是文件，继续向下
    {
        DirectoryInfo directory = new DirectoryInfo(@path);
        FileSystemInfo[] fileSystemInfos = directory.GetFileSystemInfos();
    
        foreach (var item in fileSystemInfos)
        {
           // Debug.Log(item);
            int idx = item.ToString().LastIndexOf(@"\");
            string name = item.ToString().Substring(idx + 1);

            if (!name.Contains(".meta"))
            {
                CheckFileOrDirectory(item, path + "/" + name);  //item  文件系统，加相对路径
            }
        }
    }

    public static void SetBundleName(string path)  //设置assetbundle名字
    {
        var importer = AssetImporter.GetAtPath(path);
        string[] strs = path.Split('.');
        string[] dictors = strs[0].Split('/');
        string name = "";
        for (int i = 1; i < dictors.Length; i++)
        {
            if (i < dictors.Length - 1)
            {
                name += dictors[i] + "/";
            }
            else
            {
                name += dictors[i];
            }
        }
        if (importer != null)
        {
            importer.assetBundleVariant = "bytes";
            importer.assetBundleName = name;
        }
        else
            Debug.Log("importer是空的");
    }

}
