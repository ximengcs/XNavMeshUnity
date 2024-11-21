using RVO;
using RVOCS;
using Simon001.PathFinding;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using XFrame.PathFinding;
using static Test;
using SW = System.Diagnostics.Stopwatch;


public partial class Test2 : MonoBehaviour
{
    public static bool T1;
    public static Normalizer Normalizer;
    public static AABB AABB;

    public GameObject CirclePrefab;
    public RVOArea RVOObstacle;
    public Transform StartPoint;
    public Transform EndPoint;

    private Dictionary<int, XAgent> m_Agents;

    private PolyInfo m_ShowPoly;
    private bool m_DrawGizmosFullMeshArea = true;
    private bool m_DrawGizmosPoly = true;

    private List<Triangle> m_Triangles;

    private void AddTestCommand()
    {
        Console.Inst.AddCommand("test-1", (param) =>
        {
            Console.Inst.ExecuteCommand("navmesh-add");
            Console.Inst.ExecuteCommand("poly-add 0 true");
            Console.Inst.ExecuteCommand("poly-add 1 true");
            Console.Inst.ExecuteCommand("poly-add 2 true");
        });
        Console.Inst.AddCommand("test-2", (param) =>
        {
            Console.Inst.ExecuteCommand("poly-rotate-loop 1 0.1");
        });
        Console.Inst.AddCommand("test-3", (param) =>
        {
            SW sw = SW.StartNew();
            for (int i = 0; i < 100; i++)
            {
                List<object> obj = new List<object>();
                obj.GetHashCode();
            }
            sw.Stop();
            Debug.LogWarning(sw.ElapsedTicks);
            int listCount = 16;
            XFrame.PathFinding.Pool.Spwan<object>(100, listCount);
            sw = SW.StartNew();
            List<List<object>> list = XFrame.PathFinding.Pool.Require<List<List<object>>>();
            for (int i = 0; i < listCount; i++)
            {
                List<object> obj = XFrame.PathFinding.Pool.Require<List<object>>(listCount);
                obj.GetHashCode();
                list.Add(obj);
            }
            sw.Stop();

            foreach (List<object> obj in list)
                XFrame.PathFinding.Pool.Release(obj, listCount);
            XFrame.PathFinding.Pool.Release(list);
            Debug.LogWarning(sw.ElapsedTicks);
        });
        Console.Inst.AddCommand("test-44", (param) =>
        {
            XVector2 start = StartPoint.position.ToVec();
            XVector2 end = EndPoint.position.ToVec();
            m_NavMesh.FindPath(start, end);
        });
        
        Console.Inst.AddCommand("rvo", (param) =>
        {
            Circle circle = new Circle(CirclePrefab);
            circle.setupScenario(
                new RVO.Vector2(StartPoint.position.x, StartPoint.position.y),
                new RVO.Vector2(EndPoint.position.x, EndPoint.position.y)
                );
            circle.updateVisualization();
            if (RVOObstacle)
                Simulator.Instance.addObstacle(RVOObstacle.GetVertices());
            Simulator.Instance.processObstacles();
            m_UpdaterList.Add(new Updater(() =>
            {
                circle.updateVisualization();
                circle.setPreferredVelocities();
                Simulator.Instance.doStep();
                return true;
            }));
        });

        Console.Inst.AddCommand("agent-follow", (param) =>
        {
            if (ParamToInt(param, out int id))
            {
                if (m_Agents.TryGetValue(id, out XAgent agent))
                {
                    m_UpdaterList.Add(new Updater(() =>
                    {
                        Vector3 pos = agent.Pos.ToUnityVec3();
                        pos.z = -1;
                        Camera.main.transform.position = pos;
                        return true;
                    }));
                }
            }
        });

        Console.Inst.AddCommand("to-mouse", (param) =>
        {
            m_UpdaterList.Add(new Updater(() =>
            {
                if (Input.GetMouseButtonUp(0))
                {
                    Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Console.Inst.ExecuteCommand($"agent-to 0 {pos.x},{pos.y}");
                }
                return true;
            }));
        });

        Console.Inst.AddCommand("agent-test", (param) =>
        {
            Console.Inst.ShowFPS = true;
            Console.Inst.ExecuteCommand("navmesh-open navmesh_1");
            Console.Inst.ExecuteCommand("navmesh-show");
            Console.Inst.ExecuteCommand("navmesh-poly-rotate 24 0.1");

            Simulator.Instance.setTimeStep(0.15f);
            Simulator.Instance.setAgentDefaults(2, 5, 10.0f, 10.0f, 0.5f, 2.0f, new RVO.Vector2(0.0f, 0.0f));

            m_Triangles = m_NavMesh.GetArea(AreaType.Obstacle);
            foreach (Triangle triangle in m_Triangles)
            {
                if (triangle.IsClockwise())
                    triangle.ChangeOrientation();

                List<RVO.Vector2> points = new List<RVO.Vector2>()
                {
                    new RVO.Vector2(triangle.P1.X, triangle.P1.Y),
                    new RVO.Vector2(triangle.P2.X, triangle.P2.Y),
                    new RVO.Vector2(triangle.P3.X, triangle.P3.Y)
                };
                Simulator.Instance.addObstacle(points);
            }
            Simulator.Instance.processObstacles();

            for (int i = 0; i < 100; i++)
            {
                XVector2 bornPos = m_NavMesh.GetRandomPoint();
                Console.Inst.ExecuteCommand($"agent-create {bornPos.X},{bornPos.Y}");
                //Console.Inst.ExecuteCommand($"agent-create {StartPoint.position.x},{StartPoint.position.y}");
            }
        });


        Console.Inst.AddCommand("agent-create", (param) =>
        {
            if (ParamToVec(param, out XVector2 p))
            {
                int agentId = Simulator.Instance.addAgent(new RVO.Vector2(p.X, p.Y));
                XAgent agent = new XAgent(agentId, p, CirclePrefab);
                m_Agents.Add(agent.Id, agent);
                int index = 0;
                XVector2 target = default;
                bool hasTarget = false;
                AStarPath path = null;
                XVector2 from = default;
                XVector2 to = default;
                List<XVector2> targets = null;

                m_UpdaterList.Add(new Updater(() =>
                {
                    if (!hasTarget)
                    {
                        target = m_NavMesh.GetRandomPoint();
                        Debug.LogWarning($"target {target}");
                        from = agent.Pos;
                        to = target;
                        targets = m_NavMesh.FindPath(from, to);
                        if (targets != null)
                        {
                            Debug.LogWarning("path-------------");
                            foreach (var target in targets)
                                Debug.LogWarning(target);
                            Debug.LogWarning("path=============");
                            index = 0;
                            Debug.LogWarning($" {index}:{targets.Count} {targets[index]} ");
                            hasTarget = true;
                        }
                    }
                    else
                    {
                        if (index >= targets.Count)
                        {
                            hasTarget = false;
                            agent.Pos = targets[targets.Count - 1];
                            return true;
                        }
                        RVO.Vector2 pos = Simulator.Instance.getAgentPosition(agentId);
                        agent.Pos = new XVector2(pos.x(), pos.y());

                        RVO.Vector2 vel = Simulator.Instance.getAgentVelocity(agentId);
                        agent.Towards(new XVector2(vel.x(), vel.y()));

                        XVector2 tarPos = targets[index];
                        XVector2 power = tarPos - agent.Pos;
                        RVO.Vector2 v = new RVO.Vector2(power.X, power.Y);
                        if (RVOMath.absSq(v) > 1.0f)
                        {
                            v = RVOMath.normalize(v);
                        }

                        bool notReachGoal = RVOMath.absSq(pos - new RVO.Vector2(tarPos.X, tarPos.Y)) > Simulator.Instance.getAgentRadius(agentId) * Simulator.Instance.getAgentRadius(agentId);

                        if (!notReachGoal)
                        {
                            index++;
                            //if (index < targets.Count)
                            //    Debug.LogWarning($" {index}:{targets.Count} {targets[index]} ");

                            Simulator.Instance.setAgentPrefVelocity(agentId, new RVO.Vector2());
                        }
                        else
                        {
                            Simulator.Instance.setAgentPrefVelocity(agentId, v);
                        }
                        m_Dirty = true;
                        return true;
                    }

                    return true;
                }));
            }
        });

        Console.Inst.AddCommand("agent-to", (param) =>
        {
            if (ParamToIntVec(param, out int agentId, out XVector2 p))
            {
                if (m_Agents.TryGetValue(agentId, out XAgent agent))
                {
                    Debug.LogWarning("Start");
                    m_UpdaterList.Add(new Updater(() =>
                    {
                        RVO.Vector2 v = new RVO.Vector2(p.X, p.Y);
                        v = v - Simulator.Instance.getAgentPosition(agentId);
                        if (RVOMath.absSq(v) > 1.0f)
                        {
                            v = RVOMath.normalize(v);
                        }

                        Simulator.Instance.setAgentPrefVelocity(agentId, v);
                        Simulator.Instance.doStep();

                        RVO.Vector2 pos = Simulator.Instance.getAgentPosition(agentId);
                        agent.Pos = new XVector2(pos.x(), pos.y());
                        bool notReachGoal = RVOMath.absSq(pos - new RVO.Vector2(p.X, p.Y)) > Simulator.Instance.getAgentRadius(agentId) * Simulator.Instance.getAgentRadius(agentId);

                        if (!notReachGoal)
                        {
                            Debug.LogWarning("Finish");
                            return false;
                        }

                        return true;
                    }));
                }
                else
                {
                    Debug.LogError("not agent");
                }
            }
        });
    }

