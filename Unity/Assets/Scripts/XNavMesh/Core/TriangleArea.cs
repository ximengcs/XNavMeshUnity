
namespace XFrame.PathFinding
{
    public struct TriangleArea
    {
        public Triangle Shape;
        public AreaType Area;

        public TriangleArea(Triangle triangle, AreaType area)
        {
            Shape = triangle;
            Area = area;
        }

        public TriangleArea(XVector2 p1, XVector2 p2, XVector2 p3, AreaType area)
        {
            Shape = new Triangle(p1, p2, p3);
            Area = area;
        }
    }
}
