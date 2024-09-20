using System;
using System.Collections.Generic;
using UnityEngine;
using XFrame.PathFinding;
using static Test;


public partial class Test2 : MonoBehaviour
{
    public static bool T1;
    public static XNavMesh Navmesh;

    private PolyInfo m_ShowPoly;
    private bool m_DrawGizmosFullMeshArea = true;

    private void AddTestCommand()
    {
        Console.Inst.AddCommand("test-1", (param) =>
        {
            Console.Inst.ExecuteCommand("navmesh-add");
            Console.Inst.ExecuteCommand("poly-add 2 true");
        });
        Console.Inst.AddCommand("test-2", (param) =>
        {
            Console.Inst.ExecuteCommand("poly-add 1 true");
        });
    }

    private void TestEdge(string param)
    {
        List<EdgeSet> edges = new List<EdgeSet>();

        List<List<XVector2>> list = new List<List<XVector2>>();
        list.Add(GetAllPoints(Holls[3], false));
        list.Add(GetAllPoints(Holls[4], false));
        list.Add(GetAllPoints(Holls[5], false));

        for (int j = 0; j < list.Count - 1; j++)
        {
            List<XVector2> points = list[j];
            for (int i = 0; i < points.Count; i++)
            {
                XVector2 p1 = points[i];
                XVector2 p2 = points[(i + 1) % points.Count];
                EdgeSet e1 = FindEdge(edges, p1, p2);
                Debug.LogWarning($"e1 {p1} {p2} {e1.Start} {e1.End}");

                for (int k = j + 1; k < list.Count; k++)
                {
                    List<XVector2> points2 = list[k];
                    for (int l = 0; l < points2.Count; l++)
                    {
                        XVector2 p3 = points2[l];
                        XVector2 p4 = points2[(l + 1) % points2.Count];
                        EdgeSet e2 = FindEdge(edges, p3, p4);

                        if (e2.Intersect(e1, out XVector2 newPoint))
                        {
                            Debug.LogWarning($"intersect e1 {p1} {p2} {e1.Start} {e1.End} e2 {p3} {p4} {e2.Start} {e2.End}  new point {newPoint}");
                            e1.Add(newPoint);
                            e2.Add(newPoint);
                        }
                        else if (e2.InSameLine(e1))
                        {
                            Debug.LogWarning($"In Line e1 {p1} {p2} {e1.Start} {e1.End} e2 {p3} {p4} {e2.Start} {e2.End}");
                            e1.Add(p3);
                            e1.Add(p4);
                            e2.Add(p1);
                            e2.Add(p2);
                        }
                    }
                }
            }

        }

        Debug.LogWarning("======================");
        foreach (EdgeSet edge in edges)
        {
            List<Edge> tmpEdges = new List<Edge>();
            for (int i = 0; i < edge.Vertices.Count; i++)
            {
                XVector2 p1 = edge.Vertices[i];
                XVector2 p2 = edge.Vertices[(i + 1) % edge.Vertices.Count];
                tmpEdges.Add(new Edge(p1, p2));
            }
            m_Edges.Add(tmpEdges);
            Debug.LogWarning(edge);
        }
    }

    private EdgeSet FindEdge(List<EdgeSet> edges, XVector2 start, XVector2 end)
    {
        XVector2 p1 = start;
        XVector2 p2 = end;
        EdgeSet target = null;
        foreach (EdgeSet edge in edges)
        {
            XVector2 p3 = edge.Start;
            XVector2 p4 = edge.End;

            Debug.LogWarning($"check same {edge.Start} {edge.End} -> {p1} {p2} {edge.InSameLine(p1, p2)}");
            if (edge.InSameLine(p1, p2))
            {
                target = edge;
                Debug.LogWarning("is same");
                break;
            }
        }

        if (target != null)
        {
            target.Add(p1);
            target.Add(p2);
        }
        else
        {
            Debug.LogWarning($"new edge set {p1} {p2}");
            target = new EdgeSet(p1, p2);
            edges.Add(target);
        }
        return target;
    }

    private void CreateNavMesh(string param)
    {
        if (m_NavMesh != null)
            return;
        List<XVector2> points = GetAllPoints(RectPoints, false);
        m_NavMesh = new XNavMesh(new AABB(points));
        T1 = true;
        Navmesh = m_NavMesh;
        m_NavMesh.Add(points);
        m_FullMeshArea = new MeshArea(m_NavMesh, Color.green);
        m_FullMeshArea.Refresh();
        m_NavMesh.CheckDataValid();
    }

