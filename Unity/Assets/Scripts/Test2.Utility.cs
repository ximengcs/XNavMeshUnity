﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using XFrame.PathFinding;
using XFrame.PathFinding.Extensions;
using XFrame.PathFinding.RVO;
using static Test;

public partial class Test2
{
    public static Test2 Inst;

    public Transform RectPoints;
    public List<Transform> Holls;

    private XNavmesh m_NavMesh;
    private XNavMeshRenderer m_Render;
    private MeshArea m_FullMeshArea;
    private Dictionary<int, PolyInfo> m_Polies;
    private List<List<Edge>> m_Edges;
    private HalfEdgeInfo HalfDataTest;
    private List<Updater> m_UpdaterList;

    private void OnDrawGizmos()
    {
        /*
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
        */
        if (m_Triangles != null)
        {
            InnerDrawTriangle();
        }
    }

    void Start()
    {
        Inst = this;
        m_Edges = new List<List<Edge>>();
        m_Polies = new Dictionary<int, PolyInfo>();
        m_UpdaterList = new List<Updater>();
        m_Agents = new Dictionary<int, XAgent>();
        Application.targetFrameRate = 60;
        AddTestCommand();
        Console.Inst.AddCommand("navmesh-add", CreateNavMesh);
        Console.Inst.AddCommand("navmesh-open", OpenNavmesh);
        Console.Inst.AddCommand("navmesh-show", ShowNavmesh);
        Console.Inst.AddCommand("navmesh-poly-rotate", RotateNavmeshLoopPoly);
        Console.Inst.AddCommand("poly-add", CreatePoly);
        Console.Inst.AddCommand("poly-remove", RemovePoly);
        Console.Inst.AddCommand("poly-move-x", MovePolyX);
        Console.Inst.AddCommand("poly-move-y", MovePolyY);
        Console.Inst.AddCommand("poly-rotate", RotatePoly);
        Console.Inst.AddCommand("poly-rotate-loop", RotateLoopPoly);
        Console.Inst.AddCommand("poly-scale", ScalePoly);
        Console.Inst.AddCommand("main-show", ShowMainArea);
        Console.Inst.AddCommand("main-hide", HideMainArea);
        Console.Inst.AddCommand("edge-test", TestEdge);
        Console.Inst.AddCommand("t1-on", OnT1);
        Console.Inst.AddCommand("t1-off", OffT1);
        Console.Inst.AddCommand("check-valid", CheckValid);
        Console.Inst.AddCommand("entity", CreateObject);
#if DEBUG_PATH
        Console.Inst.AddCommand("record-show", Recorder.Show);
#endif
        Console.Inst.AddCommand("record-gene-relation", GenerateRelation);
        Console.Inst.AddCommand("record-gene-new-poly", GenerateNewPloyPoints);
        Console.Inst.AddCommand("poly-show", ShowPoly);
        Console.Inst.AddCommand("poly-hide", HidePoly);
        Console.Inst.AddCommand("open", OpenData);
        Console.Inst.AddCommand("test-tri", TestTri);
        Console.Inst.AddCommand("gen-data", GenerateHalfData);

        Console.Inst.AddCommand("record-cur-new-data-entity", RecordCurNewDataEntity);
        Console.Inst.AddCommand("gen-data-point-entity", GenDataPointEntity);
    }

    private void GenDataPointEntity(string param)
    {
        GenrateFaceEntity(HalfDataTest.Triangles);
    }

