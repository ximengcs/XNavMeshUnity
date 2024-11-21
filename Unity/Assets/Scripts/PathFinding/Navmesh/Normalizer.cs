using System.Collections.Generic;

namespace XFrame.PathFinding
{
    /// <summary>
    /// 为了避免浮点数精度问题，通常将所有数据归一化到0-1
    /// </summary>
    public class Normalizer
    {
        private float m_DMax;
        private AABB m_BoundingBox;

        public AABB AABB => m_BoundingBox;

        public float MinGap => 0.1f / m_DMax;

        public Normalizer(List<XVector2> points)
        {
            m_BoundingBox = new AABB(points);
            m_DMax = m_BoundingBox.DMax;
        }

        public Normalizer(AABB aabb)
        {
            m_BoundingBox = aabb;
            m_DMax = m_BoundingBox.DMax;
        }

        public XVector2 Constraint(XVector2 point)
        {
            return AABB.Constraint(point);
        }

        /// <summary>
        /// 归一化
        /// </summary>
        /// <param name="point">点</param>
        /// <returns>归一化点</returns>
        public XVector2 Normalize(XVector2 point)
        {
            float x = (point.X - m_BoundingBox.Min.X) / m_DMax;
            float y = (point.Y - m_BoundingBox.Min.Y) / m_DMax;

            return new XVector2(x, y);
        }

        public Triangle Normalize(Triangle triangle)
        {
            Triangle result = new Triangle();
            result.P1 = Normalize(triangle.P1);
            result.P2 = Normalize(triangle.P2);
            result.P3 = Normalize(triangle.P3);
            return result;
        }

        public void Normalize(List<XVector2> points)
        {
            for (int i = 0; i < points.Count; i++)
                points[i] = Normalize(points[i]);
        }

        /// <summary>
        /// 反归一化
        /// </summary>
        /// <param name="point">反归一化点</param>
        /// <returns>结果</returns>
        public XVector2 UnNormalize(XVector2 point)
        {
            float x = (point.X * m_DMax) + m_BoundingBox.Min.X;
            float y = (point.Y * m_DMax) + m_BoundingBox.Min.Y;

            return new XVector2(x, y);
        }

        public Triangle UnNormalize(Triangle triangle)
        {
            Triangle result = new Triangle();
            result.P1 = UnNormalize(triangle.P1);
            result.P2 = UnNormalize(triangle.P2);
            result.P3 = UnNormalize(triangle.P3);
            return result;
        }

        public void UnNormalize(List<Triangle> triangles)
        {
            for (int i = 0; i < triangles.Count; i++)
                triangles[i] = UnNormalize(triangles[i]);
        }

        public void UnNormalize(List<XVector2> points)
        {
            for (int i = 0; i < points.Count; i++)
                points[i] = UnNormalize(points[i]);
        }

        public void UnNormalize(List<Edge> edges)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                Edge e = edges[i];
                e.P1 = UnNormalize(e.P1);
                e.P2 = UnNormalize(e.P2);
                edges[i] = e;
            }
        }
    }
}
