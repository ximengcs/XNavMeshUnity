using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using XFrame.PathFinding;

public class XNavMeshTools : EditorWindow
{
    private GameObject m_Root;
    private Transform m_Areas;
    private Transform m_Rect;
    private TextAsset m_File;

    private XNavmeshEditData m_Current;

    private string m_FilePath = "Assets/Scripts/XNavMesh/Editor/Data";
    private string m_FileExtension = "xnavmesh";
    private string m_CurrentPath;

    private void OnGUI()
    {
        if (GUILayout.Button("New"))
        {
            m_Current = new XNavmeshEditData();
            m_Current.Name = $"navmesh_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            m_CurrentPath = EditorUtility.SaveFilePanel("new navmesh file", m_FilePath, m_Current.Name, m_FileExtension);
            InnerNew();
        }
        if (GUILayout.Button("Open"))
        {
            m_CurrentPath = EditorUtility.OpenFilePanel("open navmesh file", m_FilePath, m_FileExtension);
            InnerOpenCurrent();
        }
        if (GUILayout.Button("Save"))
        {
            InnerSaveCurrent();
        }

        if (m_Current != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Edit Mode");
            m_Current.EditMode = (EditMode)EditorGUILayout.EnumPopup(m_Current.EditMode);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Add Area"))
            {
                InnerAddArea("area", new Vector3[]
                {
                    new Vector3(1, -1),
                    new Vector3(-1, -1),
                    new Vector3(0, 1),
                });
            }

            if (GUILayout.Button("Generate navmesh"))
            {
                InnerGenerateNavmesh();
            }
        }
    }

    private void OnDestroy()
    {
        GameObject.DestroyImmediate(m_Root);
    }

    private void InnerGenerateNavmesh()
    {
        XNavMesh navmesh = new XNavMesh(new AABB(m_Current.MinX, m_Current.MaxX, m_Current.MinY, m_Current.MaxY));
        //Test2.Normalizer = navmesh.Normalizer;
        foreach (var entry in m_Current.Areas)
        {
            List<XVector2> points = new List<XVector2>();
            foreach (var item in entry.Value)
            {
                XVector2 p = new XVector2(item.Item1, item.Item2);
                points.Add(p);
            }

            switch (m_Current.EditMode)
            {
                case EditMode.Obstacle:
                    navmesh.AddWithExtraData(points, AreaType.Obstacle, out HalfEdgeData _, out List<Edge> _);
                    break;

                case EditMode.Walk:
                    break;
            }
        }

        byte[] bytes = DataUtility.ToBytes(navmesh);
        File.WriteAllBytes($"Assets/Data/Navmesh/{m_Current.Name}.bytes", bytes);
        AssetDatabase.Refresh();
        Debug.Log($"save success, {navmesh.Data.Faces.Count} size {bytes.Length}");
    }

    private void InnerNew()
    {
        m_Root = new GameObject(m_Current.Name);
        GameObject areaRoot = new GameObject("Areas");
        m_Areas = areaRoot.transform;
        m_Areas.SetParent(m_Root.transform);

        GameObject rectRoot = new GameObject("Rect");
        m_Rect = rectRoot.transform;
        GameObject minInst = new GameObject("Min");
        GameObject maxInst = new GameObject("Max");
        m_Rect.SetParent(m_Root.transform);
        minInst.transform.SetParent(m_Rect);
        maxInst.transform.SetParent(m_Rect);

        LineRenderer line = rectRoot.AddComponent<LineRenderer>();
        line.loop = true;
        line.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
        line.positionCount = 4;
        line.startWidth = 0.5f;
        line.endWidth = 0.5f;
        line.startColor = Color.cyan;
        line.endColor = Color.cyan;
    }

    private void Update()
    {
        InnerUpdateRect();
    }

    private void InnerUpdateRect()
    {
        if (m_Rect)
        {
            Vector3 min = m_Rect.GetChild(0).position;
            Vector3 max = m_Rect.GetChild(1).position;
            LineRenderer line = m_Rect.GetComponent<LineRenderer>();
            line.SetPosition(0, min);
            line.SetPosition(1, new Vector3(max.x, min.y));
            line.SetPosition(2, max);
            line.SetPosition(3, new Vector3(min.x, max.y));
        }
    }

    private void InnerAddArea(string name, Vector3[] points)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(m_Areas);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.loop = true;
        line.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
        line.positionCount = points.Length;
        line.SetPositions(points);
        line.startWidth = 0.2f;
        line.endWidth = 0.2f;

        switch (m_Current.EditMode)
        {
            case EditMode.Obstacle:
                line.startColor = Color.red;
                line.endColor = Color.red;
                break;

            case EditMode.Walk:
                line.startColor = Color.green;
                line.endColor = Color.green;
                break;
        }

        root.AddComponent<RVOArea>();
    }

    private void InnerSaveCurrent()
    {
        Dictionary<string, List<(float, float)>> result = new();
        RVOArea[] areas = m_Areas.GetComponentsInChildren<RVOArea>(false);
        foreach (RVOArea area in areas)
        {
            List<Vector2> points = area.GetUnityVertices();
            List<(float, float)> values = new List<(float, float)>();
            foreach (Vector2 point in points)
                values.Add((point.x, point.y));
            result.Add(area.name, values);
        }
        m_Current.Areas = result;

        Vector3 min = m_Rect.GetChild(0).position;
        Vector3 max = m_Rect.GetChild(1).position;
        m_Current.MinX = min.x;
        m_Current.MaxX = max.x;
        m_Current.MinY = min.y;
        m_Current.MaxY = max.y;

        File.WriteAllText(m_CurrentPath, JsonConvert.SerializeObject(m_Current));
        AssetDatabase.Refresh();
    }

    private void InnerOpenCurrent()
    {
        string json = File.ReadAllText(m_CurrentPath);
        m_Current = JsonConvert.DeserializeObject<XNavmeshEditData>(json);

        InnerNew();
        foreach (var entry in m_Current.Areas)
        {
            Vector3[] points = new Vector3[entry.Value.Count];
            for (int i = 0; i < entry.Value.Count; i++)
                points[i] = new Vector3(entry.Value[i].Item1, entry.Value[i].Item2);
            InnerAddArea(entry.Key, points);
        }
        m_Rect.GetChild(0).position = new Vector3(m_Current.MinX, m_Current.MinY);
        m_Rect.GetChild(1).position = new Vector3(m_Current.MaxX, m_Current.MaxY);
    }

    private void InnerGenerateBorder()
    {
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (Transform tf in m_Areas)
        {
            RVOArea ob = tf.GetComponent<RVOArea>();
            List<Vector2> points = ob.GetUnityVertices();
            foreach (Vector2 p in points)
            {
                if (p.x < min.x) min.x = p.x;
                if (p.y < min.y) min.y = p.y;
                if (p.x > max.x) max.x = p.x;
                if (p.y > max.y) max.y = p.y;
            }
        }
    }

    [MenuItem("Tools/XNavmesh")]
    public static void Define()
    {
        GetWindow<XNavMeshTools>().Show();
    }
}
