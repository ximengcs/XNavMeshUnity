
using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
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

        public static float Cos(float angle)
        {
            return (float)Math.Cos(angle * Math.PI / 180.0);
        }

        public static float Sin(float angle)
        {
            return (float)Math.Sin(angle * Math.PI / 180.0);
        }

        public static bool Equals(float a, float b)
        {
            float gap = a - b;
            return gap <= EPSILON && gap >= -EPSILON;
        }

        public static bool CheckPointOnTriangleLine(Triangle triangle, XVector2 point, out XVector2 oppositePoint)
        {
            XVector2 p1 = triangle.P1;
            XVector2 p2 = triangle.P2;
            XVector2 p3 = triangle.P3;
            if (Det2(p1 - p2, p1 - point) == 0)  // 行列式为0
            {
                oppositePoint = p3;
                return true;
            }

            if (Det2(p2 - p3, p2 - point) == 0)
            {
                oppositePoint = p1;
                return true;
            }

            if (Det2(p3 - p1, p3 - point) == 0)
            {
                oppositePoint = p2;
                return true;
            }

            oppositePoint = default;
            return false;
        }

        public static float Angle(XVector2 a, XVector2 b)
        {
            float dot = a.X*b.X + a.Y*b.Y;
            float det = a.X*b.Y - a.Y*b.X;
            float angle = (float)Math.Atan2(det, dot);
            return angle;
        }

        public static float Dot(XVector2 a, XVector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
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

        //
        // Are two lines intersecting?
        //
        //http://thirdpartyninjas.com/blog/2008/10/07/line-segment-intersection/
        //Notice that there are more than one way to test if two line segments are intersecting
        //but this is the fastest according to https://www.habrador.com/tutorials/math/5-line-line-intersection/
        public static bool LineLine(Edge a, Edge b, bool includeEndPoints, bool highPrecision = false)
        {
            //To avoid floating point precision issues we can use a small value
            float epsilon = EPSILON;
            if (highPrecision)
                epsilon = float.Epsilon;

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

        public static bool LineLine2(Edge e1, Edge e2, bool includeEndPoints, out XVector2 intersectPoint)
        {
            float tolerance = EPSILON;
            intersectPoint = default;

            XVector2 v1 = e1.P1;
            XVector2 v2 = e1.P2;
            XVector2 v3 = e2.P1;
            XVector2 v4 = e2.P2;

            float a = Det2(v1.X - v2.X, v1.Y - v2.Y, v3.X - v4.X, v3.Y - v4.Y);
            if (Math.Abs(a) < tolerance) // Lines are parallel
            {
                return false;
            }

            float d1 = Det2(v1.X, v1.Y, v2.X, v2.Y);
            float d2 = Det2(v3.X, v3.Y, v4.X, v4.Y);
            float x = Det2(d1, v1.X - v2.X, d2, v3.X - v4.X) / a;
            float y = Det2(d1, v1.Y - v2.Y, d2, v3.Y - v4.Y) / a;

            if (includeEndPoints)
            {
                if (x <= Math.Min(v1.X, v2.X) - tolerance || x >= Math.Max(v1.X, v2.X) + tolerance) return false;
                if (y <= Math.Min(v1.Y, v2.Y) - tolerance || y >= Math.Max(v1.Y, v2.Y) + tolerance) return false;
                if (x <= Math.Min(v3.X, v4.X) - tolerance || x >= Math.Max(v3.X, v4.X) + tolerance) return false;
                if (y <= Math.Min(v3.Y, v4.Y) - tolerance || y >= Math.Max(v3.Y, v4.Y) + tolerance) return false;
            }
            else
            {
                if (x < Math.Min(v1.X, v2.X) - tolerance || x > Math.Max(v1.X, v2.X) + tolerance) return false;
                if (y < Math.Min(v1.Y, v2.Y) - tolerance || y > Math.Max(v1.Y, v2.Y) + tolerance) return false;
                if (x < Math.Min(v3.X, v4.X) - tolerance || x > Math.Max(v3.X, v4.X) + tolerance) return false;
                if (y < Math.Min(v3.Y, v4.Y) - tolerance || y > Math.Max(v3.Y, v4.Y) + tolerance) return false;
            }

            intersectPoint = new XVector2(x, y);
            return true;
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

        public static bool CheckPointsHasSame(XVector2 p1, XVector2 p2, XVector2 p3)
        {
            return (Equals(p1.X, p2.X) && Equals(p2.X, p3.X)) || (Equals(p1.Y, p2.Y) && Equals(p2.Y, p3.Y));
        }

        public static void FindMinMaxPoint(Triangle triangle, out XVector2 min, out XVector2 max)
        {
            XVector2 p1 = triangle.P1;
            XVector2 p2 = triangle.P2;
            XVector2 p3 = triangle.P3;

            min = p1;
            max = p1;

            if (min.X > p2.X) min.X = p2.X;
            if (min.X > p3.X) min.X = p3.X;
            if (max.X < p2.X) max.X = p2.X;
            if (max.X < p3.X) max.X = p3.X;

            if (min.Y > p2.Y) min.Y = p2.Y;
            if (min.Y > p3.Y) min.Y = p3.Y;
            if (max.Y < p2.Y) max.Y = p2.Y;
            if (max.Y < p3.Y) max.Y = p3.Y;

            // 排序
            if (p1.Equals(min))
            {
                if (!p2.Equals(max))
                {
                    XVector2 tmp = min;
                    min = max;
                    max = tmp;
                }
            }
            else if (p2.Equals(min))
            {
                if (!p3.Equals(max))
                {
                    XVector2 tmp = min;
                    min = max;
                    max = tmp;
                }
            }
            else if (p3.Equals(min))
            {
                if (!p1.Equals(max))
                {
                    XVector2 tmp = min;
                    min = max;
                    max = tmp;
                }
            }
        }
    }
}
