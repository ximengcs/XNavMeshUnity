using System.Collections.Generic;
using System;

namespace XFrame.PathFinding.Extensions
{
    internal static class HalfEdgeExtension
    {
        public static Triangle SuperTriangle = new Triangle(new XVector2(-100f, -100f), new XVector2(100f, -100f), new XVector2(0f, 100f));

        public static HalfEdgeData GenerateConstraintData(List<Edge> edgeList, bool removeEdgeConstraint = true, List<List<XVector2>> extraPointsList = null)
        {
            int count = 0;
            int num = 999;
            HalfEdgeData tmpData = new HalfEdgeData();
            Triangle superTriangle = SuperTriangle;
            tmpData.AddTriangle(superTriangle);

            foreach (Edge e in edgeList)
            {
                //Debug.LogWarning($"add point {Normalizer.UnNormalize(e.P1)}");
                DelaunayIncrementalSloan.InsertNewPointInTriangulation(e.P1, tmpData);
                if (count++ >= num)
                    return tmpData;
            }

            List<XVector2> tmpList = new List<XVector2>();  // TO DO 
            foreach (Edge e in edgeList)
            {
                tmpList.Add(e.P1);

                Func<XVector2, XVector2> f = Test2.Normalizer.UnNormalize;
                //Debug.LogWarning("tmp point " + f(e.P1));
            }

            if (extraPointsList != null)
            {
                foreach (List<XVector2> extraPoints in extraPointsList)
                {
                    foreach (XVector2 v in extraPoints)
                    {
                        // TO DO 剔除重复的
                        bool find = false;
                        foreach (XVector2 tmp in tmpList)
                        {
                            if (tmp.Equals(v))
                            {
                                find = true;
                                break;
                            }
                        }

                        if (!find)
                        {
                            //Debug.LogWarning($"add point - {Normalizer.UnNormalize(v)}");
                            DelaunayIncrementalSloan.InsertNewPointInTriangulation(v, tmpData);
                            if (count++ >= num)
                            {
                                //DelaunayIncrementalSloan.RemoveSuperTriangle(superTriangle, tmpData);
                                return tmpData;
                            }
                        }
                    }

                }
            }

            if (extraPointsList != null)
            {
                // 最好等所有点添加完后再添加限制
                foreach (List<XVector2> extraPoints in extraPointsList)
                {
                    ConstrainedDelaunaySloan.AddConstraints(tmpData, extraPoints, false);
                }
            }

            DelaunayIncrementalSloan.RemoveSuperTriangle(superTriangle, tmpData);
            ConstrainedDelaunaySloan.AddConstraints(tmpData, tmpList, removeEdgeConstraint);

            //Debug.LogWarning("----------------------------------------");
            return tmpData;
        }

    }
}
