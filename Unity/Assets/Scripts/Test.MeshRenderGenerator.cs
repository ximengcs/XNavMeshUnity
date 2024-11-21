
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class Test
{
    public class XNavMeshRenderer
    {
        public static int Z = 0;
        private GameObject m_Root;
        private List<GameObject> m_Meshs;
        private float m_Z;

        public XNavMeshRenderer()
        {
            m_Root = new GameObject("XNavMeshRenderer");
            Transform tf = m_Root.transform;
            Vector3 pos = tf.position;
            m_Z = Z--;
            pos.z = m_Z;
            tf.position = pos;
            m_Meshs = new List<GameObject>();
        }

        public void Destroy()
        {
            GameObject.DestroyImmediate(m_Root);
        }

        public void Refresh(MeshArea navMesh)
        {
            foreach (GameObject go in m_Meshs)
                Pool.ReleaseRender(go);
            m_Meshs.Clear();

            foreach (MeshInfo meshInfo in navMesh.Meshs)
            {
                GameObject go = Pool.RequireRender(m_Root.transform);
                MeshRenderer render = go.GetComponent<MeshRenderer>();
                if (render == null)
                    render = go.AddComponent<MeshRenderer>();
                Color color = meshInfo.Color;
                color.a = 0.5f;
                if (ReferenceEquals(render.sharedMaterial, null))
                    render.sharedMaterial = new Material(Resources.Load<Material>("Mesh"));

                render.sharedMaterial.color = color;
                MeshFilter filter = go.GetComponent<MeshFilter>();
                if (filter == null)
                    filter = go.AddComponent<MeshFilter>();
                filter.mesh = meshInfo.Mesh;

                LineRenderer line = go.GetComponent<LineRenderer>();
                if (line == null)
                    line = go.AddComponent<LineRenderer>();
                Vector3[] points = new Vector3[]
                {
                    new Vector3(meshInfo.Triangle.P1.X, meshInfo.Triangle.P1.Y, m_Z),
                    new Vector3(meshInfo.Triangle.P2.X, meshInfo.Triangle.P2.Y, m_Z),
                    new Vector3(meshInfo.Triangle.P3.X, meshInfo.Triangle.P3.Y, m_Z),
                    new Vector3(meshInfo.Triangle.P1.X, meshInfo.Triangle.P1.Y, m_Z)
                };
                line.positionCount = points.Length;
                line.SetPositions(points);
                line.startColor = meshInfo.Color;
                line.endColor = meshInfo.Color;
                line.startWidth = 0.2f;
                line.endWidth = 0.2f;
                if (ReferenceEquals(line.sharedMaterial, null))
                    line.sharedMaterial = new Material(Resources.Load<Material>("Line"));
                m_Meshs.Add(go);
            }
        }
    }
}
