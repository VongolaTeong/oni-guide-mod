using System.Collections.Generic;
using System.Linq;
using NextStepGuide.Rules;
using NextStepGuide.State;
using Xunit;

namespace NextStepGuide.Tests
{
    public class RuleEngineTests
    {
        private static List<string> Ids(IEnumerable<Recommendation> recs)
            => recs.Select(r => r.Id).ToList();

        [Fact]
        public void FreshBase_SurfacesSurvivalGaps_OrderedByUrgency()
        {
            var recs = Kb.Engine().Evaluate(Snap.Fresh());
            var ids = Ids(recs);

            Assert.Contains("oxygen.source", ids);
            Assert.Contains("sanitation.toilet", ids);
            Assert.Contains("food.basic_farm", ids);

            // research needs a basic station; electrolyzer needs a diffuser — neither here.
            Assert.DoesNotContain("research.early_stations", ids);
            Assert.DoesNotContain("oxygen.electrolyzer", ids);

            // Strictly non-increasing urgency.
            for (int i = 1; i < recs.Count; i++)
                Assert.True(recs[i - 1].Urgency >= recs[i].Urgency);

            // oxygen.source (96) is the most urgent of the three.
            Assert.Equal("oxygen.source", recs[0].Id);
        }

        [Fact]
        public void SolvedBase_SurfacesNothing()
        {
            // A base that has covered every active rule's next step: oxygen
            // (electrolyzer), a toilet, a farm, a kitchen, and a water sieve.
            var s = Snap.Fresh()
                .With(Prefabs.Electrolyzer)
                .With(Prefabs.FlushToilet)
                .With(Prefabs.FarmTile)
                .With(Prefabs.MicrobeMusher)   // satisfies food.cooked
                .With(Prefabs.WaterSieve);     // satisfies water.sieve

            var recs = Kb.Engine().Evaluate(s);
            Assert.Empty(recs);
        }

        [Fact]
        public void MaxResults_IsRespected()
        {
            var recs = Kb.Engine().Evaluate(Snap.Fresh(), maxResults: 2);
            Assert.Equal(2, recs.Count);
            Assert.Equal("oxygen.source", recs[0].Id); // top two by urgency
            Assert.Equal("sanitation.toilet", recs[1].Id);
        }

        [Fact]
        public void Dismissed_Ids_AreExcluded()
        {
            var dismissed = new HashSet<string> { "oxygen.source" };
            var recs = Kb.Engine().Evaluate(Snap.Fresh(), dismissed);
            Assert.DoesNotContain("oxygen.source", Ids(recs));
            Assert.Contains("sanitation.toilet", Ids(recs));
        }

        [Fact]
        public void MutedCategory_IsFiltered_BeforeTopN()
        {
            var muted = new HashSet<RuleCategory> { RuleCategory.Oxygen };
            var recs = Kb.Engine().Evaluate(Snap.Fresh(), dismissed: null, mutedCategories: muted);
            var ids = Ids(recs);

            // No Oxygen-category tips survive the mute...
            Assert.DoesNotContain("oxygen.source", ids);
            Assert.All(recs, r => Assert.NotEqual(RuleCategory.Oxygen, r.Category));
            // ...but other categories still come through.
            Assert.Contains("sanitation.toilet", ids);
            Assert.Contains("food.basic_farm", ids);
        }

        [Fact]
        public void Dependencies_GateUntilPrerequisiteSolved()
        {
            // oxygen.electrolyzer depends_on oxygen.source. With NO oxygen building
            // at all, oxygen.source is an open problem, so even a contrived low-algae
            // state must not surface the electrolyzer advice (and there's no diffuser
            // anyway). Once a diffuser exists, oxygen.source is satisfied and the
            // electrolyzer nudge can appear.
            var noOxygen = Snap.Fresh().WithResource(Elements.Algae, 50);
            Assert.DoesNotContain("oxygen.electrolyzer", Ids(Kb.Engine().Evaluate(noOxygen)));

            var onDiffusers = Snap.Fresh().With(Prefabs.OxygenDiffuser).WithResource(Elements.Algae, 50);
            Assert.Contains("oxygen.electrolyzer", Ids(Kb.Engine().Evaluate(onDiffusers)));
        }

        [Fact]
        public void StabilizedBase_HandsOff_FromPowerToRefinery()
        {
            // Survival is solved (O2/toilet/food) and the base runs on Manual
            // Generators -> the next step is "move off manual power"; it's too
            // early to nag about a Metal Refinery you can't run on hand-cranks.
            var manualBase = Snap.Fresh()
                .With(Prefabs.Electrolyzer).With(Prefabs.FlushToilet).With(Prefabs.FarmTile)
                .With(Prefabs.ManualGenerator);
            var manualIds = Ids(Kb.Engine().Evaluate(manualBase));
            Assert.Contains("power.coal", manualIds);
            Assert.DoesNotContain("industry.refined_metal", manualIds);

            // Once power is automated, "move off manual" is solved and "build a
            // Metal Refinery" becomes the next step (its depends_on power.coal is
            // now met because power.coal is no longer an open problem).
            var poweredBase = Snap.Fresh()
                .With(Prefabs.Electrolyzer).With(Prefabs.FlushToilet).With(Prefabs.FarmTile)
                .With(Prefabs.CoalGenerator);
            var poweredIds = Ids(Kb.Engine().Evaluate(poweredBase));
            Assert.DoesNotContain("power.coal", poweredIds);
            Assert.Contains("industry.refined_metal", poweredIds);
        }

        [Fact]
        public void CategoryCollapse_KeepsAtMostOnePerCategory()
        {
            // Force a contrived double-fire by disabling category collapse and
            // comparing counts isn't possible with mutually-exclusive oxygen rules,
            // so assert the property directly: no two recommendations share a category.
            var recs = Kb.Engine().Evaluate(Snap.Fresh());
            var categories = recs.Select(r => r.Category).ToList();
            Assert.Equal(categories.Count, categories.Distinct().Count());
        }

        [Fact]
        public void UnknownProbes_ProduceNoAdvice()
        {
            // A snapshot where building data failed to read must not fire
            // building-count rules (fail-soft: silence, not false positives).
            var s = Snap.Fresh();
            s.BuildingsKnown = false;
            var recs = Kb.Engine().Evaluate(s);
            Assert.DoesNotContain("oxygen.source", Ids(recs));
            Assert.DoesNotContain("sanitation.toilet", Ids(recs));
        }
    }
}
