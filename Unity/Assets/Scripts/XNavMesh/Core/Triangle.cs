
using System;
using System.Collections.Generic;

namespace XFrame.PathFinding
{
    /// <summary>
    /// 三角形
    /// </summary>
    public struct Triangle
    {
        public XVector2 P1;
        public XVector2 P2;
        public XVector2 P3;

        public Edge E1 => new Edge(P1, P2);
        public Edge E2 => new Edge(P2, P3);
        public Edge E3 => new Edge(P3, P1);

        public Triangle(XVector2 p1, XVector2 p2, XVector2 p3)
        {
            P1 = p1;
            P2 = p2;
            P3 = p3;
        }

        public Triangle(List<XVector2> points)
        {
            P1 = points[0];
            P2 = points[1];
            P3 = points[2];
        }

        public Triangle(HalfEdgeFace face)
        {
            P1 = face.Edge.Vertex.Position;
            P2 = face.Edge.NextEdge.Vertex.Position;
            P3 = face.Edge.PrevEdge.Vertex.Position;
        }

        /// <summary>
        /// 三角形重心点
        /// </summary>
        public XVector2 CenterOfGravityPoint
        {
            get
            {
                return (P1 + P2 + P3) * (1f / 3);
            }
        }

        /// <summary>
        /// 三角形内心点
        /// </summary>
        public XVector2 InnerCentrePoint
        {
            get
            {
                float a = XVector2.Distance(P1, P2);
                float b = XVector2.Distance(P2, P3);
                float c = XVector2.Distance(P3, P1);
                return new XVector2(
                    (a * P1.X + b * P2.X + c * P3.X) / (a + b + c),
                    (a * P1.Y + b * P2.Y + c * P3.Y) / (a + b + c));
            }
        }

        /// <summary>
        /// 三角形外心
        /// </summary>
        public XVector2 OuterCentrePoint
        {
            get
            {
                float x1 = P1.X;
                float x2 = P2.X;
                float x3 = P3.X;
                float y1 = P1.Y;
                float y2 = P2.Y;
                float y3 = P3.Y;

                float a1 = 2 * (x2 - x1);
                float b1 = 2 * (y2 - y1);
                float c1 = x2 * x2 + y2 * y2 - x1 * x1 - y1 * y1;
                float a2 = 2 * (x3 - x2);
                float b2 = 2 * (y3 - y2);
                float c2 = x3 * x3 + y3 * y3 - x2 * x2 - y2 * y2;

                float x = ((c1 * b2) - (c2 * b1)) / ((a1 * b2) - (a2 * b1));
                float y = ((a1 * c2) - (a2 * c1)) / ((a1 * b2) - (a2 * b1));
                return new XVector2(x, y);
            }
        }

        public bool Intersect2(XVector2 p1, XVector2 p2)
        {
            Edge e1 = new Edge(P1, P2);
            Edge e2 = new Edge(P2, P3);
            Edge e3 = new Edge(P3, P1);
            Edge e4 = new Edge(p1, p2);
            if (XMath.LineLine2(e1, e4, true, out XVector2 intersectPoint)) return true;
            if (XMath.LineLine2(e2, e4, true, out intersectPoint)) return true;
            if (XMath.LineLine2(e3, e4, true, out intersectPoint)) return true;
            return false;
        }

        public bool Contains(XVector2 point)
        {
            return GeometryUtility.PointTriangle(this, point, true);
        }

        public bool Has(XVector2 point)
        {
            return P1.Equals(point) || P2.Equals(point) || P3.Equals(point);
        }

        public bool Has(HalfEdge edge)
        {
            return Has(edge.Vertex.Position);
        }

        public bool Equals(XVector2 p1, XVector2 p2, XVector2 p3)
        {
            float checkNum = p1.X + p1.Y + p2.X + p2.Y + p3.X + p3.Y;
            float selfCheckNum = P1.X + P1.Y + P2.X + P2.Y + P3.X + P3.Y;

            if (!XMath.Equals(checkNum, selfCheckNum))
                return false;

            if (p1.Equals(P1))
            {
                if (p2.Equals(P2))
                {
                    return p3.Equals(P3);
                }
                else if (p3.Equals(P2))
                {
                    return p2.Equals(P3);
                }
            }
            else if (p1.Equals(P2))
            {
                if (p2.Equals(P1))
                {
                    return p3.Equals(P3);
                }
                else if (p3.Equals(P1))
                {
                    return p2.Equals(P3);
                }
            }
            else if (p1.Equals(P3))
            {
                if (p2.Equals(P1))
                {
                    return p3.Equals(P2);
                }
                else if (p3.Equals(P1))
                {
                    return p2.Equals(P2);
                }
            }

            return false;
        }

        public bool Equals(Triangle triangle)
        {
            return Equals(triangle.P1, triangle.P2, triangle.P3);
        }

        public bool Equals(HalfEdgeFace face)
        {
            XVector2 p1 = face.Edge.Vertex.Position;
            XVector2 p2 = face.Edge.NextEdge.Vertex.Position;
            XVector2 p3 = face.Edge.PrevEdge.Vertex.Position;
            return Equals(p1, p2, p3);
        }


