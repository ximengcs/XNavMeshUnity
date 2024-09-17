

using System.Collections.Generic;
using UnityEngine;

namespace XFrame.PathFinding
{
    public class Poly
    {
        private XNavMesh m_NavMesh;
        private AreaType m_AreaType;
        private List<XVector2> m_Points;
        private HashSet<HalfEdgeFace> m_Faces;

        internal List<XVector2> Points
        {
            get { return m_Points; }
            set { m_Points = value; }
        }

        internal Poly(XNavMesh navMesh, List<XVector2> points, AreaType areaType)
        {
            m_Points = points;
            m_NavMesh = navMesh;
            m_AreaType = areaType;
        }

        internal void SetFaces(HashSet<HalfEdgeFace> faces)
        {
            m_Faces = faces;
            foreach (HalfEdgeFace face in faces)
            {
                face.Area = m_AreaType;
                DebugUtility.Print(face, m_NavMesh.Normalizer);
                Debug.LogWarning($"set face to {m_AreaType}");
            }
        }

        public bool Move(XVector2 offset, out HalfEdgeData newAreaData, out List<Edge> newAreaOutEdges)
        {
            return m_NavMesh.ChangeWithExtraData(this, offset, out newAreaData, out newAreaOutEdges);
        }
    }
}