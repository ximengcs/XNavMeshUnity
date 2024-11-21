using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XFrame.PathFinding
{
    public class DataUtility
    {
        public static void Save(string name, HalfEdgeData data)
        {
            byte[] bytes = data.ToBytes();
            File.WriteAllBytes($"Assets/Data/{name}-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.bytes", bytes);
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            Debug.Log($"save success, size {bytes.Length}");
        }
    }
}
