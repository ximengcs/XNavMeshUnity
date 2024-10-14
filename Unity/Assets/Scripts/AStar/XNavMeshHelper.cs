
using Simon001.PathFinding;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XFrame.PathFinding
{
    internal class XNavMeshHelper : IAStarHelper
    {
        private HalfEdgeData m_Data;

        public XNavMeshHelper(HalfEdgeData data)
        {
            m_Data = data;
        }

        public int GetGValue(IAStarItem from, IAStarItem to)
        {
            HalfEdgeFace f1 = from as HalfEdgeFace;
            HalfEdgeFace f2 = to as HalfEdgeFace;

            if (f2.Area == AreaType.Obstacle)
                return int.MaxValue;

            if (f1.IsAdjacent(f2))
            {
                XVector2 p1 = new Triangle(f1).InnerCentrePoint;
                XVector2 p2 = new Triangle(f2).InnerCentrePoint;
                return (int)(XVector2.Distance(p1, p2) * 100000);
            }
            else
            {
                if(f1.GetSameVert(f2, out XVector2 insect))
                {
                    Func<Triangle, Triangle> fun = Test2.Normalizer.UnNormalize;
                    Func<XVector2, XVector2> fun2 = Test2.Normalizer.UnNormalize;
                    Debug.Log($" ===> {fun(new Triangle(f1))} {fun(new Triangle(f2))} {fun2(insect)} ");

                    XVector2 p1 = new Triangle(f1).InnerCentrePoint;
                    XVector2 p2 = new Triangle(f2).InnerCentrePoint;
                    return (int)(XVector2.Distance(p1, insect) * 100000) + (int)(XVector2.Distance(p2, insect) * 100000);
                }
                else
                {
                    XVector2 p1 = new Triangle(f1).InnerCentrePoint;
                    XVector2 p2 = new Triangle(f2).InnerCentrePoint;
                    return (int)(XVector2.Distance(p1, p2) * 100000);
                }
            }
        }

        public int GetHValue(IAStarItem start, IAStarItem end)
        {
            HalfEdgeFace f1 = start as HalfEdgeFace;
            HalfEdgeFace f2 = end as HalfEdgeFace;

            if (f2.Area == AreaType.Obstacle)
                return int.MaxValue;

            XVector2 p1 = new Triangle(f1).InnerCentrePoint;
            XVector2 p2 = new Triangle(f2).InnerCentrePoint;
            return (int)(XVector2.Distance(p1, p2) * 100000);
        }

        public void GetItemRound(IAStarItem item, List<IAStarItem> result)
        {
            HalfEdgeFace f = item as HalfEdgeFace;
            HalfEdge e1 = f.Edge;
            HalfEdge e2 = e1.NextEdge;
            HalfEdge e3 = e1.PrevEdge;

            HalfEdge ope1 = e1.OppositeEdge;
            HalfEdge ope2 = e2.OppositeEdge;
            HalfEdge ope3 = e3.OppositeEdge;

            if (ope1 != null)
            {
                HalfEdgeFace opf1 = ope1.Face;
                result.Add(opf1);

                HalfEdge e = ope1.NextEdge.OppositeEdge;
                while (e != null)
                {
                    HalfEdgeFace ef = e.Face;
                    if (ope2 != null && ef == ope2.Face) break;
                    if (ope3 != null && ef == ope3.Face) break;

                    result.Add(ef);
                    e = e.NextEdge.OppositeEdge;
                }
            }
            if (ope2 != null)
            {
                HalfEdgeFace opf2 = ope2.Face;
                result.Add(opf2);

                HalfEdge e = ope2.NextEdge.OppositeEdge;
                while (e != null)
                {
                    HalfEdgeFace ef = e.Face;

                    if (ope3 != null && ef == ope3.Face) break;
                    if (ope1 != null && ef == ope1.Face) break;

                    result.Add(ef);
                    e = e.NextEdge.OppositeEdge;
                }
            }
            if (ope3 != null)
            {
                HalfEdgeFace opf3 = ope3.Face;
                result.Add(opf3);

                HalfEdge e = ope3.NextEdge.OppositeEdge;
                while (e != null)
                {
                    HalfEdgeFace ef = e.Face;
                    if (ope1 != null && ef == ope1.Face) break;
                    if (ope2 != null && ef == ope2.Face) break;

                    result.Add(ef);
                    e = e.NextEdge.OppositeEdge;
                }
            }
        }

        public int GetUniqueId(IAStarItem item)
        {
            return item.GetHashCode();
        }
    }
}
