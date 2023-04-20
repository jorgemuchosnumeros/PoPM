using System;
using System.Threading;

namespace PoPM
{
    public class TimedAction
    {
        private int lifetime;
        private bool done;
        private Timer aTimer;
        AutoResetEvent autoEvent = new AutoResetEvent(false);

        public TimedAction(float lifetime)
        {
            this.lifetime = (int) (lifetime * 1000);
        }
        
        public void Start()
        {
            done = false;
            aTimer = new Timer(OnTimedEvent, autoEvent, lifetime, 0);
        }

        private void OnTimedEvent(object state)
        {
            done = true;
            aTimer.Dispose();
        }

        public bool TrueDone()
        {
            return done;
        }
    }
}