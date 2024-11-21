
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using XFrame.PathFinding;

/// <summary>
/// 一个Poly具有多个三角形
/// </summary>
public class PolyUtility
{
    public static List<XVector2> InsertPoint(List<XVector2> list, HashSet<XVector2> newPoints)
    {
        if (newPoints == null || newPoints.Count == 0)
            return list;

        List<EdgeSet> newEdges = new List<EdgeSet>();
        for (int i = 0; i < list.Count; i++)
        {
            XVector2 p1 = list[i];
            XVector2 p2 = list[(i + 1) % list.Count];
            EdgeSet e = FindEdge(newEdges, p1, p2);
            foreach (XVector2 p in newPoints)
            {
                if (e.InSameLine(p))
                {
                    e.Add(p);
                }
            }
        }

        List<XVector2> result = new List<XVector2>();
        foreach (EdgeSet e in newEdges)
        {
            for (int i = 0; i < e.Vertices.Count - 1; i++)
            {
                result.Add(e.Vertices[i]);
            }
        }

        return result;
    }

    public static List<XVector2> Combine(List<List<XVector2>> list, out List<List<XVector2>> newList)
    {
        if (list == null || list.Count == 0)
        {
#if DEBUG_PATH
            Recorder.Show(null);
#endif
            Debug.LogError("list is null");
            newList = null;
            return null;
        }

        //Debug.LogWarning($" Combine -------------------------- ");
        //foreach (List<XVector2> e in list)
        //{
        //    Debug.LogWarning("==============");
        //    foreach (XVector2 p in e)
        //    {
        //        Debug.LogWarning($" {Test2.Navmesh.Normalizer.UnNormalize(p)} ");
        //    }
        //}
        //Debug.LogWarning($" ------------------------------- ");

        List<EdgeSet> newEdges = new List<EdgeSet>();
        for (int j = 0; j < list.Count - 1; j++)
        {
            List<XVector2> points = list[j];
            for (int i = 0; i < points.Count; i++)
            {
                XVector2 p1 = points[i];
                XVector2 p2 = points[(i + 1) % points.Count];
                EdgeSet e1 = FindEdge(newEdges, p1, p2);
                for (int k = j + 1; k < list.Count; k++)
                {
                    List<XVector2> points2 = list[k];
                    for (int l = 0; l < points2.Count; l++)
                    {
                        XVector2 p3 = points2[l];
                        XVector2 p4 = points2[(l + 1) % points2.Count];
                        EdgeSet e2 = FindEdge(newEdges, p3, p4);

                        if (e2.Intersect(e1, out XVector2 newPoint))
                        {
                            e1.Add(newPoint);
                            e2.Add(newPoint);
                        }
                        else if (e2.InSameLine(e1))
                        {
                            e1.Add(p3);
                            e1.Add(p4);
                            e2.Add(p1);
                            e2.Add(p2);
                        }
                    }
                }
            }

        }

        newList = new List<List<XVector2>>(list.Count);
        foreach (List<XVector2> points in list)
        {
            List<XVector2> target = new List<XVector2>();
            newList.Add(target);
            for (int i = 0; i < points.Count; i++)
            {
                XVector2 p1 = points[i];
                XVector2 p2 = points[(i + 1) % points.Count];

                EdgeSet edge = FindEdge(newEdges, p1, p2);
                edge.GetPoints(p1, p2, target);
            }
        }

        // 精简点位，将小于某个值的点合并为一个
        ClipPoints(newList, list);

        List<XVector2> result = FindOutLine(newList);

        //Debug.LogWarning($" result ---------------------------{result.Count}");
        //foreach (XVector2 e in result)
        //{
        //    Debug.LogWarning(Test2.Navmesh.Normalizer.UnNormalize(e));
        //}
        //Debug.LogWarning($" ------------------");

        return result;
    }

