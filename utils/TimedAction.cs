namespace PoPM.utils
{
    public struct TimedAction
    {
        private float lifetime;
        private float end;
        private bool unscaledTime;
        private bool lied;

        public extern TimedAction(float lifetime, bool unscaledTime = false);

        public extern void Start();

        public extern void StartLifetime(float lifetime);

        public extern void Stop();

        public extern float Remaining();

        public extern float Elapsed();

        public extern float Ratio();

        public extern bool TrueDone();

        private extern float GetTime();

        public extern bool Done();
    }
}