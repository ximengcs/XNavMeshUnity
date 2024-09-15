using UnityEngine;
using XFrame.PathFinding;

public class DebugUtility
{
    public static void Print(HalfEdgeFace face, Normalizer normalizer)
    {
        HalfEdge e1 = face.Edge;
        HalfEdge e2 = e1.NextEdge;
        HalfEdge e3 = e2.NextEdge;
        Debug.Log($" [{normalizer.UnNormalize(e1.Vertex.Position)}, {normalizer.UnNormalize(e2.Vertex.Position)}, {normalizer.UnNormalize(e3.Vertex.Position)}] ");
    }

    public static void Print(Edge edge, Normalizer normalizer)
    {
        Debug.Log($" [{normalizer.UnNormalize(edge.P1)}, {normalizer.UnNormalize(edge.P2)}] ");
    }

    public static void Print(HalfEdge edge)
    {
        if (Debuger.Navmesh != null)
        {
            Debug.Log($" edge {Debuger.Navmesh.Normalizer.UnNormalize(edge.Vertex.Position)} {Debuger.Navmesh.Normalizer.UnNormalize(edge.NextEdge.Vertex.Position)} ");
        }
    }

    public static void Print(XVector2 p)
    {
        if (Debuger.Navmesh != null)
            Debug.Log($" [{Debuger.Navmesh.Normalizer.UnNormalize(p)}] ");
    }

    public static void Print(Triangle triangle)
    {
        if (Debuger.Navmesh != null)
            Debug.Log($" [{Debuger.Navmesh.Normalizer.UnNormalize(triangle.P1)},{Debuger.Navmesh.Normalizer.UnNormalize(triangle.P2)},{Debuger.Navmesh.Normalizer.UnNormalize(triangle.P3)}] ");
    }
}