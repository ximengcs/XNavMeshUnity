
using System;

public partial class Test2
{
    public class Updater
    {
        private Func<bool> m_Handler;

        public Updater(Func<bool> handler)
        {
            m_Handler = handler;
        }

        public bool OnUpdate()
        {
            return m_Handler.Invoke();
        }
    }
}
