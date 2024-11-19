
using UnityEngine;

namespace XFrame.PathFinding
{
    public class XAgent
    {
        private int m_AgentId;
        private XVector2 m_Pos;
        private GameObject m_Inst;

        public int Id => m_AgentId;

        public XVector2 Pos
        {
            get { return m_Pos; }
            set
            {
                m_Inst.transform.position = value.ToUnityVec3();
                m_Pos = value;
            }
        }

        public XAgent(int id, XVector2 initPos, GameObject prefab)
        {
            m_AgentId = id;
            m_Inst = GameObject.Instantiate(prefab);
            Pos = initPos;

            SpriteRenderer renderer = m_Inst.GetComponent<SpriteRenderer>();
            renderer.color = new Color(Random.Range(0.2f, 1), Random.Range(0.2f, 1), Random.Range(0.2f, 1));
        }
    }
}
