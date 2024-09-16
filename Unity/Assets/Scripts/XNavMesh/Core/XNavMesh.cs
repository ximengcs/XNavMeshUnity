using UnityEngine;
using System.Collections.Generic;

namespace XFrame.PathFinding
{
    public partial class XNavMesh
    {
        private AABB m_AABB;
        private Normalizer m_Normalizer;
        private HalfEdgeData m_Data;
        private List<XVector2> m_Rect;

        public AABB AABB => m_AABB;

        public Normalizer Normalizer => m_Normalizer;

        public XNavMesh(AABB aabb)
        {
            m_AABB = aabb;
            m_Normalizer = new Normalizer(aabb);
            m_Data = new HalfEdgeData();
            m_Rect = new List<XVector2>();
            Initialize();
        }

        private void Initialize()
        {
            Triangle superTriangle = GeometryUtility.SuperTriangle;
            m_Data.AddTriangle(superTriangle);

            XVector2 min = m_AABB.Min;
            XVector2 max = m_AABB.Max;
            m_Rect.Add(new XVector2(min.X, min.Y));
            m_Rect.Add(new XVector2(min.X, max.Y));
            m_Rect.Add(new XVector2(max.X, max.Y));
            m_Rect.Add(new XVector2(max.X, min.Y));
            Add(new XVector2(min.X, min.Y));
            Add(new XVector2(min.X, max.Y));
            Add(new XVector2(max.X, min.Y));
            Add(new XVector2(max.X, max.Y));

            DelaunayIncrementalSloan.RemoveSuperTriangle(superTriangle, m_Data);
        }

        public void CheckDataValid()
        {
            if (m_Data.CheckValid())
            {
                Debug.Log($"data is valid {m_Data.Faces.Count}");
            }
        }

        public void Test()
        {
            Test(m_Data, Normalizer);
        }

