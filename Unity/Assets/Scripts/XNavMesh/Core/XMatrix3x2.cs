
using System.Collections.Generic;

namespace XFrame.PathFinding
{
    public struct XMatrix2x3
    {
        public float M11;
        public float M12;
        public float M21;
        public float M22;
        public float M31;
        public float M32;

        public XMatrix2x3(XVector2 iBase, XVector2 jBase, XVector2 offset)
        {
            M11 = iBase.X;
            M12 = iBase.Y;
            M21 = jBase.X;
            M22 = jBase.Y;
            M31 = offset.X;
            M32 = offset.Y;
        }

        public static XMatrix2x3 ScaleTranslate(XVector2 scale, XVector2 offset)
        {
            return new XMatrix2x3(
                new XVector2(scale.X, 0),
                new XVector2(0, scale.Y),
                offset);
        }

        public static XMatrix2x3 RotateTranslate(float angle, XVector2 offset)
        {
            float cos = XMath.Cos(angle);
            float sin = XMath.Sin(angle);
            return new XMatrix2x3(new XVector2(cos, sin), new XVector2(-sin, cos), offset);
        }

        public static XMatrix2x3 Translate(XVector2 offset)
        {
            return new XMatrix2x3(new XVector2(1, 0), new XVector2(0, 1), offset);
        }

        public static void Multiply(XMatrix2x3 m, List<XVector2> points)
        {
            for (int i = 0; i < points.Count; i++)
                points[i] = m * points[i];
        }

        public static XVector2 operator *(XMatrix2x3 m, XVector2 v)
        {
            return new XVector2(m.M11 * v.X + m.M21 * v.Y + m.M31 * 1, m.M12 * v.X + m.M22 * v.Y + m.M32 * 1);
        }

        public static XMatrix2x3 operator *(XMatrix2x3 m, float v)
        {
            m.M11 *= v;
            m.M12 *= v;
            m.M21 *= v;
            m.M22 *= v;
            m.M31 *= v;
            m.M32 *= v;
            return m;
        }
    }
}
