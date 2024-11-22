
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

            //如果所有的值都在0"附近"
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
        /// 检查两个二维向量是否相等
        /// </summary>
        /// <param name="other">比较的向量</param>
        /// <returns>true为相等反之亦然</returns>
        public bool Equals(XVector2 other)
        {
            float xDiff = X - other.X;
            float yDiff = Y - other.Y;

            float e = XMath.EPSILON;

            //如果所有的值都在0"附近"
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
        /// 点乘运算 a·b
        /// </summary>
        /// <param name="a">向量a</param>
        /// <param name="b">向量b</param>
        /// <returns>点积</returns>
        public static float Dot(XVector2 a, XVector2 b)
        {
            return (a.X * b.X) + (a.Y * b.Y);
        }

        public static float Cross(XVector2 a, XVector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        /// <summary>
        /// 模长 ||a||
        /// </summary>
        /// <param name="a">向量</param>
        /// <returns></returns>
        public static float Magnitude(XVector2 a)
        {
            return (float)Math.Sqrt(SqrMagnitude(a));
        }

        /// <summary>
        /// 模长的平方
        /// </summary>
        /// <param name="a">向量</param>
        /// <returns>结果值</returns>
        public static float SqrMagnitude(XVector2 a)
        {
            return (a.X * a.X) + (a.Y * a.Y);
        }

        /// <summary>
        /// 两个向量的距离
        /// </summary>
        /// <param name="a">向量a</param>
        /// <param name="b">向量b</param>
        /// <returns>距离值</returns>
        public static float Distance(XVector2 a, XVector2 b)
        {
            return Magnitude(a - b);
        }

        /// <summary>
        /// 距离的平方
        /// </summary>
        /// <param name="a">向量a</param>
        /// <param name="b">向量b</param>
        /// <returns>距离值</returns>
        public static float SqrDistance(XVector2 a, XVector2 b)
        {
            return SqrMagnitude(a - b);
        }

        /// <summary>
        /// 归一化
        /// </summary>
        /// <param name="v">向量</param>
        /// <returns>结果</returns>
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