using System.Collections.Generic;
using System.Text;
using UnityEngine;
using XFrame.PathFinding;

public class EdgeSet
{
    public XVector2 Start;
    public XVector2 End;

    public XVector2 Normalized;

    public List<XVector2> Vertices;

    public EdgeSet(XVector2 start, XVector2 end)
    {
        Start = start;
        End = end;
        Normalized = XVector2.Normalize(end - Start);
        Vertices = new List<XVector2>() { start, end };
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        foreach (XVector2 p in Vertices)
            sb.Append($" {p} ");
        return sb.ToString();
    }

    public List<XVector2> GetPoints(XVector2 start, XVector2 end, List<XVector2> points)
    {
        XVector2 nor = end - start;
        float c = XVector2.Cross(nor, Normalized);
        if (!XMath.Equals(c, 0))
        {
            Debug.LogError("error happen");
            return null;
        }

        float d = XVector2.Dot(nor, Normalized);
        if (d > 0)  // 同向
        {
            int index = -1;
            int vertCount = Vertices.Count;
            for (int i = 0; i < vertCount; i++)
            {
                if (Vertices[i].Equals(start))
                {
                    index = i;
                }
            }

            if (index == -1)
            {
                Debug.LogError("error happen");
                return null;
            }
            for (int i = 0; i < vertCount; i++)
            {
                XVector2 v = Vertices[(i + index) % vertCount];
                if (v.Equals(end))
                    break;
                points.Add(v);
            }
            return points;
        }
        else // 反向
        {
            int index = -1;
            int vertCount = Vertices.Count;
            for (int i = 0; i < vertCount; i++)
            {
                if (Vertices[i].Equals(start))
                {
                    index = i;
                }
            }

            if (index == -1)
            {
                Debug.LogError("error happen");
                return null;
            }

            index += vertCount;
            for (int i = 0; i < vertCount; i++)
            {
                XVector2 v = Vertices[index-- % vertCount];
                if (v.Equals(end))
                    break;
                points.Add(v);
            }
            return points;
        }
    }

    public bool InSameLine(EdgeSet edge)
    {
        return InSameLine(edge.Start, edge.End);
    }

    public bool InSameLine(XVector2 start, XVector2 end)
    {
        float c2;
        if (start.Equals(Start))
        {
            if (end.Equals(End))
                return true;
            else
                c2 = XVector2.Cross(start, End);
        }
        else
        {
            if (start.Equals(End) && end.Equals(Start))
            {
                return true;
            }
            c2 = XVector2.Cross(start, Start);
        }

        float c1 = XVector2.Cross(end - start, Normalized);
        if (XMath.Equals(c1, c2) && XMath.Equals(c1, 0))  // 两条线平行
        {
            float d1 = XMath.Dot(start - Start, Normalized);
            float d2 = XMath.Dot(end - Start, Normalized);
            float d3 = XMath.Dot(End - Start, Normalized);
            if (d1 < 0 && d2 < 0)
                return false;
            if (d1 <= 0 && d2 >= 0)  // 假设边的两个点不相同
                return true;
            if (d1 >= 0 && d2 <= 0)
                return true;
            if (d1 > 0 && d2 > 0)
            {
                if (d3 >= d2 && d3 <= d1)
                    return true;
                if (d3 >= d1 && d3 <= d2)
                    return true;
            }

            return false;
        }
        else
        {
            return false;
        }
    }

    public bool Intersect(EdgeSet e, out XVector2 newPoint)
    {
        return Intersect(e.Start, e.End, out newPoint);
    }

    public bool Intersect(XVector2 start, XVector2 end, out XVector2 newPoint)
    {
        return XMath.LineLine2(new Edge(Start, End), new Edge(start, end), true, out newPoint);
    }

    public bool Next(XVector2 point, out XVector2 target)
    {
        int count = Vertices.Count;
        for (int i = 0; i < count; i++)
        {
            if (Vertices[i].Equals(point))
            {
                target = Vertices[(i + 1) % count];
                return true;
            }
        }

        target = default;
        return false;
    }

    public void Add(XVector2 point)
    {
        if (point.Equals(Start)) return;
        if (point.Equals(End)) return;

        float c1 = XVector2.Cross(point - Start, Normalized);
        if (XMath.Equals(c1, 0))
        {
            float dot = XVector2.Dot(point - Start, Normalized);
            if (dot < 0)
            {
                Start = point;
                Vertices.Insert(0, point);
            }
            else
            {
                for (int i = 1; i < Vertices.Count; i++)
                {
                    float vDot = XVector2.Dot(Vertices[i] - Start, Normalized);
                    if (XMath.Equals(vDot, dot))
                        return;
                    if (dot < vDot)
                    {
                        Vertices.Insert(i, point);
                        return;
                    }
                }
                Vertices.Add(point);
                End = point;
            }
        }
    }
}
