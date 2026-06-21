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
}
