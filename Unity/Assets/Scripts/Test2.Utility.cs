﻿
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using XFrame.PathFinding;
using static Test;

public partial class Test2
{
    public static Test2 Inst;

    public Transform RectPoints;
    public List<Transform> Holls;

    private XNavMesh m_NavMesh;
    private MeshArea m_FullMeshArea;
    private Dictionary<int, PolyInfo> m_Polies;
    private List<List<Edge>> m_Edges;
    private HalfEdgeInfo HalfDataTest;

    private void OnDrawGizmos()
    {
        if (m_FullMeshArea != null)
        {
            if (m_DrawGizmosFullMeshArea)
                InnerDrawMesh(m_FullMeshArea.Meshs);
        }

        if (m_ShowPoly != null)
        {
            if (m_DrawGizmosPoly)
            {
                InnerDrawMesh(m_ShowPoly.MeshArea.Meshs);
                InnerDrawLine(m_ShowPoly.ChangeLine);
            }
        }
        if (m_Edges != null)
        {
            foreach (List<Edge> edges in m_Edges)
            {
                InnerDrawLine(edges);
            }
        }
        if (HalfDataTest != null)
        {
            InnerDrawMesh(HalfDataTest.m_Meshs);
        }
    }

    void Start()
    {
        Inst = this;
        m_Edges = new List<List<Edge>>();
        m_Polies = new Dictionary<int, PolyInfo>();
        Application.targetFrameRate = 60;
        AddTestCommand();
        Console.Inst.AddCommand("navmesh-add", CreateNavMesh);
        Console.Inst.AddCommand("poly-add", CreatePoly);
        Console.Inst.AddCommand("poly-remove", RemovePoly);
        Console.Inst.AddCommand("poly-move-x", MovePolyX);
        Console.Inst.AddCommand("poly-move-y", MovePolyY);
        Console.Inst.AddCommand("poly-rotate", RotatePoly);
        Console.Inst.AddCommand("poly-rotate-loop", RotateLoopPoly);
        Console.Inst.AddCommand("poly-scale", ScalePoly);
        Console.Inst.AddCommand("main-show", ShowMainArea);
        Console.Inst.AddCommand("main-hide", HideMainArea);
        Console.Inst.AddCommand("main-save", SaveMain);
        Console.Inst.AddCommand("main-open", OpenMain);
        Console.Inst.AddCommand("edge-test", TestEdge);
        Console.Inst.AddCommand("t1-on", OnT1);
        Console.Inst.AddCommand("t1-off", OffT1);
        Console.Inst.AddCommand("check-valid", CheckValid);
        Console.Inst.AddCommand("entity", CreateObject);
        Console.Inst.AddCommand("record-show", Recorder.Show);
        Console.Inst.AddCommand("record-show", Recorder.Show);
        Console.Inst.AddCommand("poly-show", ShowPoly);
        Console.Inst.AddCommand("poly-hide", HidePoly);
        Console.Inst.AddCommand("open", OpenData);
        Console.Inst.AddCommand("test-tri", TestTri);
    }

    private void OpenData(string param)
    {
        param = param.TrimEnd(' ');
        byte[] bytes = File.ReadAllBytes($"Assets/Data/{param}.bytes");
        HalfEdgeData data = DataUtility.FromBytes(bytes);
        HalfDataTest = new HalfEdgeInfo(data, Color.cyan);
        Debug.Log($"read success, face count {data.Faces.Count}");
        Debug.Log(data.CheckValid());
    }

    private void SaveMain(string param)
    {
        byte[] bytes = DataUtility.ToBytes(Navmesh.Data);
        File.WriteAllBytes("Assets/Data/main.bytes", bytes);
        AssetDatabase.Refresh();
        Debug.Log($"save success, size {bytes.Length}");
    }

    private void OpenMain(string param)
    {
        OpenData("main");
    }

    private void CreateObject(string param)
    {
        param = param.Trim(')', '(', ' ');
        string[] values = param.Split(',');
        float[] vs = new float[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            if (!float.TryParse(values[i], out vs[i]))
            {
                Debug.LogError($"bad param {values[i]}");
                return;
            }
        }
        GameObject inst = new GameObject(param);
        inst.transform.position = new Vector3(vs[0], vs[1]);
    }

