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
}
