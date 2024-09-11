
using System.Collections.Generic;
using UnityEngine;

namespace XFrame.PathFinding
{
    public static class UnityExtension
    {
        public static Vector3 ToUnityVec3(this XVector2 point)
        {
            return new Vector3(point.X, point.Y);
        }
    }
}