    private void CheckValid(string param)
    {
        m_NavMesh.CheckDataValid();
    }

    public void OnT1(string param)
    {
        T1 = true;
    }

    public void OffT1(string param)
    {
        T1 = false;
    }

    public void AddLines(List<XVector2> points)
    {
        List<Edge> tmpEdges = new List<Edge>();
        for (int i = 0; i < points.Count; i++)
        {
            XVector2 p1 = points[i];
            XVector2 p2 = points[(i + 1) % points.Count];
            tmpEdges.Add(new Edge(p1, p2));
        }
        m_Edges.Add(tmpEdges);
    }

    private void ShowMainArea(string param)
    {
        m_DrawGizmosFullMeshArea = true;
    }

    private void HideMainArea(string param)
    {
        m_DrawGizmosFullMeshArea = false;
    }

    private void ShowPoly(string param)
    {
        m_DrawGizmosPoly = true;
    }

    private void HidePoly(string param)
    {
        m_DrawGizmosPoly = false;
    }
    private void Update()
    {
        foreach (var entry in m_Polies)
        {
            if (entry.Value.Updater != null)
                entry.Value.Updater.OnUpdate();
        }
    }

    public List<XVector2> GetAllPoints(Transform tf, bool checkActive)
    {
        List<XVector2> points = new List<XVector2>();
        foreach (Transform t in tf)
        {
            if (!checkActive || t.gameObject.activeSelf)
                points.Add(new XVector2(t.position.x, t.position.y));
        }
        return points;
    }

    private void InnerRemovePolyInfo(int id)
    {
        if (m_Polies.TryGetValue(id, out PolyInfo info))
        {
            if (m_ShowPoly == info)
                m_ShowPoly = null;

            m_Polies.Remove(id);
            info.Dispose();
        }
    }

    private PolyInfo InnerAddPolyInfo(int id, Poly poly, HalfEdgeData newAreaData, List<Edge> newOutLine)
    {
        if (!m_Polies.TryGetValue(id, out PolyInfo polyInfo))
        {
            polyInfo = new PolyInfo();
            polyInfo.Poly = poly;
            polyInfo.MeshArea = new MeshArea(m_NavMesh, Color.blue);
            m_Polies.Add(id, polyInfo);
        }

        polyInfo.ChangeData = newAreaData;
        polyInfo.ChangeLine = newOutLine;
        m_NavMesh.Normalizer.UnNormalize(newOutLine);
        polyInfo.MeshArea.Refresh(newAreaData);

        return polyInfo;
    }

    private bool ParamToInt(string param, out int target)
    {
        string[] paramStr = param.Split(' ');
        return int.TryParse(paramStr[0], out target);
    }

    private bool ParamToIntFloat(string param, out int t1, out float t2)
    {
        string[] paramStr = param.Split(' ');
        bool success = int.TryParse(paramStr[0], out t1);
        if (paramStr.Length > 1)
            success = float.TryParse(paramStr[1], out t2);
        else
        {
            t2 = 0;
            success = false;
        }
        return success;
    }

    private bool ParamToIntBool(string param, out int t1, out bool t2)
    {
        string[] paramStr = param.Split(' ');
        bool success = int.TryParse(paramStr[0], out t1);
        if (paramStr.Length > 1)
            success = bool.TryParse(paramStr[1], out t2);
        else
            t2 = false;
        return success;
    }

    private bool ParamToIntIntBool(string param, out int t1, out int t2, out bool t3)
    {
        string[] paramStr = param.Split(' ');
        bool success = int.TryParse(paramStr[0], out t1);
        if (paramStr.Length > 1)
            success = int.TryParse(paramStr[1], out t2);
        else
        {
            t2 = 0;
            success = false;
        }

        if (paramStr.Length > 2)
            success = bool.TryParse(paramStr[2], out t3);
        else
            t3 = false;
        return success;
    }

