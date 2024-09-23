using UnityEngine;
using System.Collections.Generic;

namespace XFrame.PathFinding
{
    public partial class XNavMesh
    {
        public static int s_PolyId;

        private AABB m_AABB;
        private Normalizer m_Normalizer;
        private HalfEdgeData m_Data;
        private List<XVector2> m_Rect;
        private Dictionary<int, Poly> m_Polies;

        public AABB AABB => m_AABB;

        public Normalizer Normalizer => m_Normalizer;

        public XNavMesh(AABB aabb)
        {
            m_AABB = aabb;
            m_Normalizer = new Normalizer(aabb);
            m_Data = new HalfEdgeData();
            m_Rect = new List<XVector2>();
            m_Polies = new Dictionary<int, Poly>();

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
            Debug.Log($"Face count {data.Faces.Count}");
            return;
            List<Triangle> triangles = new List<Triangle>();

            foreach (HalfEdgeFace face in data.Faces)
            {
                DebugUtility.Print(face, normalizer);

                Triangle triangle = new Triangle(face);
                bool find = false;
                foreach (Triangle triangle2 in triangles)
                {
                    if (triangle2.Equals(triangle))
                        find = true;
                }
                if (!find)
                    triangles.Add(triangle);
                else
                {
                    Debug.LogError("same tri");
                }

                XVector2 p1 = normalizer.UnNormalize(face.Edge.Vertex.Position);
                XVector2 p2 = normalizer.UnNormalize(face.Edge.NextEdge.Vertex.Position);
                XVector2 p3 = normalizer.UnNormalize(face.Edge.PrevEdge.Vertex.Position);
                Transform tf = new GameObject().transform;
                Transform t1 = new GameObject().transform;
                Transform t2 = new GameObject().transform;
                Transform t3 = new GameObject().transform;
                t1.position = p1.ToUnityVec3();
                t2.position = p2.ToUnityVec3();
                t3.position = p3.ToUnityVec3();
                t1.SetParent(tf);
                t2.SetParent(tf);
                t3.SetParent(tf);
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
                Debug.LogWarning($"real InnerFindRelationFaces triangle {Normalizer.UnNormalize(triangle)} ");

                for (int i = 0; i < points.Count; i++)
                {
                    XVector2 p1 = points[i];
                    XVector2 p2 = points[(i + 1) % points.Count];
                    Debug.LogWarning($"real InnerFindRelationFaces {Normalizer.UnNormalize(p1)} {Normalizer.UnNormalize(p2)} {triangle.Intersect(p1, p2)} {triangle.Intersect2(p1, p2)} |||| {Normalizer.UnNormalize(triangle)} ");
                    if (triangle.Intersect2(p1, p2))
                    {
                        if (!faces.Contains(face))
                            faces.Add(face);
                    }
                }

                // 检查是否存在Poly中没有加入的面
                foreach (var entry in m_Polies)
                {
                    Poly poly = entry.Value;
                    if (poly.Contains(face))
                    {
                        foreach (HalfEdgeFace polyFace in poly.Faces)
                        {
                            if (!faces.Contains(polyFace))
                                faces.Add(polyFace);
                        }
                        break;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="newPoints">原始点(非归一化)</param>
        /// <param name="newAreaData"></param>
        /// <param name="newAreaOutEdges"></param>
        /// <returns></returns>
        public bool ChangeWithExtraData(Poly poly, List<XVector2> newPoints, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            List<XVector2> oldPoints = new List<XVector2>(poly.Points);
            if (!AABB.Contains(newPoints))
            {
                newAreaData = null;
                newAreaOutEdges = null;
                Debug.Log("rotate failure");
                return false;
            }
            return InnerChangeWithExtraData(poly, oldPoints, newPoints, out newAreaData, out newAreaOutEdges);
        }

        private bool InnerChangeWithExtraData(Poly poly, List<XVector2> oldPoints, List<XVector2> newPoints, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            newAreaData = null;
            newAreaOutEdges = null;
            if (newPoints[0].Equals(oldPoints[0]))  // 点位没有发生变化直接返回失败
                return false;

            Normalizer.Normalize(oldPoints);
            Normalizer.Normalize(newPoints);

            HashSet<HalfEdgeFace> relationFaces = new HashSet<HalfEdgeFace>();
            InnerFindRelationFaces(oldPoints, relationFaces);
            Debug.LogWarning("check new points intersect");
            InnerFindRelationFaces(newPoints, relationFaces);
            newAreaOutEdges = InnerGetEdgeList3(relationFaces);
            if (newAreaOutEdges.Count < 3)  //边界点小于3直接返回失败
                return false;

            Dictionary<Poly, List<XVector2>> relationlist = InnerFindRelationPolies(poly, newPoints, relationFaces, out List<List<XVector2>> relationAllPoints);

            HashSet<XVector2> newEdgeList = new HashSet<XVector2>();
            foreach (var entry in relationlist)
            {
                foreach (XVector2 point in entry.Value)
                {
                    if (!newEdgeList.Contains(point))
                        newEdgeList.Add(point);
                }
            }

            Debug.LogWarning($" newEdgeList ------------------------- {newEdgeList.Count} ");
            foreach (XVector2 point in newEdgeList)
            {
                Debug.LogWarning($" {Normalizer.UnNormalize(point)} ");
            }
            Debug.LogWarning($" ------------------------- after ");


            Debug.LogWarning($"newAreaOutEdges ------------------- {newAreaOutEdges.Count}");
            foreach (var e in newAreaOutEdges)
            {
                Debug.LogWarning($" {Normalizer.UnNormalize(e.P1)} ");
            }
            Debug.LogWarning("newAreaOutEdges--------------- after");


            // TO DO 当点落在旧边上时 需要根据点拆分边
            newAreaData = GenerateHalfEdgeData2(newAreaOutEdges, true, relationAllPoints);

            if (newAreaData.Faces.Count == 0)
            {
                Debug.LogWarning($" relationlist ---------------------------------- {relationlist.Count}");
                foreach (var entry in relationlist)
                {
                    Debug.LogWarning("==============================");
                    foreach (XVector2 v in entry.Value)
                    {
                        Debug.LogWarning($" {Normalizer.UnNormalize(v)} ");
                    }
                    List<XVector2> vs = new List<XVector2>(entry.Value);
                    Normalizer.UnNormalize(vs);
                    Test2.Inst.AddLines(vs);
                }
                Debug.LogWarning("--------------------------------------");
                Debug.LogWarning($"relationAllPoints -------------------------- {relationAllPoints.Count}");
                foreach (List<XVector2> l in relationAllPoints)
                {
                    Debug.LogWarning("=========================");
                    foreach (XVector2 l2 in l)
                    {
                        Debug.LogWarning($" {Normalizer.UnNormalize(l2)} ");
                    }
                }
                Debug.LogWarning($"relationAllPoints -------------------------- after");

                Debug.LogWarning($"polies--------");
                foreach (var item in m_Polies)
                {
                    Poly p = item.Value;
                    Debug.LogWarning($" poly id {p.Id} ");
                    foreach (XVector2 po in p.Points)
                    {
                        Debug.LogWarning($" point {po} ");
                    }
                }
                Debug.LogWarning($"polies--------after");

                Debug.LogWarning("poly points---------------");
                foreach (XVector2 p in poly.Points)
                {
                    Debug.LogWarning($" {p} ");
                }
                Debug.LogWarning("old points---------------after");

                Debug.LogWarning("old points---------------");
                foreach (XVector2 p in oldPoints)
                {
                    Debug.LogWarning($" {Normalizer.UnNormalize(p)} ");
                }
                Debug.LogWarning("old points---------------after");

                Debug.LogWarning("new points---------------");
                foreach (XVector2 p in newPoints)
                {
                    Debug.LogWarning($" {Normalizer.UnNormalize(p)} ");
                }
                Debug.LogWarning("new points---------------after");

                Debug.LogWarning($" newAreaData {newAreaData.Faces.Count} ----------------------");
                foreach (var f in newAreaData.Faces)
                {
                    DebugUtility.Print(f, Normalizer);
                }
                Debug.LogWarning($" newAreaData ---------------------- after");
            }

            // 标记区域
            foreach (var entry in relationlist)
            {
                Poly relationPoly = entry.Key;
                HashSet<HalfEdgeFace> faces = InnerFindContainsFaces(newAreaData, entry.Value);
                relationPoly.ResetFaceArea();
                relationPoly.SetFaces(faces);
            }

            InnerReplaceHalfEdgeData(newAreaOutEdges, relationFaces, newAreaData);

            Normalizer.UnNormalize(newPoints);  // 还原点
            return true;
        }

        public void RemoveWithExtraData(Triangle triangle, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            triangle = m_Normalizer.Normalize(triangle);
            HashSet<HalfEdgeFace> relationFaces = InnerFindRelationFaces(triangle);
            newAreaOutEdges = InnerGetEdgeList(triangle, relationFaces);
            newAreaData = GenerateHalfEdgeData2(newAreaOutEdges);
            InnerReplaceHalfEdgeData(newAreaOutEdges, relationFaces, newAreaData);
        }

        private Dictionary<Poly, List<XVector2>> InnerFindRelationPolies(Poly poly, List<XVector2> points, HashSet<HalfEdgeFace> relationFaces, out List<List<XVector2>> relationAllPoints)
        {
            Dictionary<Poly, List<XVector2>> relationlist = new Dictionary<Poly, List<XVector2>>();
            HashSet<Poly> relationPolies = new HashSet<Poly>();
            foreach (var entry in m_Polies)
            {
                Poly tmpPoly = entry.Value;
                if (tmpPoly != poly)
                {
                    foreach (HalfEdgeFace face in relationFaces)
                    {
                        if (!relationPolies.Contains(tmpPoly) && tmpPoly.Contains(face))
                        {
                            relationPolies.Add(tmpPoly);
                        }
                    }
                }
            }

            if (relationPolies.Count > 0) // 相交区域处理 
            {
                List<AreaCollection> allAreaList = new List<AreaCollection>();
                AreaCollection thisArea = new AreaCollection();
                thisArea.Add(poly, points);
                allAreaList.Add(thisArea);

                foreach (Poly tmpPoly in relationPolies)
                {
                    AreaCollection targetArea = null;
                    List<XVector2> tmpPoints = new List<XVector2>(tmpPoly.Points);
                    Normalizer.Normalize(tmpPoints);

                    foreach (AreaCollection area in allAreaList)
                    {
                        if (area.Intersect(tmpPoints))
                        {
                            targetArea = area;
                            break;
                        }
                    }

                    if (targetArea == null)
                    {
                        targetArea = new AreaCollection();
                        allAreaList.Add(targetArea);
                    }

                    targetArea.Add(tmpPoly, tmpPoints);
                }

                relationAllPoints = new List<List<XVector2>>();
                foreach (AreaCollection area in allAreaList)
                {
                    List<Poly> polies = area.Polies;
                    List<List<XVector2>> allList = area.PolyPoints;
                    var areaRelationAllPoints = PolyUtility.Combine(allList, out allList);
                    relationAllPoints.Add(areaRelationAllPoints);

                    for (int i = 0; i < polies.Count; i++)
                    {
                        relationlist.Add(polies[i], allList[i]);
                    }
                }
            }
            else
            {
                relationAllPoints = new List<List<XVector2>>() { new List<XVector2>(points) };
                relationlist.Add(poly, points);
            }

            return relationlist;
        }

        public Poly AddWithExtraData(List<XVector2> points, AreaType area, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            Poly poly = new Poly(s_PolyId++, this, new List<XVector2>(points), area);
            m_Normalizer.Normalize(points);

            HashSet<HalfEdgeFace> relationFaces = InnerFindRelationFaces(points);
            newAreaOutEdges = InnerGetEdgeList3(relationFaces);

            Dictionary<Poly, List<XVector2>> relationlist = InnerFindRelationPolies(poly, points, relationFaces, out List<List<XVector2>> relationAllPoints);

            newAreaData = GenerateHalfEdgeData2(newAreaOutEdges, true, relationAllPoints);

            // 标记区域
            foreach (var entry in relationlist)
            {
                Poly relationPoly = entry.Key;
                HashSet<HalfEdgeFace> faces = InnerFindContainsFaces(newAreaData, entry.Value);
                relationPoly.SetFaces(faces);
            }

            InnerReplaceHalfEdgeData(newAreaOutEdges, relationFaces, newAreaData);

            m_Polies.Add(poly.Id, poly);
            return poly;
        }

        public bool Remove(Poly poly, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            if (!m_Polies.ContainsKey(poly.Id))
            {
                newAreaData = null;
                newAreaOutEdges = null;
                return false;
            }

            List<XVector2> points = poly.Points;
            Normalizer.UnNormalize(points);
            HashSet<HalfEdgeFace> relationFaces = InnerFindRelationFaces(points);
            newAreaOutEdges = InnerGetEdgeList3(relationFaces);
            if (newAreaOutEdges.Count < 3)  //边界点小于3直接返回失败
            {
                newAreaData = null;
                newAreaOutEdges = null;
                return false;
            }

            newAreaData = GenerateHalfEdgeData2(newAreaOutEdges, false);
            InnerReplaceHalfEdgeData(newAreaOutEdges, relationFaces, newAreaData);
            m_Polies.Remove(poly.Id);
            return true;
        }

        public void AddConstraint(List<XVector2> points)
        {
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
            HashSet<HalfEdge> faceEdges = new HashSet<HalfEdge>();

            HalfEdge startEdge = null;
            // 找一个没有临边的边，从这个边开始迭代
            foreach (HalfEdgeFace face in faces)
            {
                HalfEdge e1 = face.Edge;
                HalfEdge e2 = e1.NextEdge;
                HalfEdge e3 = e2.NextEdge;
                faceEdges.Add(e1);
                faceEdges.Add(e2);
                faceEdges.Add(e3);

                if (startEdge == null)
                {
                    if (e1.OppositeEdge == null || !faces.Contains(e1.OppositeEdge.Face))
                    {
                        startEdge = e1;
                    }

                    if (e2.OppositeEdge == null || !faces.Contains(e2.OppositeEdge.Face))
                    {
                        startEdge = e2;
                    }

                    if (e3.OppositeEdge == null || !faces.Contains(e3.OppositeEdge.Face))
                    {
                        startEdge = e3;
                    }
                }
            }

            int count = 0;
            HalfEdge current = startEdge;

            do
            {
                if (count++ >= 100)
                {
                    throw new System.Exception($"next edge opposite error");
                }

                int count2 = 0;
                // 找到所有下一条边(因为临边不在面列表里，所以只需要从一个方向找)
                List<HalfEdge> nextEdges = new List<HalfEdge>();
                HalfEdge tmp = current;
                while (tmp != null)
                {
                    if (faceEdges.Contains(tmp))
                    {
                        nextEdges.Add(tmp);
                    }
                    else
                        break;
                    tmp = tmp.NextEdge.OppositeEdge;

                    if (nextEdges.Contains(tmp))
                        break;
                    if (count2++ >= 100)
                    {
                        throw new System.Exception($"next edge opposite error");
                    }
                }

                // 因为是同向，所以只需要计算角度最大的边即可, 只需计算单位向量点积
                Edge curE = new Edge(current.PrevEdge.Vertex.Position, current.Vertex.Position);
                XVector2 n1 = XVector2.Normalize(curE.P1 - curE.P2);

                float d = 1f;
                float angle = 0;
                foreach (HalfEdge e in nextEdges)
                {
                    XVector2 p1 = e.Vertex.Position;
                    XVector2 p2 = e.NextEdge.Vertex.Position;
                    if (!p1.Equals(curE.P2))
                    {
                        Debug.LogError($"error happen {Normalizer.UnNormalize(p1)} {Normalizer.UnNormalize(p2)}");
                    }

                    XVector2 n2 = XVector2.Normalize(p2 - curE.P2);
                    float a = XMath.Angle(n1, n2);
                    float tmpD = XMath.Dot(n1, n2);
                    float tmpCross = XVector2.Cross(n1, n2);

                    if (angle == 0)
                    {
                        angle = a;
                        current = e;
                    }
                    else
                    {
                        if (a < 0)
                        {
                            if (angle < 0)
                            {
                                if (a > angle)
                                {
                                    angle = a;
                                    current = e;
                                }
                            }
                            else
                            {
                                angle = a;
                                current = e;
                            }
                        }
                        else
                        {
                            if (angle > 0 && a > angle)
                            {
                                angle = a;
                                current = e;
                            }
                        }
                    }
                }

                edgeList.Add(new Edge(current.Vertex.Position, current.NextEdge.Vertex.Position));
                current = current.NextEdge;
            } while (current != startEdge);

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
            }
        }

        private List<Edge> InnerSortEdge(List<Edge> edgeList)
        {
            int sortCount;
            List<Edge> sortEdge = new List<Edge>();
            int targetCount = edgeList.Count;
            Edge curEdge = edgeList[0];
            int count = 0;
            do
            {
                if (count++ > 1000)
                    throw new System.Exception("loop error");
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
                            if (XMath.CheckPointsHasSame(p1, p2, p3) && AABB.InSide(Normalizer.UnNormalize(p1)))
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
                if (XMath.CheckPointsHasSame(p1, p2, p3) && AABB.InSide(Normalizer.UnNormalize(p1)))
                    sortEdge.RemoveAt(sortCount - 1);

                sortCount = sortEdge.Count;
                if (sortCount >= 4)
                {
                    p1 = sortEdge[1].P1;
                    p2 = sortEdge[0].P1;
                    p3 = sortEdge[sortCount - 1].P1;
                    if (XMath.CheckPointsHasSame(p1, p2, p3) && AABB.InSide(Normalizer.UnNormalize(p1)))
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

        public HalfEdgeData GenerateHalfEdgeData2(List<Edge> edgeList, bool removeEdgeConstraint = true, List<List<XVector2>> extraPointsList = null)
        {
            //Debug.LogWarning("----------------------------------------");
            HalfEdgeData tmpData = new HalfEdgeData();
            Triangle superTriangle = GeometryUtility.SuperTriangle;
            tmpData.AddTriangle(superTriangle);
            foreach (Edge e in edgeList)
            {
                //Debug.LogWarning($"add point {Normalizer.UnNormalize(e.P1)}");
                DelaunayIncrementalSloan.InsertNewPointInTriangulation(e.P1, tmpData);
            }

            List<XVector2> tmpList = new List<XVector2>();  // TO DO 
            foreach (Edge e in edgeList)
            {
                tmpList.Add(e.P1);
            }

            DelaunayIncrementalSloan.RemoveSuperTriangle(superTriangle, tmpData);

            if (extraPointsList != null)
            {
                foreach (List<XVector2> extraPoints in extraPointsList)
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
                            //Debug.LogWarning($"add point - {Normalizer.UnNormalize(v)}");
                            DelaunayIncrementalSloan.InsertNewPointInTriangulation(v, tmpData);
                        }
                    }

                }

                // 最好等所有点添加完后再添加限制
                foreach (List<XVector2> extraPoints in extraPointsList)
                {
                    ConstrainedDelaunaySloan.AddConstraints(tmpData, extraPoints, false);
                }
            }

            lastLimit = tmpList;

            ConstrainedDelaunaySloan.AddConstraints(tmpData, tmpList, removeEdgeConstraint);

            //Debug.LogWarning("----------------------------------------");
            return tmpData;
        }

        public void ShowLastLimit()
        {
            Debug.LogWarning($"ShowLastLimit ----------------- {lastLimit.Count}");
            foreach (XVector2 v in lastLimit)
                Debug.LogWarning($" {Normalizer.UnNormalize(v)} ");
            Debug.LogWarning("==============");
        }

        public static List<XVector2> lastLimit = null;

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