        public static void Test(XNavMesh nav, Normalizer normalizer)
        {
            Test(nav.m_Data, normalizer);
        }
        public static void Test(HalfEdgeData data, Normalizer normalizer)
        {
            Debug.Log($"Edge count {data.Edges.Count}");
            Debug.Log($"Face count {data.Faces.Count}");

            foreach (HalfEdgeFace face in data.Faces)
            {
                DebugUtility.Print(face, normalizer);
            }
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

        private HashSet<HalfEdgeFace> InnerFindRelationFaces(List<XVector2> points, HashSet<HalfEdgeFace> result = null)
        {
            HashSet<HalfEdgeFace> faces = result != null ? result : new HashSet<HalfEdgeFace>();
            foreach (HalfEdgeFace face in m_Data.Faces)
            {
                Triangle triangle = new Triangle(face);
                for (int i = 0; i < points.Count; i++)
                {
                    XVector2 p1 = points[i];
                    XVector2 p2 = points[(i + 1) % points.Count];
                    if (triangle.Intersect(p1, p2))
                    {
                        if (!faces.Contains(face))
                            faces.Add(face);
                    }
                }
            }
            return faces;
        }

        private static HashSet<HalfEdgeFace> InnerFindContainsFaces(HalfEdgeData data, List<XVector2> edgeList, HashSet<HalfEdgeFace> result = null)
        {
            HashSet<HalfEdgeFace> faces = result != null ? result : new HashSet<HalfEdgeFace>();
            foreach (HalfEdgeFace face in data.Faces)
            {
                HalfEdge e1 = face.Edge;
                HalfEdge e2 = e1.NextEdge;
                HalfEdge e3 = e2.NextEdge;

                for (int i = 0; i < edgeList.Count; i++)
                {
                    Edge edge = new Edge(edgeList[i], edgeList[(i + 1) % edgeList.Count]);
                    if (e1.EqualsEdge(edge))
                    {
                        faces.Add(face);
                        break;
                    }
                    if (e2.EqualsEdge(edge))
                    {
                        faces.Add(face);
                        break;
                    }
                    if (e3.EqualsEdge(edge))
                    {
                        faces.Add(face);
                        break;
                    }
                }
            }
            return faces;
        }

        public bool ChangeWithExtraData(Triangle triangle, Triangle tarTriangle, out Triangle newTriangle, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            newAreaData = null;
            newAreaOutEdges = null;
            newTriangle = triangle;

            tarTriangle = AABB.Constraint(tarTriangle);
            if (tarTriangle.Equals(triangle))
            {
                return false;
            }

            tarTriangle = m_Normalizer.Normalize(tarTriangle);
            triangle = m_Normalizer.Normalize(triangle);

            AreaType areaType = InnerGetTriangleAreaType(triangle);
            HashSet<HalfEdgeFace> relationFaces = InnerFindRelationFaces(triangle);
            InnerFindRelationFaces(tarTriangle, relationFaces);
            newAreaOutEdges = InnerGetEdgeList2(triangle, tarTriangle, relationFaces);
            if (newAreaOutEdges.Count < 3)
            {
                return false;
            }

            newAreaData = GenerateHalfEdgeData2(newAreaOutEdges, true, tarTriangle.ToPoints());
            InnerReplaceHalfEdgeData(newAreaOutEdges, relationFaces, newAreaData);
            InnerSetTriangleAreaType(tarTriangle, areaType);
            newTriangle = m_Normalizer.UnNormalize(tarTriangle);
            return true;
        }

        public bool MoveWithExtraData(Triangle triangle, XVector2 offset, out Triangle newTriangle, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            return ChangeWithExtraData(triangle, triangle + offset, out newTriangle, out newAreaData, out newAreaOutEdges);
        }

        public void RemoveWithExtraData(Triangle triangle, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            triangle = m_Normalizer.Normalize(triangle);
            HashSet<HalfEdgeFace> relationFaces = InnerFindRelationFaces(triangle);
            newAreaOutEdges = InnerGetEdgeList(triangle, relationFaces);
            newAreaData = GenerateHalfEdgeData2(newAreaOutEdges);
            InnerReplaceHalfEdgeData(newAreaOutEdges, relationFaces, newAreaData);
        }

        public void AddWithExtraData(Triangle triangle, AreaType area, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            triangle = m_Normalizer.Normalize(triangle);
            HashSet<HalfEdgeFace> relationFaces = InnerFindRelationFaces(triangle);

            newAreaOutEdges = InnerGetEdgeList(triangle, relationFaces);

            Debug.LogWarning("relation edges");
            foreach (Edge edge in newAreaOutEdges)
            {
                DebugUtility.Print(edge.P1);
            }
            Debug.LogWarning("-------------------");

            newAreaData = GenerateHalfEdgeData2(newAreaOutEdges, true, triangle.ToPoints());
            InnerReplaceHalfEdgeData(newAreaOutEdges, relationFaces, newAreaData);

            // 标记区域
            InnerSetTriangleAreaType(triangle, area);
        }

        public void AddWithExtraData(List<XVector2> points, AreaType area, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            m_Normalizer.Normalize(points);
            HashSet<HalfEdgeFace> relationFaces = InnerFindRelationFaces(points);
            newAreaOutEdges = InnerGetEdgeList3(relationFaces);

            Debug.LogWarning("edge----------");
            foreach (Edge edge in newAreaOutEdges)
            {
                DebugUtility.Print(edge.P1);
            }
            newAreaData = GenerateHalfEdgeData2(newAreaOutEdges, false, points);

            // 标记区域
            foreach (HalfEdgeFace face in InnerFindContainsFaces(newAreaData, points))
                face.Area = area;

            InnerReplaceHalfEdgeData(newAreaOutEdges, relationFaces, newAreaData);
        }

        public void AddConstraint(List<XVector2> points)
        {
            Normalizer.Normalize(points);
            foreach (XVector2 point in points)
            {
                bool find = false;
                foreach (HalfEdgeVertex vert in m_Data.Vertices)
                {
                    if (vert.Position.Equals(point))
                    {
                        find = true;
                        break;
                    }
                }

                if (!find)
                {
                    Debug.LogWarning($"add111 {point}");
                    InnerAdd(point);
                }
            }
            ConstrainedDelaunaySloan.AddConstraints(m_Data, points, false);
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
                if (e1.NextEdge.OppositeEdge == null) TryAddEdgeList(e1, edgeList);
                if (e2.NextEdge.OppositeEdge == null) TryAddEdgeList(e2, edgeList);
                if (e3.NextEdge.OppositeEdge == null) TryAddEdgeList(e3, edgeList);

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
                if (e1.NextEdge.OppositeEdge == null) TryAddEdgeList(e1, edgeList);
                if (e2.NextEdge.OppositeEdge == null) TryAddEdgeList(e2, edgeList);
                if (e3.NextEdge.OppositeEdge == null) TryAddEdgeList(e3, edgeList);

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

        private List<Edge> InnerGetEdgeList3(HashSet<HalfEdgeFace> faces)
        {
            List<Edge> edgeList = new List<Edge>();

            HalfEdge startEdge = null;
            // 找一个没有临边的边，从这个边开始迭代
            foreach (HalfEdgeFace face in faces)
            {
                HalfEdge e1 = face.Edge;
                HalfEdge e2 = e1.NextEdge;
                HalfEdge e3 = e2.NextEdge;

                if (e1.OppositeEdge == null || !faces.Contains(e1.OppositeEdge.Face))
                {
                    startEdge = e1;
                    break;
                }

                if (e2.OppositeEdge == null || !faces.Contains(e2.OppositeEdge.Face))
                {
                    startEdge = e2;
                    break;
                }


                if (e3.OppositeEdge == null || !faces.Contains(e3.OppositeEdge.Face))
                {
                    startEdge = e3;
                    break;
                }
            }

            int count = 0;
            HalfEdge current = startEdge;
            do
            {
                if (count++ >= 100)
                {
                    Debug.LogError("error");
                    break;
                }

                // 找到所有下一条边(因为临边不在面列表里，所以只需要从一个方向找)
                List<HalfEdge> nextEdges = new List<HalfEdge>();
                HalfEdge tmp = current.NextEdge.OppositeEdge;
                while (tmp != null)
                {
                    nextEdges.Add(tmp);
                    tmp = tmp.NextEdge.OppositeEdge;
                }

                // 因为是同向，所以只需要计算角度最大的边即可, 只需计算单位向量点积
                Edge curE = new Edge(current.Vertex.Position, current.NextEdge.Vertex.Position);
                XVector2 n1 = XVector2.Normalize(curE.P2 - curE.P1);

                float d = 1f;
                foreach (HalfEdge e in nextEdges)
                {
                    XVector2 p1 = e.Vertex.Position;
                    XVector2 p2 = e.NextEdge.Vertex.Position;
                    if (!p1.Equals(curE.P1))
                    {
                        Debug.LogError("error happen");
                    }

                    XVector2 n2 = XVector2.Normalize(p2 - curE.P1);
                    float tmpD = XMath.Dot(n1, n2);
                    if (tmpD < d)
                    {
                        d = tmpD;
                        current = e;
                    }
                }

                edgeList.Add(new Edge(current.Vertex.Position, current.NextEdge.Vertex.Position));
                current = current.NextEdge;
            } while (current != startEdge);

            return edgeList;
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
            }
        }

        private List<Edge> InnerSortEdge(List<Edge> edgeList)
        {
            int sortCount = 0;
            List<Edge> sortEdge = new List<Edge>();
            int targetCount = edgeList.Count;
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

                        // 检查三个点是否在同一条线上，并且处于边缘，如果是，则移除掉中间的点
                        sortCount = sortEdge.Count;
                        if (sortCount >= 3)
                        {
                            XVector2 p1 = sortEdge[sortCount - 1].P1;
                            XVector2 p2 = sortEdge[sortCount - 2].P1;
                            XVector2 p3 = sortEdge[sortCount - 3].P1;
                            if (XMath.CheckPointsOnLine(p1, p2, p3) && AABB.InSide(Normalizer.UnNormalize(p1)))
                            {
                                sortEdge.RemoveAt(sortCount - 2);
                                targetCount--;
                            }
                        }

                        break;
                    }
                }
            } while (curEdge != null && sortEdge.Count < targetCount);


