
namespace XFrame.PathFinding
{
    public struct TriangleArea
    {
        public HalfEdgeFace Face;
        public Triangle Shape;
        public AreaType Area;
        public Normalizer Normalizer;
        public int PolyId;

        public TriangleArea(HalfEdgeFace face, int polyId, Normalizer normalizer)
        {
            Face = face;
            PolyId = polyId;
            Shape = normalizer.UnNormalize(new Triangle(face));
            Area = Face.Area;
            Normalizer = normalizer;
        }
    }
}