    private void TestTri(string param)
    {
        //XNavMesh.DelaunayIncrementalSloan.TriangulationWalk(new XVector2(-0.9968367f, -6.047449f), null, HalfDataTest.Data);
    }

    private void TestEdge(string param)
    {
        List<List<XVector2>> list = new List<List<XVector2>>();
        var l1 = GetAllPoints(Holls[4], false);
        var l2 = GetAllPoints(Holls[3], false);
        Normalizer.Normalize(l1);
        Normalizer.Normalize(l2);
        list.Add(l1);
        list.Add(l2);
        //list.Add(GetAllPoints(Holls[5], false));

        List<XVector2> result = PolyUtility.Combine(list, out list);

        Debug.LogWarning("===============================");
        List<Edge> tmpEdges = new List<Edge>();
        for (int i = 0; i < result.Count; i++)
        {
            XVector2 p1 = result[i];
            XVector2 p2 = result[(i + 1) % result.Count];
            Debug.LogWarning($" {p1} ");
            tmpEdges.Add(new Edge(p1, p2));
        }
        Normalizer.UnNormalize(tmpEdges);
        m_Edges.Add(tmpEdges);
        Debug.LogWarning("===============================");
    }

    private void CreateNavMesh(string param)
    {
        if (m_NavMesh != null)
            return;
        List<XVector2> points = GetAllPoints(RectPoints, false);
        Normalizer = new Normalizer(new AABB(points));
        m_NavMesh = new XNavMesh(new AABB(points));
        Normalizer = m_NavMesh.Normalizer;
        m_NavMesh.Add(points);
        m_FullMeshArea = new MeshArea(m_NavMesh, Color.green);
        m_FullMeshArea.Refresh();
        m_NavMesh.CheckDataValid();
    }

