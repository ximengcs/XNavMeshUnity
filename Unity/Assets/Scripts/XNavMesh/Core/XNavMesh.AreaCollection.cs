
using System.Collections.Generic;
using UnityEngine;

namespace XFrame.PathFinding
{
    public partial class XNavMesh
    {
        private class AreaCollection
        {
            public List<List<XVector2>> PolyPoints;
            public List<Poly> Polies;

            public AreaCollection()
            {
                PolyPoints = new List<List<XVector2>>();
                Polies = new List<Poly>();
            }

            public void Add(Poly poly, List<XVector2> points)
            {
                Polies.Add(poly);
                PolyPoints.Add(points);
            }

            public bool Intersect(List<XVector2> targetPoints)
            {
                for (int i = 0; i < PolyPoints.Count; i++)
                {
                    List<XVector2> points = PolyPoints[i];
                    if (Poly.Intersect(points, targetPoints))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
