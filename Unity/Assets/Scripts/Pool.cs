using System;
using System.Collections.Generic;
using UnityEngine;

public static class Pool
{
    private static List<GameObject> m_Renders = new List<GameObject>(64);

    public static GameObject RequireRender(Transform parent)
    {
        GameObject inst;
        if (m_Renders.Count > 0)
        {
            int index = m_Renders.Count - 1;
            inst = m_Renders[index];
            inst.SetActive(true);
            m_Renders.RemoveAt(index);
        }
        else
        {
            inst = new GameObject();
        }
        inst.transform.SetParent(parent);
        return inst;
    }

    public static void ReleaseRender(GameObject inst)
    {
        inst.SetActive(false);
        m_Renders.Add(inst);
    }

    private static List<Mesh> m_Meshes = new List<Mesh>(64);

    public static Mesh RequireMesh()
    {
        Mesh inst;
        if (m_Meshes.Count > 0)
        {
            int index = m_Meshes.Count - 1;
            inst = m_Meshes[index];
            m_Meshes.RemoveAt(index);
        }
        else
        {
            inst = new Mesh();
        }
        return inst;
    }

    public static void ReleaseMesh(Mesh mesh)
    {
        mesh.Clear();
        m_Meshes.Add(mesh);
    }
}