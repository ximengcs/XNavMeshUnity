﻿
using Simon001.PathFinding;
using System;
using System.Collections.Generic;

namespace XFrame.PathFinding
{
    internal class XNavMeshHelper : IAStarHelper
    {
        private HalfEdgeData m_Data;
        private bool m_ContainsOneIntersect;

        public XNavMeshHelper(HalfEdgeData data)
        {
            m_Data = data;
        }

        public int GetGValue(object from, object to)
        {
            HalfEdgeFace f1 = from as HalfEdgeFace;
            HalfEdgeFace f2 = to as HalfEdgeFace;

            //Func<Triangle, Triangle> fun = Test2.Normalizer.UnNormalize;
            //Func<XVector2, XVector2> fun2 = Test2.Normalizer.UnNormalize;

            if (f1.IsAdjacent(f2))
            {
                XVector2 p1 = new Triangle(f1).InnerCentrePoint;
                XVector2 p2 = new Triangle(f2).InnerCentrePoint;
                int value = (int)(XVector2.Distance(p1, p2) * 100000);
                //Debug.Log($" ---> {fun(new Triangle(f1))} ||||||||||||||||| {fun(new Triangle(f2))}                 {value} ");
                return value;
            }
            else
            {
                if (f1.GetSameVert(f2, out XVector2 insect))
                {
                    XVector2 p1 = new Triangle(f1).InnerCentrePoint;
                    XVector2 p2 = new Triangle(f2).InnerCentrePoint;
                    int value = (int)(XVector2.Distance(p1, insect) * 100000) + (int)(XVector2.Distance(p2, insect) * 100000);
                    //Debug.Log($" ===> {fun(new Triangle(f1))} ||||||||||||||||| {fun(new Triangle(f2))}                 {fun2(insect)} {value} ");
                    return value;
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public int GetHValue(object start, object end)
        {
            HalfEdgeFace f1 = start as HalfEdgeFace;
            HalfEdgeFace f2 = end as HalfEdgeFace;

            XVector2 p1 = new Triangle(f1).InnerCentrePoint;
            XVector2 p2 = new Triangle(f2).InnerCentrePoint;
            return (int)(XVector2.Distance(p1, p2) * 100000);
        }

        public void GetItemRound(object item, HashSet<object> result)
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
                if (opf1.Area != AreaType.Obstacle)
                    result.Add(opf1);

                HalfEdge e = ope1.NextEdge.OppositeEdge;
                while (m_ContainsOneIntersect && e != null)
                {
                    HalfEdgeFace ef = e.Face;
                    if (ope2 != null && ef == ope2.Face) break;
                    if (ope3 != null && ef == ope3.Face) break;

                    if (ef.Area != AreaType.Obstacle)
                        result.Add(ef);

                    e = e.NextEdge.OppositeEdge;
                }

                e = ope1;
                if (m_ContainsOneIntersect && e != null)
                {
                    int count = 0;
                    e = e.Face.FindSameValueVert(e1.PrevEdge);
                    e = e.PrevEdge.OppositeEdge;
                    while (e != null && count++ < 100)
                    {
                        HalfEdgeFace ef = e.Face;
                        if (ope2 != null && ef == ope2.Face) break;
                        if (ope3 != null && ef == ope3.Face) break;

                        if (ef.Area != AreaType.Obstacle)
                            if (!result.Contains(ef))
                                result.Add(ef);

                        e = e.PrevEdge.OppositeEdge;
                    }
                }
            } 

            if (ope2 != null)
            {
                HalfEdgeFace opf2 = ope2.Face;
                if (opf2.Area != AreaType.Obstacle)
                    result.Add(opf2);

                HalfEdge e = ope2.NextEdge.OppositeEdge;
                while (m_ContainsOneIntersect && e != null)
                {
                    HalfEdgeFace ef = e.Face;

                    if (ope3 != null && ef == ope3.Face) break;
                    if (ope1 != null && ef == ope1.Face) break;
                    if (ef.Area != AreaType.Obstacle)
                        if (!result.Contains(ef))
                            result.Add(ef);

                    e = e.NextEdge.OppositeEdge;
                }

                e = ope2;
                if (m_ContainsOneIntersect && e != null)
                {
                    int count = 0;
                    
                    e = e.Face.FindSameValueVert(e2.PrevEdge);
                    e = e.PrevEdge.OppositeEdge;
                    
                    while (e != null && count++ < 100)
                    {
                        HalfEdgeFace ef = e.Face;
                        if (ope3 != null && ef == ope3.Face) break;
                        if (ope1 != null && ef == ope1.Face) break;
                        if (ef.Area != AreaType.Obstacle)
                            if (!result.Contains(ef))
                                result.Add(ef);

                        e = e.PrevEdge.OppositeEdge;
                    }
                }

            }

            if (ope3 != null)
            {
                HalfEdgeFace opf3 = ope3.Face;
                if (opf3.Area != AreaType.Obstacle)
                    result.Add(opf3);

                HalfEdge e = ope3.NextEdge.OppositeEdge;
                while (m_ContainsOneIntersect && e != null)
                {
                    HalfEdgeFace ef = e.Face;
                    if (ope1 != null && ef == ope1.Face) break;
                    if (ope2 != null && ef == ope2.Face) break;
                    if (ef.Area != AreaType.Obstacle)
                        if (!result.Contains(ef))
                            result.Add(ef);

                    e = e.NextEdge.OppositeEdge;
                }

                e = ope3;
                if (m_ContainsOneIntersect && e != null)
                {
                    int count = 0;
                    e = e.Face.FindSameValueVert(e3.PrevEdge);
                    e = e.PrevEdge.OppositeEdge;
                    while (e != null && count++ < 100)
                    {
                        HalfEdgeFace ef = e.Face;
                        if (ope1 != null && ef == ope1.Face) break;
                        if (ope2 != null && ef == ope2.Face) break;
                        if (ef.Area != AreaType.Obstacle)
                            if (!result.Contains(ef))
                                result.Add(ef);

                        e = e.PrevEdge.OppositeEdge;
                    }
                }
            }
        }

        public List<XVector2> GetPathPoints(AStarPath path, XVector2 startPos, XVector2 endPos)
        {
            List<XVector2> points = new List<XVector2>();
            for (int i = 0; i < path.Count - 1; i++)
            {
                object a = path[i];
                object b = path[i + 1];
                List<XVector2> subPoints = GetPathPoints(a, b);
                for (int j = 0; j < subPoints.Count - 1; j++)
                    points.Add(subPoints[j]);
            }
            if (points.Count > 0)
                points[0] = endPos;
            else
                points.Add(endPos);
            points.Add(startPos);
            points.Reverse();
            return points;
        }

        public List<XVector2> GetPathPoints(object from, object to)
        {
            HalfEdgeFace f1 = from as HalfEdgeFace;
            HalfEdgeFace f2 = to as HalfEdgeFace;
            if (f1.IsAdjacent(f2))
            {
                return new List<XVector2>()
                {
                    new Triangle(f1).InnerCentrePoint,
                    new Triangle(f2).InnerCentrePoint
                };
            }
            else
            {
                if (f1.GetSameVert(f2, out XVector2 insect))
                {
                    return new List<XVector2>()
                    {
                        new Triangle(f1).InnerCentrePoint,
                        insect,
                        new Triangle(f2).InnerCentrePoint
                    };
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public int GetUniqueId(object item)
        {
            return item.GetHashCode();
        }
    }
}
