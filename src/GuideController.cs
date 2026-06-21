using NextStepGuide.UI;
using NextStepGuide.Util;
using UnityEngine;

namespace NextStepGuide
{
    /// <summary>
    /// Per-colony driver attached to the Game GameObject. Throttles the heavy
    /// StateReader + RuleEngine work off the per-frame hot path (default every
    /// 3 real seconds) and skips recompute while the game is paused. Owns panel
    /// creation/refresh; the panel only ever renders GuideRuntime.Latest.
    /// </summary>
    public sealed class GuideController : MonoBehaviour
    {
        private Throttle _throttle = new Throttle(3f);
        private float _throttleInterval = 3f;
        private int _lastRenderedVersion = -1;

        private void Start()
        {
            GuideRuntime.EnsureInitialised();
            GuideRuntime.Recompute(logToConsole: true);
        }

        private void Update()
        {
            // Create the panel as soon as the HUD canvas exists (retries each tick).
            GuidePanel.EnsureCreated();

            // Honour the user's configured refresh interval (rebuild the gate if it changed).
            float interval = GuideRuntime.Settings != null ? GuideRuntime.Settings.RefreshIntervalSeconds : 3f;
            if (Mathf.Abs(interval - _throttleInterval) > 0.01f)
            {
                _throttleInterval = interval;
                _throttle = new Throttle(interval);
            }

            // Recompute on the throttle, but not while paused (display stays put).
            if (_throttle.Ready(Time.unscaledTime) && !IsPaused())
                GuideRuntime.Recompute(logToConsole: true);

            // Redraw only when the evaluation actually changed (or panel is new).
            var panel = GuidePanel.Instance;
            if (panel != null && GuideRuntime.Version != _lastRenderedVersion)
            {
                panel.Render(GuideRuntime.LastSnapshot, GuideRuntime.Latest);
                _lastRenderedVersion = GuideRuntime.Version;
            }
        }

        private static bool IsPaused()
        {
            try { return SpeedControlScreen.Instance != null && SpeedControlScreen.Instance.IsPaused; }
            catch { return false; }
        }
    }
}
