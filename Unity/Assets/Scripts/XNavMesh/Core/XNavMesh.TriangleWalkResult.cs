
namespace XFrame.PathFinding
{
    public partial class XNavMesh
    {
        public enum PointTriangleRelation
        {
            None,
            In,
            On
        }

        public struct TriangleWalkResult
        {
            public PointTriangleRelation Relation;
            public HalfEdge Edge;
        }
    }
}
