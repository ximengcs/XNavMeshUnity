using System;
using System.Collections.Generic;

namespace XFrame.PathFinding
{
    internal class PoolItem
    {
        private const int MIN = 4;
        private Dictionary<int, List<object>> m_Items;

        public PoolItem()
        {
            m_Items = new Dictionary<int, List<object>>();
        }

        private int InnerGetSize(int count)
        {
            int num = (int)Math.Ceiling(Math.Log(count, 2));
            num = Math.Min(num, MIN);
            return num;
        }

        public T Require<T>(int count) where T : class, new()
        {
            int num = InnerGetSize(count);
            if (!m_Items.TryGetValue(num, out List<object> items))
            {
                items = new List<object>(16);
                m_Items.Add(num, items);
            }

            if (items.Count > 0)
            {
                int lastIndex = items.Count - 1;
                object inst = items[lastIndex];
                items.RemoveAt(lastIndex);
                return (T)inst;
            }
            else
            {
                return new T();
            }
        }

        public void Release(object inst, int count)
        {
            int num = InnerGetSize(count);
            if (!m_Items.TryGetValue(num, out List<object> items))
            {
                items = new List<object>(16);
                m_Items.Add(num, items);
            }

            items.Add(inst);
        }

        public void Spwan<T>(int count, int capacity, bool fillInst) where T : class, new()
        {
            Type type = typeof(T);
            int num = InnerGetSize(count);
            if (!m_Items.TryGetValue(num, out List<object> items))
            {
                items = new List<object>(capacity);
                m_Items.Add(num, items);
            }
            if (fillInst)
            {
                for (int j = items.Count; j < items.Capacity; j++)
                    items.Add(new T());
            }
        }
    }

    internal static class XPool
    {
        private static Dictionary<Type, PoolItem> m_Objects = new Dictionary<Type, PoolItem>();

        public static void Spwan<T>(int count, int capacity, bool fillInst) where T : class, new()
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

        public static void Release(object inst, int count = 16)
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
