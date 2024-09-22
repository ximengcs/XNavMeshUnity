
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XFrame.PathFinding
{
    public partial class XNavMesh
    {
        /// <summary>
        /// Delaunay三角形算法
        /// </summary>
        private class DelaunayIncrementalSloan
        {
            /// <summary>
            /// 移除大三角形
            /// </summary>
            /// <param name="superTriangle"></param>
            /// <param name="triangulationData"></param>
            public static void RemoveSuperTriangle(Triangle superTriangle, HalfEdgeData triangulationData)
            {
                //The super triangle doesnt exists anymore because we have split it into many new triangles
                //But we can use its vertices to figure out which new triangles (or faces belonging to the triangle) 
                //we should delete

                HashSet<HalfEdgeFace> triangleFacesToDelete = new HashSet<HalfEdgeFace>();

                //Loop through all vertices belongin to the triangulation
                foreach (HalfEdgeVertex v in triangulationData.Vertices)
                {
                    //If the face attached to this vertex already exists in the list of faces we want to delete
                    //Then dont add it again
                    if (triangleFacesToDelete.Contains(v.Edge.Face))
                    {
                        continue;
                    }

                    XVector2 v1 = v.Position;

                    //Is this vertex in the triangulation a vertex in the super triangle?
                    if (v1.Equals(superTriangle.P1) || v1.Equals(superTriangle.P2) || v1.Equals(superTriangle.P3))
                    {
                        triangleFacesToDelete.Add(v.Edge.Face);
                    }
                }

                //Debug.Log("Triangles to delete: " + trianglesToDelete.Count);

                //Delete the new triangles with vertices attached to the super triangle
                foreach (HalfEdgeFace f in triangleFacesToDelete)
                {
                    DeleteTriangleFace(f, triangulationData, shouldSetOppositeToNull: true);
                }
            }

            public static bool FindVert(XVector2 p, HalfEdgeData triangulationData)
            {
                foreach (HalfEdgeVertex vert in triangulationData.Vertices)
                {
                    if (vert.Position.Equals(p))
                        return true;
                }
                return false;
            }

            /// <summary>
            /// 插入一个新点
            /// </summary>
            /// <param name="p">需要插入的点</param>
            /// <param name="triangulationData"></param>
            /// <param name="missedPoints"></param>
            /// <param name="flippedEdges"></param>
            public static void InsertNewPointInTriangulation(XVector2 p, HalfEdgeData triangulationData)
            {
                // 找到点所在的三角形面
                HalfEdgeFace f = TriangulationWalk(p, null, triangulationData);

                // 如果没有找到三角面，可能的原因是点在范围之外
                if (f == null)
                {
                    return;
                }

                if (FindVert(p, triangulationData))
                {
                    return;
                }

                // 如果点落在三角形边上,并且所在边对边为空(即在边缘上)，则直接连接此点到对点
                if (XMath.CheckPointOnTriangleLine(new Triangle(f), p, out XVector2 oppositePoint))
                {
                    HalfEdge edge;
                    HalfEdge e1 = f.Edge;
                    HalfEdge e2 = e1.NextEdge;
                    HalfEdge e3 = e2.NextEdge;

                    if (e1.Vertex.Position.Equals(oppositePoint))
                        edge = e1;
                    else if (e2.Vertex.Position.Equals(oppositePoint))
                        edge = e2;
                    else
                        edge = e3;

                    if (edge.PrevEdge.OppositeEdge == null) // 所在边对边为空
                    {
                        HalfEdge f1_e2 = edge.NextEdge;
                        HalfEdge f2_e3 = edge.PrevEdge;

                        HalfEdgeVertex f1_p = new HalfEdgeVertex(p);
                        HalfEdge f1_e1 = edge;
                        HalfEdge f1_e3 = new HalfEdge(f1_p);

                        f1_p.Edge = f1_e2;
                        f1_e2.Vertex.Edge = f1_e3;
                        f1_e3.Vertex.Edge = f1_e1;

                        f1_e2.NextEdge = f1_e3;
                        f1_e3.PrevEdge = f1_e2;

                        f1_e3.NextEdge = f1_e1;
                        f1_e1.PrevEdge = f1_e3;

                        HalfEdgeFace f1 = new HalfEdgeFace(f1_e1);
                        f1_e1.Face = f1;
                        f1_e2.Face = f1;
                        f1_e3.Face = f1;

                        HalfEdgeVertex f2_p1 = new HalfEdgeVertex(edge.Vertex.Position);
                        HalfEdgeVertex f2_p2 = new HalfEdgeVertex(p);
                        HalfEdge f2_e1 = new HalfEdge(f2_p1);
                        HalfEdge f2_e2 = new HalfEdge(f2_p2);

                        f2_p1.Edge = f2_e2;
                        f2_p2.Edge = f2_e3;
                        f2_e3.Vertex.Edge = f2_e1;

                        f2_e1.NextEdge = f2_e2;
                        f2_e2.PrevEdge = f2_e1;
                        f2_e2.NextEdge = f2_e3;
                        f2_e3.PrevEdge = f2_e2;
                        f2_e3.NextEdge = f2_e1;
                        f2_e1.PrevEdge = f2_e3;

                        f2_e1.OppositeEdge = f1_e1.OppositeEdge;
                        if (f2_e1.OppositeEdge != null)
                            f2_e1.OppositeEdge.OppositeEdge = f2_e1;

                        f2_e2.OppositeEdge = f1_e1;
                        f1_e1.OppositeEdge = f2_e2;

                        HalfEdgeFace f2 = new HalfEdgeFace(f2_e1);
                        f2_e1.Face = f2;
                        f2_e2.Face = f2;
                        f2_e3.Face = f2;

                        triangulationData.Vertices.Add(f1_p);
                        triangulationData.Vertices.Add(f2_p1);
                        triangulationData.Vertices.Add(f2_p2);

                        triangulationData.Edges.Add(f1_e3);
                        triangulationData.Edges.Add(f2_e1);
                        triangulationData.Edges.Add(f2_e2);

                        triangulationData.Faces.Add(f1);
                        triangulationData.Faces.Add(f2);
                        triangulationData.Faces.Remove(f);
                        return;
                    }
                }

                // 删除这个三角形，并连接由此点分开的三个三角形
                SplitTriangleFaceAtPoint(f, p, triangulationData);

                // 此时新创建的三个三角形 不一定是符合Delaunay三角形的, 所以需要验证外接圆
                Stack<HalfEdge> trianglesToInvestigate = new Stack<HalfEdge>();
                AddTrianglesOppositePToStack(p, trianglesToInvestigate, triangulationData);

                int count = 0;
                while (trianglesToInvestigate.Count > 0)
                {
                    if (count++ > 1000)
                        throw new Exception("loop error");

                    HalfEdge edgeToTest = trianglesToInvestigate.Pop();

                    // 当p点在这个三角形的外接圆上，或者外接圆外面时，则处理下一个
                    XVector2 a = edgeToTest.Vertex.Position;
                    XVector2 b = edgeToTest.PrevEdge.Vertex.Position;
                    XVector2 c = edgeToTest.NextEdge.Vertex.Position;

                    // 是否在外接圆内
                    if (GeometryUtility.ShouldFlipEdgeStable(a, b, c, p))
                    {
                        // 翻转边
                        GeometryUtility.FlipTriangleEdge(edgeToTest);

                        // 重新寻找此点的对向边
                        AddTrianglesOppositePToStack(p, trianglesToInvestigate, triangulationData);
                    }
                }
            }

            /// <summary>
            /// 根据给定的点找到所有围绕该点的对边(点对向的面)
            /// </summary>
            /// <param name="p"></param>
            /// <param name="trianglesOppositeP"></param>
            /// <param name="triangulationData"></param>
            private static void AddTrianglesOppositePToStack(XVector2 p, Stack<HalfEdge> trianglesOppositeP, HalfEdgeData triangulationData)
            {
                // 围绕的点
                HalfEdgeVertex rotateAroundThis = null;

                foreach (HalfEdgeVertex v in triangulationData.Vertices)
                {
                    if (v.Position.Equals(p))
                    {
                        rotateAroundThis = v;
                        break;
                    }
                }

                int count = 0;
                // 起始面，然后围绕一圈
                HalfEdgeFace tStart = rotateAroundThis.Edge.Face;
                HalfEdgeFace tCurrent = null;
                while (tCurrent != tStart)
                {
                    if (count++ > 1000)
                        throw new Exception("loop error");

                    //点对向的边
                    HalfEdge edgeOppositeRotateVertex = rotateAroundThis.Edge.NextEdge.OppositeEdge;

                    // 如果不在列表中就添加，为null时可能是边界, 需保证点的唯一
                    if (edgeOppositeRotateVertex != null && !trianglesOppositeP.Contains(edgeOppositeRotateVertex))
                    {
                        trianglesOppositeP.Push(edgeOppositeRotateVertex);
                    }

                    // 向左边旋转，接着检查下一个
                    if (rotateAroundThis.Edge.OppositeEdge == null)
                    {
                        DebugUtility.Print(rotateAroundThis.Edge);
                    }

                    rotateAroundThis = rotateAroundThis.Edge.OppositeEdge.Vertex;

                    // 面
                    tCurrent = rotateAroundThis.Edge.Face;
                }
            }

            public static HalfEdgeFace TriangulationWalk(XVector2 p, HalfEdgeFace startTriangle, HalfEdgeData triangulationData)
            {
                // 点所在的三角形面
                HalfEdgeFace intersectingTriangle;

                // 指定一个起始面可以加速算法
                HalfEdgeFace currentTriangle = null;

                // 有时候指定一个三角形面会更快
                if (startTriangle != null)
                {
                    currentTriangle = startTriangle;
                }
                else  // 随机找一个点比从起始点更快
                {
                    int randomPos = HalfEdgeUtility.RandInt(0, triangulationData.Faces.Count);
                    int i = 0;

                    foreach (HalfEdgeFace f in triangulationData.Faces)
                    {
                        if (i == randomPos)
                        {
                            currentTriangle = f;
                            break;
                        }

                        i++;
                    }
                }

                if (currentTriangle == null)
                {
                    return null;
                }

                int count = 0;
                // 从上面随机到的起始点开始寻找 点所在的三角形面
                while (true)
                {
                    if (count++ > 1000)
                        throw new Exception("loop exec");

                    // 如果这个点在三角形的三边的右侧，则它就在三角形内，如果不是，则移动到下一个三角形
                    HalfEdge e1 = currentTriangle.Edge;
                    HalfEdge e2 = e1.NextEdge;
                    HalfEdge e3 = e2.NextEdge;

                    if (IsPointToTheRightOrOnLine(e1.PrevEdge.Vertex.Position, e1.Vertex.Position, p))
                    {
                        if (IsPointToTheRightOrOnLine(e2.PrevEdge.Vertex.Position, e2.Vertex.Position, p))
                        {
                            if (IsPointToTheRightOrOnLine(e3.PrevEdge.Vertex.Position, e3.Vertex.Position, p))
                            {
                                //需要插入的点在这个三角形内
                                intersectingTriangle = currentTriangle;
                                break;
                            }
                            else
                            {
                                // 不在此三角形内，移动到左边的三角形
                                if (e3.OppositeEdge == null)
                                {
                                    if (Test2.Navmesh != null)
                                    {
                                        Func<XVector2, XVector2> f = Test2.Navmesh.Normalizer.UnNormalize;
                                        Debug.LogError($" {e3.GetHashCode()}  {f(e3.Vertex.Position)} {f(e3.NextEdge.Vertex.Position)} {f(p)} opposite edge is null ");
                                        DebugUtility.Print(e1.Face, Test2.Navmesh.Normalizer);
                                    }
                                    else
                                    {
                                        Debug.LogError($" {(e3.Vertex.Position)} {(e3.NextEdge.Vertex.Position)} opposite edge is null ");
                                    }
                                }
                                currentTriangle = e3.OppositeEdge.Face;
                            }
                        }
                        else
                        {
                            if (e2.OppositeEdge == null)
                            {
                                if (Test2.Navmesh != null)
                                {
                                    Func<XVector2, XVector2> f = Test2.Navmesh.Normalizer.UnNormalize;
                                    Debug.LogError($" {e2.GetHashCode()} {f(e2.Vertex.Position)} {f(e2.NextEdge.Vertex.Position)} {f(p)} opposite edge is null ");
                                    DebugUtility.Print(e1.Face, Test2.Navmesh.Normalizer);
                                }
                                else
                                {
                                    Debug.LogError($" {(e2.Vertex.Position)} {(e2.NextEdge.Vertex.Position)} opposite edge is null ");
                                }
                            }
                            // 不在此三角形内，移动到左边的三角形
                            currentTriangle = e2.OppositeEdge.Face;
                        }
                    }
                    else
                    {
                        if (e1.OppositeEdge == null)
                        {
                            if (Test2.Navmesh != null)
                            {
                                Func<XVector2, XVector2> f = Test2.Navmesh.Normalizer.UnNormalize;
                                Debug.LogError($" {e1.GetHashCode()}  {f(e1.Vertex.Position)} {f(e1.NextEdge.Vertex.Position)} {f(p)} opposite edge is null ");
                                DebugUtility.Print(e1.Face, Test2.Navmesh.Normalizer);
                            }
                            else
                            {
                                Debug.LogError($" {(e1.Vertex.Position)} {(e1.NextEdge.Vertex.Position)} opposite edge is null ");
                            }
                        }
                        // 不在此三角形内，移动到左边的三角形
                        currentTriangle = e1.OppositeEdge.Face;
                    }
                }

                return intersectingTriangle;
            }

            /// <summary>
            /// 使用点分裂三角形面
            /// </summary>
            /// <param name="f">三角形面</param>
            /// <param name="splitPosition">分裂点</param>
            /// <param name="data"></param>
            public static void SplitTriangleFaceAtPoint(HalfEdgeFace f, XVector2 splitPosition, HalfEdgeData data)
            {
                //面的三条边
                HalfEdge e_1 = f.Edge;
                HalfEdge e_2 = e_1.NextEdge;
                HalfEdge e_3 = e_2.NextEdge;

                //创建出的新边需要正确设置临边的对边
                HashSet<HalfEdge> newEdges = new HashSet<HalfEdge>();

                CreateNewFace(e_1, splitPosition, data, newEdges);
                CreateNewFace(e_2, splitPosition, data, newEdges);
                CreateNewFace(e_3, splitPosition, data, newEdges);

                // 寻找刚才创建的新半边的对边
                foreach (HalfEdge e in newEdges)
                {
                    // 如果已经找到对边
                    if (e.OppositeEdge != null)
                    {
                        continue;
                    }

                    XVector2 eGoingTo = e.Vertex.Position;
                    XVector2 eGoingFrom = e.PrevEdge.Vertex.Position;

                    foreach (HalfEdge eOpposite in newEdges)
                    {
                        if (e == eOpposite || eOpposite.OppositeEdge != null)
                        {
                            continue;
                        }

                        XVector2 eGoingTo_Other = eOpposite.Vertex.Position;
                        XVector2 eGoingFrom_Other = eOpposite.PrevEdge.Vertex.Position;

                        // 如果来向和去向相反就表示互为对边
                        if (eGoingTo.Equals(eGoingFrom_Other) && eGoingFrom.Equals(eGoingTo_Other))
                        {
                            e.OppositeEdge = eOpposite;
                            eOpposite.OppositeEdge = e;
                        }
                    }
                }

                // 删除旧的三角形
                DeleteTriangleFace(f, data, false);
            }

            /// <summary>
            /// 使用一个边和点创建三角形面
            /// </summary>
            /// <param name="e_old"></param>
            /// <param name="splitPosition"></param>
            /// <param name="data"></param>
            /// <param name="newEdges"></param>
            private static void CreateNewFace(HalfEdge e_old, XVector2 splitPosition, HalfEdgeData data, HashSet<HalfEdge> newEdges)
            {
                // 新的三角形的三个点
                XVector2 p_split = splitPosition;
                XVector2 p_next = e_old.PrevEdge.Vertex.Position;
                XVector2 p_prev = e_old.Vertex.Position;

                // 创建半边的顶点
                HalfEdgeVertex v_split = new HalfEdgeVertex(p_split);
                HalfEdgeVertex v_next = new HalfEdgeVertex(p_next);
                HalfEdgeVertex v_prev = new HalfEdgeVertex(p_prev);

                // 半边
                HalfEdge e_1 = new HalfEdge(v_prev);
                HalfEdge e_2 = new HalfEdge(v_split);
                HalfEdge e_3 = new HalfEdge(v_next);

                // 三角形面
                HalfEdgeFace f = new HalfEdgeFace(e_1);

                // 新边的对边应和旧的边保持一致
                e_1.OppositeEdge = e_old.OppositeEdge;
                // 如果此边不是边界则要设置对边的对边为自己
                if (e_1.OppositeEdge != null)
                {
                    e_old.OppositeEdge.OppositeEdge = e_1;
                }

                // 两条新边需要查找新的对边，所以先添加进去
                newEdges.Add(e_2);
                newEdges.Add(e_3);

                // 创建三个边的连接
                e_1.NextEdge = e_2;
                e_1.PrevEdge = e_3;

                e_2.NextEdge = e_3;
                e_2.PrevEdge = e_1;

                e_3.NextEdge = e_1;
                e_3.PrevEdge = e_2;

                // 没条边的面
                e_1.Face = f;
                e_2.Face = f;
                e_3.Face = f;

                // 设置顶点的半边
                v_split.Edge = e_3;
                v_next.Edge = e_1;
                v_prev.Edge = e_2;

                //添加新项到列表中
                data.Faces.Add(f);

                data.Edges.Add(e_1);
                data.Edges.Add(e_2);
                data.Edges.Add(e_3);

                data.Vertices.Add(v_split);
                data.Vertices.Add(v_next);
                data.Vertices.Add(v_prev);
            }

            /// <summary>
            /// 删除一个三角形
            /// </summary>
            /// <param name="t"></param>
            /// <param name="data"></param>
            /// <param name="shouldSetOppositeToNull"></param>
            public static void DeleteTriangleFace(HalfEdgeFace t, HalfEdgeData data, bool shouldSetOppositeToNull)
            {
                HalfEdge t_e1 = t.Edge;
                HalfEdge t_e2 = t_e1.NextEdge;
                HalfEdge t_e3 = t_e2.NextEdge;

                // 挖洞
                if (shouldSetOppositeToNull)
                {
                    if (t_e1.OppositeEdge != null)
                    {
                        t_e1.OppositeEdge.OppositeEdge = null;
                    }
                    if (t_e2.OppositeEdge != null)
                    {
                        t_e2.OppositeEdge.OppositeEdge = null;
                    }
                    if (t_e3.OppositeEdge != null)
                    {
                        t_e3.OppositeEdge.OppositeEdge = null;
                    }
                }

                data.Faces.Remove(t);

                data.Edges.Remove(t_e1);
                data.Edges.Remove(t_e2);
                data.Edges.Remove(t_e3);

                data.Vertices.Remove(t_e1.Vertex);
                data.Vertices.Remove(t_e2.Vertex);
                data.Vertices.Remove(t_e3.Vertex);
            }

            private static bool IsPointToTheRightOrOnLine(XVector2 a, XVector2 b, XVector2 p)
            {
                bool isToTheRight = false;
                LeftOnRight pointPos = IsPoint_Left_On_Right_OfVector(a, b, p);

                if (pointPos == LeftOnRight.Right || pointPos == LeftOnRight.On)
                {
                    isToTheRight = true;
                }

                return isToTheRight;
            }

            public static LeftOnRight IsPoint_Left_On_Right_OfVector(XVector2 a, XVector2 b, XVector2 p)
            {
                float relationValue = GetPointInRelationToVectorValue(a, b, p);

                //To avoid floating point precision issues we can add a small value
                float epsilon = XMath.EPSILON;

                //To the right
                if (relationValue < -epsilon)
                {
                    return LeftOnRight.Right;
                }
                //To the left
                else if (relationValue > epsilon)
                {
                    return LeftOnRight.Left;
                }
                //= 0 -> on the line
                else
                {
                    return LeftOnRight.On;
                }
            }

            //
            // Does a point p lie to the left, to the right, or on a vector going from a to b
            //
            //https://gamedev.stackexchange.com/questions/71328/how-can-i-add-and-subtract-convex-polygons
            public static float GetPointInRelationToVectorValue(XVector2 a, XVector2 b, XVector2 p)
            {
                float x1 = a.X - p.X;
                float x2 = a.Y - p.Y;
                float y1 = b.X - p.X;
                float y2 = b.Y - p.Y;

                float determinant = XMath.Det2(x1, x2, y1, y2);

                return determinant;
            }
        }
    }
}
