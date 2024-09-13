using UnityEngine;
using System.Collections.Generic;
using System;

namespace XFrame.PathFinding
{
    /// <summary>
    /// 轴对齐包围盒
    /// </summary>
    public struct AABB
    {
        /// <summary>
        /// 最小点
        /// </summary>
        public XVector2 Min;

        /// <summary>
        /// 最大点
        /// </summary>
        public XVector2 Max;

        /// <summary>
        /// 最长边的长度
        /// </summary>
        public float DMax
        {
            get { return Math.Max(Max.X - Min.X, Max.Y - Min.Y); }
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="minX">x最小值</param>
        /// <param name="maxX">x最大值</param>
        /// <param name="minY">y最小值</param>
        /// <param name="maxY">y最大值</param>
        public AABB(float minX, float maxX, float minY, float maxY)
        {
            Min = new XVector2(minX, minY);
            Max = new XVector2(maxX, maxY);
        }

        /// <summary>
        /// 通过给定点列表构造包围盒
        /// </summary>
        /// <param name="points">点列表</param>
        public AABB(List<XVector2> points)
        {
            XVector2 p1 = points[0];

            float minX = p1.X;
            float maxX = p1.X;
            float minY = p1.Y;
            float maxY = p1.Y;

            for (int i = 1; i < points.Count; i++)
            {
                XVector2 p = points[i];

                if (p.X < minX)
                {
                    minX = p.X;
                }
                else if (p.X > maxX)
                {
                    maxX = p.X;
                }

                if (p.Y < minY)
                {
                    minY = p.Y;
                }
                else if (p.Y > maxY)
                {
                    maxY = p.Y;
                }
            }

            Min = new XVector2(minX, minY);
            Max = new XVector2(maxX, maxY);
        }

        /// <summary>
        /// 检查该包围盒是否为矩形(最大边和最小边不重合)
        /// </summary>
        /// <returns>true为矩形，反之亦然</returns>
        public bool IsRectangleARectangle()
        {
            float xWidth = Mathf.Abs(Max.X - Min.X);
            float yWidth = Mathf.Abs(Max.Y - Min.Y);

            float epsilon = XMath.EPSILON;

            if (xWidth < epsilon || yWidth < epsilon)
            {
                return false;
            }

            return true;
        }

        public bool Contains(Triangle triangle)
        {
            return Contains(triangle.P1) && Contains(triangle.P2) && Contains(triangle.P3);
        }

        public bool Contains(XVector2 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                point.Y >= Min.Y && point.Y <= Max.Y;
        }

        public override string ToString()
        {
            return $" {Min}, {Max} ";
        }
    }
}
