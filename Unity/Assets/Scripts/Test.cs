using UnityEngine;
using UnityEditor;
using XFrame.PathFinding;
using System.Collections.Generic;

public partial class Test : MonoBehaviour
{
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
        var points = GetAllPoints(RectPoints);
        m_MeshRender1 = new XNavMeshRenderer(MeshPrefab);
        m_MeshRender2 = new XNavMeshRenderer(MeshPrefab);
        m_NavMesh = new XNavMesh(new AABB(points));
        m_NavMesh.Add(points);
        RefreshMeshArea();
    }

    public void AddHole()
    {
        var holePoints = GetAllPoints(HolePoints);
        m_Triangle = new Triangle(holePoints);
        m_NavMesh.AddWithExtraData(m_Triangle, AreaType.Obstacle, out HalfEdgeData newAreaData, out List<Edge> newOutLine);
        RefreshMeshArea();
        RefreshMeshArea2(newAreaData, Color.blue);
        RefreshLine(newOutLine);
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
        if(data == null) return;
        m_Mesh2 = new MeshArea(m_NavMesh, data, Color.blue);
        m_MeshRender2.Refresh(m_Mesh2);
    }

    private void RefreshLine(List<Edge> lines)
    {
        if(lines == null) return;
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
                Gizmos.DrawLine(item.Triangle.P1.ToUnityVec3(), item.Triangle.P2.ToUnityVec3());
                Gizmos.DrawLine(item.Triangle.P2.ToUnityVec3(), item.Triangle.P3.ToUnityVec3());
                Gizmos.DrawLine(item.Triangle.P3.ToUnityVec3(), item.Triangle.P1.ToUnityVec3());
                color.a = 0.5f;
                Gizmos.color = color;
                Gizmos.DrawMesh(item.Mesh);
            }
        }
    }

    public List<XVector2> GetAllPoints(Transform tf)
    {
        List<XVector2> points = new List<XVector2>();
        foreach (Transform t in tf)
        {
            points.Add(new XVector2(t.position.x, t.position.y));
        }
        return points;
    }
}
