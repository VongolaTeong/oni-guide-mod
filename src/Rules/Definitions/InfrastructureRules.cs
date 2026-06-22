using NextStepGuide.State;

namespace NextStepGuide.Rules.Definitions
{
    // =========================================================================
    // TIER 3 — INFRASTRUCTURE. The heart of the "what now?" problem: a stable
    // base with no obvious next build. These are STRUCTURE-gated progression
    // rules detected purely from building counts (no fragile state probes), so
    // they're high-precision and fully unit-testable.
    // =========================================================================

    /// <summary>
    /// industry.refined_metal — the colony has real (automated) power but no
    /// Metal Refinery yet. PROGRESSION RULE: gates on STRUCTURE — you've got the
    /// power a 1200W refinery needs, and the next pivot (refined metal → steel,
    /// advanced power, cooling, rocketry) hasn't been built.
    ///
    /// The automated-power gate keeps it from firing on a cycle-1 base: "build a
    /// Metal Refinery" only makes sense once you can actually run one. It persists
    /// until a refinery exists; a Rock Crusher (which trickles out some refined
    /// metal) does NOT satisfy it, because the refinery is the real solution the
    /// milestone is pointing at.
    /// </summary>
    public sealed class MetalRefineryRule : RuleBase
    {
        public override string Id => "industry.refined_metal";

        public override bool IsRelevant(ColonySnapshot s)
        {
            if (!s.DuplicantsKnown || s.LiveDuplicants <= 0) return false;
            if (!s.BuildingsKnown) return false;

            // Structural: the base has automated power (ready for a refinery) and
            // the next step isn't built yet.
            return s.HasAnyBuilding(Prefabs.AutomatedGenerators)
                && !s.HasAnyBuilding(Prefabs.MetalRefinery);
        }

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.MetalRefinery);
    }

    /// <summary>
    /// industry.steel — you have a Metal Refinery (so you CAN make steel) but no
    /// meaningful steel on hand yet. Steel has no dedicated building — it's a
    /// refinery recipe — so "have you produced it" is a stock question; a small
    /// stockpile means the capability exists and the tip clears.
    /// </summary>
    public sealed class SteelRule : RuleBase
    {
        public const float SteelStockedKg = 100f;

        public override string Id => "industry.steel";

        public override bool IsRelevant(ColonySnapshot s)
            => s.DuplicantsKnown && s.LiveDuplicants > 0 && s.BuildingsKnown && s.ResourcesKnown
               && s.HasAnyBuilding(Prefabs.MetalRefinery)
               && s.Resource(Elements.Steel) < SteelStockedKg;

        public override bool IsSatisfied(ColonySnapshot s)
            => s.Resource(Elements.Steel) >= SteelStockedKg;
    }

    /// <summary>
    /// industry.plastic — the base is industrialising (a Metal Refinery exists)
    /// but there's no plastic source. Satisfied by either a Polymer Press OR
    /// accessible plastic on hand — the stock check means a Drecko rancher (who
    /// has plastic but no press) is never wrongly nagged.
    /// </summary>
    public sealed class PlasticRule : RuleBase
    {
        public const float PlasticStockedKg = 100f;

        public override string Id => "industry.plastic";

        public override bool IsRelevant(ColonySnapshot s)
            => s.DuplicantsKnown && s.LiveDuplicants > 0 && s.BuildingsKnown && s.ResourcesKnown
               && s.HasAnyBuilding(Prefabs.MetalRefinery)         // industrial mid-game gate
               && !s.HasAnyBuilding(Prefabs.PolymerPress)
               && s.Resource(Elements.Plastic) < PlasticStockedKg;

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.PolymerPress)
               || s.Resource(Elements.Plastic) >= PlasticStockedKg;
    }

    /// <summary>
    /// ranching.coal — you're burning coal but have no ranch backing it up, so
    /// the coal supply is a depleting clock. Detected via a ranch-building proxy
    /// (Grooming Station / Critter Feeder / Egg Incubator): a Hatch ranch turns
    /// rock into coal (and meat) indefinitely.
    /// </summary>
    public sealed class RanchingRule : RuleBase
    {
        public override string Id => "ranching.coal";

        public override bool IsRelevant(ColonySnapshot s)
            => s.DuplicantsKnown && s.LiveDuplicants > 0 && s.BuildingsKnown
               && s.HasAnyBuilding(Prefabs.CoalGenerator)
               && !s.HasAnyBuilding(Prefabs.RanchBuildings);

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.RanchBuildings);
    }

    /// <summary>
    /// power.petroleum — the base is industrialising (a Metal Refinery exists) and
    /// still runs on coal with no denser-fuel generator. Coal is finite without a
    /// ranch; natural gas or petroleum is denser and longer-lasting. Satisfied by
    /// any natural-gas / petroleum / hydrogen generator.
    /// </summary>
    public sealed class PetroleumPowerRule : RuleBase
    {
        public override string Id => "power.petroleum";

        public override bool IsRelevant(ColonySnapshot s)
            => s.DuplicantsKnown && s.LiveDuplicants > 0 && s.BuildingsKnown
               && s.HasAnyBuilding(Prefabs.MetalRefinery)        // industrial mid-game gate
               && s.HasAnyBuilding(Prefabs.CoalGenerator)
               && !s.HasAnyBuilding(Prefabs.DenserGenerators);

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.DenserGenerators);
    }

    /// <summary>
    /// cooling.aquatuner — the base is genuinely hot AND you have the materials
    /// (steel or plastic on hand) but no AquaTuner. The AquaTuner + Steam Turbine
    /// loop is the workhorse of ONI cooling. Builds on the heat probe; gates on a
    /// real heat problem so it never fires on a cool base.
    /// </summary>
    public sealed class AquaTunerRule : RuleBase
    {
        public const float HotK = 308.15f;        // 35 C — a real heat problem
        public const float MaterialKg = 100f;

        public override string Id => "cooling.aquatuner";

        public override bool IsRelevant(ColonySnapshot s)
            => s.HeatKnown && s.DuplicantsKnown && s.LiveDuplicants > 0
               && s.BuildingsKnown && s.ResourcesKnown
               && s.AvgBaseTempK >= HotK
               && (s.Resource(Elements.Steel) >= MaterialKg || s.Resource(Elements.Plastic) >= MaterialKg)
               && !s.HasAnyBuilding(Prefabs.AquaTuner);

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.AquaTuner);

        public override int Urgency(ColonySnapshot s, MilestoneDef def)
        {
            int b = def?.UrgencyBase ?? 55;
            // Hotter -> more urgent (35 C baseline ramping toward 50 C).
            float t = (s.AvgBaseTempK - HotK) / (323.15f - HotK);
            return Scale(t, b, 90);
        }
    }
}
