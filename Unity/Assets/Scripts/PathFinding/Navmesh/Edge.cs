
using System;

namespace XFrame.PathFinding
{
    public struct Edge
    {
        public XVector2 P1;
        public XVector2 P2;

        public Edge(XVector2 p1, XVector2 p2)
        {
            P1 = p1;
            P2 = p2;
        }

        public override string ToString()
        {
            return $" ({P1} -> {P2}) ";
        }

        public bool Equals(XVector2 p1, XVector2 p2)
        {
            return (P1.Equals(p1) && P2.Equals(p2)) || (P1.Equals(p2) && P2.Equals(p1));
        }

        public override bool Equals(object obj)
        {
            if (obj is Edge edge)
                return Equals(edge.P1, edge.P2);
            if (obj is HalfEdge hEdge)
                return hEdge.Equals(this);
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(P1, P2);
        }

        public static bool operator ==(Edge left, Edge right)
        {
            if (ReferenceEquals(left, default))
                return ReferenceEquals(right, default);
            else if (ReferenceEquals(right, default))
                return false;
            return left.P1.Equals(right.P1) && left.P2.Equals(right.P2);
        }

        public static bool operator !=(Edge left, Edge right)
        {
            return !(left == right);
        }
    }
}
