
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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

        public bool E1HasOpposite()
        {
            HalfEdge e1 = AreaInfo.Face.Edge;
            HalfEdge e2 = e1.NextEdge;
            return InnerCheckHasOpposite(e1, e2);
        }

        public bool E2HasOpposite()
        {
            HalfEdge e2 = AreaInfo.Face.Edge.NextEdge;
            HalfEdge e3 = e2.NextEdge;
            return InnerCheckHasOpposite(e2, e3);
        }

        public bool E3HasOpposite()
        {
            HalfEdge e1 = AreaInfo.Face.Edge;
            HalfEdge e3 = e1.PrevEdge;
            return InnerCheckHasOpposite(e3, e1);
        }

        private bool InnerCheckHasOpposite(HalfEdge e1, HalfEdge e2)
        {
            if (AreaInfo.Face.IsSide)
                return false;

            if (e2.OppositeEdge != null)
            {
                XVector2 p1 = e1.Vertex.Position;
                XVector2 p2 = e2.Vertex.Position;
                XVector2 p3 = e2.OppositeEdge.Vertex.Position;
                XVector2 p4 = e2.OppositeEdge.PrevEdge.Vertex.Position;

                //string str = $" {AreaInfo.Normalizer.UnNormalize(p1)},{AreaInfo.Normalizer.UnNormalize(p2)} ";
                //if (e1.OppositeEdge != null)
                //    str += $" op {AreaInfo.Normalizer.UnNormalize(p3)}, {AreaInfo.Normalizer.UnNormalize(p4)} ";
                //else
                //    str += " op is null";
                //UnityEngine.Debug.Log(str);

                if (p1.Equals(p3) && p2.Equals(p4))
                {
                    return true;
                }
            }

            return false;
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

        public MeshArea(XNavMesh navMesh, HalfEdgeData data, Color color)
        {
            m_Color = color;
            m_NavMesh = navMesh;
            Refresh(XNavMesh.ToTriangles(navMesh, data));
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
            Refresh(XNavMesh.ToTriangles(m_NavMesh, data));
        }

        public void Refresh(XNavMeshList<TriangleArea> triangles)
        {
            Dispose();
            m_Meshs = new List<MeshInfo>();
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

                Color origin = m_Color;
                if (triangle.Area == AreaType.Obstacle)
                {
                    origin = Color.red;
                }
                m_Meshs.Add(new(triangle, mesh, origin));
            }
        }
    }
}