    private void GenerateHalfData(string param)
    {
        if (string.IsNullOrEmpty(param)) return;
        if (Normalizer == null)
        {
            List<XVector2> points = GetAllPoints(RectPoints, false);
            Normalizer = new Normalizer(points);
        }

        string text = File.ReadAllText($"Assets/Data/HalfEdgeTest/{param}");
        List<List<XVector2>> relationAllPoints = new List<List<XVector2>>();
        List<XVector2> edgePoints = new List<XVector2>();
        List<XVector2> polyPoints = null;

        const int POLY = 1, EDGE = 2;
        int mode = 0;
        string line = null;
        StringReader reader = new StringReader(text);
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();

            if (string.IsNullOrEmpty(line) || line.Length <= 0)
                continue;
            if (line.StartsWith("-") || line.StartsWith("="))
                continue;

            if (line.Contains("poly"))
            {
                polyPoints = new List<XVector2>();
                relationAllPoints.Add(polyPoints);
                mode = POLY;
                continue;
            }
            else if (line.Contains("NewAreaOutEdges"))
            {
                mode = EDGE;
                continue;
            }
            else if (line.Contains("End"))
            {
                break;
            }

            switch (mode)
            {
                case POLY:
                    {
                        line = line.Trim('(', ')', ' ');
                        string[] contents = line.Split(',');
                        XVector2 p = new XVector2();
                        p.X = float.Parse(contents[0]);
                        p.Y = float.Parse(contents[1]);
                        polyPoints.Add(p);
                    }
                    break;

                case EDGE:
                    {
                        line = line.Trim('(', ')', ' ');
                        string[] contents = line.Split(',');
                        XVector2 p = new XVector2();
                        p.X = float.Parse(contents[0]);
                        p.Y = float.Parse(contents[1]);
                        edgePoints.Add(p);
                    }
                    break;
            }
        }

        foreach (List<XVector2> points in relationAllPoints)
            Normalizer.Normalize(points);
        Normalizer.Normalize(edgePoints);

        List<Edge> edges = new List<Edge>();
        for (int i = 0; i < edgePoints.Count; i++)
        {
            XVector2 cur = edgePoints[i];
            XVector2 next = edgePoints[(i + 1) % edgePoints.Count];
            edges.Add(new Edge(cur, next));
        }

