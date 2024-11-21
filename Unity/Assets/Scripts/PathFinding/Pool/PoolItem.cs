
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
            int num = XMath.LogInt(2, count);
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

        public void Spwan<T>(int capacity, int count, bool fillInst) where T : class, new()
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
}
