
namespace XFrame.PathFinding
{
    public class PoolUtility
    {
        public static XNavMeshList<TriangleArea> RequireTriangleList(int capacity)
        {
            return new XNavMeshList<TriangleArea>(capacity);
        }
    }
}
