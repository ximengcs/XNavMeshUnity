
using Simon001.PathFinding;
using System;
using System.Collections.Generic;

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
                return AStar.MAX_VALUE;

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

        public int GetHValue(IAStarItem start, IAStarItem end)
        {
            HalfEdgeFace f1 = start as HalfEdgeFace;
            HalfEdgeFace f2 = end as HalfEdgeFace;

            if (f2.Area == AreaType.Obstacle)
                return AStar.INVALID;

            XVector2 p1 = new Triangle(f1).InnerCentrePoint;
            XVector2 p2 = new Triangle(f2).InnerCentrePoint;
            return (int)(XVector2.Distance(p1, p2) * 100000);
        }

        public void GetItemRound(IAStarItem item, HashSet<IAStarItem> result)
        {
            HalfEdgeFace f = item as HalfEdgeFace;
            HalfEdge e1 = f.Edge;
            HalfEdge e2 = e1.NextEdge;
            HalfEdge e3 = e1.PrevEdge;

            HalfEdge ope1 = e1.OppositeEdge;
            HalfEdge ope2 = e2.OppositeEdge;
            HalfEdge ope3 = e3.OppositeEdge;

            //Func<Triangle, Triangle> fun = Test2.Normalizer.UnNormalize;
            //Func<XVector2, XVector2> fun2 = Test2.Normalizer.UnNormalize;
            //Debug.LogWarning($"check item around ~~~~~~~~~ {fun(new Triangle(f))}");

            //Debug.LogWarning($"check item around1 -------- {fun2(e1.Vertex.Position)}");
            if (ope1 != null)
            {
                HalfEdgeFace opf1 = ope1.Face;
                if (opf1.Area != AreaType.Obstacle)
                    result.Add(opf1);

                HalfEdge e = ope1.NextEdge.OppositeEdge;
                while (e != null)
                {
                    HalfEdgeFace ef = e.Face;
                    if (ope2 != null && ef == ope2.Face) break;
                    if (ope3 != null && ef == ope3.Face) break;

                    if (ef.Area != AreaType.Obstacle)
                        result.Add(ef);

                    e = e.NextEdge.OppositeEdge;
                }

                e = ope1;
                if (e != null)
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

                //foreach (HalfEdgeFace t in result)
                //{
                //    Debug.LogWarning($" {fun(new Triangle(t))} ");
                //}
            }
            //Debug.LogWarning("===============");

            //Debug.LogWarning($"check item around2 -------- {fun2(e2.Vertex.Position)}");
            if (ope2 != null)
            {
                HalfEdgeFace opf2 = ope2.Face;
                if (opf2.Area != AreaType.Obstacle)
                    result.Add(opf2);

                HalfEdge e = ope2.NextEdge.OppositeEdge;
                while (e != null)
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
                if (e != null)
                {
                    int count = 0;
                    //Debug.LogWarning($"check {fun(new Triangle(f))} {fun2(ope2.PrevEdge.Vertex.Position)} {fun(new Triangle(e.Face))} ");
                    e = e.Face.FindSameValueVert(e2.PrevEdge);
                    e = e.PrevEdge.OppositeEdge;
                    //Debug.LogWarning($"check after {fun2(e.Vertex.Position)} {fun(new Triangle(e.Face))}");
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

                //foreach (HalfEdgeFace t in result)
                //{
                //    Debug.LogWarning($" {fun(new Triangle(t))} ");
                //}
            }
            //Debug.LogWarning("===============");

            //Debug.LogWarning($"check item around3 -------- {fun2(e3.Vertex.Position)}");
            if (ope3 != null)
            {
                HalfEdgeFace opf3 = ope3.Face;
                if (opf3.Area != AreaType.Obstacle)
                    result.Add(opf3);

                HalfEdge e = ope3.NextEdge.OppositeEdge;
                while (e != null)
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
                if (e != null)
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

                //foreach (HalfEdgeFace t in result)
                //{
                //    Debug.LogWarning($" {fun(new Triangle(t))} ");
                //}
            }
            //Debug.LogWarning("===============");
        }

        public List<XVector2> GetPathPoints(AStarPath path, XVector2 startPos, XVector2 endPos)
        {
            List<XVector2> points = new List<XVector2>();
            for (int i = 0; i < path.Count - 1; i++)
            {
                IAStarItem a = path[i];
                IAStarItem b = path[i + 1];
                List<XVector2> subPoints = GetPathPoints(a, b);
                for (int j = 0; j < subPoints.Count - 1; j++)
                    points.Add(subPoints[j]);
            }
            points[0] = endPos;
            points.Add(startPos);
            points.Reverse();
            return points;
        }

        public List<XVector2> GetPathPoints(IAStarItem from, IAStarItem to)
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

        public int GetUniqueId(IAStarItem item)
        {
            return item.GetHashCode();
        }
    }
}
