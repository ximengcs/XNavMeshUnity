
namespace XFrame.PathFinding
{
    public struct TriangleArea
    {
        public HalfEdgeFace Face;
        public Triangle Shape;
        public AreaType Area;
        public Normalizer Normalizer;

        public TriangleArea(HalfEdgeFace face, Normalizer normalizer)
        {
            Face = face;
            Shape = normalizer.UnNormalize(new Triangle(face));
            Area = Face.Area;
            Normalizer = normalizer;
        }
    }
}
