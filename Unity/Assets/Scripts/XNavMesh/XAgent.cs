
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
            get { return new XVector2(m_Inst.transform.position.x, m_Inst.transform.position.y); }
            set
            {
                m_Inst.transform.position = value.ToUnityVec3();
                m_Pos = value;
            }
        }

        public void Towards(XVector2 dir)
        {
            float angle = XMath.Angle(new XVector2(1, 0), dir) * (180 / XMath.PI);
            Quaternion q = Quaternion.Euler(0, 0, angle);
            m_Inst.transform.rotation = q;
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
