using System;
using System.Timers;

namespace PoPM
{
    public class TimedAction
    {
        private float lifetime;
        private bool done;
        private Timer aTimer;

        public TimedAction(float lifetime)
        {
            this.lifetime = lifetime;
        }
        
        public void Start()
        {
            done = false;
            aTimer = new Timer(lifetime * 1000);
            aTimer.Elapsed += OnTimedEvent;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            done = true;
        }
        
        public bool TrueDone()
        {
            return done;
        }
    }
}