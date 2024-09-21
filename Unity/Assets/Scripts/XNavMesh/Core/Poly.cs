using UnityEngine;
using System.Collections.Generic;
using System;

namespace XFrame.PathFinding
{
    public class Poly
    {
        private int m_Id;
        private XNavMesh m_NavMesh;
        private AreaType m_AreaType;
        private List<XVector2> m_Points;
        private HashSet<HalfEdgeFace> m_Faces;

        public int Id => m_Id;

        public XVector2 CenterOfGravityPoint
        {
            get
            {
                float xAll = 0f;
                float yAll = 0f;
                foreach (XVector2 point in m_Points)
                {
                    xAll += point.X;
                    yAll += point.Y;
                }
                return new XVector2(xAll / m_Points.Count, yAll / m_Points.Count);
            }
        }

        internal List<XVector2> Points
        {
            get { return m_Points; }
            set { m_Points = value; }
        }

        internal Poly(int id, XNavMesh navMesh, List<XVector2> points, AreaType areaType)
        {
            m_Id = id;
            m_Points = points;
            m_NavMesh = navMesh;
            m_AreaType = areaType;
        }

        internal static bool Intersect(List<XVector2> points, List<XVector2> points2)
        {
            for (int i = 0; i < points2.Count; i++)
            {
                XVector2 p1 = points2[i];
                XVector2 p2 = points2[(i + 1) % points2.Count];
                Edge e1 = new Edge(p1, p2);
                for (int j = 0; j < points.Count; j++)
                {
                    XVector2 p3 = points[j];
                    XVector2 p4 = points[(j + 1) % points.Count];
                    Edge e2 = new Edge(p3, p4);
                    if (XMath.LineLine2(e1, e2, true, out XVector2 newPoint))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal bool Contains(HalfEdgeFace face)
        {
            if (m_Faces == null)
                return false;

            foreach (HalfEdgeFace tmp in m_Faces)
            {
                if (face == tmp)
                    return true;
            }
            return false;
        }

        internal void ResetFaceArea()
        {
            foreach (HalfEdgeFace face in m_Faces)
                face.Area = AreaType.Walk;
        }

        internal void SetFaces(HashSet<HalfEdgeFace> faces)
        {
            m_Faces = faces;
            foreach (HalfEdgeFace face in faces)
            {
                face.Area = m_AreaType;
            }
        }

        public bool Scale(XVector2 scale, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            if (scale.Equals(XVector2.Zero) || scale.Equals(XVector2.One))
            {
                newAreaData = null;
                newAreaOutEdges = null;
                return false;
            }

            List<XVector2> points = new List<XVector2>(m_Points);
            // 找重心点
            XVector2 centre = CenterOfGravityPoint;

            // 以重心为中心移动到零点
            XMatrix2x3 toZeroMatrix = XMatrix2x3.Translate(-centre);
            XMatrix2x3.Multiply(toZeroMatrix, points);

            // 变换旋转 -> 位移到重心点
            XMatrix2x3 matrix = XMatrix2x3.ScaleTranslate(scale, centre);
            XMatrix2x3.Multiply(matrix, points);

            if (m_NavMesh.ChangeWithExtraData(this, points, out newAreaData, out newAreaOutEdges))
            {
                m_Points = points;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Rotate(float angle, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            angle %= 360;
            if (XMath.Equals(angle, 0))
            {
                newAreaData = null;
                newAreaOutEdges = null;
                return false;
            }

            List<XVector2> points = new List<XVector2>(m_Points);
            // 找重心点
            XVector2 centre = CenterOfGravityPoint;

            // 以重心为中心移动到零点
            XMatrix2x3 toZeroMatrix = XMatrix2x3.Translate(-centre);
            XMatrix2x3.Multiply(toZeroMatrix, points);

            // 变换旋转 -> 位移到重心点
            XMatrix2x3 matrix = XMatrix2x3.RotateTranslate(angle, centre);
            XMatrix2x3.Multiply(matrix, points);

            if (m_NavMesh.ChangeWithExtraData(this, points, out newAreaData, out newAreaOutEdges))
            {
                m_Points = points;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Move(XVector2 offset, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            if (offset.Equals(XVector2.Zero))
            {
                newAreaData = null;
                newAreaOutEdges = null;
                return false;
            }
            List<XVector2> points = new List<XVector2>(m_Points.Count);
            foreach (XVector2 point in m_Points)
                points.Add(point + offset);

            if (m_NavMesh.ChangeWithExtraData(this, points, out newAreaData, out newAreaOutEdges))
            {
                m_Points = points;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}