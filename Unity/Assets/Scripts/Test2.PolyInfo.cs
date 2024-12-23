﻿
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using XFrame.PathFinding;
using static Test;

public partial class Test2
{
    private class PolyInfo
    {
        public Poly Poly;
        public XNavmesh NavMesh;
        public MeshArea MeshArea;
        public HalfEdgeData ChangeData;
        public List<Edge> ChangeLine;
        public Updater Updater;

        public void Dispose()
        {
            MeshArea.Dispose();
            MeshArea = null;
            ChangeLine = null;
        }
    }

    private class HalfEdgeInfo
    {
        public List<TriangleArea> Triangles;
        public List<MeshInfo> m_Meshs;

        public HalfEdgeInfo(XNavmesh navmesh, HalfEdgeData data, Color color)
        {
            Triangles = navmesh.ToTriangles(data);
            m_Meshs = MeshArea.GenerateMesh(Triangles, color);
        }

        public void Dispose()
        {
            if (m_Meshs != null)
            {
                foreach (MeshInfo m in m_Meshs)
                {
                    Pool.ReleaseMesh(m.Mesh);
                }
            }
        }
    }
}