    private void CreatePoly(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToIntBool(param, out int index, out bool includeNonActive))
        {
            if (index >= 0 && index < Holls.Count)
            {
                List<XVector2> points = GetAllPoints(Holls[index], includeNonActive);
                Poly poly = m_NavMesh.AddWithExtraData(points, AreaType.Obstacle, out HalfEdgeData newAreaData, out List<Edge> newOutLine);
                m_ShowPoly = InnerAddPolyInfo(poly.Id, poly, newAreaData, newOutLine);
                m_FullMeshArea.Refresh();
                m_NavMesh.CheckDataValid();
                m_NavMesh.Test();
            }
        }
    }

    private void RemovePoly(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToInt(param, out int id))
        {
            if (m_Polies.TryGetValue(id, out PolyInfo polyInfo))
            {
                m_NavMesh.Remove(polyInfo.Poly, out HalfEdgeData newAreaData, out List<Edge> newOutLine);
                m_FullMeshArea.Refresh();
                InnerRemovePolyInfo(id);
                m_NavMesh.CheckDataValid();
            }
        }
    }

    private void MovePolyX(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToIntFloat(param, out int id, out float offset))
        {
            if (m_Polies.TryGetValue(id, out PolyInfo polyInfo))
            {
                if (polyInfo.Poly.Move(new XVector2(offset, 0f), out HalfEdgeData newAreaData, out List<Edge> newOutLine))
                {
                    m_ShowPoly = InnerAddPolyInfo(id, polyInfo.Poly, newAreaData, newOutLine);
                    m_FullMeshArea.Refresh();
                    m_NavMesh.CheckDataValid();
                    m_NavMesh.Test();
                }
                else
                {
                    Debug.Log($"move poly {id} with x failure");
                }
            }
        }
    }

    private void MovePolyY(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToIntFloat(param, out int id, out float offset))
        {
            if (m_Polies.TryGetValue(id, out PolyInfo polyInfo))
            {
                if (polyInfo.Poly.Move(new XVector2(0f, offset), out HalfEdgeData newAreaData, out List<Edge> newOutLine))
                {
                    m_ShowPoly = InnerAddPolyInfo(id, polyInfo.Poly, newAreaData, newOutLine);
                    m_FullMeshArea.Refresh();
                    m_NavMesh.CheckDataValid();
                }
                else
                {
                    Debug.Log($"move poly {id} with y failure");
                }
            }
        }
    }

    private void RotatePoly(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToIntFloat(param, out int id, out float angle))
        {
            if (m_Polies.TryGetValue(id, out PolyInfo info))
            {
                if (info.Poly.Rotate(angle, out HalfEdgeData newAreaData, out List<Edge> newOutLine))
                {
                    m_ShowPoly = InnerAddPolyInfo(id, info.Poly, newAreaData, newOutLine);
                    m_FullMeshArea.Refresh();
                }
                else
                {
                    Debug.Log($"move poly {id} with y failure");
                }
            }
        }
    }

    private void RotateLoopPoly(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToIntFloat(param, out int id, out float angle))
        {
            if (m_Polies.TryGetValue(id, out PolyInfo info))
            {
                info.Updater = new Updater(() =>
                {
                    if (info.Poly.Rotate(angle, out HalfEdgeData newAreaData, out List<Edge> newOutLine))
                    {
                        m_ShowPoly = InnerAddPolyInfo(id, info.Poly, newAreaData, newOutLine);
                        m_FullMeshArea.Refresh();
                    }
                    else
                    {
                        Debug.Log($"move poly {id} with y failure");
                    }
                    return true;
                });
            }
        }
    }

    private void ScalePoly(string param)
    {
        if (m_NavMesh == null)
            return;
        if (ParamToIntFloat(param, out int id, out float scale))
        {
            if (m_Polies.TryGetValue(id, out PolyInfo info))
            {
                if (info.Poly.Scale(XVector2.One * scale, out HalfEdgeData newAreaData, out List<Edge> newOutLine))
                {
                    m_ShowPoly = InnerAddPolyInfo(id, info.Poly, newAreaData, newOutLine);
                    m_FullMeshArea.Refresh();
                }
                else
                {
                    Debug.Log($"move poly {id} with y failure");
                }
            }
        }
    }
}
