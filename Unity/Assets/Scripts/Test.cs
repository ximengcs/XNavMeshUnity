using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using XFrame.PathFinding;
using System.Collections.Generic;

public static class Debuger
{
    public static bool T1;
    public static XNavMesh Navmesh;
}

public partial class Test : MonoBehaviour
{
    public Transform RectPoints;
    public Transform HolePoints;
    public Transform HolePoints2;
    public GameObject MeshPrefab;

    private XNavMesh m_NavMesh;

    private XNavMeshRenderer m_MeshRender1;
    private XNavMeshRenderer m_MeshRender2;
    private MeshArea m_Mesh1;
    private MeshArea m_Mesh2;

    private Triangle m_Triangle;
    private Triangle m_Triangle2;
    private List<Edge> m_Lines;

    public void Init()
    {
        List<XVector2> points = GetAllPoints(RectPoints, false);
        m_MeshRender1 = new XNavMeshRenderer(MeshPrefab);
        m_MeshRender2 = new XNavMeshRenderer(MeshPrefab);
        m_NavMesh = new XNavMesh(new AABB(points));

        var holePoints = GetAllPoints(HolePoints, true);
        m_Triangle = new Triangle(holePoints);

        var holePoints2 = GetAllPoints(HolePoints2, true);
        m_Triangle2 = new Triangle(holePoints2);

        Debuger.T1 = true;
        Debuger.Navmesh = m_NavMesh;
        List<XVector2> addPoints = GetAllPoints(RectPoints, true);
        m_NavMesh.Add(addPoints);
        RefreshMeshArea();
        m_NavMesh.CheckDataValid();
    }

    public void AddHole()
    {
        m_NavMesh.AddWithExtraData(m_Triangle2, AreaType.Obstacle, out HalfEdgeData newAreaData2, out List<Edge> newOutLine2);
        m_NavMesh.AddWithExtraData(m_Triangle, AreaType.Obstacle, out HalfEdgeData newAreaData, out List<Edge> newOutLine);
        RefreshMeshArea();
        RefreshMeshArea2(newAreaData, Color.blue);
        RefreshLine(newOutLine);
        m_NavMesh.CheckDataValid();
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
        InnerMove(new XVector2(10f, 0f));
    }

    private void Update()
    {
        if (!m_Rotating)
            return;

        InnerRotate2();
        InnerRotate();
    }

    private bool m_Rotating;
    public void Rotate()
    {
        m_Rotating = !m_Rotating;
    }

    private void InnerRotate()
    {
        Vector3 tar = m_Triangle.OuterCentrePoint.ToUnityVec3();
        HolePoints.RotateAround(tar, Vector3.back, 1);
        var holePoints = GetAllPoints(HolePoints, true);
        Triangle tarTriangle = new Triangle(holePoints);
        if (m_NavMesh.ChangeWithExtraData(m_Triangle, tarTriangle, out m_Triangle, out HalfEdgeData newAreaData, out List<Edge> newOutLine))
        {
            RefreshAll(newAreaData, newOutLine);
        }
    }

    private void InnerRotate2()
    {
        Vector3 tar = m_Triangle2.OuterCentrePoint.ToUnityVec3();
        HolePoints2.RotateAround(tar, Vector3.back, 1);
        var holePoints = GetAllPoints(HolePoints2, true);
        Triangle tarTriangle = new Triangle(holePoints);
        if (m_NavMesh.ChangeWithExtraData(m_Triangle2, tarTriangle, out m_Triangle2, out HalfEdgeData newAreaData, out List<Edge> newOutLine))
        {
            RefreshAll(null, null);
        }
    }

    private void InnerMove(XVector2 offset)
    {
        if (m_NavMesh.MoveWithExtraData(m_Triangle, offset, out m_Triangle, out HalfEdgeData newAreaData, out List<Edge> newOutLine))
        {
            RefreshAll(newAreaData, newOutLine);
        }
    }

    public void RefreshAll(HalfEdgeData newAreaData, List<Edge> newOutLine)
    {
        RefreshMeshArea();
        RefreshMeshArea2(newAreaData, Color.blue);
        RefreshLine(newOutLine);
        m_NavMesh.CheckDataValid();
        //m_NavMesh.Test();
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
#if UNITY_EDITOR
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
#endif
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
