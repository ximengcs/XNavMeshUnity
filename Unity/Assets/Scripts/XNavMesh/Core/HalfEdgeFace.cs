
using Simon001.PathFinding;
using System;

namespace XFrame.PathFinding
{
    /// <summary>
    /// 半边结构面
    /// </summary>
    public class HalfEdgeFace : IAStarItem
    {
        /// <summary>
        /// 每个面持有它的半边的引用, 起始半边
        /// </summary>
        public HalfEdge Edge;

        public AreaType Area;

        public bool IsSide
        {
            get
            {
                XVector2 p1 = Edge.Vertex.Position;
                XVector2 p2 = Edge.NextEdge.Vertex.Position;
                XVector2 p3 = Edge.PrevEdge.Vertex.Position;
                return XMath.CheckPointsHasSame(p1, p2, p3);
            }
        }

        public HalfEdgeFace(HalfEdge edge)
        {
            Edge = edge;
            Area = AreaType.Walk;
        }

        public bool GetSameVert(HalfEdgeFace other, out XVector2 insect)
        {
            HalfEdge e1 = other.Edge;
            HalfEdge e2 = e1.NextEdge;
            HalfEdge e3 = e2.NextEdge;

            XVector2 p1 = e1.Vertex.Position;
            XVector2 p2 = e2.Vertex.Position;
            XVector2 p3 = e3.Vertex.Position;

            XVector2 p4 = Edge.Vertex.Position;
            XVector2 p5 = Edge.NextEdge.Vertex.Position;
            XVector2 p6 = Edge.PrevEdge.Vertex.Position;

            int count = 0;
            insect = default;
            if (p1.Equals(p4) || p1.Equals(p5) || p1.Equals(p6))
            {
                insect = p1;
                count++;
            }
            if (p2.Equals(p4) || p2.Equals(p5) || p2.Equals(p6))
            {
                insect = p2;
                count++;
            }
            if (p3.Equals(p4) || p3.Equals(p5) || p3.Equals(p6))
            {
                insect = p3;
                count++;
            }
            return count == 1;
        }

        public HalfEdge FindSameValueVert(HalfEdge edge)
        {
            XVector2 p = edge.Vertex.Position;
            if (Edge.Vertex.Position.Equals(p))
                return Edge;
            if (Edge.NextEdge.Vertex.Position.Equals(p))
                return Edge.NextEdge;
            if (Edge.PrevEdge.Vertex.Position.Equals(p))
                return Edge.PrevEdge;
            return null;
        }

        public bool IsAdjacent(HalfEdgeFace other)
        {
            HalfEdge e1 = other.Edge;
            HalfEdge e2 = e1.NextEdge;
            HalfEdge e3 = e2.NextEdge;

            if (e1.OppositeEdge != null)
            {
                if (e1.OppositeEdge.Face == this)
                    return true;
            }

            if (e2.OppositeEdge != null)
            {
                if (e2.OppositeEdge.Face == this)
                    return true;
            }

            if (e3.OppositeEdge != null)
            {
                if (e3.OppositeEdge.Face == this)
                    return true;
            }

            return false;
        }

        public bool Contains(XVector2 point)
        {
            return Edge.Vertex.Position.Equals(point) ||
                Edge.NextEdge.Vertex.Position.Equals(point) ||
                Edge.PrevEdge.Vertex.Position.Equals(point);
        }

        public bool FindEdge(Edge edge, out HalfEdge halfEdge)
        {
            HalfEdge next = Edge.NextEdge;
            HalfEdge prev = Edge.PrevEdge;
            if (Edge.EqualsEdge(edge))
            {
                halfEdge = Edge;
                return true;
            }
            if (next.EqualsEdge(edge))
            {
                halfEdge = next;
                return true;
            }
            if (prev.EqualsEdge(edge))
            {
                halfEdge = prev;
                return true;
            }
            halfEdge = null;
            return false;
        }

        public override string ToString()
        {
            return $" {Edge} -> {Edge.NextEdge} -> {Edge.NextEdge.NextEdge} ";
        }
    }
}
