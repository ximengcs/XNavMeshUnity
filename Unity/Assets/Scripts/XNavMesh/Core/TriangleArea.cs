
using TMPro;

namespace XFrame.PathFinding
{
    public struct TriangleArea
    {
        public Triangle Shape;
        public AreaType Area;
        public int PolyId;

        public bool IsSide;
        public bool E1HasOpposite;
        public bool E2HasOpposite;
        public bool E3HasOpposite;

        internal TriangleArea(HalfEdgeFace face, int polyId, Normalizer normalizer)
        {
            PolyId = polyId;
            Shape = normalizer.UnNormalize(new Triangle(face));
            Area = face.Area;
            IsSide = face.IsSide;

            HalfEdge e1 = face.Edge;
            HalfEdge e2 = e1.NextEdge;
            HalfEdge e3 = e2.NextEdge;
            E1HasOpposite = InnerCheckHasOpposite(IsSide, e1, e2);
            E2HasOpposite = InnerCheckHasOpposite(IsSide, e2, e3);
            E3HasOpposite = InnerCheckHasOpposite(IsSide, e3, e1);
        }

        private static bool InnerCheckHasOpposite(bool isSide, HalfEdge e1, HalfEdge e2)
        {
            if (isSide)
                return false;

            if (e2.OppositeEdge != null)
            {
                XVector2 p1 = e1.Vertex.Position;
                XVector2 p2 = e2.Vertex.Position;
                XVector2 p3 = e2.OppositeEdge.Vertex.Position;
                XVector2 p4 = e2.OppositeEdge.PrevEdge.Vertex.Position;

                if (p1.Equals(p3) && p2.Equals(p4))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
