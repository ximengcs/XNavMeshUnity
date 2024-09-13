
using System;
using System.Collections.Generic;
using UnityEngine;

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

        public bool Intersect(Triangle triangle)
        {
            Edge e1 = new Edge(P1, P2);
            Edge e2 = new Edge(P2, P3);
            Edge e3 = new Edge(P2, P3);
            Edge e4 = new Edge(triangle.P1, triangle.P2);
            Edge e5 = new Edge(triangle.P2, triangle.P3);
            Edge e6 = new Edge(triangle.P3, triangle.P1);

            if (XMath.LineLine(e1, e4, true)) return true;
            if (XMath.LineLine(e1, e5, true)) return true;
            if (XMath.LineLine(e1, e6, true)) return true;
            if (XMath.LineLine(e2, e4, true)) return true;
            if (XMath.LineLine(e2, e5, true)) return true;
            if (XMath.LineLine(e2, e6, true)) return true;
            if (XMath.LineLine(e3, e4, true)) return true;
            if (XMath.LineLine(e3, e5, true)) return true;
            if (XMath.LineLine(e3, e6, true)) return true;
            return false;
        }

        public bool Intersect(HalfEdgeFace face)
        {
            Edge e1 = new Edge(P1, P2);
            Edge e2 = new Edge(P2, P3);
            Edge e3 = new Edge(P3, P1);

            XVector2 p1 = face.Edge.Vertex.Position;
            XVector2 p2 = face.Edge.NextEdge.Vertex.Position;
            XVector2 p3 = face.Edge.PrevEdge.Vertex.Position;
            Edge e4 = new Edge(p1, p2);
            Edge e5 = new Edge(p2, p3);
            Edge e6 = new Edge(p3, p1);

            if (XMath.LineLine(e1, e4, true)) return true;
            if (XMath.LineLine(e1, e5, true)) return true;
            if (XMath.LineLine(e1, e6, true)) return true;
            if (XMath.LineLine(e2, e4, true)) return true;
            if (XMath.LineLine(e2, e5, true)) return true;
            if (XMath.LineLine(e2, e6, true)) return true;
            if (XMath.LineLine(e3, e4, true)) return true;
            if (XMath.LineLine(e3, e5, true)) return true;
            if (XMath.LineLine(e3, e6, true)) return true;
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

        public bool Equals(HalfEdgeFace face)
        {
            XVector2 p1 = face.Edge.Vertex.Position;
            XVector2 p2 = face.Edge.NextEdge.Vertex.Position;
            XVector2 p3 = face.Edge.PrevEdge.Vertex.Position;

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

        /// <summary>
        /// 确保列表中的所有三角形都是顺时针，如果不是则修正
        /// </summary>
        public void OrientTrianglesClockwise()
        {
            if (!GeometryUtility.IsTriangleOrientedClockwise(P1, P2, P3))
                ChangeOrientation();
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
