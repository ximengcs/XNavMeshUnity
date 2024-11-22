
using System;

namespace XFrame.PathFinding
{
    public struct XVector2
    {
        public static XVector2 Zero = new XVector2(0f, 0f);
        public static XVector2 One = new XVector2(1f, 1f);

        public float X;
        public float Y;

        public XVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public bool EqualsFull(XVector2 other)
        {
            float xDiff = X - other.X;
            float yDiff = Y - other.Y;

            float e = float.Epsilon;

            //������е�ֵ����0"����"
            if (
                xDiff < e && xDiff > -e &&
                yDiff < e && yDiff > -e)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// ���������ά�����Ƿ����
        /// </summary>
        /// <param name="other">�Ƚϵ�����</param>
        /// <returns>trueΪ��ȷ�֮��Ȼ</returns>
        public bool Equals(XVector2 other)
        {
            float xDiff = X - other.X;
            float yDiff = Y - other.Y;

            float e = XMath.EPSILON;

            //������е�ֵ����0"����"
            if (
                xDiff < e && xDiff > -e &&
                yDiff < e && yDiff > -e)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals((XVector2)obj);
        }

        /// <summary>
        /// ������� a��b
        /// </summary>
        /// <param name="a">����a</param>
        /// <param name="b">����b</param>
        /// <returns>���</returns>
        public static float Dot(XVector2 a, XVector2 b)
        {
            return (a.X * b.X) + (a.Y * b.Y);
        }

        public static float Cross(XVector2 a, XVector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        /// <summary>
        /// ģ�� ||a||
        /// </summary>
        /// <param name="a">����</param>
        /// <returns></returns>
        public static float Magnitude(XVector2 a)
        {
            return (float)Math.Sqrt(SqrMagnitude(a));
        }

        /// <summary>
        /// ģ����ƽ��
        /// </summary>
        /// <param name="a">����</param>
        /// <returns>���ֵ</returns>
        public static float SqrMagnitude(XVector2 a)
        {
            return (a.X * a.X) + (a.Y * a.Y);
        }

        /// <summary>
        /// ���������ľ���
        /// </summary>
        /// <param name="a">����a</param>
        /// <param name="b">����b</param>
        /// <returns>����ֵ</returns>
        public static float Distance(XVector2 a, XVector2 b)
        {
            return Magnitude(a - b);
        }

        /// <summary>
        /// �����ƽ��
        /// </summary>
        /// <param name="a">����a</param>
        /// <param name="b">����b</param>
        /// <returns>����ֵ</returns>
        public static float SqrDistance(XVector2 a, XVector2 b)
        {
            return SqrMagnitude(a - b);
        }

        /// <summary>
        /// ��һ��
        /// </summary>
        /// <param name="v">����</param>
        /// <returns>���</returns>
        public static XVector2 Normalize(XVector2 v)
        {
            if (Math.Abs(v.X) <= float.Epsilon && Math.Abs(v.Y) <= float.Epsilon)
                return XVector2.Zero;
            float magnitude = Magnitude(v);
            return new XVector2(v.X / magnitude, v.Y / magnitude);
        }

        public static float operator *(XVector2 vector1, XVector2 vector2)
        {
            return vector1.X * vector2.X + vector1.Y * vector2.Y;
        }

        public static XVector2 operator /(XVector2 vector, float scalar)
        {
            return new XVector2(vector.X / scalar, vector.Y / scalar);
        }

        public static XVector2 operator +(XVector2 a, XVector2 b)
        {
            return new XVector2(a.X + b.X, a.Y + b.Y);
        }

        public static XVector2 operator -(XVector2 a, XVector2 b)
        {
            return new XVector2(a.X - b.X, a.Y - b.Y);
        }

        public static XVector2 operator *(XVector2 a, float b)
        {
            return new XVector2(a.X * b, a.Y * b);
        }

        public static XVector2 operator *(float b, XVector2 a)
        {
            return new XVector2(a.X * b, a.Y * b);
        }

        public static XVector2 operator -(XVector2 a)
        {
            return a * -1f;
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }
}