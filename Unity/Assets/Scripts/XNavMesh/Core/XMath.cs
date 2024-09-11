﻿
using System;
using System.Numerics;
using UnityEngine;

namespace XFrame.PathFinding
{
    /// <summary>
    /// 数学库
    /// </summary>
    public static class XMath
    {
        /// <summary>
        /// 这个值用于避免浮点数精度问题
        /// <see cref="http://sandervanrossen.blogspot.com/2009/12/realtime-csg-part-1.html"/>
        /// </summary>
        public const float EPSILON = 0.00001f;

        public const float PI = (float)Math.PI;

        public static bool Equals(float a, float b)
        {
            float gap = a - b;
            return gap <= EPSILON && gap >= -EPSILON;
        }

        public static float Det2(float x1, float x2, float y1, float y2)
        {
            return x1 * y2 - y1 * x2;
        }

        public static float Det2(XVector2 a, XVector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        public static int ClampListIndex(int index, int listSize)
        {
            index = ((index % listSize) + listSize) % listSize;

            return index;
        }

        public static bool CheckLineOutOfTriangle(Triangle triangle, HalfEdge e)
        {
            Edge lineEdge = e.ToEdge();
            if (!LineLine(triangle.E1, lineEdge, true) &&
                !LineLine(triangle.E2, lineEdge, true) &&
                !LineLine(triangle.E3, lineEdge, true))
            {
                if (!triangle.Contains(e.Vertex.Position))
                    return true;
            }
            return false;
        }

        //
        // Are two lines intersecting?
        //
        //http://thirdpartyninjas.com/blog/2008/10/07/line-segment-intersection/
        //Notice that there are more than one way to test if two line segments are intersecting
        //but this is the fastest according to https://www.habrador.com/tutorials/math/5-line-line-intersection/
        public static bool LineLine(Edge a, Edge b, bool includeEndPoints)
        {
            //To avoid floating point precision issues we can use a small value
            float epsilon = EPSILON;

            bool isIntersecting = false;

            float denominator = (b.P2.Y - b.P1.Y) * (a.P2.X - a.P1.X) - (b.P2.X - b.P1.X) * (a.P2.Y - a.P1.Y);

            //Make sure the denominator is != 0 (or the lines are parallel)
            if (denominator > 0f + epsilon || denominator < 0f - epsilon)
            {
                float u_a = ((b.P2.X - b.P1.X) * (a.P1.Y - b.P1.Y) - (b.P2.Y - b.P1.Y) * (a.P1.X - b.P1.X)) / denominator;
                float u_b = ((a.P2.X - a.P1.X) * (a.P1.Y - b.P1.Y) - (a.P2.Y - a.P1.Y) * (a.P1.X - b.P1.X)) / denominator;

                //Are the line segments intersecting if the end points are the same
                if (includeEndPoints)
                {
                    //The only difference between endpoints not included is the =, which will never happen so we have to subtract 0 by epsilon
                    float zero = 0f - epsilon;
                    float one = 1f + epsilon;

                    //Are intersecting if u_a and u_b are between 0 and 1 or exactly 0 or 1
                    if (u_a >= zero && u_a <= one && u_b >= zero && u_b <= one)
                    {
                        isIntersecting = true;
                    }
                }
                else
                {
                    float zero = 0f + epsilon;
                    float one = 1f - epsilon;

                    //Are intersecting if u_a and u_b are between 0 and 1
                    if (u_a > zero && u_a < one && u_b > zero && u_b < one)
                    {
                        isIntersecting = true;
                    }
                }

            }

            return isIntersecting;
        }

        //
        // Is a point intersecting with a circle?
        //
        //Is a point d inside, outside or on the same circle where a, b, c are all on the circle's edge
        public static IntersectionCases PointCircle(XVector2 a, XVector2 b, XVector2 c, XVector2 testPoint)
        {
            //Center of circle
            XVector2 circleCenter = CalculateCircleCenter(a, b, c);

            //The radius sqr of the circle
            float radiusSqr = XVector2.SqrDistance(a, circleCenter);

            //The distance sqr from the point to the circle center
            float distPointCenterSqr = XVector2.SqrDistance(testPoint, circleCenter);

            //Add/remove a small value becuse we will never be exactly on the edge because of floating point precision issues
            //Mutiply epsilon by two because we are using sqr root???
            if (distPointCenterSqr < radiusSqr - EPSILON * 2f)
            {
                return IntersectionCases.IsInside;
            }
            else if (distPointCenterSqr > radiusSqr + EPSILON * 2f)
            {
                return IntersectionCases.NoIntersection;
            }
            else
            {
                return IntersectionCases.IsOnEdge;
            }
        }

        //
        // Calculate the center of circle in 2d space given three coordinates
        //
        //From the book "Geometric Tools for Computer Graphics"
        public static XVector2 CalculateCircleCenter(XVector2 a, XVector2 b, XVector2 c)
        {
            //Make sure the triangle a-b-c is counterclockwise
            if (!GeometryUtility.IsTriangleOrientedClockwise(a, b, c))
            {
                //Swap two vertices to change orientation
                (a, b) = (b, a);

                //Debug.Log("Swapped vertices");
            }


            //The area of the triangle
            float X_1 = b.X - a.X;
            float X_2 = c.X - a.X;
            float Y_1 = b.Y - a.Y;
            float Y_2 = c.Y - a.Y;

            float A = 0.5f * XMath.Det2(X_1, Y_1, X_2, Y_2);

            //Debug.Log(A);


            //The center coordinates:
            //float L_10 = MyVector2.Magnitude(b - a);
            //float L_20 = MyVector2.Magnitude(c - a);

            //float L_10_square = L_10 * L_10;
            //float L_20_square = L_20 * L_20;

            float L_10_square = XVector2.SqrMagnitude(b - a);
            float L_20_square = XVector2.SqrMagnitude(c - a);

            float one_divided_by_4A = 1f / (4f * A);

            float x = a.X + one_divided_by_4A * ((Y_2 * L_10_square) - (Y_1 * L_20_square));
            float y = a.Y + one_divided_by_4A * ((X_1 * L_20_square) - (X_2 * L_10_square));

            XVector2 center = new XVector2(x, y);

            return center;
        }


        //In 2d space [radians]
        //If you want to calculate the angle from vector a to b both originating from c, from is a-c and to is b-c
        public static float AngleFromToCCW(XVector2 from, XVector2 to, bool shouldNormalize = false)
        {
            from = XVector2.Normalize(from);
            to = XVector2.Normalize(to);

            float angleRad = AngleBetween(from, to, shouldNormalize = false);

            //The determinant is similar to the dot product
            //The dot product is always 0 no matter in which direction the perpendicular vector is pointing
            //But the determinant is -1 or 1 depending on which way the perpendicular vector is pointing (up or down)
            //AngleBetween goes from 0 to 180 so we can now determine if we need to compensate to get 360 degrees
            if (XMath.Det2(from, to) > 0f)
            {
                return angleRad;
            }
            else
            {
                return (XMath.PI * 2f) - angleRad;
            }
        }

        //The angle between two vectors 0 <= angle <= 180
        //Same as Vector2.Angle() but we are using MyVector2
        public static float AngleBetween(XVector2 from, XVector2 to, bool shouldNormalize = true)
        {
            //from and to should be normalized
            //But sometimes they are already normalized and then we dont need to do it again
            if (shouldNormalize)
            {
                from = XVector2.Normalize(from);
                to = XVector2.Normalize(to);
            }

            //dot(a_normalized, b_normalized) = cos(alpha) -> acos(dot(a_normalized, b_normalized)) = alpha
            float dot = XVector2.Dot(from, to);

            //This shouldn't happen but may happen because of floating point precision issues
            dot = Mathf.Clamp(dot, -1f, 1f);

            float angleRad = Mathf.Acos(dot);

            return angleRad;
        }

        //Whats the coordinate of the intersection point between two lines in 2d space if we know they are intersecting
        //http://thirdpartyninjas.com/blog/2008/10/07/line-segment-intersection/        
        public static XVector2 GetLineLineIntersectionPoint(Edge a, Edge b)
        {
            float denominator = (b.P2.Y - b.P1.Y) * (a.P2.X - a.P1.X) - (b.P2.X - b.P1.X) * (a.P2.Y - a.P1.Y);

            float u_a = ((b.P2.X - b.P1.X) * (a.P1.Y - b.P1.Y) - (b.P2.Y - b.P1.Y) * (a.P1.X - b.P1.X)) / denominator;

            XVector2 intersectionPoint = a.P1 + u_a * (a.P2 - a.P1);

            return intersectionPoint;
        }
    }
}