    private static EdgeSet FindEdge(List<EdgeSet> edges, XVector2 start, XVector2 end)
    {
        XVector2 p1 = start;
        XVector2 p2 = end;
        EdgeSet target = null;
        foreach (EdgeSet edge in edges)
        {
            XVector2 p3 = edge.Start;
            XVector2 p4 = edge.End;

            if (edge.InSameLine(p1, p2))
            {
                target = edge;
                break;
            }
        }

        if (target != null)
        {
            target.Add(p1);
            target.Add(p2);
        }
        else
        {
            target = new EdgeSet(p1, p2);
            edges.Add(target);
        }
        return target;
    }

    public static void ClipPoints(List<List<XVector2>> list, List<List<XVector2>> mainList)
    {
        float gap = Test2.Normalizer.MinGap;
        Func<XVector2, XVector2> f = Test2.Normalizer.UnNormalize;
        //Debug.LogWarning($"gap {gap}");
        for (int k = 0; k < list.Count; k++)
        {
            List<XVector2> points = list[k];
            //Debug.LogWarning("clip points -----------------------");
            for (int i = 0; i < points.Count; i++)
            {
                XVector2 cur = points[i];
                // 如果有，从主点列表中找到距离最近的点
                XVector2 mainPointTarget = default;
                float toMainDis = float.MaxValue;
                bool findMainPoint = false;
                List<XVector2> mainPoints = mainList[k];
                foreach (XVector2 mainPoint in mainPoints)
                {
                    float dis = XVector2.Distance(mainPoint, cur);
                    if (dis < gap && dis < toMainDis)
                    {
                        mainPointTarget = mainPoint;
                        findMainPoint = true;
                        toMainDis = dis;
                    }
                }
                if (findMainPoint)
                {
                    //Debug.LogWarning($" to main point {f(cur)} -> {f(mainPointTarget)} ");
                    cur = mainPointTarget;
                    points[i] = cur;
                }

                ConstraintSamePoint(gap, points, i, cur);

                foreach (List<XVector2> otherPoints in list)
                {
                    if (otherPoints == points)
                        continue;
                    bool lastEquals = false;
                    for (int j = 0; j < otherPoints.Count; j++)
                    {
                        XVector2 p = otherPoints[j];
                        bool equals = XVector2.Distance(p, cur) < gap;
                        if (j > 0)
                        {
                            if (equals)
                            {
                                if (lastEquals)
                                {
                                    //Debug.LogWarning($"remove at");
                                    otherPoints.RemoveAt(j);
                                    j--;
                                }
                                else
                                {
                                    otherPoints[j] = cur;
                                    if (j == otherPoints.Count - 1 && otherPoints.Count > 1)
                                    {
                                        bool nextEquals = XVector2.Distance(otherPoints[0], cur) < gap;
                                        if (nextEquals)
                                        {
                                            //Debug.LogWarning($"remove at");
                                            otherPoints.RemoveAt(j);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (equals)
                                otherPoints[j] = cur;
                        }
                        lastEquals = equals;
                    }
                }
            }
        }

        // 去掉"小"三角形
        foreach (List<XVector2> points in list)
        {
            if (points.Count > 2)
            {
                for (int i = points.Count - 1; i >= 0; i--)
                {
                    int j = (points.Count + i - 2) % points.Count;
                    if (i == j)
                        break;

                    XVector2 cur = points[i];
                    XVector2 next = points[j];
                    if (cur.Equals(next))
                    {
                        points.RemoveAt((points.Count + i - 1) % points.Count);
                        points.RemoveAt((points.Count + i - 2) % points.Count);
                    }
                }
            }
        }

        //Debug.LogWarning("======================");
        //foreach (List<XVector2> points in list)
        //{
        //    Debug.LogWarning("----------------------");
        //    foreach (XVector2 p in points)
        //        Debug.LogWarning($" {Test2.Navmesh.Normalizer.UnNormalize(p)} ");
        //}
        //Debug.LogWarning("====================== after");
    }

    private static void ConstraintSamePoint(float gap, List<XVector2> points, int offset, XVector2 cur)
    {
        int count = points.Count;
        Func<XVector2, XVector2> f = Test2.Normalizer.UnNormalize;
        // 下一个点
        for (int j = 0; j < count - 1; j++)
        {
            int index = (j + offset + 1) % count;
            XVector2 next = points[index];
            //Debug.LogWarning($" distance {f(cur)} {f(next)} {XVector2.Distance(cur, next)} ");
            if (XVector2.Distance(cur, next) < gap)
            {
                if (j != 0)
                {
                    points.RemoveAt(index);
                    j--;
                    count--;
                }
                else
                {
                    points[index] = cur;
                }
            }
            else
            {
                break;
            }
        }

        // 上一个点
        count = points.Count;
        for (int j = 0; j < count - 1; j++)
        {
            int index = (offset + count - j - 1) % count;
            XVector2 next = points[index];
            if (XVector2.Distance(cur, next) < gap)
            {
                if (j != 0)
                {
                    points.RemoveAt(index);
                    count--;
                }
                else
                {
                    points[index] = cur;
                }
            }
            else
            {
                break;
            }
        }
    }

    public static List<XVector2> FindOutLine(List<List<XVector2>> list)
    {
        // 找最小点
        XVector2 min = new XVector2(float.MaxValue, float.MaxValue);

        foreach (List<XVector2> points in list)
        {
            foreach (XVector2 p in points)
            {
                if (p.X < min.X) min.X = p.X;
                if (p.Y < min.Y) min.Y = p.Y;
            }
        }

        float distance = float.MaxValue;
        XVector2 leftBottom = default;
        foreach (List<XVector2> points in list)
        {
            foreach (XVector2 p in points)
            {
                if (XMath.Equals(p.X, min.X) || XMath.Equals(p.Y, min.Y))  //至少有一个点在边界上
                {
                    float dis = XVector2.Distance(p, min);
                    if (dis < distance)
                    {
                        distance = dis;
                        leftBottom = p;
                    }
                }
            }
        }

        List<XVector2> result = new List<XVector2>();
        Edge current = default;
        foreach (List<XVector2> points in list)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Equals(leftBottom))
                {
                    current = new Edge(points[(i + 1) % points.Count], leftBottom);
                    break;
                }
            }
        }
        result.Add(current.P1);
        Func<XVector2, XVector2> f = Test2.Normalizer.UnNormalize;
        //Debug.LogWarning($" left botttom {f(leftBottom)} ");
        int calCount = 0;
        do
        {
            if (calCount++ >= 1000)
            {
                Debug.LogWarning($" left botttom {f(leftBottom)} ");
                foreach (List<XVector2> points in list)
                {
                    Debug.LogWarning("======================");
                    foreach (XVector2 p in points)
                    {
                        Debug.LogWarning($" {Test2.Normalizer.UnNormalize(p)} ");
                    }
                    Debug.LogWarning("======================");
                }
#if DEBUG_PATH
                Recorder.Show(null);
#endif
                Debug.LogError($"Error happen {current.P1} {list.Count} ");
                break;
            }

            List<Edge> edges = new List<Edge>();
            foreach (List<XVector2> points in list)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    XVector2 p = points[i];
                    if (p.EqualsFull(current.P1))
                    {
                        Edge e = new Edge(points[(i + 1) % points.Count], current.P1);
                        edges.Add(e);
                        break;
                    }
                }
            }

            if (edges.Count == 0)
            {
#if DEBUG_PATH
                Recorder.Show(null);
#endif
                Debug.LogError($"error happen {current.P1} ");
                break;
            }
            else if (edges.Count == 1)
            {

                current = edges[0];
                result.Add(current.P1);
                if (calCount++ >= 950)
                {
                    Debug.LogWarning($"Start check edge count is zero {f(current.P1)} {f(current.P2)}");
                }
            }
            else
            {
                Edge e1 = edges[0];

                XVector2 cur = XVector2.Normalize(current.P2 - current.P1);
                XVector2 n1 = XVector2.Normalize(e1.P1 - e1.P2);
                float c1 = XVector2.Cross(cur, n1);
                float d1 = XVector2.Dot(cur, n1);

                if (cur.Equals(XVector2.Zero))
                {
#if DEBUG_PATH
                    Recorder.Show(null);
#endif
                    Debug.LogError($" nan error {current.P2} {current.P1}");
                }

                if (calCount++ >= 950)
                {
                    Debug.LogWarning($"Start check {f(current.P1)} {f(current.P2)}");
                }
                for (int i = 1; i < edges.Count; i++)
                {
                    Edge e2 = edges[i];
                    XVector2 n2 = XVector2.Normalize(e2.P1 - e2.P2);

                    // 用来判断方向是否相同
                    float c2 = XVector2.Cross(cur, n2);

                    // 用来判断角度
                    float d2 = XVector2.Dot(cur, n2);

                    if (calCount++ >= 950)
                    {
                        Debug.LogWarning($"check e {f(e1.P1)} {f(e1.P2)} {f(e2.P1)} {f(e2.P2)} {cur.X} {cur.Y} {c1} {d1} {c2} {d2} ");
                    }

                    if (c1 >= 0 && c2 >= 0)
                    {
                        if (d1 > d2)
                        {
                            e1 = e2;
                            c1 = c2;
                            d1 = d2;
                        }
                    }
                    else if (c1 < 0 && c2 < 0)
                    {
                        if (d1 < d2)
                        {
                            e1 = e2;
                            c1 = c2;
                            d1 = d2;
                        }
                    }
                    else if (c2 < 0)
                    {
                        e1 = e2;
                        c1 = c2;
                        d1 = d2;
                    }
                }

                current = e1;
                result.Add(e1.P1);
            }

        } while (!current.P1.Equals(leftBottom));

