using System.Collections.Generic;
using NextStepGuide.Rules.Definitions;

namespace NextStepGuide.Rules
{
    /// <summary>
    /// The set of detection rules wired up in code. Adding a rule = add its class
    /// here AND ensure milestones.yaml has a matching active entry by id. The
    /// engine ignores any rule whose milestone is missing or status: draft.
    /// </summary>
    public static class RuleRegistry
    {
        public static IReadOnlyList<IRule> CreateAll() => new IRule[]
        {
            // Survival
            new OxygenSourceRule(),
            new ToiletRule(),
            new BasicFarmRule(),
            new ResearchStationsRule(),
            new PowerBrownoutRule(),
            new MoraleBasicsRule(),

            // Stabilization
            new ElectrolyzerTransitionRule(),
            new PowerCoalRule(),
            new CookFoodRule(),
            new WaterSieveRule(),
            new LavatoryUpgradeRule(),
            new HeatAwarenessRule(),
            new AtmoSuitsRule(),

            // Infrastructure
            new MetalRefineryRule(),
            new SteelRule(),
            new PlasticRule(),
            new RanchingRule(),
            new PetroleumPowerRule(),
            new AquaTunerRule(),
        };
    }
}
