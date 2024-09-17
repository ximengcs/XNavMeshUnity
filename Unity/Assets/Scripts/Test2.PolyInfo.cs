
using System.Collections.Generic;
using XFrame.PathFinding;
using static Test;

public partial class Test2
{
    private class PolyInfo
    {
        public Poly Poly;
        public XNavMesh NavMesh;
        public MeshArea MeshArea;
        public HalfEdgeData ChangeData;
        public List<Edge> ChangeLine;

        public void Dispose()
        {
            MeshArea.Dispose();
            MeshArea = null;
            ChangeLine = null;
        }
    }
}