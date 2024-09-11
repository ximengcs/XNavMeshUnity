using System;
using System.Collections.Generic;

namespace XFrame.PathFinding
{
    public class XNavMeshList<T> : List<T>, IDisposable
    {
        public XNavMeshList(int capacity) : base(capacity) { }

        public void Dispose()
        {

        }
    }
}
