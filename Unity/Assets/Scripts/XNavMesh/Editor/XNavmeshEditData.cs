
using System.Collections.Generic;
using UnityEngine;

public class XNavmeshEditData
{
    public string Name;
    public EditMode EditMode;
    public Dictionary<string, List<(float, float)>> Areas;
    public float MinX;
    public float MaxX;
    public float MinY;
    public float MaxY;
    public float PointMinX;
    public float PointMinY;
    public float PointMaxX;
    public float PointMaxY;
}