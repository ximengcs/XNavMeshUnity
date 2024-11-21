using System;
using System.Collections.Generic;

namespace XFrame.PathFinding
{
    internal static class Pool
    {
        private static Dictionary<Type, PoolItem> m_Objects = new Dictionary<Type, PoolItem>();

        public static void Spwan<T>(int capacity, int count = 1, bool fillInst = true) where T : class, new()
        {
            Type type = typeof(T);
            if (!m_Objects.TryGetValue(type, out PoolItem poolItem))
            {
                poolItem = new PoolItem();
                m_Objects[type] = poolItem;
            }
            poolItem.Spwan<T>(count, capacity, fillInst);
        }

        public static T Require<T>(int count = 16) where T : class, new()
        {
            Type type = typeof(T);
            if (!m_Objects.TryGetValue(type, out PoolItem poolItem))
            {
                poolItem = new PoolItem();
                m_Objects[type] = poolItem;
            }
            return poolItem.Require<T>(count);
        }

        public static void Release<ItemType>(ICollection<ItemType> inst)
        {
            inst.Clear();
            Type type = inst.GetType();
            if (!m_Objects.TryGetValue(type, out PoolItem item))
            {
                item = new PoolItem();
                m_Objects[type] = item;
            }
            item.Release(inst, inst.Count);
        }

        public static void Release(object inst, int count)
        {
            Type type = inst.GetType();
            if (!m_Objects.TryGetValue(type, out PoolItem item))
            {
                item = new PoolItem();
                m_Objects[type] = item;
            }
            item.Release(inst, count);
        }
    }
}
