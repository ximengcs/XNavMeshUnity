using System;
using UnityEngine;
using Simon001.PathFinding;
using System.Collections.Generic;
using XFrame.PathFinding.Extensions;
using XFrame.PathFinding.RVO;

namespace XFrame.PathFinding
{
    public partial class XNavmesh
    {
        private AABB m_AABB;
        private Normalizer m_Normalizer;
        private HalfEdgeData m_Data;
        private Simulator m_Simulator;

        private int s_PolyId;
        private Dictionary<int, Poly> m_Polies;

        public XVector2 Min => m_AABB.Min;
        public XVector2 Max => m_AABB.Max;

        public Normalizer Normalizer => m_Normalizer;
        public Dictionary<int, Poly> Polies => m_Polies;

        public int AreaCount => m_Data.Faces.Count;

        public XNavmesh(AABB aabb)
        {
            m_AABB = aabb;
            m_Normalizer = new Normalizer(aabb);
            m_Data = new HalfEdgeData();
            m_Polies = new Dictionary<int, Poly>();

            InnerInitialize();
        }

        public XNavmesh(byte[] data)
        {
            InnerToNavmesh(data);
        }

        public IAgent AddAgent<T>(XVector2 pos, object userData) where T : IAgent, new()
        {
            T agent = new T();
            m_Simulator.addAgent(pos);
            return agent;
        }

        private void InnerInitialize()
        {
            Triangle superTriangle = HalfEdgeExtension.SuperTriangle;
            m_Data.AddTriangle(superTriangle);

            XVector2 min = m_AABB.Min;
            XVector2 max = m_AABB.Max;
            Add(new XVector2(min.X, min.Y));
            Add(new XVector2(min.X, max.Y));
            Add(new XVector2(max.X, min.Y));
            Add(new XVector2(max.X, max.Y));

            DelaunayIncrementalSloan.RemoveSuperTriangle(superTriangle, m_Data);
        }

        public List<XVector2> FindPath(XVector2 startPoint, XVector2 endPoint)
        {
            XNavMeshHelper helper = new XNavMeshHelper(m_Data);
            AStar aStar = new AStar(helper);
            HalfEdgeFace start = FindWalkFace(startPoint);
            HalfEdgeFace end = FindWalkFace(endPoint);
            if (start != null && end != null)
            {
                AStarPath path = aStar.Execute(start, end);

                if (path != null)
                {
                    List<XVector2> points = new List<XVector2>();
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        object a = path[i];
                        object b = path[i + 1];
                        List<XVector2> subPoints = InnerGetPathPoints(a, b);
                        for (int j = 0; j < subPoints.Count - 1; j++)
                            points.Add(m_Normalizer.UnNormalize(subPoints[j]));
                    }
                    if (points.Count > 0)
                        points[0] = endPoint;
                    else
                        points.Add(endPoint);
                    points.Reverse();
                    return points;
                }
                else
                {
                    Debug.LogError($"calculate path count is null {startPoint} {endPoint}");
                    return null;
                }
            }
            else
            {
                Debug.LogError($"start or end is null. {start == null} {end == null} ");
                return null;
            }
        }

