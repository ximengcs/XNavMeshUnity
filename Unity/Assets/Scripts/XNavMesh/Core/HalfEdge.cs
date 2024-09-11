
namespace XFrame.PathFinding
{
    /// <summary>
    /// 半边
    /// </summary>
    public class HalfEdge
    {
        /// <summary>
        /// 这条半边指向的顶点
        /// </summary>
        public HalfEdgeVertex Vertex;

        /// <summary>
        /// 这条半边所属于的面
        /// </summary>
        public HalfEdgeFace Face;

        /// <summary>
        /// 共面的下一条半边(顺时针)
        /// </summary>
        public HalfEdge NextEdge;

        /// <summary>
        /// 半边的另一半
        /// </summary>
        public HalfEdge OppositeEdge;

        /// <summary>
        /// 共面的上一条半边
        /// </summary>
        public HalfEdge PrevEdge;

        public HalfEdge(HalfEdgeVertex vertex)
        {
            Vertex = vertex;
        }

        public bool PointEquals(HalfEdge edge)
        {
            return Vertex.Position.Equals(edge.Vertex.Position);
        }

        public bool EqualsEdge(Edge edge)
        {
            return Vertex.Position.Equals(edge.P1) && NextEdge.Vertex.Position.Equals(edge.P2);
        }

        public override string ToString()
        {
            return Vertex.ToString();
        }
    }
}
