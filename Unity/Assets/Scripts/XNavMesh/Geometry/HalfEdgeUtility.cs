
namespace XFrame.PathFinding
{
    public class HalfEdgeUtility
    {
        public static int RandInt(int start, int end)
        {
            return UnityEngine.Random.Range(start, end);
        }

        //Test if we should flip an edge
        //a, b, c belongs to the triangle and d is the point on the other triangle
        //a-c is the edge, which is important so we can flip it, by making the edge b-d
        public static bool ShouldFlipEdge(XVector2 a, XVector2 b, XVector2 c, XVector2 d)
        {
            bool shouldFlipEdge = false;

            //Use the circle test to test if we need to flip this edge
            //We should flip if d is inside a circle formed by a, b, c
            IntersectionCases intersectionCases = XMath.PointCircle(a, b, c, d);

            if (intersectionCases == IntersectionCases.IsInside)
            {
                //Are these the two triangles forming a convex quadrilateral? Otherwise the edge cant be flipped
                if (GeometryUtility.IsQuadrilateralConvex(a, b, c, d))
                {
                    //If the new triangle after a flip is not better, then dont flip
                    //This will also stop the algorithm from ending up in an endless loop
                    IntersectionCases intersectionCases2 = XMath.PointCircle(b, c, d, a);

                    if (intersectionCases2 == IntersectionCases.IsOnEdge || intersectionCases2 == IntersectionCases.IsInside)
                    {
                        shouldFlipEdge = false;
                    }
                    else
                    {
                        shouldFlipEdge = true;
                    }
                }
            }

            return shouldFlipEdge;
        }

        //
        // Flip triangle edge
        //
        //So the edge shared by two triangles is going between the two other vertices originally not part of the edge
        public static void FlipTriangleEdge(HalfEdge e)
        {
            //The data we need
            //This edge's triangle edges
            HalfEdge e_1 = e;
            HalfEdge e_2 = e_1.NextEdge;
            HalfEdge e_3 = e_1.PrevEdge;
            //The opposite edge's triangle edges
            HalfEdge e_4 = e_1.OppositeEdge;
            HalfEdge e_5 = e_4.NextEdge;
            HalfEdge e_6 = e_4.PrevEdge;
            //The 4 vertex positions
            XVector2 aPos = e_1.Vertex.Position;
            XVector2 bPos = e_2.Vertex.Position;
            XVector2 cPos = e_3.Vertex.Position;
            XVector2 dPos = e_5.Vertex.Position;

            //The 6 old vertices, we can use
            HalfEdgeVertex a_old = e_1.Vertex;
            HalfEdgeVertex b_old = e_1.NextEdge.Vertex;
            HalfEdgeVertex c_old = e_1.PrevEdge.Vertex;
            HalfEdgeVertex a_opposite_old = e_4.PrevEdge.Vertex;
            HalfEdgeVertex c_opposite_old = e_4.Vertex;
            HalfEdgeVertex d_old = e_4.NextEdge.Vertex;

            //Flip

            //Vertices
            //Triangle 1: b-c-d
            HalfEdgeVertex b = b_old;
            HalfEdgeVertex c = c_old;
            HalfEdgeVertex d = d_old;
            //Triangle 1: b-d-a
            HalfEdgeVertex b_opposite = a_opposite_old;
            b_opposite.Position = bPos;
            HalfEdgeVertex d_opposite = c_opposite_old;
            d_opposite.Position = dPos;
            HalfEdgeVertex a = a_old;


            //Change half-edge - half-edge connections
            e_1.NextEdge = e_3;
            e_1.PrevEdge = e_5;

            e_2.NextEdge = e_4;
            e_2.PrevEdge = e_6;

            e_3.NextEdge = e_5;
            e_3.PrevEdge = e_1;

            e_4.NextEdge = e_6;
            e_4.PrevEdge = e_2;

            e_5.NextEdge = e_1;
            e_5.PrevEdge = e_3;

            e_6.NextEdge = e_2;
            e_6.PrevEdge = e_4;

            //Half-edge - vertex connection
            e_1.Vertex = b;
            e_2.Vertex = b_opposite;
            e_3.Vertex = c;
            e_4.Vertex = d_opposite;
            e_5.Vertex = d;
            e_6.Vertex = a;

            //Half-edge - face connection
            HalfEdgeFace f1 = e_1.Face;
            HalfEdgeFace f2 = e_4.Face;

            e_1.Face = f1;
            e_3.Face = f1;
            e_5.Face = f1;

            e_2.Face = f2;
            e_4.Face = f2;
            e_6.Face = f2;

            //Face - half-edge connection
            f1.Edge = e_3;
            f2.Edge = e_4;

            //Vertices connection, which should have a reference to a half-edge going away from the vertex
            //Triangle 1: b-c-d
            b.Edge = e_3;
            c.Edge = e_5;
            d.Edge = e_1;
            //Triangle 1: b-d-a
            b_opposite.Edge = e_4;
            d_opposite.Edge = e_6;
            a.Edge = e_2;

            //Opposite-edges are not changing!
            //And neither are we adding, removing data so we dont need to update the lists with all data
        }
    }
}
