using UnityEngine;
using UnityEditor;
using XFrame.PathFinding;
using System.Collections.Generic;
using System.Security.Cryptography;

public static class Debuger
{
    public static bool T1;
    public static XNavMesh Navmesh;
}

public partial class Test : MonoBehaviour
{
    public static bool Debug;

    public Transform RectPoints;
    public Transform HolePoints;
    public GameObject MeshPrefab;

    private XNavMesh m_NavMesh;

    private XNavMeshRenderer m_MeshRender1;
    private XNavMeshRenderer m_MeshRender2;
    private MeshArea m_Mesh1;
    private MeshArea m_Mesh2;

    private Triangle m_Triangle;
    private List<Edge> m_Lines;

    public void Init()
    {
        List<XVector2> points = GetAllPoints(RectPoints, false);
        m_MeshRender1 = new XNavMeshRenderer(MeshPrefab);
        m_MeshRender2 = new XNavMeshRenderer(MeshPrefab);
        m_NavMesh = new XNavMesh(new AABB(points));

        Debuger.T1 = true;
        Debuger.Navmesh = m_NavMesh;
        List<XVector2> addPoints = GetAllPoints(RectPoints, true);

        XNavMesh.Test(m_NavMesh, m_NavMesh.Normalizer);
        UnityEngine.Debug.Log("===============");

        m_NavMesh.Add(addPoints);
        RefreshMeshArea();
        XNavMesh.Test(m_NavMesh, m_NavMesh.Normalizer);
    }

    public void AddHole()
    {
        var holePoints = GetAllPoints(HolePoints, true);
        m_Triangle = new Triangle(holePoints);
        m_NavMesh.AddWithExtraData(m_Triangle, AreaType.Obstacle, out HalfEdgeData newAreaData, out List<Edge> newOutLine);
        RefreshMeshArea();
        RefreshMeshArea2(newAreaData, Color.blue);
        RefreshLine(newOutLine);
        XNavMesh.Test(newAreaData, m_NavMesh.Normalizer);
    }

    public void RemoveHole()
    {
        m_NavMesh.RemoveWithExtraData(m_Triangle, out HalfEdgeData newAreaData, out List<Edge> newOutLine);
        RefreshMeshArea();
        RefreshMeshArea2(newAreaData, Color.blue);
        RefreshLine(newOutLine);
    }

    public void MoveHole()
    {
        InnerMove(new XVector2(2f, 0f));
    }

    public void Up()
    {
        InnerMove(new XVector2(0f, 2f));
    }

    public void Down()
    {
        InnerMove(new XVector2(0f, -2f));
    }

    public void Left()
    {
        InnerMove(new XVector2(-2f, 0f));
    }

    public void Right()
    {
        InnerMove(new XVector2(2f, 0f));
    }

    private void InnerMove(XVector2 offset)
    {
        m_Triangle = m_NavMesh.MoveWithExtraData(m_Triangle, offset, out HalfEdgeData newAreaData, out List<Edge> newOutLine);
        RefreshMeshArea();
        RefreshMeshArea2(newAreaData, Color.blue);
        RefreshLine(newOutLine);
    }

    private void RefreshMeshArea()
    {
        m_Mesh1 = new MeshArea(m_NavMesh, Color.green);
        m_MeshRender1.Refresh(m_Mesh1);
    }

    private void RefreshMeshArea2(HalfEdgeData data, Color color)
    {
        if (data == null) return;
        m_Mesh2 = new MeshArea(m_NavMesh, data, Color.blue);
        m_MeshRender2.Refresh(m_Mesh2);
    }

    private void RefreshLine(List<Edge> lines)
    {
        if (lines == null) return;
        m_NavMesh.Normalizer.UnNormalize(lines);
        m_Lines = lines;
    }

    private void OnDrawGizmos()
    {
        InnerDrawMesh(m_Mesh1);
        InnerDrawMesh(m_Mesh2);

        if (m_Lines != null)
        {
            Handles.color = Color.yellow;
            for (int i = 0; i < m_Lines.Count; i++)
            {
                Edge e = m_Lines[i];
                Vector3 p1 = e.P1.ToUnityVec3();
                Vector3 p2 = e.P2.ToUnityVec3();

                Handles.DrawAAPolyLine(10, p1, p2);
            }
        }
    }

    private void InnerDrawMesh(MeshArea area)
    {
        if (area != null)
        {
            foreach (MeshInfo item in area.Meshs)
            {
                Color color = item.Color;
                Gizmos.color = color;
                Vector3 p1 = item.Triangle.P1.ToUnityVec3();
                Vector3 p2 = item.Triangle.P2.ToUnityVec3();
                Vector3 p3 = item.Triangle.P3.ToUnityVec3();
                if (!XMath.CheckPointsOnLine(item.Triangle.P1, item.Triangle.P2, item.Triangle.P3))
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
                //Gizmos.DrawMesh(item.Mesh);
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
}
