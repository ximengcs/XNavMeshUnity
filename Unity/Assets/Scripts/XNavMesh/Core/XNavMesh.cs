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

        public XNavMeshList<TriangleArea> Add2(Triangle triangle, AreaType area, out List<XVector2> edges)
        {
            triangle = m_Normalizer.Normalize(triangle);
            XNavMeshList<TriangleArea> re = new XNavMeshList<TriangleArea>(8);
            List<HalfEdgeFace> relationFaces = FindRelationFaces(triangle);
            foreach (HalfEdgeFace face in relationFaces)
            {
                re.Add(new TriangleArea(m_Normalizer.UnNormalize(new Triangle(face)), AreaType.Walk));
            }

            edges = new List<XVector2>();
            foreach (HalfEdgeFace face in relationFaces)
            {
                HalfEdge e1 = face.Edge;
                HalfEdge e2 = e1.NextEdge;
                HalfEdge e3 = e1.PrevEdge;
                Debug.LogWarning($" tri {m_Normalizer.UnNormalize(e1.Vertex.Position)} {m_Normalizer.UnNormalize(e2.Vertex.Position)} {m_Normalizer.UnNormalize(e3.Vertex.Position)} ");
                if (XMath.CheckTriangleLineSameDir(triangle, e1))
                {
                    edges.Add(m_Normalizer.UnNormalize(e1.Vertex.Position));
                    //Debug.Log($"a1 {m_Normalizer.UnNormalize(e1.Vertex.Position)}");
                }
                if (XMath.CheckTriangleLineSameDir(triangle, e2))
                {
                    edges.Add(m_Normalizer.UnNormalize(e2.Vertex.Position));
                    //Debug.Log($"a2 {m_Normalizer.UnNormalize(e2.Vertex.Position)}");
                }
                if (XMath.CheckTriangleLineSameDir(triangle, e3))
                {
                    edges.Add(m_Normalizer.UnNormalize(e3.Vertex.Position));
                    //Debug.Log($"a3 {m_Normalizer.UnNormalize(e3.Vertex.Position)}");
                }
            }

            Debug.LogWarning(edges.Count);
            Debug.LogWarning(re.Count);
            foreach (XVector2 v in edges)
                ;// Debug.LogWarning(v);
            return re;

            HashSet<HalfEdgeFace> faces = new HashSet<HalfEdgeFace>();

            // 找出所有在三角形内的点所在的三角形
            foreach (HalfEdgeVertex vert in m_Data.Vertices)
            {
                if (triangle.Contains(vert.Position))
                {
                    if (!faces.Contains(vert.Edge.Face))
                        faces.Add(vert.Edge.Face);
                }
            }

            // 所有边界的点, 需为逆时针以方便添加Constraint
            List<XVector2> edgePoints = new List<XVector2>();
            List<HalfEdgeFace> edgeFaces = new List<HalfEdgeFace>();
            // 计算所有三角形边合并的多边形
            foreach (HalfEdgeFace face in faces)
            {
                XVector2 p1 = face.Edge.Vertex.Position;
                XVector2 p2 = face.Edge.NextEdge.Vertex.Position;
                XVector2 p3 = face.Edge.PrevEdge.Vertex.Position;

                if (triangle.Has(p1) && triangle.Has(p2) && triangle.Has(p3))
                    continue;

                edgeFaces.Add(face);
            }

            Debug.Log($" triangle {triangle} ");

            // 如果目标三角形内有其他点，那么需要把点相关的面也添加进来，并且之后要在原数据中删除这些点
            List<XVector2> willDeletePoints = new List<XVector2>();
            XVector2 tmpPoint = default;
            HalfEdge tmpEdge = edgeFaces[0].Edge;
            do
            {
                XVector2 p1 = tmpEdge.Vertex.Position;
                XVector2 p2 = tmpEdge.NextEdge.Vertex.Position;
                XVector2 p3 = tmpEdge.PrevEdge.Vertex.Position;

                if (triangle.Contains(p1))
                {
                    tmpPoint = p2;
                    if (!edgePoints.Contains(p2))
                    {
                        if (!triangle.Contains(p2)) // 目标三角形中包含这个点
                        {
                            edgePoints.Add(p2);
                        }
                        else
                        {
                            willDeletePoints.Add(p2);
                        }
                    }

                    tmpEdge = tmpEdge.NextEdge.OppositeEdge;
                }
                else if (triangle.Contains(p2))
                {
                    tmpPoint = p3;
                    if (!edgePoints.Contains(p3))
                    {
                        if (!triangle.Contains(p3))
                            edgePoints.Add(p3);
                        else
                            willDeletePoints.Add(p3);
                    }

                    tmpEdge = tmpEdge.PrevEdge.OppositeEdge;
                }
                else if (triangle.Contains(p3))
                {
                    tmpPoint = p1;
                    if (!edgePoints.Contains(p1))
                    {
                        if (!triangle.Contains(p1))
                            edgePoints.Add(p1);
                        else
                            willDeletePoints.Add(p1);
                    }

                    tmpEdge = tmpEdge.OppositeEdge;
                }

                // count == 0 : 第一个节点在目标三角形内，需要剔除掉
                // count == 1 : 第一个合法的节点
            }
            while (edgePoints.Count <= 1 || (edgePoints.Count > 1 && !edgePoints[0].Equals(tmpPoint)));

            // 临时半边数据用于构建三角形涉及的区域
            HalfEdgeData tmpData = new HalfEdgeData();
            Triangle superTriangle = GeometryUtility.SuperTriangle;
            tmpData.AddTriangle(superTriangle);

            // 将边界点添加到临时半边数据中
            foreach (XVector2 p in edgePoints)
            {
                DelaunayIncrementalSloan.InsertNewPointInTriangulation(p, tmpData);
            }

            // 添加三角形的三个点
            //DelaunayIncrementalSloan.InsertNewPointInTriangulation(triangle.P1, tmpData);
            //DelaunayIncrementalSloan.InsertNewPointInTriangulation(triangle.P2, tmpData);
            //DelaunayIncrementalSloan.InsertNewPointInTriangulation(triangle.P3, tmpData);

            // 添加Constraint以剪切形状
            ConstrainedDelaunaySloan.AddConstraints(tmpData, edgePoints, true);

            // 移除大三角形
            DelaunayIncrementalSloan.RemoveSuperTriangle(superTriangle, tmpData);

            // 替换结构的中三角形

            // 找出所有关联的边并移除旧边，添加新边
            for (int i = 0; i < edgePoints.Count; i++)
            {
                XVector2 from = edgePoints[i];
                XVector2 to = edgePoints[(i + 1) % edgePoints.Count];
                Edge edge = new Edge(from, to);

                foreach (HalfEdge e in tmpData.Edges)
                {
                    if (e.EqualsEdge(edge))
                    {
                        foreach (HalfEdgeFace face in faces)
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

            foreach (XVector2 p in willDeletePoints) // 删除和这些点关联的数据
            {
                Debug.Log($"remove {m_Normalizer.UnNormalize(p)}");
                foreach (HalfEdgeFace face in m_Data.Faces)
                {
                    if (face.Contains(p))
                    {
                        m_Data.Faces.Remove(face);

                        HalfEdge e1 = face.Edge;
                        HalfEdge e2 = e1.NextEdge;
                        HalfEdge e3 = e2.PrevEdge;
                        m_Data.Edges.Remove(e1);
                        m_Data.Edges.Remove(e2);
                        m_Data.Edges.Remove(e3);
                        m_Data.Vertices.Remove(e1.Vertex);
                        m_Data.Vertices.Remove(e2.Vertex);
                        m_Data.Vertices.Remove(e3.Vertex);
                        break;
                    }
                }
            }

            // 添加新边数据
            foreach (HalfEdgeVertex v in tmpData.Vertices)
                m_Data.Vertices.Add(v);
            foreach (HalfEdge e in tmpData.Edges)
                m_Data.Edges.Add(e);
            foreach (HalfEdgeFace f in tmpData.Faces)
                m_Data.Faces.Add(f);

            XNavMeshList<TriangleArea> triangles = HalfEdgeUtility.HalfEdgeToTriangle(tmpData);
            for (int i = 0; i < triangles.Count; i++)
            {
                TriangleArea origin = triangles[i];
                triangles[i] = new TriangleArea(m_Normalizer.UnNormalize(origin.Shape), origin.Area);
            }

            // 标记区域

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
