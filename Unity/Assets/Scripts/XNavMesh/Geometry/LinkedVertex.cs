
namespace XFrame.PathFinding
{
    public class LinkedVertex
    {
        public XVector2 Pos;

        public LinkedVertex prevLinkedVertex;
        public LinkedVertex nextLinkedVertex;

        public LinkedVertex(XVector2 pos)
        {
            Pos = pos;
        }
    }
}
