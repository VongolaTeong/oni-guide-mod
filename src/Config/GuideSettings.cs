using System;
using System.Collections.Generic;
using NextStepGuide.Rules;
using PeterHan.PLib.Options;

namespace NextStepGuide.Config
{
    /// <summary>
    /// User-facing settings, shown in the mod's Options dialog (PLib) and
    /// persisted to config.json. Also stores the persisted set of dismissed tip
    /// ids (not shown in the dialog; reset from the panel's "[reset]" link).
    ///
    /// Implements <see cref="IOptions"/> so the engine picks up edits live via
    /// <see cref="OnOptionsChanged"/> without a restart.
    /// </summary>
    [ConfigFile("config.json", true)]
    public sealed class GuideSettings : IOptions
    {
        // ---- Display ----
        [Option("Show reasons", "Show the one-line 'why' under each tip.", "Display")]
        public bool ShowWhy { get; set; } = true;

        [Option("Max tips shown", "Most recommendations to show at once.", "Display")]
        [Limit(1, 8, 1)]
        public int MaxTips { get; set; } = 4;

        [Option("Refresh interval (s)", "How often to re-check the colony.", "Display")]
        [Limit(1, 30, 1)]
        public float RefreshIntervalSeconds { get; set; } = 3f;

        // ---- Mute categories ----
        // Some players never want certain nags (morale, dupes, ...). Muted
        // categories are filtered out before the top-N cut.
        private const string Mutes = "Mute categories";
        [Option("Oxygen", null, Mutes)] public bool MuteOxygen { get; set; }
        [Option("Power", null, Mutes)] public bool MutePower { get; set; }
        [Option("Food", null, Mutes)] public bool MuteFood { get; set; }
        [Option("Water", null, Mutes)] public bool MuteWater { get; set; }
        [Option("Sanitation", null, Mutes)] public bool MuteSanitation { get; set; }
        [Option("Heat", null, Mutes)] public bool MuteHeat { get; set; }
        [Option("Morale", null, Mutes)] public bool MuteMorale { get; set; }
        [Option("Research", null, Mutes)] public bool MuteResearch { get; set; }
        [Option("Industry", null, Mutes)] public bool MuteIndustry { get; set; }
        [Option("Exploration", null, Mutes)] public bool MuteExploration { get; set; }
        [Option("Space", null, Mutes)] public bool MuteSpace { get; set; }
        [Option("Dupes", null, Mutes)] public bool MuteDupes { get; set; }

        // ---- Persisted state (not shown in the dialog) ----
        public List<string> DismissedIds { get; set; } = new List<string>();

        // =====================================================================

        public bool IsMuted(RuleCategory c)
        {
            switch (c)
            {
                case RuleCategory.Oxygen: return MuteOxygen;
                case RuleCategory.Power: return MutePower;
                case RuleCategory.Food: return MuteFood;
                case RuleCategory.Water: return MuteWater;
                case RuleCategory.Sanitation: return MuteSanitation;
                case RuleCategory.Heat: return MuteHeat;
                case RuleCategory.Morale: return MuteMorale;
                case RuleCategory.Research: return MuteResearch;
                case RuleCategory.Industry: return MuteIndustry;
                case RuleCategory.Exploration: return MuteExploration;
                case RuleCategory.Space: return MuteSpace;
                case RuleCategory.Dupes: return MuteDupes;
                default: return false;
            }
        }

        public ISet<RuleCategory> MutedCategorySet()
        {
            var set = new HashSet<RuleCategory>();
            foreach (RuleCategory c in Enum.GetValues(typeof(RuleCategory)))
                if (IsMuted(c)) set.Add(c);
            return set;
        }

        public ISet<string> DismissedSet()
            => new HashSet<string>(DismissedIds ?? new List<string>(), StringComparer.Ordinal);

        // ---- IOptions ----
        public IEnumerable<IOptionsEntry> CreateOptions() { yield break; }

        public void OnOptionsChanged() => GuideRuntime.ApplyOptionChanges(this);
    }
}
