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
    public static XNavMesh Navmesh;
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
                object obj = new object();
                obj.GetHashCode();
            }
            sw.Stop();
            Debug.LogWarning(sw.ElapsedTicks);

            XPool.Spwan<object>(1, 100, true);

            sw = SW.StartNew();
            for (int i = 0; i < 100; i++)
            {
                object obj = XPool.Require<object>();
                obj.GetHashCode();
            }
            sw.Stop();
            Debug.LogWarning(sw.ElapsedTicks);
        });
        Console.Inst.AddCommand("test-4", (param) =>
        {
            if (ParamToVecVec(param, out XVector2 p1, out XVector2 p2))
            {
                TestPath(Navmesh, p1, p2, out _);
            }
        });
        Console.Inst.AddCommand("test-44", (param) =>
        {
            XVector2 start = StartPoint.position.ToVec();
            XVector2 end = EndPoint.position.ToVec();
            TestPath(Navmesh, start, end, out _);
        });
        Console.Inst.AddCommand("test-5", (param) =>
        {
            foreach (HalfEdgeFace f in m_NavMesh.Data.Faces)
            {
                List<HalfEdgeFace> faces = new List<HalfEdgeFace>();
                HalfEdge e1 = f.Edge;
                HalfEdge e2 = e1.NextEdge;
                HalfEdge e3 = e1.PrevEdge;

                HalfEdge ope1 = e1.OppositeEdge;
                HalfEdge ope2 = e2.OppositeEdge;
                HalfEdge ope3 = e3.OppositeEdge;

                if (ope1 != null)
                {
                    HalfEdgeFace opf1 = ope1.Face;
                    faces.Add(opf1);

                    HalfEdge e = ope1.NextEdge.OppositeEdge;
                    while (e != null)
                    {
                        HalfEdgeFace ef = e.Face;
                        if (ope2 != null && ef == ope2.Face) break;
                        if (ope3 != null && ef == ope3.Face) break;

                        faces.Add(ef);
                        e = e.NextEdge.OppositeEdge;
                    }
                }
                if (ope2 != null)
                {
                    HalfEdgeFace opf2 = ope2.Face;
                    faces.Add(opf2);

                    HalfEdge e = ope2.NextEdge.OppositeEdge;
                    while (e != null)
                    {
                        HalfEdgeFace ef = e.Face;

                        if (ope3 != null && ef == ope3.Face) break;
                        if (ope1 != null && ef == ope1.Face) break;

                        faces.Add(ef);
                        e = e.NextEdge.OppositeEdge;
                    }
                }
                if (ope3 != null)
                {
                    HalfEdgeFace opf3 = ope3.Face;
                    faces.Add(opf3);

                    HalfEdge e = ope3.NextEdge.OppositeEdge;
                    while (e != null)
                    {
                        HalfEdgeFace ef = e.Face;
                        if (ope1 != null && ef == ope1.Face) break;
                        if (ope2 != null && ef == ope2.Face) break;

                        faces.Add(ef);
                        e = e.NextEdge.OppositeEdge;
                    }
                }

                Func<Triangle, Triangle> fun = Test2.Normalizer.UnNormalize;
                StringBuilder s = new StringBuilder();
                s.AppendLine($"face {fun(new Triangle(f))}");
                foreach (HalfEdgeFace face in faces)
                    s.AppendLine($" -- {fun(new Triangle(face))}");
                Debug.Log(s.ToString());
            }
        });

        Console.Inst.AddCommand("rvo", (param) =>
        {
            Simulator.Instance.setTimeStep(0.25f);
            Simulator.Instance.setAgentDefaults(15.0f, 10, 10.0f, 10.0f, 1.5f, 2.0f, new RVO.Vector2(0.0f, 0.0f));

            List<Triangle> triangles = m_NavMesh.GetObstacles(1.5f);
            foreach (Triangle triangle in triangles)
            {
                List<RVO.Vector2> points = new List<RVO.Vector2>()
                {
                    new RVO.Vector2(triangle.P1.X, triangle.P1.Y),
                    new RVO.Vector2(triangle.P2.X, triangle.P2.Y),
                    new RVO.Vector2(triangle.P3.X, triangle.P3.Y)
                };
                Simulator.Instance.addObstacle(points);
            }
            Simulator.Instance.processObstacles();

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
                    Console.Inst.ExecuteCommand($"agent-to 1 {pos.x},{pos.y}");
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

            Simulator.Instance.setTimeStep(0.25f);
            Simulator.Instance.setAgentDefaults(15.0f, 10, 10.0f, 10.0f, 1f, 2.0f, new RVO.Vector2(0.0f, 0.0f));

            m_Triangles = m_NavMesh.GetObstacles(2.1f);
            Debug.LogWarning($" {m_Triangles.Count} ");

            foreach (Triangle triangle in m_Triangles)
            {
                List<RVO.Vector2> points = new List<RVO.Vector2>()
                {
                    new RVO.Vector2(triangle.P1.X, triangle.P1.Y),
                    new RVO.Vector2(triangle.P2.X, triangle.P2.Y),
                    new RVO.Vector2(triangle.P3.X, triangle.P3.Y)
                };
                Simulator.Instance.addObstacle(points);
            }
            Simulator.Instance.processObstacles();

            for (int i = 0; i < 1; i++)
            {
                XVector2 bornPos = m_NavMesh.GetRandomPoint();
                Console.Inst.ExecuteCommand($"agent-create {bornPos.X},{bornPos.Y}");
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
                        path = TestPath(m_NavMesh, agent.Pos, target, out XNavMeshHelper helper);

                        from = m_NavMesh.Normalizer.Normalize(agent.Pos);
                        to = m_NavMesh.Normalizer.Normalize(target);
                        targets = helper.GetPathPoints(path, from, to);
                        m_NavMesh.Normalizer.UnNormalize(targets);
                        index = 0;
                        hasTarget = true;
                    }
                    else
                    {
                        if (index >= targets.Count)
                        {
                            hasTarget = false;
                            agent.Pos = targets[targets.Count - 1];
                            Debug.LogWarning($"next");
                            return true;
                        }
                        XVector2 tarPos = targets[index];
                        XVector2 power = tarPos - agent.Pos;
                        RVO.Vector2 v = new RVO.Vector2(power.X, power.Y);
                        if (RVOMath.absSq(v) > 1.0f)
                        {
                            v = RVOMath.normalize(v);
                        }
                        //power *= Time.deltaTime * 10;
                        //agent.Pos += power;

                        Simulator.Instance.setAgentPrefVelocity(agentId, v);
                        Simulator.Instance.doStep();
                        RVO.Vector2 pos = Simulator.Instance.getAgentPosition(agentId);
                        agent.Pos = new XVector2(pos.x(), pos.y());

                        bool notReachGoal = RVOMath.absSq(pos - new RVO.Vector2(tarPos.X, tarPos.Y)) > Simulator.Instance.getAgentRadius(agentId) * Simulator.Instance.getAgentRadius(agentId);

                        Debug.LogWarning($"notReachGoal |{v.x()}|{v.y()}| {agent.Pos} {tarPos} {RVOMath.absSq(pos - new RVO.Vector2(tarPos.X, tarPos.Y))} {Simulator.Instance.getAgentRadius(agentId) * Simulator.Instance.getAgentRadius(agentId)} ");
                        if (!notReachGoal)
                        {
                            index++;
                        }

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
                    AStarPath path = TestPath(m_NavMesh, agent.Pos, p, out XNavMeshHelper helper);
                    XVector2 from = m_NavMesh.Normalizer.Normalize(agent.Pos);
                    XVector2 to = m_NavMesh.Normalizer.Normalize(p);
                    List<XVector2> targets = helper.GetPathPoints(path, from, to);
                    m_NavMesh.Normalizer.UnNormalize(targets);

                    int index = 1;
                    m_UpdaterList.Add(new Updater(() =>
                    {
                        if (index >= targets.Count)
                            return false;
                        XVector2 tarPos = targets[index];
                        XVector2 power = XVector2.Normalize(tarPos - agent.Pos);
                        power *= Time.deltaTime * 10;
                        agent.Pos += power;

                        if (XVector2.Distance(agent.Pos, tarPos) < 0.1f)
                        {
                            index++;
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

    private AStarPath TestPath(XNavMesh navmesh, XVector2 p1, XVector2 p2, out XNavMeshHelper helper)
    {
        Normalizer normalizer = navmesh.Normalizer;
        helper = new XNavMeshHelper(navmesh.Data);
        AStar aStar = new AStar(helper);
        IAStarItem start = navmesh.FindWalkFace(p1);
        IAStarItem end = navmesh.FindWalkFace(p2);
        if (start != null && end != null)
        {
            AStarPath path = aStar.Execute(start, end);
            List<XVector2> points = new List<XVector2>();
            List<Edge> edges = new List<Edge>();
            for (int i = 0; i < points.Count - 1; i++)
                edges.Add(new Edge(points[i], points[i + 1]));
            m_Edges.Add(edges);

            if (path == null)
            {
                Debug.LogError("calculate path count is null");
            }

            return path;
        }
        else
        {
            Debug.LogError($"start or end is null. {start == null} {end == null} ");
        }
        return null;
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
        Navmesh = m_NavMesh;
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
