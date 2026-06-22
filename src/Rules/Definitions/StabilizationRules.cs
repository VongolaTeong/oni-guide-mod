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

    /// <summary>
    /// power.coal — the colony still runs on hand-cranked Manual Generators and
    /// hasn't built any automated power source yet. PROGRESSION RULE: it gates on
    /// STRUCTURE (a Manual Generator exists, no automated generator does), never on
    /// a power-Watt measurement — so the nudge persists from the moment you're on
    /// manual power until you actually automate it.
    ///
    /// Requiring a Manual Generator to be present keeps it precise: a brand-new
    /// base with no power at all is the survival "add power" case (power.basic),
    /// not this "move OFF manual" one. Satisfied by ANY automated generator
    /// (coal/wood/hydrogen/gas/petroleum/turbine/solar) so a player who jumped
    /// straight past coal is never told to "move to coal generators".
    ///
    /// Urgency stays at the milestone baseline for now; once the StateReader can
    /// read circuit load it can ramp up when consumption outpaces a single dupe's
    /// cranking. (CLAUDE.md §5 power probe — Phase 5 follow-up.)
    /// </summary>
    public sealed class PowerCoalRule : RuleBase
    {
        public override string Id => "power.coal";

        public override bool IsRelevant(ColonySnapshot s)
        {
            if (!s.DuplicantsKnown || s.LiveDuplicants <= 0) return false;
            if (!s.BuildingsKnown) return false;

            return s.HasAnyBuilding(Prefabs.ManualGenerator)
                && !s.HasAnyBuilding(Prefabs.AutomatedGenerators);
        }

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.AutomatedGenerators);
    }

    /// <summary>
    /// food.cooked — a farm is running but nothing cooks the harvest. A concrete
    /// small win between "start a farm" and "move to a sustainable crop": cooking
    /// raises calories-per-plant and morale. Gated on a farm existing (there's
    /// food to cook) so it never fires before the player has any crops.
    /// </summary>
    public sealed class CookFoodRule : RuleBase
    {
        public override string Id => "food.cooked";

        public override bool IsRelevant(ColonySnapshot s)
            => s.DuplicantsKnown && s.LiveDuplicants > 0 && s.BuildingsKnown
               && s.HasAnyBuilding(Prefabs.Farms)
               && !s.HasAnyBuilding(Prefabs.CookingStations);

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.CookingStations);
    }

    /// <summary>
    /// water.sieve — a Flush Toilet (a polluted-water source) is running but no
    /// Water Sieve recycles its output. Keys off the Flush Toilet specifically:
    /// an outhouse-only base produces Polluted Dirt, not Polluted Water, so it
    /// has nothing to sieve yet — keeping this precise.
    /// </summary>
    public sealed class WaterSieveRule : RuleBase
    {
        public override string Id => "water.sieve";

        public override bool IsRelevant(ColonySnapshot s)
            => s.DuplicantsKnown && s.LiveDuplicants > 0 && s.BuildingsKnown
               && s.HasAnyBuilding(Prefabs.FlushToilet)
               && !s.HasAnyBuilding(Prefabs.WaterSieve);

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.WaterSieve);
    }

    /// <summary>
    /// sanitation.lavatory — the base is established (a Research Station exists)
    /// but still runs purely on Outhouses. Nudges the upgrade to Flush Toilets.
    /// The Research-Station gate keeps it out of the opening survival phase, where
    /// outhouses are the correct choice; once a player has both an outhouse and a
    /// flush toilet (mid-transition) it goes quiet.
    /// </summary>
    public sealed class LavatoryUpgradeRule : RuleBase
    {
        public override string Id => "sanitation.lavatory";

        public override bool IsRelevant(ColonySnapshot s)
            => s.DuplicantsKnown && s.LiveDuplicants > 0 && s.BuildingsKnown
               && s.HasAnyBuilding(Prefabs.ResearchCenter)   // established-base gate
               && s.HasAnyBuilding(Prefabs.Outhouse)
               && !s.HasAnyBuilding(Prefabs.FlushToilet);

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.FlushToilet);
    }

    /// <summary>
    /// heat.awareness — the temperature where dupes live is climbing past comfort
    /// (~30 C) and rising. Heat only accumulates, so this is a genuine creeping
    /// problem: it gates on the measured base temperature (not structure) and
    /// ramps urgency as it gets hotter. If the base cools back down (insulation,
    /// cooling, wheezeworts) the tip clears on its own.
    /// </summary>
    public sealed class HeatAwarenessRule : RuleBase
    {
        public const float WarmAboveK = 303.15f;  // 30 C — start paying attention
        public const float HotK = 318.15f;        // 45 C — clearly a problem

        public override string Id => "heat.awareness";

        public override bool IsRelevant(ColonySnapshot s)
            => s.HeatKnown && s.DuplicantsKnown && s.LiveDuplicants > 0;

        public override bool IsSatisfied(ColonySnapshot s)
            => s.AvgBaseTempK < WarmAboveK;

        public override int Urgency(ColonySnapshot s, MilestoneDef def)
        {
            int b = def?.UrgencyBase ?? 50;
            float t = (s.AvgBaseTempK - WarmAboveK) / (HotK - WarmAboveK);
            return Scale(t, b, 88);
        }
    }

    /// <summary>
    /// suits.atmo — refined metal is available (a Metal Refinery exists) but there's
    /// no atmo-suit setup yet. Suits let dupes work safely in hot/polluted/vacuum
    /// biomes, which is the gateway to the whole mid-game (oil, cool steam vents,
    /// gas geysers). Structure-gated on the refinery; the "before you dig into a
    /// hazard biome" nuance is in the why text since biome-adjacency isn't read.
    /// </summary>
    public sealed class AtmoSuitsRule : RuleBase
    {
        public override string Id => "suits.atmo";

        public override bool IsRelevant(ColonySnapshot s)
            => s.DuplicantsKnown && s.LiveDuplicants > 0 && s.BuildingsKnown
               && s.HasAnyBuilding(Prefabs.MetalRefinery)
               && !s.HasAnyBuilding(Prefabs.AtmoSuitStations);

        public override bool IsSatisfied(ColonySnapshot s)
            => s.HasAnyBuilding(Prefabs.AtmoSuitStations);
    }
}