        private List<XVector2> InnerGetPathPoints(object from, object to)
        {
            HalfEdgeFace f1 = from as HalfEdgeFace;
            HalfEdgeFace f2 = to as HalfEdgeFace;
            if (f1.IsAdjacent(f2))
            {
                return new List<XVector2>()
                {
                    new Triangle(f1).InnerCentrePoint,
                    new Triangle(f2).InnerCentrePoint
                };
            }
            else
            {
                if (f1.GetSameVert(f2, out XVector2 insect))
                {
                    return new List<XVector2>()
                    {
                        new Triangle(f1).InnerCentrePoint,
                        insect,
                        new Triangle(f2).InnerCentrePoint
                    };
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public List<Triangle> GetArea(AreaType areaType)
        {
            List<Triangle> result = new List<Triangle>();
            foreach (HalfEdgeFace face in m_Data.Faces)
            {
                if (face.Area == areaType)
                {
                    Triangle triangle = new Triangle(face);
                    triangle = m_Normalizer.UnNormalize(triangle);
                    result.Add(triangle);
                }
            }
            return result;
        }

        public XVector2 GetRandomPoint()
        {
            List<HalfEdgeFace> faces = new List<HalfEdgeFace>();
            foreach (var face in m_Data.Faces)
            {
                if (face.Area != AreaType.Obstacle)
                    faces.Add(face);
            }

            HalfEdgeFace target = faces[UnityEngine.Random.Range(0, faces.Count)];
            XVector2 point = new Triangle(target).RandomPoint();
            point = m_Normalizer.UnNormalize(point);
            point = m_Normalizer.Constraint(point);
            return point;
        }

        public HalfEdgeFace FindWalkFace(XVector2 point)
        {
            point = m_Normalizer.Normalize(point);
            return InnerFindFace(point, AreaType.Walk);
        }

        private HalfEdgeFace InnerFindFace(XVector2 point, AreaType type)
        {
            foreach (HalfEdgeFace face in m_Data.Faces)
            {
                if (face.Area == type)
                {
                    if (new Triangle(face).Contains(point))
                    {
                        return face;
                    }
                }
            }
            return null;
        }

        private HashSet<HalfEdgeFace> InnerFindRelationFaces(List<XVector2> points, HashSet<HalfEdgeFace> result = null)
        {
            //Debug.LogWarning($" InnerFindRelationFaces {m_Data.Faces.Count} ");
            HashSet<HalfEdgeFace> faces = result != null ? result : new HashSet<HalfEdgeFace>();
            foreach (HalfEdgeFace face in m_Data.Faces)
            {
                Triangle triangle = new Triangle(face);
                bool add = false;
                //Debug.LogWarning($"real InnerFindRelationFaces triangle {Normalizer.UnNormalize(triangle)} ");

                for (int i = 0; i < points.Count; i++)
                {
                    XVector2 p1 = points[i];
                    XVector2 p2 = points[(i + 1) % points.Count];
                    //Debug.LogWarning($"real InnerFindRelationFaces {Normalizer.UnNormalize(p1)} {Normalizer.UnNormalize(p2)} {triangle.Intersect2(p1, p2)} {triangle.Contains(p1)} |||| {Normalizer.UnNormalize(triangle)} ");
                    if (triangle.Intersect2(p1, p2) || triangle.Contains(p1))
                    {
                        if (!faces.Contains(face))
                        {
                            //Debug.LogWarning($"add InnerFindRelationFaces {Normalizer.UnNormalize(triangle)} ");
                            faces.Add(face);
                            add = true;
                        }
                    }
                }

                if (add)
                {
                    // 检查是否存在Poly中没有加入的面
                    foreach (var entry in m_Polies)
                    {
                        Poly poly = entry.Value;
                        if (poly.Contains(face))
                        {
                            foreach (HalfEdgeFace polyFace in poly.Faces)
                            {
                                if (!faces.Contains(polyFace))
                                {
                                    faces.Add(polyFace);
                                    //Debug.LogWarning($"add 2 InnerFindRelationFaces {Normalizer.UnNormalize(new Triangle(polyFace))} ");
                                }
                            }
                            break;
                        }
                    }
                }
            }
            return faces;
        }

        private HashSet<HalfEdgeFace> InnerFindContainsFaces(HalfEdgeData data, List<XVector2> edgeList, HashSet<HalfEdgeFace> result = null)
        {
            Dictionary<HalfEdgeFace, int> opFaceCount = new Dictionary<HalfEdgeFace, int>();
            HashSet<HalfEdgeFace> faces = result != null ? result : new HashSet<HalfEdgeFace>();
            foreach (HalfEdgeFace face in data.Faces)
            {
                HalfEdge e1 = face.Edge;
                HalfEdge e2 = e1.NextEdge;
                HalfEdge e3 = e2.NextEdge;

                bool isContains = false;
                for (int i = 0; i < edgeList.Count; i++)
                {
                    Edge edge = new Edge(edgeList[i], edgeList[(i + 1) % edgeList.Count]);
                    if (e1.Equals(edge))
                    {
                        faces.Add(face);
                        isContains = true;
                        break;
                    }
                    if (e2.Equals(edge))
                    {
                        faces.Add(face);
                        isContains = true;
                        break;
                    }
                    if (e3.Equals(edge))
                    {
                        faces.Add(face);
                        isContains = true;
                        break;
                    }
                }

                if (isContains)
                {
                    if (e1.OppositeEdge != null)
                    {
                        HalfEdgeFace f = e1.OppositeEdge.Face;
                        if (!faces.Contains(f))
                        {
                            if (opFaceCount.TryGetValue(f, out int count))
                                opFaceCount[f] = count + 1;
                            else
                                opFaceCount.Add(f, 1);
                        }
                    }

                    if (e2.OppositeEdge != null)
                    {
                        HalfEdgeFace f = e2.OppositeEdge.Face;
                        if (!faces.Contains(f))
                        {
                            if (opFaceCount.TryGetValue(f, out int count))
                                opFaceCount[f] = count + 1;
                            else
                                opFaceCount.Add(f, 1);
                        }
                    }

                    if (e3.OppositeEdge != null)
                    {
                        HalfEdgeFace f = e3.OppositeEdge.Face;
                        if (!faces.Contains(f))
                        {
                            if (opFaceCount.TryGetValue(f, out int count))
                                opFaceCount[f] = count + 1;
                            else
                                opFaceCount.Add(f, 1);
                        }
                    }
                }
            }

            foreach (var item in opFaceCount)
            {
                if (item.Value >= 3)
                {
                    faces.Add(item.Key);
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
            if (!m_AABB.Contains(newPoints))
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
#if DEBUG_PATH
            Recorder.MarkCurrent();
#endif
            newAreaData = null;
            newAreaOutEdges = null;
            if (newPoints[0].Equals(oldPoints[0]))  // 点位没有发生变化直接返回失败
                return false;

#if DEBUG_PATH
            Recorder.SetPolyId(poly.Id);
#endif

            m_Normalizer.Normalize(oldPoints);
            m_Normalizer.Normalize(newPoints);

#if DEBUG_PATH
            Recorder.SetOldPoints(oldPoints);
            Recorder.SetNewPoints(newPoints);
#endif

            HashSet<HalfEdgeFace> relationFaces = new HashSet<HalfEdgeFace>();
            InnerFindRelationFaces(oldPoints, relationFaces);

            InnerFindRelationFaces(newPoints, relationFaces);

#if DEBUG_PATH
            Recorder.SetRelationFaces(relationFaces);
#endif

            newAreaOutEdges = InnerGetEdgeList3(relationFaces);
#if DEBUG_PATH
            Recorder.SetNewAreaOutEdges(newAreaOutEdges);
#endif
            if (newAreaOutEdges.Count < 3)  //边界点小于3直接返回失败
                return false;

#if DEBUG_PATH
            Recorder.SetPolies(m_Polies);
#endif

            Dictionary<Poly, List<XVector2>> relationlist = InnerFindRelationPolies(poly, newPoints, relationFaces, out List<List<XVector2>> relationAllPoints);

#if DEBUG_PATH
            Recorder.SetRelationNewPoint(relationlist);
            Recorder.SetRelationAllPoints(relationAllPoints);
#endif

            newAreaData = HalfEdgeExtension.GenerateConstraintData(newAreaOutEdges, true, relationAllPoints);

#if DEBUG_PATH
            Recorder.SetHalfEdgeData(newAreaData);
#endif

            if (newAreaData.Faces.Count == 0)
            {
#if DEBUG_PATH
                Recorder.Show(null);
#endif
                Debug.LogError("error");
                return false;
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

            m_Normalizer.UnNormalize(newPoints);  // 还原点
            return true;
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
                    m_Normalizer.Normalize(tmpPoints);

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

            newAreaData = HalfEdgeExtension.GenerateConstraintData(newAreaOutEdges, true, relationAllPoints);
            if (newAreaData.Faces.Count > 0)
            {
                // 标记区域
                foreach (var entry in relationlist)
                {
                    Poly relationPoly = entry.Key;
                    HashSet<HalfEdgeFace> faces = InnerFindContainsFaces(newAreaData, entry.Value);
                    relationPoly.SetFaces(faces);
                }

                InnerReplaceHalfEdgeData(newAreaOutEdges, relationFaces, newAreaData);

                m_Polies.Add(poly.Id, poly);
            }
            else
            {
                throw new Exception("add error");
            }

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
            m_Normalizer.UnNormalize(points);
            HashSet<HalfEdgeFace> relationFaces = InnerFindRelationFaces(points);
            newAreaOutEdges = InnerGetEdgeList3(relationFaces);
            if (newAreaOutEdges.Count < 3)  //边界点小于3直接返回失败
            {
                newAreaData = null;
                newAreaOutEdges = null;
                return false;
            }

            newAreaData = HalfEdgeExtension.GenerateConstraintData(newAreaOutEdges, false);
            InnerReplaceHalfEdgeData(newAreaOutEdges, relationFaces, newAreaData);
            m_Polies.Remove(poly.Id);
            return true;
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

            if (current == null)
            {
                Debug.LogError($"current is null, {faces.Count}");
            }

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
#if DEBUG_PATH
                        Recorder.Show(null);
#endif
                        Debug.LogError($"error happen {m_Normalizer.UnNormalize(p1)} {m_Normalizer.UnNormalize(p2)}");
                    }

                    XVector2 n2 = XVector2.Normalize(p2 - curE.P2);
                    float a = XMath.Angle(n1, n2);
                    float tmpD = XMath.Dot(n1, n2);
                    float tmpCross = XVector2.Cross(n1, n2);

                    Func<XVector2, XVector2> f = m_Normalizer.UnNormalize;
                    //Debug.LogWarning($"[edge] {f(p1)} {f(p2)} {f(curE.P2)} {a} {angle} ");

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

                Func<XVector2, XVector2> f_ = Test2.Normalizer.UnNormalize;
                //Debug.LogWarning($"next ------------- {f_(current.Vertex.Position)} {f_(current.NextEdge.Vertex.Position)}");
                edgeList.Add(new Edge(current.Vertex.Position, current.NextEdge.Vertex.Position));
                current = current.NextEdge;
            } while (current != startEdge);

            return InnerSortEdge(edgeList);
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
                {
#if DEBUG_PATH
                    Recorder.Show(null);
#endif
                    throw new System.Exception("loop error");
                }
                Edge tmp = curEdge;
                curEdge = default;
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
                            if (XMath.CheckPointsHasSame(p1, p2, p3) && m_AABB.InSide(m_Normalizer.UnNormalize(p1)))
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
                if (XMath.CheckPointsHasSame(p1, p2, p3) && m_AABB.InSide(m_Normalizer.UnNormalize(p1)))
                    sortEdge.RemoveAt(sortCount - 1);

                sortCount = sortEdge.Count;
                if (sortCount >= 4)
                {
                    p1 = sortEdge[1].P1;
                    p2 = sortEdge[0].P1;
                    p3 = sortEdge[sortCount - 1].P1;
                    if (XMath.CheckPointsHasSame(p1, p2, p3) && m_AABB.InSide(m_Normalizer.UnNormalize(p1)))
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
                    if (e.Equals(edge))
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

        public List<TriangleArea> ToTriangles(HalfEdgeData data = null)
        {
            if (data == null)
                data = m_Data;
            List<TriangleArea> triangles = new List<TriangleArea>(8);
            foreach (HalfEdgeFace face in data.Faces)
            {
                int polyId = -1;
                foreach (Poly poly in m_Polies.Values)
                {
                    if (poly.Contains(face))
                    {
                        polyId = poly.Id;
                        break;
                    }
                }

                triangles.Add(new TriangleArea(face, polyId, m_Normalizer));
            }

            return triangles;
        }
    }
}