            // 因为是循环列表，需要检查元素的前两个(至少4个才需要检查)
            sortCount = sortEdge.Count;
            if (sortCount >= 4)
            {
                XVector2 p1 = sortEdge[0].P1;
                XVector2 p2 = sortEdge[sortCount - 1].P1;
                XVector2 p3 = sortEdge[sortCount - 2].P1;
                if (XMath.CheckPointsOnLine(p1, p2, p3) && AABB.InSide(Normalizer.UnNormalize(p1)))
                    sortEdge.RemoveAt(sortCount - 1);

                sortCount = sortEdge.Count;
                if (sortCount >= 4)
                {
                    p1 = sortEdge[1].P1;
                    p2 = sortEdge[0].P1;
                    p3 = sortEdge[sortCount - 1].P1;
                    if (XMath.CheckPointsOnLine(p1, p2, p3) && AABB.InSide(Normalizer.UnNormalize(p1)))
                        sortEdge.RemoveAt(0);
                }
            }

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
                                halfEdge = halfEdge.NextEdge;
                                // 找到了需要替换的边
                                if (halfEdge.OppositeEdge != null)
                                {
                                    halfEdge.OppositeEdge.OppositeEdge = e.NextEdge;
                                }

                                e.NextEdge.OppositeEdge = halfEdge.OppositeEdge;

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

        public HalfEdgeData GenerateHalfEdgeData2(List<Edge> edgeList, bool removeEdgeConstraint = true, List<XVector2> extraPoints = null)
        {
            HalfEdgeData tmpData = new HalfEdgeData();
            Triangle superTriangle = GeometryUtility.SuperTriangle;
            tmpData.AddTriangle(superTriangle);
            foreach (Edge e in edgeList)
                DelaunayIncrementalSloan.InsertNewPointInTriangulation(e.P1, tmpData);

            List<XVector2> tmpList = new List<XVector2>();  // TO DO 
            foreach (Edge e in edgeList)
                tmpList.Add(e.P1);
            DelaunayIncrementalSloan.RemoveSuperTriangle(superTriangle, tmpData);

            if (extraPoints != null)
            {
                foreach (XVector2 v in extraPoints)
                {
                    // TO DO 剔除重复的
                    bool find = false;
                    foreach (XVector2 tmp in tmpList)
                    {
                        if (tmp.Equals(v))
                        {
                            find = true;
                            break;
                        }
                    }

                    if (!find)
                    {
                        DelaunayIncrementalSloan.InsertNewPointInTriangulation(v, tmpData);
                    }
                }
                ConstrainedDelaunaySloan.AddConstraints(tmpData, extraPoints, false);
            }

            ConstrainedDelaunaySloan.AddConstraints(tmpData, tmpList, removeEdgeConstraint);

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
            XNavMeshList<TriangleArea> triangles = new XNavMeshList<TriangleArea>(8);
            foreach (HalfEdgeFace face in data.Faces)
            {
                triangles.Add(new TriangleArea(face, navMesh.m_Normalizer));
            }

            return triangles;
        }

        public XNavMeshList<TriangleArea> ToTriangles()
        {
            return ToTriangles(this, m_Data);
        }
    }
}
