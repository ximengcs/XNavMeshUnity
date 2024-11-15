using System.Collections.Generic;
using UnityEngine;
using XFrame.PathFinding;
using Vector2 = RVO.Vector2;

public class RVOArea : MonoBehaviour
{
    public List<Vector2> GetVertices()
    {
        UnityEngine.Vector2 basePos = transform.position;
        LineRenderer line = GetComponent<LineRenderer>();
        Vector3[] list = new Vector3[line.positionCount];
        int count = line.GetPositions(list);
        List<Vector2> result = new List<Vector2>(count);
        for (int i = 0; i < count; ++i)
            result.Add(new Vector2(list[i].x + basePos.x, list[i].y + basePos.y));
        return result;
    }

    public List<UnityEngine.Vector2> GetUnityVertices()
    {
        UnityEngine.Vector2 basePos = transform.position;
        LineRenderer line = GetComponent<LineRenderer>();
        Vector3[] list = new Vector3[line.positionCount];
        int count = line.GetPositions(list);
        List<UnityEngine.Vector2> result = new List<UnityEngine.Vector2>(count);
        for (int i = 0; i < count; ++i)
            result.Add(new UnityEngine.Vector2(list[i].x, list[i].y) + basePos);
        return result;
    }

    public List<XVector2> GetNavmeshVertices()
    {
        UnityEngine.Vector2 basePos = transform.position;
        LineRenderer line = GetComponent<LineRenderer>();
        Vector3[] list = new Vector3[line.positionCount];
        int count = line.GetPositions(list);
        List<XVector2> result = new List<XVector2>(count);
        for (int i = 0; i < count; ++i)
            result.Add(new XVector2(list[i].x + basePos.x, list[i].y + basePos.y));
        return result;
    }
}
