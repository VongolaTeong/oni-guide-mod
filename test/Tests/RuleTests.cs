using NextStepGuide.Rules;
using NextStepGuide.Rules.Definitions;
using NextStepGuide.State;
using Xunit;

namespace NextStepGuide.Tests
{
    /// <summary>
    /// Per-rule fixtures: one snapshot where the rule should FIRE, one where it's
    /// SATISFIED, and one where it's IRRELEVANT.
    /// </summary>
    public class RuleTests
    {
        // ---- oxygen.source --------------------------------------------------

        [Fact]
        public void OxygenSource_Fires_WhenNoOxygenBuilding()
        {
            var r = new OxygenSourceRule();
            var s = Snap.Fresh();
            Assert.True(r.IsRelevant(s));
            Assert.False(r.IsSatisfied(s));
        }

        [Fact]
        public void OxygenSource_Satisfied_WithAnyOxygenBuilding()
        {
            var r = new OxygenSourceRule();
            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.OxygenDiffuser)));
            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.Electrolyzer)));
        }

        [Fact]
        public void OxygenSource_Irrelevant_WhenNoDupesOrUnknownBuildings()
        {
            var r = new OxygenSourceRule();
            Assert.False(r.IsRelevant(Snap.Fresh(dupes: 0)));
            var unknown = Snap.Fresh();
            unknown.BuildingsKnown = false;
            Assert.False(r.IsRelevant(unknown));
        }

        // ---- sanitation.toilet ---------------------------------------------

        [Fact]
        public void Toilet_Fires_ThenSatisfied()
        {
            var r = new ToiletRule();
            Assert.False(r.IsSatisfied(Snap.Fresh()));
            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.Outhouse)));
            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.FlushToilet)));
        }

        // ---- food.basic_farm ------------------------------------------------

        [Fact]
        public void Farm_Fires_ThenSatisfied()
        {
            var r = new BasicFarmRule();
            Assert.True(r.IsRelevant(Snap.Fresh()) && !r.IsSatisfied(Snap.Fresh()));
            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.PlanterBox)));
            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.FarmTile)));
        }

        // ---- research.early_stations ---------------------------------------

        [Fact]
        public void Research_Irrelevant_UntilBasicStationBuilt()
        {
            var r = new ResearchStationsRule();
            Assert.False(r.IsRelevant(Snap.Fresh())); // no basic station yet
        }

        [Fact]
        public void Research_Fires_WithBasicButNoAdvanced_ThenSatisfied()
        {
            var r = new ResearchStationsRule();
            var withBasic = Snap.Fresh().With(Prefabs.ResearchCenter);
            Assert.True(r.IsRelevant(withBasic));
            Assert.False(r.IsSatisfied(withBasic));

            var withBoth = Snap.Fresh().With(Prefabs.ResearchCenter).With(Prefabs.AdvancedResearchCenter);
            Assert.True(r.IsSatisfied(withBoth));
        }

        // ---- oxygen.electrolyzer (progression: structure-gated) ------------

        [Fact]
        public void Electrolyzer_Irrelevant_WithoutADiffuser()
        {
            var r = new ElectrolyzerTransitionRule();
            // Not on algae oxygen at all → nothing to transition from.
            Assert.False(r.IsRelevant(Snap.Fresh().WithResource(Elements.Algae, 50)));
        }

        [Fact]
        public void Electrolyzer_Stays_Relevant_EvenWhenAlgaePlentiful_ButLowerUrgency()
        {
            // Progression rule: a big algae buffer must NOT hide the advice — it
            // only lowers urgency into the "plan ahead" (Progress) band.
            var r = new ElectrolyzerTransitionRule();
            var def = Kb.Library.Get(r.Id);

            var plentiful = Snap.Fresh().With(Prefabs.OxygenDiffuser).WithResource(Elements.Algae, 3000);
            Assert.True(r.IsRelevant(plentiful));
            Assert.False(r.IsSatisfied(plentiful));
            Assert.InRange(r.Urgency(plentiful, def), 30, 59); // Progress band, not silent
        }

        [Fact]
        public void Electrolyzer_Fires_WhenDiffuserAndLowAlgaeAndNoElectrolyzer()
        {
            var r = new ElectrolyzerTransitionRule();
            var s = Snap.Fresh().With(Prefabs.OxygenDiffuser).WithResource(Elements.Algae, 100);
            Assert.True(r.IsRelevant(s));
            Assert.False(r.IsSatisfied(s));
        }

        [Fact]
        public void Electrolyzer_Satisfied_OnceElectrolyzerExists()
        {
            var r = new ElectrolyzerTransitionRule();
            var s = Snap.Fresh().With(Prefabs.OxygenDiffuser).With(Prefabs.Electrolyzer)
                                .WithResource(Elements.Algae, 100);
            Assert.True(r.IsSatisfied(s));
        }

        [Fact]
        public void Electrolyzer_Urgency_RampsUp_AsAlgaeFalls()
        {
            var r = new ElectrolyzerTransitionRule();
            var def = Kb.Library.Get(r.Id);

            var higherAlgae = Snap.Fresh().With(Prefabs.OxygenDiffuser).WithResource(Elements.Algae, 500);
            var lowerAlgae = Snap.Fresh().With(Prefabs.OxygenDiffuser).WithResource(Elements.Algae, 50);

            int uHigh = r.Urgency(higherAlgae, def);
            int uLow = r.Urgency(lowerAlgae, def);

            Assert.True(uLow > uHigh, $"expected lower algae to be more urgent ({uLow} vs {uHigh})");
            Assert.InRange(uLow, 1, 89);
            Assert.True(uHigh >= def.UrgencyBase);
        }
    }
}
