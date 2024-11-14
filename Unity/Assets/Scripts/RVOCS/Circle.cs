using RVO;
using System;
using System.Collections.Generic;
using UnityEngine;
using Vector2 = RVO.Vector2;

namespace RVOCS
{
    public class Circle
    {
        readonly IList<Vector2> goals;
        readonly IList<GameObject> items;
        private GameObject m_Prefab;

        public Circle(GameObject prefab)
        {
            m_Prefab = prefab;
            goals = new List<Vector2>();
            items = new List<GameObject>();
        }


        public void setupScenario()
        {
            /* Specify the global time step of the simulation. */
            Simulator.Instance.setTimeStep(0.25f);

            /*
             * Specify the default parameters for agents that are subsequently
             * added.
             */
            Simulator.Instance.setAgentDefaults(15.0f, 10, 10.0f, 10.0f, 1.5f, 2.0f, new Vector2(0.0f, 0.0f));

            /*
             * Add agents, specifying their start position, and store their
             * goals on the opposite side of the environment.
             */
            for (int i = 0; i < 250; ++i)
            {
                Vector2 pos = 200.0f *
                    new Vector2((float)Math.Cos(i * 2.0f * Math.PI / 250.0f),
                        (float)Math.Sin(i * 2.0f * Math.PI / 250.0f));
                Simulator.Instance.addAgent(pos);
                goals.Add(-Simulator.Instance.getAgentPosition(i));

                GameObject go = GameObject.Instantiate(m_Prefab);
                go.name = $"{i}";
                go.transform.position = new UnityEngine.Vector3(pos.x(), pos.y());
                items.Add(go);
            }
        }

        public void updateVisualization()
        {
            for (int i = 0; i < Simulator.Instance.getNumAgents(); ++i)
            {
                Vector2 pos = Simulator.Instance.getAgentPosition(i);
                GameObject go = items[i];
                go.transform.position = new UnityEngine.Vector3(pos.x(), pos.y());
            }
        }

        public void setPreferredVelocities()
        {
            /*
             * Set the preferred velocity to be a vector of unit magnitude
             * (speed) in the direction of the goal.
             */
            for (int i = 0; i < Simulator.Instance.getNumAgents(); ++i)
            {
                Vector2 goalVector = goals[i] - Simulator.Instance.getAgentPosition(i);

                if (RVOMath.absSq(goalVector) > 1.0f)
                {
                    goalVector = RVOMath.normalize(goalVector);
                }

                Simulator.Instance.setAgentPrefVelocity(i, goalVector);
            }
        }

        public bool reachedGoal()
        {
            /* Check if all agents have reached their goals. */
            for (int i = 0; i < Simulator.Instance.getNumAgents(); ++i)
            {
                if (RVOMath.absSq(Simulator.Instance.getAgentPosition(i) - goals[i]) > Simulator.Instance.getAgentRadius(i) * Simulator.Instance.getAgentRadius(i))
                {
                    return false;
                }
            }

            return true;
        }

    }
}
