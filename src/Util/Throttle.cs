namespace NextStepGuide.Util
{
    /// <summary>
    /// Minimal time gate: returns true at most once per interval. Pure (the
    /// caller passes the current time), so it carries no game dependency and is
    /// trivially testable.
    /// </summary>
    public sealed class Throttle
    {
        private readonly float _intervalSeconds;
        private float _nextReadyAt;
        private bool _primed;

        public Throttle(float intervalSeconds)
        {
            _intervalSeconds = intervalSeconds < 0f ? 0f : intervalSeconds;
        }

        /// <summary>True if at least the interval has elapsed since the last true.</summary>
        public bool Ready(float now)
        {
            if (!_primed || now >= _nextReadyAt)
            {
                _primed = true;
                _nextReadyAt = now + _intervalSeconds;
                return true;
            }
            return false;
        }
    }
}