        s_Cache = new StringBuilder();
        HalfEdgeData data = HalfEdgeExtension.GenerateConstraintData(edges, true, relationAllPoints);
        HalfDataTest?.Dispose();
        HalfDataTest = new HalfEdgeInfo(m_NavMesh, data, Color.cyan);

#if DEBUG_PATH
        Debug.LogWarning("check valid------------------");
        Debug.LogWarning(data.CheckValid());
        Debug.LogWarning("-----------------------------");
#endif
    }

    public static StringBuilder s_Cache;

    private void GenerateRelation(string param)
    {
#if DEBUG_PATH
        Func<XVector2, XVector2> f = Test2.Normalizer.UnNormalize;
        List<List<XVector2>> list = Recorder.CurrentInfo.RelationAllPoints;

        GameObject root = new GameObject("relation");
        foreach (List<XVector2> pt in list)
        {
            GameObject inst = new GameObject("points");
            inst.transform.SetParent(root.transform);
            foreach (XVector2 pt2 in pt)
            {
                GameObject pInst = new GameObject($"{f(pt2)}");
                pInst.transform.SetParent(inst.transform);
                pInst.transform.position = f(pt2).ToUnityVec3();
            }
        }
#endif
    }

    private void GenerateNewPloyPoints(string param)
    {
#if DEBUG_PATH
        Func<XVector2, XVector2> f = Test2.Normalizer.UnNormalize;
        Dictionary<int, List<XVector2>> list = Recorder.CurrentInfo.PolyNewPoints;
        GameObject root = new GameObject("new points");
        foreach (var item in list)
        {
            GameObject inst = new GameObject($"poly {item.Key}");
            inst.transform.SetParent(root.transform);
            foreach (XVector2 point in item.Value)
            {
                GameObject p = new GameObject($" {f(point)} ");
                p.transform.SetParent(inst.transform);
                p.transform.position = f(point).ToUnityVec3();
            }
        }
#endif
    }

    private void RecordCurNewDataEntity(string param)
    {
#if DEBUG_PATH
        GenrateFaceEntity(Recorder.CurrentInfo.CloneData.Faces);
#endif
    }

    public void GenrateFaceEntity(List<TriangleArea> faces)
    {
        GameObject dataInst = new GameObject("triangle");
        foreach (TriangleArea area in faces)
        {
            Triangle triangle = area.Shape;
            GameObject tri = new GameObject($"< {triangle.P1} {triangle.P2} {triangle.P3}>");
            tri.transform.SetParent(dataInst.transform);

            GameObject selfPoints = new GameObject("selfPoints");
            selfPoints.transform.SetParent(tri.transform);

            GameObject p1 = new GameObject((triangle.P1).ToString());
            p1.transform.SetParent(selfPoints.transform);
            p1.transform.position = triangle.P1.ToUnityVec3();

            GameObject p2 = new GameObject((triangle.P2).ToString());
            p2.transform.SetParent(selfPoints.transform);
            p2.transform.position = triangle.P2.ToUnityVec3();

            GameObject p3 = new GameObject((triangle.P3).ToString());
            p3.transform.SetParent(selfPoints.transform);
            p3.transform.position = triangle.P3.ToUnityVec3();

            //if (e1.OppositeEdge != null) GenerateSubStruct(e1, p1);
            //if (e2.OppositeEdge != null) GenerateSubStruct(e2, p2);
            //if (e3.OppositeEdge != null) GenerateSubStruct(e3, p3);
        }
    }

    private void GenerateSubStruct(HalfEdge e1, GameObject p1)
    {
        Func<XVector2, XVector2> f = Test2.Normalizer.UnNormalize;
        HalfEdge ope1 = e1.OppositeEdge;
        HalfEdge ope2 = ope1.NextEdge;
        HalfEdge ope3 = ope2.NextEdge;

        GameObject p1Op_ = new GameObject("p1Op");
        p1Op_.transform.SetParent(p1.transform);

        GameObject p1Op_p1 = new GameObject("p1");
        p1Op_p1.transform.SetParent(p1Op_.transform);
        p1Op_p1.transform.position = f(ope1.Vertex.Position).ToUnityVec3();

        GameObject p1Op_p2 = new GameObject("p2");
        p1Op_p2.transform.SetParent(p1Op_.transform);
        p1Op_p2.transform.position = f(ope2.Vertex.Position).ToUnityVec3();

        GameObject p1Op = new GameObject("p1OpFace");
        p1Op.transform.SetParent(p1.transform);

        GameObject p1Opp1 = new GameObject("p1");
        p1Opp1.transform.SetParent(p1Op.transform);
        p1Opp1.transform.position = f(ope1.Vertex.Position).ToUnityVec3();

        GameObject p1Opp2 = new GameObject("p2");
        p1Opp2.transform.SetParent(p1Op.transform);
        p1Opp2.transform.position = f(ope2.Vertex.Position).ToUnityVec3();

        GameObject p1Opp3 = new GameObject("p3");
        p1Opp3.transform.SetParent(p1Op.transform);
        p1Opp3.transform.position = f(ope3.Vertex.Position).ToUnityVec3();
    }

    private void RotateNavmeshLoopPoly(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToIntFloat(param, out int id, out float angle))
        {
            if (m_NavMesh.Polies.TryGetValue(id, out Poly info))
            {
                m_UpdaterList.Add(new Updater(() =>
                {
                    if (info.Rotate(angle, out HalfEdgeData newAreaData, out List<Edge> newOutLine))
                    {
                        m_FullMeshArea.Refresh();
                        m_Render.Refresh(m_FullMeshArea);
                    }
                    else
                    {
                        Debug.Log($"move poly {id} with y failure");
                    }
                    return true;
                }));
            }
        }
    }

    private void ShowNavmesh(string param)
    {
        if (m_NavMesh != null)
        {
            m_Render = new XNavMeshRenderer();
            m_Render.Refresh(m_FullMeshArea);
        }
    }

    private void OpenNavmesh(string param)
    {
        param = param.TrimEnd(' ');
        byte[] bytes = File.ReadAllBytes($"Assets/Data/Navmesh/{param}.bytes");
        m_NavMesh = new XNavmesh(bytes);
        Normalizer = m_NavMesh.Normalizer;

        m_FullMeshArea = new MeshArea(m_NavMesh, Color.green);
        m_FullMeshArea.Refresh();
        Debug.Log($"read success, navmesh face count {m_NavMesh.AreaCount}");
    }

    private void OpenData(string param)
    {
        param = param.TrimEnd(' ');
        byte[] bytes = File.ReadAllBytes($"Assets/Data/{param}.bytes");
        HalfEdgeData data = new HalfEdgeData(bytes);
        HalfDataTest = new HalfEdgeInfo(m_NavMesh, data, Color.cyan);
        Debug.Log($"read success, face count {data.Faces.Count}");
#if DEBUG_PATH
        Debug.Log(data.CheckValid());
#endif
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

    private bool m_Dirty = false;
    private void Update()
    {
        foreach (var entry in m_Polies)
        {
            if (entry.Value.Updater != null)
                entry.Value.Updater.OnUpdate();
        }

        for (int i = m_UpdaterList.Count - 1; i >= 0; i--)
        {
            if (!m_UpdaterList[i].OnUpdate())
                m_UpdaterList.RemoveAt(i);
        }

        if (m_Dirty)
            Simulator.Instance.doStep();
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

    private bool ParamToIntVec(string param, out int target, out XVector2 p1)
    {
        p1 = default;
        string[] paramStr = param.Split(' ');
        if (!int.TryParse(paramStr[0], out target))
            return false;

        if (paramStr.Length >= 2)
        {
            string[] valueStr = paramStr[1].Split(',');
            if (valueStr.Length >= 2)
            {
                if (!float.TryParse(valueStr[0], out p1.X)) return false;
                if (!float.TryParse(valueStr[1], out p1.Y)) return false;
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool ParamToVec(string param, out XVector2 p1)
    {
        p1 = default;

        string[] paramStr = param.Split(' ');
        if (paramStr.Length >= 2)
        {
            string[] valueStr = paramStr[0].Split(',');
            if (valueStr.Length >= 2)
            {
                if (!float.TryParse(valueStr[0], out p1.X)) return false;
                if (!float.TryParse(valueStr[1], out p1.Y)) return false;
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    private bool ParamToVecVec(string param, out XVector2 p1, out XVector2 p2)
    {
        p1 = default;
        p2 = default;

        string[] paramStr = param.Split(' ');
        if (paramStr.Length >= 2)
        {
            string[] valueStr = paramStr[0].Split(',');
            if (valueStr.Length >= 2)
            {
                if (!float.TryParse(valueStr[0], out p1.X)) return false;
                if (!float.TryParse(valueStr[1], out p1.Y)) return false;
            }

            valueStr = paramStr[1].Split(',');
            if (valueStr.Length >= 2)
            {
                if (!float.TryParse(valueStr[0], out p2.X)) return false;
                if (!float.TryParse(valueStr[1], out p2.Y)) return false;
            }
            return true;
        }
        else
        {
            return false;
        }
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

    private void InnerDrawTriangle()
    {
        foreach (Triangle triangle in m_Triangles)
        {
            Vector3 p1 = triangle.P1.ToUnityVec3();
            Vector3 p2 = triangle.P2.ToUnityVec3();
            Vector3 p3 = triangle.P3.ToUnityVec3();

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p1);
        }
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
#if UNITY_EDITOR
                    if (item.AreaInfo.PolyId != -1)
                        Handles.Label(item.Triangle.InnerCentrePoint.ToUnityVec3(), $"[Poly {item.AreaInfo.PolyId}]");
#endif

                    Gizmos.DrawLine(p1, p2);
                    Gizmos.DrawLine(p2, p3);
                    Gizmos.DrawLine(p3, p1);
                    InnerDrawArrow(p1, p2, Color.yellow, false);
                    InnerDrawArrow(p2, p3, Color.yellow, false);
                    InnerDrawArrow(p3, p1, Color.yellow, false);
                    color.a = 0.5f;
                    Gizmos.color = color;

                    if (item.AreaInfo.E1HasOpposite)
                    {
                        InnerDrawArrow2(p1, p2, Color.magenta);
                    }
                    if (item.AreaInfo.E2HasOpposite)
                    {
                        InnerDrawArrow2(p2, p3, Color.magenta);
                    }
                    if (item.AreaInfo.E3HasOpposite)
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