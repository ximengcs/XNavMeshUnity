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
        if (Test2.Normalizer != null)
        {
            Debug.Log($" edge {Test2.Normalizer.UnNormalize(edge.Vertex.Position)} {Test2.Normalizer.UnNormalize(edge.NextEdge.Vertex.Position)} ");
        }
    }

    public static void Print(XVector2 p)
    {
        if (Test2.Normalizer != null)
            Debug.Log($" [{Test2.Normalizer.UnNormalize(p)}] ");
    }

    public static void Print(Triangle triangle)
    {
        if (Test2.Normalizer != null)
            Debug.Log($" [{Test2.Normalizer.UnNormalize(triangle.P1)},{Test2.Normalizer.UnNormalize(triangle.P2)},{Test2.Normalizer.UnNormalize(triangle.P3)}] ");
    }
}