
using System.Collections.Generic;
using UnityEngine;
using XFrame.PathFinding;

public partial class Test
{
    public struct MeshInfo
    {
        public Mesh Mesh;
        public Color Color;
        public Triangle Triangle;

        public MeshInfo(Mesh mesh, Color color, Triangle triangle)
        {
            Mesh = mesh;
            Color = color;
            Triangle = triangle;
        }
    }

    private class MeshArea
    {
        private List<MeshInfo> m_Meshs;

        public List<MeshInfo> Meshs => m_Meshs;

        public MeshArea(XNavMesh navMesh, Color color)
        {
            Refresh(navMesh, navMesh.ToTriangles(), color);
        }

        public MeshArea(XNavMesh navMesh, HalfEdgeData data, Color color)
        {
            Refresh(navMesh, XNavMesh.ToTriangles(navMesh, data), color);
        }

        public void Refresh(XNavMesh navMesh, XNavMeshList<TriangleArea> triangles, Color color)
        {
            m_Meshs = new List<MeshInfo>();
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

                Color origin = color;
                if (triangle.Area == AreaType.Obstacle)
                {
                    origin = Color.red;
                }
                m_Meshs.Add(new(mesh, origin, triangle.Shape));
            }
        }
    }
}