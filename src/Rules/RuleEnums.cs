using System;

namespace NextStepGuide.Rules
{
    /// <summary>Advice category. Mirrors the `category` field in milestones.yaml.</summary>
    public enum RuleCategory
    {
        Oxygen, Power, Food, Water, Sanitation, Heat, Morale,
        Research, Industry, Exploration, Space, Dupes,
        Unknown
    }

    /// <summary>Progression tier. Mirrors the `tier` field in milestones.yaml.</summary>
    public enum RuleTier
    {
        Survival, Stabilization, Infrastructure, Endgame,
        Unknown
    }

    /// <summary>Urgency banding from the spec (used for colour + ordering).</summary>
    public enum UrgencyBand
    {
        Polish,   // 1..29   grey
        Progress, // 30..59  blue   (the "what now?" sweet spot)
        Pressing, // 60..89  amber
        Crisis    // 90..100 red
    }

    public static class RuleEnumParse
    {
        public static RuleCategory Category(string s)
            => Enum.TryParse(s, true, out RuleCategory c) ? c : RuleCategory.Unknown;

        public static RuleTier Tier(string s)
            => Enum.TryParse(s, true, out RuleTier t) ? t : RuleTier.Unknown;

        public static UrgencyBand Band(int urgency)
        {
            if (urgency >= 90) return UrgencyBand.Crisis;
            if (urgency >= 60) return UrgencyBand.Pressing;
            if (urgency >= 30) return UrgencyBand.Progress;
            return UrgencyBand.Polish;
        }

        /// <summary>Survival sorts before Stabilization, etc. Lower = earlier.</summary>
        public static int TierRank(RuleTier t)
        {
            switch (t)
            {
                case RuleTier.Survival: return 0;
                case RuleTier.Stabilization: return 1;
                case RuleTier.Infrastructure: return 2;
                case RuleTier.Endgame: return 3;
                default: return 4;
            }
        }
    }
}
