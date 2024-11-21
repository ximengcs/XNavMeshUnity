using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using XFrame.PathFinding;
using static Test;

public struct CurrentNavmesh
{
    public XNavMesh Navmesh;
    public MeshArea MeshArea;
    public XNavMeshRenderer Renderer;
}

public class XNavMeshTools : EditorWindow
{
    private GameObject m_Root;
    private Transform m_Areas;
    private Transform m_Rect;

    private List<GameObject> m_AreasInst;
    private CurrentNavmesh m_CurrentNavmesh;

    private bool m_ExitDestroy;
    private XNavmeshEditData m_Current;
    private Vector2 m_AreaListPos;

    private string m_FilePath = "Assets/Scripts/XNavMesh/Editor/Data";
    private string m_FileExtension = "xnavmesh";
    private string m_CurrentPath;

    private void OnEnable()
    {
        m_ExitDestroy = true;
        m_AreasInst = new List<GameObject>();
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.textField);
        if (GUILayout.Button("New"))
        {
            string name = $"navmesh_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            string path = EditorUtility.SaveFilePanel("new navmesh file", m_FilePath, name, m_FileExtension);
            if (!string.IsNullOrEmpty(path))
            {
                InnerClear();
                InnerNew();
                m_Current = new XNavmeshEditData();
                m_CurrentPath = path;
                m_Current.Name = name;
                m_Current.Name = Path.GetFileNameWithoutExtension(m_CurrentPath);
            }
        }
        if (GUILayout.Button("Open"))
        {
            string path = EditorUtility.OpenFilePanel("open navmesh file", m_FilePath, m_FileExtension);
            if (!string.IsNullOrEmpty(path))
            {
                InnerClear();
                m_CurrentPath = path;
                InnerOpenCurrent();
                m_Current.Name = Path.GetFileNameWithoutExtension(m_CurrentPath);
            }
        }
        if (GUILayout.Button("Save"))
        {
            InnerSaveCurrent();
        }
        if (GUILayout.Button("Clear"))
        {
            InnerClear();
        }
        EditorGUILayout.EndHorizontal();