        /// <summary>
        /// 1.Define the vectors a = P2 - P1 and b = P3 - P1. The vectors define the sides of the triangle when it is translated to the origin.
        /// 2.Generate random uniform values u1, u2 ~ U(0,1)
        /// 3.If u1 + u2 > 1, apply the transformation u1 → 1 - u1 and u2 → 1 - u2.
        /// 4.Form w = u1 a + u2 b, which is a random point in the triangle at the origin.
        /// 5.The point w + P1 is a random point in the original triangle.
        /// </summary>
        /// <see cref="https://blogs.sas.com/content/iml/2020/10/19/random-points-in-triangle.html"/>
        /// <returns></returns>
        public XVector2 RandomPoint()
        {
            XVector2 a = P2 - P1;
            XVector2 b = P3 - P1;
            float u1 = UnityEngine.Random.Range(0.0f, 1.0f);
            float u2 = UnityEngine.Random.Range(0.0f, 1.0f);
            if (u1 + u2 > 1)
            {
                u1 = 1 - u1;
                u2 = 1 - u2;
            }
            XVector2 w = u1 * a + u2 * b;
            return w + P1;
        }

        /// <summary>
        /// 确保列表中的所有三角形都是顺时针，如果不是则修正
        /// </summary>
        public void OrientTrianglesClockwise()
        {
            if (!GeometryUtility.IsTriangleOrientedClockwise(P1, P2, P3))
                ChangeOrientation();
        }

        public bool IsClockwise()
        {
            return GeometryUtility.IsTriangleOrientedClockwise(P1, P2, P3);
        }

        /// <summary>
        /// 改变三角形方向 P1->P2->P3  =>  P3->P2->P1
        /// </summary>
        public void ChangeOrientation()
        {
            (P1, P2) = (P2, P1);
        }

        /// <summary>
        /// 获取三角形x轴最小点，在检测与AABB相交时很有用
        /// </summary>
        /// <returns>x轴最小点</returns>
        public float MinX()
        {
            return Math.Min(P1.X, Math.Min(P2.X, P3.X));
        }

        /// <summary>
        /// 获取三角形x轴最大点，在检测与AABB相交时很有用
        /// </summary>
        /// <returns>x轴最大点</returns>
        public float MaxX()
        {
            return Math.Max(P1.X, Math.Max(P2.X, P3.X));
        }
        /// <summary>
        /// 获取三角形y轴最小点，在检测与AABB相交时很有用
        /// </summary>
        /// <returns>y轴最小点</returns>
        public float MinY()
        {
            return Math.Min(P1.Y, Math.Min(P2.Y, P3.Y));
        }

        /// <summary>
        /// 获取三角形y轴最大点，在检测与AABB相交时很有用
        /// </summary>
        /// <returns>y轴最大点</returns>
        public float MaxY()
        {
            return Math.Max(P1.Y, Math.Max(P2.Y, P3.Y));
        }

        /// <summary>
        /// 获取三角形顶点的对边(不能传入非三角形点)
        /// </summary>
        /// <param name="p">点</param>
        /// <returns>边</returns>
        public Edge FindOppositeEdgeToVertex(XVector2 p)
        {
            if (p.Equals(P1))
            {
                return new Edge(P2, P3);
            }
            else if (p.Equals(P2))
            {
                return new Edge(P3, P1);
            }
            else
            {
                return new Edge(P1, P2);
            }
        }

        /// <summary>
        /// 检查给定边是否是三角形的边
        /// </summary>
        /// <param name="e">需要检查的边</param>
        /// <returns>true表示给定边是三角形的边</returns>
        public bool IsEdgePartOfTriangle(Edge e)
        {
            if ((e.P1.Equals(P1) && e.P2.Equals(P2)) || (e.P1.Equals(P2) && e.P2.Equals(P1)))
            {
                return true;
            }
            if ((e.P1.Equals(P2) && e.P2.Equals(P3)) || (e.P1.Equals(P3) && e.P2.Equals(P2)))
            {
                return true;
            }
            if ((e.P1.Equals(P3) && e.P2.Equals(P1)) || (e.P1.Equals(P1) && e.P2.Equals(P3)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 取得不是给定边的另一个三角形顶点
        /// </summary>
        /// <param name="e">给定边</param>
        /// <returns>另一个顶点</returns>
        public XVector2 GetVertexWhichIsNotPartOfEdge(Edge e)
        {
            if (!P1.Equals(e.P1) && !P1.Equals(e.P2))
            {
                return P1;
            }
            if (!P2.Equals(e.P1) && !P2.Equals(e.P2))
            {
                return P2;
            }

            return P3;
        }

        public List<XVector2> ToPoints()
        {
            return new List<XVector2>()
            {
                P1 , P2 , P3
            };
        }

        public override string ToString()
        {
            return $"({P1}, {P2}, {P3})";
        }

        public static Triangle operator +(Triangle triangle, XVector2 v)
        {
            triangle.P1 += v;
            triangle.P2 += v;
            triangle.P3 += v;
            return triangle;
        }
    }
}
