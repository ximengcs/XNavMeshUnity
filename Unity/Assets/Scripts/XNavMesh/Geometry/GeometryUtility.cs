
using System;
using System.Collections.Generic;
using UnityEngine;

namespace XFrame.PathFinding
{
    public class GeometryUtility
    {
        public static Triangle SuperTriangle = new Triangle(new XVector2(-100f, -100f), new XVector2(100f, -100f), new XVector2(0f, 100f));

        /// <summary>
        /// 确保列表中的所有三角形都是顺时针，如果不是则修正
        /// </summary>
        /// <param name="triangles">三角形列表</param>
        /// <returns>修正的列表</returns>
        public static HashSet<Triangle> OrientTrianglesClockwise(HashSet<Triangle> triangles)
        {
            List<Triangle> trianglesList = new List<Triangle>(triangles);
            for (int i = 0; i < trianglesList.Count; i++)
            {
                Triangle t = trianglesList[i];

                if (!IsTriangleOrientedClockwise(t.P1, t.P2, t.P3))
                {
                    t.ChangeOrientation();
                    trianglesList[i] = t;
                }
            }

            triangles = new HashSet<Triangle>(trianglesList);
            return triangles;
        }

        /// <summary>
        /// 检查三角形是否为顺时针
        /// <a href="https://math.stackexchange.com/questions/1324179/how-to-tell-if-3-connected-points-are-connected-clockwise-or-counter-clockwise"/>
        /// <a href="https://en.wikipedia.org/wiki/Curve_orientation"/>
        /// </summary>
        /// <param name="p1">三角形点1</param>
        /// <param name="p2">三角形点2</param>
        /// <param name="p3">三角形点3</param>
        /// <returns>true表示顺时针</returns>
        public static bool IsTriangleOrientedClockwise(XVector2 p1, XVector2 p2, XVector2 p3)
        {
            bool isClockWise = true;

            float determinant = p1.X * p2.Y + p3.X * p1.Y + p2.X * p3.Y - p1.X * p3.Y - p3.X * p2.Y - p2.X * p1.Y;

            if (determinant > 0f)
                isClockWise = false;

            if (Math.Abs(determinant) <= float.Epsilon)
                return true;

            return isClockWise;
        }

        //
        // Is a quadrilateral convex? Assume no 3 points are colinear and the shape doesnt look like an hourglass
        //
        //A quadrilateral is a polygon with four edges (or sides) and four vertices or corners
        public static bool IsQuadrilateralConvex(XVector2 a, XVector2 b, XVector2 c, XVector2 d)
        {
            bool isConvex = false;

            //Convex if the convex hull includes all 4 points - will require just 4 determinant operations
            //In this case we dont kneed to know the order of the points, which is better
            //We could split it up into triangles, but still messy because of interior/exterior angles
            //Another version is if we know the edge between the triangles that form a quadrilateral
            //then we could measure the 4 angles of the edge, add them together (2 and 2) to get the interior angle
            //But it will still require 8 magnitude operations which is slow
            //From: https://stackoverflow.com/questions/2122305/convex-hull-of-4-points
            bool abc = IsTriangleOrientedClockwise(a, b, c);
            bool abd = IsTriangleOrientedClockwise(a, b, d);
            bool bcd = IsTriangleOrientedClockwise(b, c, d);
            bool cad = IsTriangleOrientedClockwise(c, a, d);

            if (abc && abd && bcd & !cad)
            {
                isConvex = true;
            }
            else if (abc && abd && !bcd & cad)
            {
                isConvex = true;
            }
            else if (abc && !abd && bcd & cad)
            {
                isConvex = true;
            }
            //The opposite sign, which makes everything inverted
            else if (!abc && !abd && !bcd & cad)
            {
                isConvex = true;
            }
            else if (!abc && !abd && bcd & !cad)
            {
                isConvex = true;
            }
            else if (!abc && abd && !bcd & !cad)
            {
                isConvex = true;
            }


            return isConvex;
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

        //From "A fast algortihm for generating constrained delaunay..."
        //Is numerically stable
        //v1, v2 should belong to the edge we ant to flip
        //v1, v2, v3 are counter-clockwise
        //Is also checking if the edge can be swapped
        public static bool ShouldFlipEdgeStable(XVector2 v1, XVector2 v2, XVector2 v3, XVector2 vp)
        {
            float x_13 = v1.X - v3.X;
            float x_23 = v2.X - v3.X;
            float x_1p = v1.X - vp.X;
            float x_2p = v2.X - vp.X;

            float y_13 = v1.Y - v3.Y;
            float y_23 = v2.Y - v3.Y;
            float y_1p = v1.Y - vp.Y;
            float y_2p = v2.Y - vp.Y;

            float cos_a = x_13 * x_23 + y_13 * y_23;
            float cos_b = x_2p * x_1p + y_2p * y_1p;

            if (cos_a >= 0f && cos_b >= 0f)
            {
                return false;
            }
            if (cos_a < 0f && cos_b < 0)
            {
                return true;
            }

            float sin_ab = (x_13 * y_23 - x_23 * y_13) * cos_b + (x_2p * y_1p - x_1p * y_2p) * cos_a;

            if (sin_ab < 0)
            {
                return true;
            }

            return false;
        }

        //
        // Is a point inside a triangle?
        //
        //From http://totologic.blogspot.se/2014/01/accurate-point-in-triangle-test.html
        public static bool PointTriangle(Triangle t, XVector2 p, bool includeBorder)
        {
            //To avoid floating point precision issues we can add a small value
            float epsilon = XMath.EPSILON;

            //Based on Barycentric coordinates
            float denominator = ((t.P2.Y - t.P3.Y) * (t.P1.X - t.P3.X) + (t.P3.X - t.P2.X) * (t.P1.Y - t.P3.Y));

            float a = ((t.P2.Y - t.P3.Y) * (p.X - t.P3.X) + (t.P3.X - t.P2.X) * (p.Y - t.P3.Y)) / denominator;
            float b = ((t.P3.Y - t.P1.Y) * (p.X - t.P3.X) + (t.P1.X - t.P3.X) * (p.Y - t.P3.Y)) / denominator;
            float c = 1 - a - b;

            bool isWithinTriangle = false;

            if (includeBorder)
            {
                float zero = 0f - epsilon;
                float one = 1f + epsilon;

                //The point is within the triangle or on the border
                if (a >= zero && a <= one && b >= zero && b <= one && c >= zero && c <= one)
                {
                    isWithinTriangle = true;
                }
            }
            else
            {
                float zero = 0f + epsilon;
                float one = 1f - epsilon;

                //The point is within the triangle
                if (a > zero && a < one && b > zero && b < one && c > zero && c < one)
                {
                    isWithinTriangle = true;
                }
            }

            return isWithinTriangle;
        }
    }
}
