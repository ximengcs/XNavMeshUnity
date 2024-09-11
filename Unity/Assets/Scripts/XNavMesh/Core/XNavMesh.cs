using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace XFrame.PathFinding
{
    public partial class XNavMesh
    {
        private AABB m_AABB;
        private Normalizer m_Normalizer;
        private HalfEdgeData m_Data;

        public AABB AABB => m_AABB;

        public Normalizer Normalizer => m_Normalizer;

        public XNavMesh(AABB aabb)
        {
            m_AABB = aabb;
            m_Normalizer = new Normalizer(aabb);
            m_Data = new HalfEdgeData();
            Initialize();
        }

        public void Add(Triangle triangle, AreaType area)
        {
            Add(triangle.P1);
            Add(triangle.P2);
            Add(triangle.P3);

            HalfEdgeFace face = InnerFindFace(m_Normalizer.Normalize(triangle));
            face.Area = area;
        }

        private List<HalfEdgeFace> FindRelationFaces(HalfEdgeVertex vert)
        {
            List<HalfEdgeFace> faces = new List<HalfEdgeFace>();
            foreach (HalfEdgeFace face in m_Data.Faces)
            {
                if (face.Contains(vert.Position))
                {
                    faces.Add(face);
                }
            }
            return faces;
        }

        private List<HalfEdgeFace> FindRelationFaces(Triangle triangle)
        {
            List<HalfEdgeFace> faces = new List<HalfEdgeFace>();
            foreach (HalfEdgeFace face in m_Data.Faces)
            {
                if (triangle.Intersect(face))
                    faces.Add(face);
            }
            return faces;
        }

        public HalfEdgeFace FindFace(Triangle triangle)
        {
            foreach (HalfEdgeFace face in m_Data.Faces)
            {
                if (triangle.Equals(face))
                    return face;
            }
            return null;
        }

        public XNavMeshList<TriangleArea> Add2(Triangle triangle, AreaType area, out List<XVector2> edges)
        {
            triangle = m_Normalizer.Normalize(triangle);
            XNavMeshList<TriangleArea> re = new XNavMeshList<TriangleArea>(8);
            List<HalfEdgeFace> relationFaces = FindRelationFaces(triangle);
            foreach (HalfEdgeFace face in relationFaces)
            {
                re.Add(new TriangleArea(m_Normalizer.UnNormalize(new Triangle(face)), AreaType.Walk));
            }

            List<Edge> edgeList = new List<Edge>();
            foreach (HalfEdgeFace face in relationFaces)
            {
                HalfEdge e1 = face.Edge;
                HalfEdge e2 = e1.NextEdge;
                HalfEdge e3 = e1.PrevEdge;
                if (XMath.CheckLineOutOfTriangle(triangle, e1))
                {
                    edgeList.Add(e1.ToEdge());
                }
                if (XMath.CheckLineOutOfTriangle(triangle, e2))
                {
                    edgeList.Add(e2.ToEdge());
                }
                if (XMath.CheckLineOutOfTriangle(triangle, e3))
                {
                    edgeList.Add(e3.ToEdge());
                }
            }

            edges = new List<XVector2>();
            Edge curEdge = edgeList[0];
            do
            {
                edges.Add(m_Normalizer.UnNormalize(curEdge.P1));
                Edge tmp = curEdge;
                curEdge = null;
                foreach (Edge e in edgeList)
                {
                    if (e.P1.Equals(tmp.P2))
                    {
                        curEdge = e;
                        break;
                    }
                }
            } while (curEdge != null && edges.Count < edgeList.Count);

            // 临时半边数据用于构建三角形涉及的区域
            HalfEdgeData tmpData = new HalfEdgeData();
            Triangle superTriangle = GeometryUtility.SuperTriangle;
            tmpData.AddTriangle(superTriangle);

            // 将边界点添加到临时半边数据中
            List<XVector2> contraintList = new List<XVector2>();
            for (int i = edges.Count - 1; i >= 0; i--)
                contraintList.Add(m_Normalizer.Normalize(edges[i]));
            foreach (XVector2 p in contraintList)
            {
                DelaunayIncrementalSloan.InsertNewPointInTriangulation(p, tmpData);
            }

            // 添加三角形的三个点
            DelaunayIncrementalSloan.InsertNewPointInTriangulation(triangle.P1, tmpData);
            DelaunayIncrementalSloan.InsertNewPointInTriangulation(triangle.P2, tmpData);
            DelaunayIncrementalSloan.InsertNewPointInTriangulation(triangle.P3, tmpData);

            // 添加Constraint以剪切形状
            ConstrainedDelaunaySloan.AddConstraints(tmpData, contraintList, true);

            // 移除大三角形
            DelaunayIncrementalSloan.RemoveSuperTriangle(superTriangle, tmpData);

            // 替换结构的中三角形
            // 找出所有关联的边并移除旧边，添加新边
            foreach (Edge edge in edgeList)
            {
                foreach (HalfEdge e in tmpData.Edges)
                {
                    if (e.EqualsEdge(edge))
                    {
                        foreach (HalfEdgeFace face in relationFaces)  //只需要找相关联的面就可以
                        {
                            if (face.FindEdge(edge, out HalfEdge halfEdge))
                            {
                                // 找到了需要替换的边
                                if (halfEdge.OppositeEdge != null)
                                    halfEdge.OppositeEdge.OppositeEdge = e;
                                e.OppositeEdge = halfEdge.OppositeEdge;

                                // 移除旧边的相关数据
                                m_Data.Vertices.Remove(halfEdge.Vertex);
                                m_Data.Vertices.Remove(halfEdge.NextEdge.Vertex);
                                m_Data.Vertices.Remove(halfEdge.PrevEdge.Vertex);
                                m_Data.Edges.Remove(halfEdge);
                                m_Data.Edges.Remove(halfEdge.NextEdge);
                                m_Data.Edges.Remove(halfEdge.PrevEdge);
                                m_Data.Faces.Remove(halfEdge.Face);
                                break;
                            }
                        }
                    }
                }
            }

            // 删除旧的相关联的面

            foreach (HalfEdgeFace face in relationFaces)
            {
                m_Data.Vertices.Remove(face.Edge.Vertex);
                m_Data.Vertices.Remove(face.Edge.NextEdge.Vertex);
                m_Data.Vertices.Remove(face.Edge.PrevEdge.Vertex);
                m_Data.Edges.Remove(face.Edge);
                m_Data.Edges.Remove(face.Edge.NextEdge);
                m_Data.Edges.Remove(face.Edge.PrevEdge);
                m_Data.Faces.Remove(face);
            }

            // 添加新边数据
            foreach (HalfEdgeVertex v in tmpData.Vertices)
                m_Data.Vertices.Add(v);
            foreach (HalfEdge e in tmpData.Edges)
                m_Data.Edges.Add(e);
            foreach (HalfEdgeFace f in tmpData.Faces)
                m_Data.Faces.Add(f);

            // 标记区域
            HalfEdgeFace target = FindFace(triangle);
            if (target != null)
            {
                Debug.LogWarning("set ob");
                target.Area = AreaType.Obstacle;
            }
            else
            {
                Debug.LogError("not find");
            }

            // 移动障碍物时需要 把旧的关联的区域和新区域合并

            XNavMeshList<TriangleArea> triangles = HalfEdgeUtility.HalfEdgeToTriangle(m_Data);
            for (int i = 0; i < triangles.Count; i++)
            {
                TriangleArea origin = triangles[i];
                triangles[i] = new TriangleArea(m_Normalizer.UnNormalize(origin.Shape), origin.Area);
            }

            return triangles;
        }

        public List<XVector2> Remove(Triangle triangle)
        {
            triangle = m_Normalizer.Normalize(triangle);

            List<XVector2> result = new List<XVector2>();

            // 找出三角形所在的面
            HalfEdgeFace face = InnerFindFace(triangle);

            // 从一个起始面开始绕着所在的面循环
            HalfEdgeFace startFace;
            HalfEdge o_e1 = face.Edge.OppositeEdge;
            HalfEdge o_e2 = face.Edge.NextEdge.OppositeEdge;
            HalfEdge o_e3 = face.Edge.NextEdge.NextEdge.OppositeEdge;

            if (o_e1 == null)
            {
                if (o_e2 == null)
                    startFace = o_e3.Face;
                else
                    startFace = o_e2.Face;
            }
            else if (o_e2 == null)
            {
                startFace = o_e3.Face;
            }
            else
            {
                startFace = o_e1.Face;
            }

            HalfEdgeFace cur = startFace;
            while (cur != null)
            {
                XVector2 p1 = cur.Edge.Vertex.Position;
                XVector2 p2 = cur.Edge.NextEdge.Vertex.Position;
                XVector2 p3 = cur.Edge.PrevEdge.Vertex.Position;

                bool p1Has = triangle.Has(p1);
                bool p2Has = triangle.Has(p2);
                bool p3Has = triangle.Has(p3);

                HalfEdge nextEdge = null;

                if (!p1Has)
                {
                    if (!result.Contains(p1))
                    {
                        result.Add(p1);
                    }
                }
                else
                {
                    if (!p3Has)
                        nextEdge = cur.Edge;
                    else
                        nextEdge = cur.Edge.PrevEdge;
                }

                if (!p2Has)
                {
                    if (!result.Contains(p2))
                    {
                        result.Add(p2);
                    }
                }
                else
                {
                    if (!p1Has)
                        nextEdge = cur.Edge.NextEdge;
                    else
                        nextEdge = cur.Edge;
                }

                if (!p3Has)
                {
                    if (!result.Contains(p3))
                    {
                        result.Add(p3);
                    }
                }
                else
                {
                    if (!p2Has)
                        nextEdge = cur.Edge.PrevEdge;
                    else
                        nextEdge = cur.Edge.NextEdge;
                }

                if (nextEdge != null)
                {
                    cur = nextEdge.OppositeEdge.Face;
                    if (cur == startFace)
                        cur = null;
                }
                else
                {
                    cur = null;
                }
            }

            m_Normalizer.UnNormalize(result);
            result.Reverse();
            return result;
        }

        public void Add(List<XVector2> points)
        {
            foreach (XVector2 point in points)
                Add(point);
        }

        /// <summary>
        /// 添加一个点
        /// </summary>
        /// <param name="newPoint"></param>
        public void Add(XVector2 newPoint)
        {
            XVector2 normalize = m_Normalizer.Normalize(newPoint);
            DelaunayIncrementalSloan.InsertNewPointInTriangulation(normalize, m_Data);
        }

        public void AddConstraint(List<XVector2> contraint)
        {
            m_Normalizer.Normalize(contraint);
            ConstrainedDelaunaySloan.AddConstraints(m_Data, contraint, true);
        }

        private HalfEdgeFace InnerFindFace(Triangle triangle)
        {
            foreach (HalfEdgeFace face in m_Data.Faces)
            {
                if (triangle.Equals(face))
                    return face;
            }
            return null;
        }

        private void Initialize()
        {
            Triangle superTriangle = GeometryUtility.SuperTriangle;
            m_Data.AddTriangle(superTriangle);

            XVector2 min = m_AABB.Min;
            XVector2 max = m_AABB.Max;
            Add(new XVector2(min.X, min.Y));
            Add(new XVector2(min.X, max.Y));
            Add(new XVector2(max.X, min.Y));
            Add(new XVector2(max.X, max.Y));

            DelaunayIncrementalSloan.RemoveSuperTriangle(superTriangle, m_Data);
        }

        public XNavMeshList<TriangleArea> ToTriangles()
        {
            XNavMeshList<TriangleArea> triangles = HalfEdgeUtility.HalfEdgeToTriangle(m_Data);

            for (int i = 0; i < triangles.Count; i++)
            {
                TriangleArea origin = triangles[i];
                triangles[i] = new TriangleArea(m_Normalizer.UnNormalize(origin.Shape), origin.Area);
            }

            return triangles;
        }
    }
}
