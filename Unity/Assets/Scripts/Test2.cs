using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using XFrame.PathFinding;
using static Test;


public partial class Test2 : MonoBehaviour
{
    public static bool T1;
    public static XNavMesh Navmesh;
    public static Normalizer Normalizer;

    private PolyInfo m_ShowPoly;
    private bool m_DrawGizmosFullMeshArea = true;
    private bool m_DrawGizmosPoly = true;

    private void AddTestCommand()
    {
        Console.Inst.AddCommand("test-1", (param) =>
        {
            Console.Inst.ExecuteCommand("navmesh-add");
            Console.Inst.ExecuteCommand("poly-add 2 true");
            Console.Inst.ExecuteCommand("poly-add 1 true");
        });
        Console.Inst.AddCommand("test-2", (param) =>
        {
            //Console.Inst.ExecuteCommand("t1-on");
            Console.Inst.ExecuteCommand("poly-rotate 1 0.1");
        });
        Console.Inst.AddCommand("test-3", (param) =>
        {
            Console.Inst.ExecuteCommand("navmesh-add");
            Console.Inst.ExecuteCommand("main-hide");
            Console.Inst.ExecuteCommand("open walk-error-2024-17-24");
            Console.Inst.ExecuteCommand("test-tri");
        });
    }

    private void TestTri(string param)
    {
        XNavMesh.DelaunayIncrementalSloan.TriangulationWalk(new XVector2(-0.9968367f, -6.047449f), null, HalfDataTest.Data);
    }

    private void TestEdge(string param)
    {
        List<List<XVector2>> list = new List<List<XVector2>>();
        var l1 = GetAllPoints(Holls[4], false);
        var l2 = GetAllPoints(Holls[3], false);
        Normalizer.Normalize(l1);
        Normalizer.Normalize(l2);
        list.Add(l1);
        list.Add(l2);
        //list.Add(GetAllPoints(Holls[5], false));

        List<XVector2> result = PolyUtility.Combine(list, out list);

        Debug.LogWarning("===============================");
        List<Edge> tmpEdges = new List<Edge>();
        for (int i = 0; i < result.Count; i++)
        {
            XVector2 p1 = result[i];
            XVector2 p2 = result[(i + 1) % result.Count];
            Debug.LogWarning($" {p1} ");
            tmpEdges.Add(new Edge(p1, p2));
        }
        Normalizer.UnNormalize(tmpEdges);
        m_Edges.Add(tmpEdges);
        Debug.LogWarning("===============================");
    }

    private void CreateNavMesh(string param)
    {
        if (m_NavMesh != null)
            return;
        List<XVector2> points = GetAllPoints(RectPoints, false);
        m_NavMesh = new XNavMesh(new AABB(points));
        Navmesh = m_NavMesh;
        Normalizer = m_NavMesh.Normalizer;  
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