    private void CreatePoly(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToIntBool(param, out int index, out bool includeNonActive))
        {
            if (index >= 0 && index < Holls.Count)
            {
                List<XVector2> points = GetAllPoints(Holls[index], includeNonActive);
                Poly poly = m_NavMesh.AddWithExtraData(points, AreaType.Obstacle, out HalfEdgeData newAreaData, out List<Edge> newOutLine);
                m_ShowPoly = InnerAddPolyInfo(poly.Id, poly, newAreaData, newOutLine);
                m_FullMeshArea.Refresh();
                m_NavMesh.CheckDataValid();
                m_NavMesh.Test();
            }
        }
    }

    private void RemovePoly(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToInt(param, out int id))
        {
            if (m_Polies.TryGetValue(id, out PolyInfo polyInfo))
            {
                m_NavMesh.Remove(polyInfo.Poly, out HalfEdgeData newAreaData, out List<Edge> newOutLine);
                m_FullMeshArea.Refresh();
                InnerRemovePolyInfo(id);
                m_NavMesh.CheckDataValid();
            }
        }
    }

    private void MovePolyX(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToIntFloat(param, out int id, out float offset))
        {
            if (m_Polies.TryGetValue(id, out PolyInfo polyInfo))
            {
                if (polyInfo.Poly.Move(new XVector2(offset, 0f), out HalfEdgeData newAreaData, out List<Edge> newOutLine))
                {
                    m_ShowPoly = InnerAddPolyInfo(id, polyInfo.Poly, newAreaData, newOutLine);
                    m_FullMeshArea.Refresh();
                    m_NavMesh.CheckDataValid();
                    m_NavMesh.Test();
                }
                else
                {
                    Debug.Log($"move poly {id} with x failure");
                }
            }
        }
    }

    private void MovePolyY(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToIntFloat(param, out int id, out float offset))
        {
            if (m_Polies.TryGetValue(id, out PolyInfo polyInfo))
            {
                if (polyInfo.Poly.Move(new XVector2(0f, offset), out HalfEdgeData newAreaData, out List<Edge> newOutLine))
                {
                    m_ShowPoly = InnerAddPolyInfo(id, polyInfo.Poly, newAreaData, newOutLine);
                    m_FullMeshArea.Refresh();
                    m_NavMesh.CheckDataValid();
                }
                else
                {
                    Debug.Log($"move poly {id} with y failure");
                }
            }
        }
    }

    private void RotatePoly(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToIntFloat(param, out int id, out float angle))
        {
            if (m_Polies.TryGetValue(id, out PolyInfo info))
            {
                if (info.Poly.Rotate(angle, out HalfEdgeData newAreaData, out List<Edge> newOutLine))
                {
                    m_ShowPoly = InnerAddPolyInfo(id, info.Poly, newAreaData, newOutLine);
                    m_FullMeshArea.Refresh();
                }
                else
                {
                    Debug.Log($"move poly {id} with y failure");
                }
            }
        }
    }

    private void RotateLoopPoly(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToIntFloat(param, out int id, out float angle))
        {
            if (m_Polies.TryGetValue(id, out PolyInfo info))
            {
                info.Updater = new Updater(() =>
                {
                    if (info.Poly.Rotate(angle, out HalfEdgeData newAreaData, out List<Edge> newOutLine))
                    {
                        m_ShowPoly = InnerAddPolyInfo(id, info.Poly, newAreaData, newOutLine);
                        m_FullMeshArea.Refresh();
                    }
                    else
                    {
                        Debug.Log($"move poly {id} with y failure");
                    }
                });
            }
        }
    }

    private void ScalePoly(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToIntFloat(param, out int id, out float scale))
        {
            if (m_Polies.TryGetValue(id, out PolyInfo info))
            {
                if (info.Poly.Scale(XVector2.One * scale, out HalfEdgeData newAreaData, out List<Edge> newOutLine))
                {
                    m_ShowPoly = InnerAddPolyInfo(id, info.Poly, newAreaData, newOutLine);
                    m_FullMeshArea.Refresh();
                }
                else
                {
                    Debug.Log($"move poly {id} with y failure");
                }
            }
        }
    }
}