    private void InnerDrawLine(List<Edge> edges)
    {
#if UNITY_EDITOR
        if (edges != null)
        {
            Handles.color = Color.yellow;
            for (int i = 0; i < edges.Count; i++)
            {
                Edge e = edges[i];
                Vector3 p1 = e.P1.ToUnityVec3();
                Vector3 p2 = e.P2.ToUnityVec3();

                Color color = Gizmos.color;
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(p1, 0.05f);
                Gizmos.color = color;
                Handles.DrawAAPolyLine(10, p1, p2);
            }
        }
#endif
    }

    #region Draw Gizmos MeshArea
    private void InnerDrawMesh(List<MeshInfo> area)
    {
        if (area != null)
        {
            foreach (MeshInfo item in area)
            {
                Color color = item.Color;
                Gizmos.color = color;
                Vector3 p1 = item.Triangle.P1.ToUnityVec3();
                Vector3 p2 = item.Triangle.P2.ToUnityVec3();
                Vector3 p3 = item.Triangle.P3.ToUnityVec3();

                Vector3 t1 = p1;
                Vector3 t2 = p2;
                Vector3 t3 = p3;
                t1.z = -5;
                t2.z = -5;
                t3.z = -5;

#if UNITY_EDITOR
                Handles.Label(t1, $"({t1.x},{t1.y})");
                Handles.Label(t2, $"({t2.x},{t2.y})");
                Handles.Label(t3, $"({t3.x},{t3.y})");
#endif

                if (!XMath.CheckPointsHasSame(item.Triangle.P1, item.Triangle.P2, item.Triangle.P3))
                {
                    Gizmos.DrawLine(p1, p2);
                    Gizmos.DrawLine(p2, p3);
                    Gizmos.DrawLine(p3, p1);
                    InnerDrawArrow(p1, p2, Color.yellow, false);
                    InnerDrawArrow(p2, p3, Color.yellow, false);
                    InnerDrawArrow(p3, p1, Color.yellow, false);
                    color.a = 0.5f;
                    Gizmos.color = color;

                    if (item.E1HasOpposite())
                    {
                        InnerDrawArrow2(p1, p2, Color.magenta);
                    }
                    if (item.E2HasOpposite())
                    {
                        InnerDrawArrow2(p2, p3, Color.magenta);
                    }
                    if (item.E3HasOpposite())
                    {
                        InnerDrawArrow2(p3, p1, Color.magenta);
                    }
                }
                else
                {
                    XMath.FindMinMaxPoint(item.Triangle, out XVector2 min, out XVector2 max);
                    InnerDrawArrow(min.ToUnityVec3(), max.ToUnityVec3(), Color.cyan, true);
                }
                Gizmos.DrawMesh(item.Mesh);
            }
        }
    }

    private void InnerDrawArrow2(Vector3 from, Vector3 to, Color color)
    {
        Vector3 orgDir = (to - from).normalized;
        Vector3 tmpDir = orgDir;
        tmpDir = Quaternion.Euler(new Vector3(0, 0, 90)) * tmpDir;
        orgDir = Quaternion.Euler(new Vector3(0, 0, -90)) * orgDir;
        from += orgDir * 0.1f;
        to += orgDir * 0.1f;
        Vector3 center = from + (to - from) / 2;
        Vector3 tar = center + tmpDir * 0.05f;

        Color old = Gizmos.color;
        Gizmos.color = color;
        Gizmos.DrawLine(center, tar);
        Gizmos.DrawSphere(center, 0.01f);
        Gizmos.color = old;
    }

    private void InnerDrawArrow(Vector3 from, Vector3 to, Color color, bool side)
    {
        Color old = Gizmos.color;
        Vector3 orgDir = (to - from).normalized;
        Vector3 scaleDir = (to - from) * 0.2f;
        Vector3 dir = orgDir * 0.1f;
        dir = Quaternion.Euler(new Vector3(0, 0, side ? 90 : -90)) * dir;
        from += dir;
        to += dir;
        from += scaleDir;
        to -= scaleDir;

        Gizmos.color = color;
        Gizmos.DrawLine(from, to);

        dir = Quaternion.Euler(new Vector3(0, 0, side ? 150 : -150)) * orgDir * 0.5f;
        dir += to;
        Gizmos.DrawLine(to, dir);

        Gizmos.color = old;
    }
    #endregion
}