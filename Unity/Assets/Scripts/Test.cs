using UnityEngine;
using XFrame.PathFinding;
using System.Collections.Generic;
using UnityEditor;

public class Test : MonoBehaviour
{
    public Transform RectPoints;
    public Transform HolePoints;

    private Triangle m_Triangle;
    private XNavMesh m_NavMesh;
    private List<(Mesh, Color, Triangle)> m_Meshs;
    private List<XVector2> m_Lines;

    public void Init()
    {
        var points = GetAllPoints(RectPoints);
        m_NavMesh = new XNavMesh(new AABB(points));
        m_NavMesh.Add(points);
        //m_NavMesh.AddConstraint(points);
        GenerateMesh(m_NavMesh.ToTriangles());
    }

    private void Update()
    {
        if (m_NavMesh != null)
        {
            //RandomPoint();
        }
    }

    public void AddHole()
    {
        var holePoints = GetAllPoints(HolePoints);
        m_Triangle = new Triangle(holePoints);
        //m_NavMesh.Add2(m_Triangle, AreaType.Obstacle);
        //m_NavMesh.AddConstraint(holePoints);
        GenerateMesh(m_NavMesh.Add2(m_Triangle, AreaType.Obstacle, out m_Lines));
    }

    public void RemoveHole()
    {
        m_Lines = m_NavMesh.Remove(m_Triangle);

        List<Triangle> triangles = new List<Triangle>(EarClipping.Triangulate(m_Lines));
        GenerateMesh(triangles);
    }

    public void RandomPoint()
    {
        XVector2 min = m_NavMesh.AABB.Min;
        XVector2 max = m_NavMesh.AABB.Max;
        m_NavMesh.Add(new XVector2(Random.Range(min.X, max.X), Random.Range(min.Y, max.Y)));
        GenerateMesh(m_NavMesh.ToTriangles());
    }

    private void GenMesh(List<Triangle> triangles)
    {
        GenerateMesh(triangles);
    }

    private void GenerateMesh(List<TriangleArea> triangles)
    {
        m_Meshs = new List<(Mesh, Color, Triangle)>();
        foreach (TriangleArea triangle in triangles)
        {
            XVector2 v1 = triangle.Shape.P1;
            XVector2 v2 = triangle.Shape.P2;
            XVector2 v3 = triangle.Shape.P3;

            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[3];
            vertices[0] = new Vector3(v1.X, v1.Y);
            vertices[1] = new Vector3(v2.X, v2.Y);
            vertices[2] = new Vector3(v3.X, v3.Y);

            int[] faces = new int[3];
            faces[0] = 0;
            faces[1] = 1;
            faces[2] = 2;

            mesh.vertices = vertices;
            mesh.triangles = faces;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            Color color = Color.green;
            if (triangle.Area == AreaType.Obstacle)
                color = Color.red;
            m_Meshs.Add(new(mesh, color, triangle.Shape));
        }
    }

    private void GenerateMesh(List<Triangle> triangles)
    {
        m_Meshs = new List<(Mesh, Color, Triangle)>();
        foreach (Triangle triangle in triangles)
        {
            XVector2 v1 = triangle.P1;
            XVector2 v2 = triangle.P2;
            XVector2 v3 = triangle.P3;

            Mesh mesh = new Mesh();
            Vector3[] vertices = new Vector3[3];
            vertices[0] = new Vector3(v1.X, v1.Y);
            vertices[1] = new Vector3(v2.X, v2.Y);
            vertices[2] = new Vector3(v3.X, v3.Y);

            int[] faces = new int[3];
            faces[0] = 0;
            faces[1] = 1;
            faces[2] = 2;

            mesh.vertices = vertices;
            mesh.triangles = faces;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            m_Meshs.Add(new(mesh, new Color(Random.Range(0f, 1), Random.Range(0f, 1), Random.Range(0f, 1), 1), triangle));
        }
    }

    private void OnDrawGizmos()
    {
        if (m_Meshs != null)
        {
            foreach (var item in m_Meshs)
            {
                Gizmos.color = item.Item2;
                Gizmos.DrawLine(item.Item3.P1.ToUnityVec3(), item.Item3.P2.ToUnityVec3());
                Gizmos.DrawLine(item.Item3.P2.ToUnityVec3(), item.Item3.P3.ToUnityVec3());
                Gizmos.DrawLine(item.Item3.P3.ToUnityVec3(), item.Item3.P1.ToUnityVec3());
                Gizmos.color = new Color(item.Item2.r, item.Item2.g, item.Item2.b, 0.5f);
                Gizmos.DrawMesh(item.Item1);
            }
        }

        if (m_Lines != null)
        {
            Handles.color = Color.yellow;
            for (int i = 0; i < m_Lines.Count; i++)
            {
                Vector3 p1 = m_Lines[i].ToUnityVec3();
                Vector3 p2 = m_Lines[XMath.ClampListIndex(i + 1, m_Lines.Count)].ToUnityVec3();

                Handles.DrawAAPolyLine(10, p1, p2);
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
