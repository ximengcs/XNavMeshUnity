
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

        public static Vector2 ToUnityVec2(this XVector2 point)
        {
            return new Vector2(point.X, point.Y);
        }

        public static XVector2 ToVec(this Vector3 point)
        {
            return new XVector2(point.x, point.y);
        }

        public static System.Numerics.Vector2 ToSystemVec2(this XVector2 point)
        {
            return new System.Numerics.Vector2(point.X, point.Y);
        }
    }
}
