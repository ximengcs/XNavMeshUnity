
using System.Collections.Generic;

namespace XFrame.PathFinding
{
    /// <summary>
    /// <see cref="https://www.graphics.rwth-aachen.de/media/openmesh_static/Documentations/OpenMesh-6.3-Documentation/a00010.html"/>
    /// 半边数据结构
    /// </summary>
    public class HalfEdgeData
    {
        /// <summary>
        /// 所有的顶点
        /// </summary>
        public HashSet<HalfEdgeVertex> Vertices;

        /// <summary>
        /// 所有的面
        /// </summary>
        public HashSet<HalfEdgeFace> Faces;

        /// <summary>
        /// 所有的边
        /// </summary>
        public HashSet<HalfEdge> Edges;

        public HalfEdgeData()
        {
            Vertices = new HashSet<HalfEdgeVertex>();
            Faces = new HashSet<HalfEdgeFace>();
            Edges = new HashSet<HalfEdge>();
        }

        public bool CheckValid()
        {
            HashSet<HalfEdge> edges = new HashSet<HalfEdge>();
            HashSet<HalfEdgeVertex> vertes = new HashSet<HalfEdgeVertex>();
            foreach (HalfEdgeFace face in Faces)
            {
                HalfEdge e1 = face.Edge;
                HalfEdge e2 = e1.NextEdge;
                HalfEdge e3 = e2.NextEdge;
                edges.Add(e1);
                edges.Add(e2);
                edges.Add(e3);

                if (!Edges.Contains(e1) || !Edges.Contains(e2) || !Edges.Contains(e3))
                {
                    UnityEngine.Debug.LogError("check edge valid error");
                    return false;
                }

                HalfEdgeVertex v1 = e1.Vertex;
                HalfEdgeVertex v2 = e2.Vertex;
                HalfEdgeVertex v3 = e3.Vertex;
                vertes.Add(v1);
                vertes.Add(v2);
                vertes.Add(v3);
                if (!Vertices.Contains(v1) || !Vertices.Contains(v2) || !Vertices.Contains(v3))
                {
                    UnityEngine.Debug.LogError("check vert valid error");
                    return false;
                }
            }

            if (edges.Count != Edges.Count)
            {
                UnityEngine.Debug.LogError("check edge count valid error");
                return false;
            }
            if (vertes.Count != Vertices.Count)
            {
                UnityEngine.Debug.LogError("check vert count valid error");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 添加一个三角形
        /// </summary>
        /// <param name="triangle">三角形</param>
        public void AddTriangle(Triangle triangle)
        {
            //确保所有点都是顺时针
            triangle.OrientTrianglesClockwise();

            HalfEdgeVertex v1 = new HalfEdgeVertex(triangle.P1);
            HalfEdgeVertex v2 = new HalfEdgeVertex(triangle.P2);
            HalfEdgeVertex v3 = new HalfEdgeVertex(triangle.P3);

            HalfEdge he1 = new HalfEdge(v1);
            HalfEdge he2 = new HalfEdge(v2);
            HalfEdge he3 = new HalfEdge(v3);

            he1.NextEdge = he2;
            he2.NextEdge = he3;
            he3.NextEdge = he1;

            he1.PrevEdge = he3;
            he2.PrevEdge = he1;
            he3.PrevEdge = he2;

            v1.Edge = he2;
            v2.Edge = he3;
            v3.Edge = he1;

            HalfEdgeFace face = new HalfEdgeFace(he1);

            he1.Face = face;
            he2.Face = face;
            he3.Face = face;

            Edges.Add(he1);
            Edges.Add(he2);
            Edges.Add(he3);

            Faces.Add(face);

            Vertices.Add(v1);
            Vertices.Add(v2);
            Vertices.Add(v3);

            // 找出每条半边的另一边
            foreach (HalfEdge e in Edges)
            {
                HalfEdgeVertex goingToVertex = e.Vertex;
                HalfEdgeVertex goingFromVertex = e.PrevEdge.Vertex;

                foreach (HalfEdge eOther in Edges)
                {
                    if (e == eOther)
                    {
                        continue;
                    }

                    // 如果边的去向和来向相反，则为对边
                    if (goingFromVertex.Position.Equals(eOther.Vertex.Position) && goingToVertex.Position.Equals(eOther.PrevEdge.Vertex.Position))
                    {
                        e.OppositeEdge = eOther;
                        break;
                    }
                }
            }
        }
    }
}
