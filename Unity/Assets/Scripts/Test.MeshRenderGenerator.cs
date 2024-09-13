
using System.Collections.Generic;
using UnityEngine;

public partial class Test
{
    private class XNavMeshRenderer
    {
        public static int Z = 0;
        private GameObject m_Prefab;
        private GameObject m_Root;
        private List<GameObject> m_Meshs;
        private float m_Z;

        public XNavMeshRenderer(GameObject prefab)
        {
            m_Prefab = prefab;
            m_Root = new GameObject();
            Transform tf = m_Root.transform;
            Vector3 pos = tf.position;
            m_Z = Z--;
            pos.z = m_Z;
            tf.position = pos;
            m_Meshs = new List<GameObject>();
        }

        public void Refresh(MeshArea navMesh)
        {
            foreach (GameObject go in m_Meshs)
                GameObject.Destroy(go);
            m_Meshs.Clear();

            foreach (MeshInfo meshInfo in navMesh.Meshs)
            {
                GameObject go = GameObject.Instantiate(m_Prefab, m_Root.transform);
                MeshRenderer render = go.GetComponent<MeshRenderer>();
                Color color = meshInfo.Color;
                color.a = 0.5f;
                render.material.color = color;
                MeshFilter filter = go.GetComponent<MeshFilter>();
                filter.mesh = meshInfo.Mesh;

                LineRenderer line = go.GetComponent<LineRenderer>();
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
                m_Meshs.Add(go);
            }
        }
    }
}
