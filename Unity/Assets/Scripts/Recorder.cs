
using System.Collections.Generic;
using UnityEngine;
using XFrame.PathFinding;

public static partial class Recorder
{
    public static Info CurrentInfo;
    public static Info LastInfo;

    public static void SetPolyId(int polyId)
    {
        CurrentInfo.PolyId = polyId;
    }

    public static void SetOldPoints(List<XVector2> oldPoints)
    {
        CurrentInfo.SetOldPoints(oldPoints);
    }

    public static void SetNewPoints(List<XVector2> newPoints)
    {
        CurrentInfo.SetNewPoints(newPoints);
    }

    public static void SetRelationFaces(HashSet<HalfEdgeFace> faces)
    {
        CurrentInfo.SetRelationFaces(faces);
    }

    public static void SetNewAreaOutEdges(List<Edge> newAreaOutEdges)
    {
        CurrentInfo.SetNewAreaOutEdges(newAreaOutEdges);
    }

    public static void SetPolies(Dictionary<int, Poly> polies)
    {
        CurrentInfo.SetPolies(polies);
    }

    public static void SetRelationNewPoint(Dictionary<Poly, List<XVector2>> list)
    {
        CurrentInfo.SetRelationNewPoint(list);
    }

    public static void SetRelationAllPoints(List<List<XVector2>> relationAllPoints)
    {
        CurrentInfo.SetRelationAllPoints(relationAllPoints);
    }

    public static void SetHalfEdgeData(HalfEdgeData data)
    {
        Debug.LogWarning("check data valid");
        Debug.LogWarning($" {data.CheckValid()} ");
        Debug.LogWarning("===================");
        CurrentInfo.CloneData = data.Clone();
        CurrentInfo.SetHalfEdgeData(data);
    }

    public static void MarkCurrent()
    {
        LastInfo = CurrentInfo;
        CurrentInfo = new Info();
    }
}
