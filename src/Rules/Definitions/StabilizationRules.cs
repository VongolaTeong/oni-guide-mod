using NextStepGuide.State;

namespace NextStepGuide.Rules.Definitions
{
    // =========================================================================
    // TIER 2 — STABILIZATION. The first "what now?" zone.
    // =========================================================================

    /// <summary>
    /// oxygen.electrolyzer — the colony still makes oxygen from algae Diffusers
    /// and hasn't built the electrolyzer step yet. PROGRESSION RULE: it gates on
    /// STRUCTURE (on diffusers, no electrolyzer), NOT on how much algae is left.
    /// A big algae buffer does not hide the advice — it only lowers its urgency.
    /// The whole point is to answer "what to build next" even when nothing is on
    /// fire; a comfortable stockpile of a FINITE resource is the best time to plan
    /// the transition, not a reason to go silent. See CLAUDE.md §6 (progression
    /// gates on structure, stock only scales urgency).
    /// </summary>
    public sealed class ElectrolyzerTransitionRule : RuleBase
    {
        /// <summary>
        /// At/above this much accessible algae the transition is low-priority
        /// "plan ahead"; as algae falls to zero it ramps toward near-crisis.
        /// Affects URGENCY ONLY — never whether the advice shows.
        /// </summary>
        public const float AlgaePlentifulKg = 1200f;

        public override string Id => "oxygen.electrolyzer";

        public override bool IsRelevant(ColonySnapshot s)
        {
            if (!s.DuplicantsKnown || s.LiveDuplicants <= 0) return false;
            if (!s.BuildingsKnown) return false;

            // Structural: on algae oxygen and the next step isn't built yet.
            return s.HasAnyBuilding(Prefabs.OxygenDiffuser)
                && !s.HasAnyBuilding(Prefabs.Electrolyzer);
        }

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.Electrolyzer);

        public override int Urgency(ColonySnapshot s, MilestoneDef def)
        {
            // Plenty of algae -> low "plan ahead" urgency (Progress band);
            // depleting -> ramps toward the top of Pressing. If algae is unknown,
            // fall back to the milestone baseline.
            if (!s.ResourcesKnown) return def?.UrgencyBase ?? 55;

            float algae = s.Resource(Elements.Algae);
            float t = (AlgaePlentifulKg - algae) / AlgaePlentifulKg; // 0 plentiful, 1 empty
            return Scale(t, 38, 89);
        }
    }
}