        if (m_Current != null)
        {
            EditorGUILayout.BeginVertical(GUI.skin.window);

            EditorGUILayout.BeginHorizontal();
            m_Current.Name = EditorGUILayout.TextField(m_Current.Name);
            if (GUILayout.Button("OK", GUILayout.Width(50)))
            {
                InnerChangeFileName();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical(GUI.skin.textField);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Edit Mode");
            m_Current.EditMode = (EditMode)EditorGUILayout.EnumPopup(m_Current.EditMode);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Exit Delete");
            m_ExitDestroy = EditorGUILayout.Toggle(m_ExitDestroy);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Add Area"))
            {
                InnerAddArea($"Area{m_AreasInst.Count + 1}", new Vector3[]
                {
                    new Vector3(1, -1),
                    new Vector3(-1, -1),
                    new Vector3(0, 1),
                });
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Navmesh"))
            {
                InnerGenerateNavmesh();
            }
            if (GUILayout.Button("Clear"))
            {
                m_CurrentNavmesh.Renderer.Destroy();
                m_CurrentNavmesh = default;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal(GUI.skin.textField);
            if (GUILayout.Button("Min"))
            {
                Selection.activeGameObject = m_Rect.GetChild(0).gameObject;
            }
            if (GUILayout.Button("Max"))
            {
                Selection.activeGameObject = m_Rect.GetChild(1).gameObject;
            }
            EditorGUILayout.EndHorizontal();

            m_AreaListPos = EditorGUILayout.BeginScrollView(m_AreaListPos, GUI.skin.textField);
            for (int i = 0; i < m_AreasInst.Count; i++)
            {
                GameObject obj = m_AreasInst[i];
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(obj.name))
                {
                    Selection.activeGameObject = obj;
                }
                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    GameObject.DestroyImmediate(obj);
                    m_AreasInst.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }
    }

    private void OnDestroy()
    {
        InnerClear();
    }

    private void InnerClear()
    {
        if (m_ExitDestroy)
            GameObject.DestroyImmediate(m_Root);
        if (m_CurrentNavmesh.Renderer != null)
            m_CurrentNavmesh.Renderer.Destroy();
        m_Current = null;
        m_AreasInst.Clear();
        m_Root = null;
        m_CurrentNavmesh.Renderer = null;
        m_CurrentNavmesh.Navmesh = null;
        m_CurrentNavmesh.MeshArea = null;
        m_Areas = null;
        m_Rect = null;
    }

    private void InnerChangeFileName()
    {
        if (string.IsNullOrEmpty(m_CurrentPath) || m_Current == null)
            return;

        string orgPath = m_CurrentPath;
        string tarPath = Path.Combine(m_FilePath, $"{m_Current.Name}.{m_FileExtension}");
        if (orgPath == tarPath)
            return;
        if (!File.Exists(orgPath))
            return;

        m_CurrentPath = tarPath;
        m_Root.name = m_Current.Name;
        File.Copy(orgPath, tarPath, true);
        File.Delete(orgPath);
        File.Delete($"{orgPath}.meta");
        InnerSaveCurrent();
        AssetDatabase.Refresh();
    }

    private void InnerGenerateNavmesh()
    {
        InnerSyncData();
        XNavMesh navmesh = new XNavMesh(new AABB(m_Current.MinX, m_Current.MaxX, m_Current.MinY, m_Current.MaxY));

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
        if (m_CurrentNavmesh.Renderer != null)
            m_CurrentNavmesh.Renderer.Destroy();
        m_CurrentNavmesh.Navmesh = navmesh;
        m_CurrentNavmesh.MeshArea = new MeshArea(navmesh, Color.green);
        m_CurrentNavmesh.Renderer = new XNavMeshRenderer();
        m_CurrentNavmesh.Renderer.Refresh(m_CurrentNavmesh.MeshArea);

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
        line.useWorldSpace = false;
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
        InnerGenerateBorder();
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

            m_Current.MinX = min.x;
            m_Current.MinY = min.y;
            m_Current.MaxX = max.x;
            m_Current.MaxY = max.y;
        }
    }

    private void InnerAddArea(string name, Vector3[] points)
    {
        GameObject root = new GameObject(name);
        m_AreasInst.Add(root);
        root.transform.SetParent(m_Areas);
        LineRenderer line = root.AddComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = false;
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

        Vector2[] points2d = new Vector2[points.Length];
        for (int i = 0; i < points.Length; i++)
            points2d[i] = points[i];
        PolygonCollider2D collider = root.AddComponent<PolygonCollider2D>();
        collider.points = points2d;
    }

    private void InnerSaveCurrent()
    {
        InnerSyncData();
        File.WriteAllText(m_CurrentPath, JsonConvert.SerializeObject(m_Current));
        AssetDatabase.Refresh();
    }

    private void InnerSyncData()
    {
        if (m_Areas == null)
            return;
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
        if (m_Areas)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (Transform tf in m_Areas)
            {
                RVOArea ob = tf.GetComponent<RVOArea>();
                ob.UpdatePoints();
                List<Vector2> points = ob.GetUnityVertices2(m_Current.MinX, m_Current.MinY, m_Current.MaxX, m_Current.MaxY);
                foreach (Vector2 p in points)
                {
                    if (p.x < min.x) min.x = p.x;
                    if (p.y < min.y) min.y = p.y;
                    if (p.x > max.x) max.x = p.x;
                    if (p.y > max.y) max.y = p.y;
                }
            }

            if (m_Current != null)
            {
                m_Current.PointMinX = min.x;
                m_Current.PointMinY = min.x;
                m_Current.PointMaxX = max.x;
                m_Current.PointMaxY = max.y;
            }
        }
    }

    [MenuItem("Tools/XNavmesh")]
    public static void Define()
    {
        GetWindow<XNavMeshTools>().Show();
    }
}
