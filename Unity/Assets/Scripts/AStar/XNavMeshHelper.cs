
using Simon001.PathFinding;
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
                return int.MaxValue;

            XVector2 p1 = new Triangle(f1).CenterOfGravityPoint;
            XVector2 p2 = new Triangle(f2).CenterOfGravityPoint;
            return (int)(XVector2.Distance(p1, p2) * 100000);
        }

        public int GetHValue(IAStarItem start, IAStarItem end)
        {
            HalfEdgeFace f1 = start as HalfEdgeFace;
            HalfEdgeFace f2 = end as HalfEdgeFace;

            if (f2.Area == AreaType.Obstacle)
                return int.MaxValue;

            XVector2 p1 = new Triangle(f1).CenterOfGravityPoint;
            XVector2 p2 = new Triangle(f2).CenterOfGravityPoint;
            return (int)(XVector2.Distance(p1, p2) * 100000);
        }

        public void GetItemRound(IAStarItem item, List<IAStarItem> result)
        {
            HalfEdgeFace f = item as HalfEdgeFace;
            HalfEdge e1 = f.Edge;
            HalfEdge e2 = e1.NextEdge;
            HalfEdge e3 = e1.PrevEdge;
            if (e1.OppositeEdge != null) result.Add(e1.OppositeEdge.Face);
            if (e2.OppositeEdge != null) result.Add(e2.OppositeEdge.Face);
            if (e3.OppositeEdge != null) result.Add(e3.OppositeEdge.Face);
        }

        public int GetUniqueId(IAStarItem item)
        {
            return item.GetHashCode();
        }
    }
}