        //Debug.LogWarning($"end--------------------------");
        return result;
    }

    /// <summary>
    /// </summary>
    /// <param name="points1"></param>
    /// <param name="points2"></param>
    /// <param name="newPoints1"></param>
    /// <param name="newPoints2"></param>
    /// <returns></returns>
    public static List<XVector2> Conbine(List<XVector2> points1, List<XVector2> points2, out List<XVector2> newPoints1, out List<XVector2> newPoints2)
    {
        for (int i = 0; i < points1.Count; i++)
        {
            XVector2 p1 = points1[i];
            XVector2 p2 = points1[(i + 1) % points1.Count];
            Edge e1 = new Edge(p1, p2);

            List<XVector2> newPoints = new List<XVector2>();

            for (int j = 0; j < points2.Count; j++)
            {
                XVector2 p3 = points2[j];
                XVector2 p4 = points2[(j + 1) % points2.Count];
                Edge e2 = new Edge(p3, p4);

                if (XMath.LineLine2(e1, e2, false, out XVector2 target))
                {
                    newPoints.Add(target);
                    int index = ++j;
                    if (index < points2.Count)
                        points2.Insert(index, target);
                    else
                        points2.Add(target);
                }
            }

            newPoints.Sort((a, b) =>
            {
                float d1 = XVector2.Distance(a, p1);
                float d2 = XVector2.Distance(b, p1);
                if (d1 > d2)
                    return 1;
                else if (d1 < d2)
                    return -1;
                return 0;
            });
            foreach (XVector2 newPoint in newPoints)
            {
                i++;
                if (i < points1.Count)
                    points1.Insert(i, newPoint);
                else
                    points1.Add(newPoint);
            }
        }

        Debug.LogWarning("poly 1");
        foreach (XVector2 p in points1)
        {
            Debug.LogWarning($"{p}");
        }

        Debug.LogWarning("poly 2");
        foreach (XVector2 p in points2)
        {
            Debug.LogWarning($"{p}");
        }

        // 找最小点
        XVector2 min = new XVector2(float.MaxValue, float.MaxValue);

        foreach (XVector2 p in points1)
        {
            if (p.X < min.X) min.X = p.X;
            if (p.Y < min.Y) min.Y = p.Y;
        }
        foreach (XVector2 p in points2)
        {
            if (p.X < min.X) min.X = p.X;
            if (p.Y < min.Y) min.Y = p.Y;
        }

        float distance = float.MaxValue;
        XVector2 leftBottom = default;
        foreach (XVector2 p in points1)
        {
            float dis = XVector2.Distance(p, min);
            if (dis < distance)
            {
                distance = dis;
                leftBottom = p;
            }
        }
        foreach (XVector2 p in points2)
        {
            float dis = XVector2.Distance(p, min);
            if (dis < distance)
            {
                distance = dis;
                leftBottom = p;
            }
        }

        Debug.LogWarning($" lfet bottom {min} {leftBottom}");
        List<XVector2> result = new List<XVector2>();
        Edge current = default;
        for (int i = 0; i < points1.Count; i++)
        {
            if (points1[i].Equals(leftBottom))
            {
                current = new Edge(points1[(i + 1) % points1.Count], leftBottom);
                break;
            }
        }
        for (int i = 0; i < points2.Count; i++)
        {
            if (points2[i].Equals(leftBottom))
            {
                current = new Edge(points2[(i + 1) % points2.Count], leftBottom);
                break;
            }
        }
        result.Add(current.P1);
        Debug.Log($"add {current.P1}");

        int calCount = 0;
        do
        {
            if (calCount++ >= 100)
            {
#if DEBUG_PATH
                Recorder.Show(null);
#endif
                Debug.LogError("Error happen");
                break;
            }

            Edge e1 = default;
            Edge e2 = default;

            for (int i = 0; i < points1.Count; i++)
            {
                XVector2 p = points1[i];
                if (p.Equals(current.P1))
                {
                    e1 = new Edge(points1[(i + 1) % points1.Count], current.P1);
                    break;
                }
            }
            for (int i = 0; i < points2.Count; i++)
            {
                XVector2 p = points2[i];
                if (p.Equals(current.P1))
                {
                    e2 = new Edge(points2[(i + 1) % points2.Count], current.P1);
                    break;
                }
            }

            if (e1 == null && e2 == null)
            {
#if DEBUG_PATH
                Recorder.Show(null);
#endif
                Debug.LogError($"error happen {(e1 == null)} {(e2 == null)} {current.P1} ");
                break;
            }
            else if (e1 == null)
            {
                current = e2;
                result.Add(e2.P1);
                Debug.Log($"add {e2.P1}");
            }
            else if (e2 == null)
            {
                current = e1;
                result.Add(e1.P1);
                Debug.Log($"add {e1.P1}");
            }
            else
            {
                XVector2 cur = XVector2.Normalize(current.P2 - current.P1);
                XVector2 n1 = XVector2.Normalize(e1.P1 - e1.P2);
                XVector2 n2 = XVector2.Normalize(e2.P1 - e2.P2);

                // 用来判断方向是否相同
                float c1 = XVector2.Cross(cur, n1);
                float c2 = XVector2.Cross(cur, n2);

                // 用来判断角度
                float d1 = XVector2.Dot(cur, n1);
                float d2 = XVector2.Dot(cur, n2);

                Debug.LogWarning($" {current} {e1} {e2} {c1} {c2} {d1} {d2} ");

                if (c1 > 0 && c2 > 0)
                {
                    if (d1 < d2)
                    {
                        current = e1;
                        result.Add(e1.P1);
                        Debug.Log($"add {e1.P1}");
                    }
                    else
                    {
                        current = e2;
                        result.Add(e2.P1);
                        Debug.Log($"add {e2.P1}");
                    }
                }
                else if (c1 < 0 && c2 < 0)
                {
                    if (d1 > d2)
                    {
                        current = e1;
                        result.Add(e1.P1);
                        Debug.Log($"add {e1.P1}");
                    }
                    else
                    {
                        current = e2;
                        result.Add(e2.P1);
                        Debug.Log($"add {e2.P1}");
                    }
                }
                else if (c2 > 0)
                {
                    current = e1;
                    result.Add(e1.P1);
                    Debug.Log($"add {e1.P1}");
                }
                else
                {
                    current = e2;
                    result.Add(e2.P1);
                    Debug.Log($"add {e2.P1}");
                }
            }
        } while (!current.P1.Equals(leftBottom));


        List<Edge> edges = new List<Edge>();
        for (int i = 0; i < result.Count; i++)
        {
            edges.Add(new Edge(result[i], result[(i + 1) % result.Count]));
        }

        newPoints1 = points1;
        newPoints2 = points2;

        return result;
    }
}