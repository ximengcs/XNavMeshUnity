
using XFrame.PathFinding.RVO;

namespace XFrame.PathFinding
{
    internal class AgentProxy : IAgentProxy
    {
        private int m_AgentId;
        private Simulator m_Simulator;

        public XVector2 Pos => m_Simulator.getAgentPosition(m_AgentId);

        public AgentProxy(int agentId, Simulator simulator)
        {
            m_AgentId = agentId;
            m_Simulator = simulator;
        }
    }
}
