
using System.Collections.Generic;
using UnityEngine;
using XFrame.PathFinding;

public partial class Test
{
    public struct MeshInfo
    {
        public TriangleArea AreaInfo;
        public Mesh Mesh;
        public Color Color;
        public Triangle Triangle;

        public MeshInfo(TriangleArea areaInfo, Mesh mesh, Color color)
        {
            AreaInfo = areaInfo;
            Mesh = mesh;
            Color = color;
            Triangle = areaInfo.Shape;
        }
    }

    public class MeshArea
    {
        private XNavMesh m_NavMesh;
        private Color m_Color;
        private List<MeshInfo> m_Meshs;

        public List<MeshInfo> Meshs => m_Meshs;

        public MeshArea(XNavMesh navMesh, Color color)
        {
            m_Color = color;
            m_NavMesh = navMesh;
            Refresh(navMesh.ToTriangles());
        }

        public void Dispose()
        {
            if (m_Meshs != null)
            {
                foreach (MeshInfo m in m_Meshs)
                {
                    Pool.ReleaseMesh(m.Mesh);
                }
            }
        }
        public void Refresh()
        {
            Refresh(m_NavMesh.ToTriangles());
        }

        public void Refresh(HalfEdgeData data)
        {
            Refresh(m_NavMesh.ToTriangles(data));
        }

        public void Refresh(List<TriangleArea> triangles)
        {
            Dispose();
            m_Meshs = GenerateMesh(triangles, m_Color);
        }

        public static List<MeshInfo> GenerateMesh(IEnumerable<TriangleArea> triangles, Color originColor)
        {
            List<MeshInfo> meshes = new List<MeshInfo>();
            foreach (TriangleArea triangle in triangles)
            {
                XVector2 v1 = triangle.Shape.P1;
                XVector2 v2 = triangle.Shape.P2;
                XVector2 v3 = triangle.Shape.P3;

                Mesh mesh = Pool.RequireMesh();
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

                Color origin = originColor;
                if (triangle.Area == AreaType.Obstacle)
                {
                    origin = Color.red;
                }
                meshes.Add(new(triangle, mesh, origin));
            }
            return meshes;
        }
    }
}
