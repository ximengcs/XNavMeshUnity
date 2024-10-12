
using Simon001.PathFinding;

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
