
using System.Collections.Generic;
using UnityEngine;
using XFrame.PathFinding;

/// <summary>
/// 一个Poly具有多个三角形
/// </summary>
public class PolyUtility
{
    private class PolyVertex
    {
        public XVector2 Point;

        public PolyVertex(XVector2 point)
        {
            Point = point;
        }
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
        //result.Add(leftBottom);

        Edge current = null;
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
                Debug.LogError("Error happen");
                break;
            }

            Edge e1 = null;
            Edge e2 = null;

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