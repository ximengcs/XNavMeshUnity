
using System;

public partial class Test2
{
    public class Updater
    {
        private Action m_Handler;

        public Updater(Action handler)
        {
            m_Handler = handler;
        }

        public void OnUpdate()
        {
            m_Handler?.Invoke();
        }
    }
}
