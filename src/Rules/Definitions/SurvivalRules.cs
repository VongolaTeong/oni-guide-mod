using NextStepGuide.State;

namespace NextStepGuide.Rules.Definitions
{
    // =========================================================================
    // TIER 1 — SURVIVAL. High-precision safety nets: only fire on a genuine gap,
    // detected purely from building counts so there are no false positives from
    // unknown/estimated data. Each gates on DuplicantsKnown + BuildingsKnown so a
    // failed probe hides the rule rather than firing on zeros.
    // =========================================================================

    /// <summary>oxygen.source — no oxygen-generating building exists.</summary>
    public sealed class OxygenSourceRule : RuleBase
    {
        public override string Id => "oxygen.source";

        public override bool IsRelevant(ColonySnapshot s)
            => s.DuplicantsKnown && s.LiveDuplicants > 0 && s.BuildingsKnown;

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.OxygenSources);
    }

    /// <summary>sanitation.toilet — no Outhouse or Lavatory exists.</summary>
    public sealed class ToiletRule : RuleBase
    {
        public override string Id => "sanitation.toilet";

        public override bool IsRelevant(ColonySnapshot s)
            => s.DuplicantsKnown && s.LiveDuplicants > 0 && s.BuildingsKnown;

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.Toilets);
    }

    /// <summary>food.basic_farm — no cultivated food source (planters/farm tiles).</summary>
    public sealed class BasicFarmRule : RuleBase
    {
        public override string Id => "food.basic_farm";

        public override bool IsRelevant(ColonySnapshot s)
            => s.DuplicantsKnown && s.LiveDuplicants > 0 && s.BuildingsKnown;

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.Farms);
    }

    /// <summary>
    /// research.early_stations — the player has the basic Research Station running
    /// but hasn't built the advanced station (Super Computer) yet. Gating on the
    /// basic station keeps this precise: it only nudges the natural NEXT step,
    /// never "build a Super Computer" on a cycle-1 base.
    /// </summary>
    public sealed class ResearchStationsRule : RuleBase
    {
        public override string Id => "research.early_stations";

        public override bool IsRelevant(ColonySnapshot s)
            => s.DuplicantsKnown && s.LiveDuplicants > 0 && s.BuildingsKnown
               && s.HasAnyBuilding(Prefabs.ResearchCenter);

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.AdvancedResearchCenter);
    }

    /// <summary>
    /// power.basic — the base draws power but has no battery buffer, or its
    /// batteries are nearly drained while consumption outstrips generation. Uses
    /// the battery level as the integrated signal (a low battery means a sustained
    /// shortfall, not a momentary spike). Fires when there's no battery at all
    /// (the milestone's literal "add a battery" advice) or the most-drained
    /// circuit is below ~20%.
    /// </summary>
    public sealed class PowerBrownoutRule : RuleBase
    {
        public const float LowBatteryFraction = 0.20f;

        public override string Id => "power.basic";

        public override bool IsRelevant(ColonySnapshot s)
            => s.PowerKnown && s.DuplicantsKnown && s.LiveDuplicants > 0
               && s.PowerConsumedW > 0f;

        public override bool IsSatisfied(ColonySnapshot s)
            // Solved when there's a healthy battery buffer to ride out shortfalls.
            => s.PowerHasBattery && s.BatteryChargeFraction >= LowBatteryFraction;

        public override int Urgency(ColonySnapshot s, MilestoneDef def)
        {
            int b = def?.UrgencyBase ?? 70;
            if (!s.PowerHasBattery) return b; // no battery yet -> baseline "add one"
            // Draining battery -> ramp from baseline toward crisis as it empties.
            float t = (LowBatteryFraction - s.BatteryChargeFraction) / LowBatteryFraction;
            return Scale(t, b, 95);
        }
    }

    /// <summary>
    /// morale.basics — average duplicant stress is elevated. A genuine survival
    /// gap (like missing oxygen), so it DOES gate on the measured problem: it
    /// surfaces once average stress crosses ~30% and clears when it drops back
    /// down. Urgency ramps with the stress level.
    /// </summary>
    public sealed class MoraleBasicsRule : RuleBase
    {
        public const float StressFireAbovePct = 30f;

        public override string Id => "morale.basics";

        public override bool IsRelevant(ColonySnapshot s)
            => s.StressKnown && s.DuplicantsKnown && s.LiveDuplicants > 0;

        public override bool IsSatisfied(ColonySnapshot s)
            => s.AvgStress < StressFireAbovePct;

        public override int Urgency(ColonySnapshot s, MilestoneDef def)
        {
            int b = def?.UrgencyBase ?? 60;
            float t = (s.AvgStress - StressFireAbovePct) / (100f - StressFireAbovePct);
            return Scale(t, b, 92);
        }
    }
}
