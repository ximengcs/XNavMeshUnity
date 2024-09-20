
using System.Collections.Generic;
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
