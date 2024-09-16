
namespace XFrame.PathFinding
{
    public class Edge
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

        public static bool operator ==(Edge left, Edge right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);
            else if (ReferenceEquals(right, null))
                return false;
            return left.P1.Equals(right.P1) && left.P2.Equals(right.P2);
        }

        public static bool operator !=(Edge left, Edge right)
        {
            return !(left == right);
        }
    }
}
