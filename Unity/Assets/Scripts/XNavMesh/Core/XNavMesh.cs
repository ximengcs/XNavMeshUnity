using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Runtime.ConstrainedExecution;

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

        /// <summary>
        /// 添加一个三角形区域
        /// </summary>
        /// <param name="triangle">三角形(非归一化)</param>
        /// <param name="area">区域类型</param>
        public void Add(Triangle triangle, AreaType area)
        {
            triangle = m_Normalizer.Normalize(triangle);
            InnerAdd(triangle.P1);
            InnerAdd(triangle.P2);
            InnerAdd(triangle.P3);
            HalfEdgeFace face = InnerFindFace(triangle);
            face.Area = area;
        }

        /// <summary>
        /// 寻找与三角形相关联的面
        /// </summary>
        /// <param name="triangle">三角形(归一化)</param>
        /// <returns>相关联的面集合</returns>
        private HashSet<HalfEdgeFace> InnerFindRelationFaces(Triangle triangle, HashSet<HalfEdgeFace> result = null)
        {
            HashSet<HalfEdgeFace> faces = result != null ? result : new HashSet<HalfEdgeFace>();
            foreach (HalfEdgeFace face in m_Data.Faces)
            {
                if (triangle.Intersect(face))
                {
                    if (!faces.Contains(face))
                        faces.Add(face);
                }
            }
            return faces;
        }

        public Triangle MoveWithExtraData(Triangle triangle, XVector2 offset, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            Debug.LogWarning($"AABB {AABB} ");
            Triangle newTriangle = triangle + offset;
            Debug.LogWarning($"new tri {newTriangle} ");
            newTriangle = AABB.Constraint(newTriangle);
            Debug.LogWarning($"new tri to {newTriangle} ");
            newTriangle = m_Normalizer.Normalize(newTriangle);
            triangle = m_Normalizer.Normalize(triangle);
            offset = m_Normalizer.Normalize(offset);
            AreaType areaType = InnerGetTriangleAreaType(triangle);
            HashSet<HalfEdgeFace> relationFaces = InnerFindRelationFaces(triangle);
            InnerFindRelationFaces(newTriangle, relationFaces);
            Debug.LogWarning("face start--------");
            foreach (HalfEdgeFace face in relationFaces)
            {
                Debug.LogWarning($"{face}");
            }
            Debug.LogWarning("face end--------");

            newAreaOutEdges = InnerGetEdgeList2(triangle, newTriangle, relationFaces);
            Debug.LogWarning("edge start--------");
            foreach (Edge edge in newAreaOutEdges)
            {
                Debug.LogWarning($"{edge.P1} -> {edge.P2} ");
            }
            Debug.LogWarning("edge end--------");

            newAreaData = GenerateHalfEdgeData(newAreaOutEdges, newTriangle.ToPoints());
            InnerReplaceHalfEdgeData(newAreaOutEdges, relationFaces, newAreaData);
            InnerSetTriangleAreaType(newTriangle, areaType);
            return m_Normalizer.UnNormalize(newTriangle);
        }

        public void RemoveWithExtraData(Triangle triangle, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            triangle = m_Normalizer.Normalize(triangle);
            HashSet<HalfEdgeFace> relationFaces = InnerFindRelationFaces(triangle);
            newAreaOutEdges = InnerGetEdgeList(triangle, relationFaces);
            newAreaData = GenerateHalfEdgeData(newAreaOutEdges);
            InnerReplaceHalfEdgeData(newAreaOutEdges, relationFaces, newAreaData);
        }

        public void AddWithExtraData(Triangle triangle, AreaType area, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            triangle = m_Normalizer.Normalize(triangle);
            HashSet<HalfEdgeFace> relationFaces = InnerFindRelationFaces(triangle);
            newAreaOutEdges = InnerGetEdgeList(triangle, relationFaces);
            newAreaData = GenerateHalfEdgeData(newAreaOutEdges, triangle.ToPoints());
            InnerReplaceHalfEdgeData(newAreaOutEdges, relationFaces, newAreaData);

            // 标记区域
            InnerSetTriangleAreaType(triangle, area);
        }

        private AreaType InnerGetTriangleAreaType(Triangle triangle)
        {
            HalfEdgeFace target = InnerFindFace(triangle);
            return target != null ? target.Area : AreaType.None;
        }

        private void InnerSetTriangleAreaType(Triangle triangle, AreaType areaType)
        {
            HalfEdgeFace target = InnerFindFace(triangle);
            if (target != null)
            {
                target.Area = areaType;
            }
            else
            {
                Debug.LogError("not find new triangle");
            }
        }

        private List<Edge> InnerGetEdgeList(Triangle triangle, HashSet<HalfEdgeFace> faces)
        {
            List<Edge> edgeList = new List<Edge>();
            foreach (HalfEdgeFace face in faces)
            {
                HalfEdge e1 = face.Edge;
                HalfEdge e2 = e1.NextEdge;
                HalfEdge e3 = e1.PrevEdge;

                // 边界必然加入
                if (e1.OppositeEdge == null) TryAddEdgeList(e1, edgeList);
                if (e2.OppositeEdge == null) TryAddEdgeList(e2, edgeList);
                if (e3.OppositeEdge == null) TryAddEdgeList(e3, edgeList);

                // 与三角形不相交且在外面则加入
                InnerCheckTriangleOut(triangle, e1, edgeList);
                InnerCheckTriangleOut(triangle, e2, edgeList);
                InnerCheckTriangleOut(triangle, e3, edgeList);
            }

            return InnerSortEdge(edgeList);
        }

        private List<Edge> InnerGetEdgeList2(Triangle triangle, Triangle triangle2, HashSet<HalfEdgeFace> faces)
        {
            List<Edge> edgeList = new List<Edge>();
            foreach (HalfEdgeFace face in faces)
            {
                HalfEdge e1 = face.Edge;
                HalfEdge e2 = e1.NextEdge;
                HalfEdge e3 = e1.PrevEdge;

                // 边界必然加入
                if (e1.OppositeEdge == null) TryAddEdgeList(e1, edgeList);
                if (e2.OppositeEdge == null) TryAddEdgeList(e2, edgeList);
                if (e3.OppositeEdge == null) TryAddEdgeList(e3, edgeList);

                // 与三角形不相交且在外面则加入
                if (XMath.CheckLineOutOfTriangle(triangle, e1))
                {
                    InnerCheckTriangleOut(triangle2, e1, edgeList);
                }
                if (XMath.CheckLineOutOfTriangle(triangle, e2))
                {
                    InnerCheckTriangleOut(triangle2, e2, edgeList);
                }
                if (XMath.CheckLineOutOfTriangle(triangle, e3))
                {
                    InnerCheckTriangleOut(triangle2, e3, edgeList);
                }
            }

            return InnerSortEdge(edgeList);
        }

        private void InnerCheckTriangleOut(Triangle triangle, HalfEdge e, List<Edge> edgeList)
        {
            if (XMath.CheckLineOutOfTriangle(triangle, e))
            {
                TryAddEdgeList(e, edgeList);
            }
        }

        private void TryAddEdgeList(HalfEdge e, List<Edge> edgeList)
        {
            Edge cur = e.ToEdge();
            Edge tar = null;
            foreach (Edge tmp in edgeList)
            {
                if (tmp == cur)
                {
                    tar = tmp;
                    break;
                }
            }

            if (tar == null)
            {
                edgeList.Add(cur);
                Debug.LogWarning($"add -> [{cur.P1} {cur.P2}] {m_Normalizer.UnNormalize(cur.P1)} {m_Normalizer.UnNormalize(cur.P2)} ");
            }
        }

        private List<Edge> InnerSortEdge(List<Edge> edgeList)
        {
            List<Edge> sortEdge = new List<Edge>();
            Edge curEdge = edgeList[0];
            do
            {
                Edge tmp = curEdge;
                curEdge = null;
                foreach (Edge e in edgeList)
                {
                    if (e.P2.Equals(tmp.P1))
                    {
                        curEdge = e;
                        sortEdge.Add(e);
                        break;
                    }
                }
            } while (curEdge != null && sortEdge.Count < edgeList.Count);
            return sortEdge;
        }


        private void InnerReplaceHalfEdgeData(List<Edge> edgeList, HashSet<HalfEdgeFace> relationFaces, HalfEdgeData tmpData)
        {
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
        }

        public HalfEdgeData GenerateHalfEdgeData(List<Edge> edgeList, List<XVector2> extraPoints = null)
        {
            HalfEdgeData tmpData = new HalfEdgeData();
            Triangle superTriangle = GeometryUtility.SuperTriangle;
            tmpData.AddTriangle(superTriangle);
            foreach (Edge e in edgeList)
                DelaunayIncrementalSloan.InsertNewPointInTriangulation(e.P1, tmpData);
            // 添加Constraint以剪切形状
            List<XVector2> tmpList = new List<XVector2>();  // TO DO 
            foreach (Edge e in edgeList)
                tmpList.Add(e.P1);

            if (extraPoints != null)
            {
                foreach (XVector2 v in extraPoints)
                {
                    DelaunayIncrementalSloan.InsertNewPointInTriangulation(v, tmpData);
                }
            }

            ConstrainedDelaunaySloan.AddConstraints(tmpData, extraPoints, false);
            ConstrainedDelaunaySloan.AddConstraints(tmpData, tmpList, true);
            DelaunayIncrementalSloan.RemoveSuperTriangle(superTriangle, tmpData);
            return tmpData;
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
            InnerAdd(m_Normalizer.Normalize(newPoint));
        }

        /// <summary>
        /// 添加一个点(归一化)
        /// </summary>
        /// <param name="newPoint">点(归一化)</param>
        private void InnerAdd(XVector2 point)
        {
            DelaunayIncrementalSloan.InsertNewPointInTriangulation(point, m_Data);
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

        public static XNavMeshList<TriangleArea> ToTriangles(XNavMesh navMesh, HalfEdgeData data)
        {
            XNavMeshList<TriangleArea> triangles = HalfEdgeUtility.HalfEdgeToTriangle(data);

            for (int i = 0; i < triangles.Count; i++)
            {
                TriangleArea origin = triangles[i];
                triangles[i] = new TriangleArea(navMesh.m_Normalizer.UnNormalize(origin.Shape), origin.Area);
            }

            return triangles;
        }

        public XNavMeshList<TriangleArea> ToTriangles()
        {
            return ToTriangles(this, m_Data);
        }
    }
}
