﻿
using System;

namespace XFrame.PathFinding
{
    /// <summary>
    /// 半边结构顶点
    /// </summary>
    public class HalfEdgeVertex
    {
        /// <summary>
        /// 顶点
        /// </summary>
        public XVector2 Position;

        /// <summary>
        /// 这个顶点为起点，指向的半边
        /// </summary>
        public HalfEdge Edge;

        public HalfEdgeVertex(XVector2 pos)
        {
            Position = pos;
        }

        public override string ToString()
        {
            if (Test2.Normalizer != null)
            {
                Func<XVector2, XVector2> fun2 = Test2.Normalizer.UnNormalize;
                return fun2(Position).ToString();
            }
            else
            {
                return Position.ToString();
            }
        }
    }
}
