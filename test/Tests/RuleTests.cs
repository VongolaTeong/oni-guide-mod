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

        // ---- power.coal (progression: structure-gated) ---------------------

        [Fact]
        public void PowerCoal_Irrelevant_WithNoGeneratorsAtAll()
        {
            // No power yet is the survival "add power" case, not "move off manual".
            var r = new PowerCoalRule();
            Assert.False(r.IsRelevant(Snap.Fresh()));
        }

        [Fact]
        public void PowerCoal_Fires_OnManualOnly_ThenSatisfiedWhenAutomated()
        {
            var r = new PowerCoalRule();

            var manualOnly = Snap.Fresh().With(Prefabs.ManualGenerator);
            Assert.True(r.IsRelevant(manualOnly));
            Assert.False(r.IsSatisfied(manualOnly));

            var automated = Snap.Fresh().With(Prefabs.ManualGenerator).With(Prefabs.CoalGenerator);
            Assert.True(r.IsSatisfied(automated));
            Assert.False(r.IsRelevant(automated));
        }

        [Fact]
        public void PowerCoal_Satisfied_ByAnyAutomatedGenerator_EvenSkippingCoal()
        {
            // A player who jumped straight to petroleum must never be told to
            // "move to coal generators".
            var r = new PowerCoalRule();
            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.PetroleumGenerator)));
            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.SolarPanel)));
            Assert.False(r.IsRelevant(Snap.Fresh().With(Prefabs.ManualGenerator).With(Prefabs.SolarPanel)));
        }

        // ---- industry.refined_metal (progression: structure-gated) ---------

        [Fact]
        public void MetalRefinery_Irrelevant_BeforeAutomatedPower()
        {
            // Don't tell a cycle-1 base to build a 1200W refinery it can't run.
            var r = new MetalRefineryRule();
            Assert.False(r.IsRelevant(Snap.Fresh()));
            Assert.False(r.IsRelevant(Snap.Fresh().With(Prefabs.ManualGenerator)));
        }

        [Fact]
        public void MetalRefinery_Fires_WithPowerButNoRefinery_ThenSatisfied()
        {
            var r = new MetalRefineryRule();

            var powered = Snap.Fresh().With(Prefabs.CoalGenerator);
            Assert.True(r.IsRelevant(powered));
            Assert.False(r.IsSatisfied(powered));

            var withRefinery = Snap.Fresh().With(Prefabs.CoalGenerator).With(Prefabs.MetalRefinery);
            Assert.True(r.IsSatisfied(withRefinery));
            Assert.False(r.IsRelevant(withRefinery));
        }

        // ---- food.cooked (gap-filler) --------------------------------------

        [Fact]
        public void CookFood_Irrelevant_WithoutAFarm()
        {
            // Nothing to cook before there's a farm.
            var r = new CookFoodRule();
            Assert.False(r.IsRelevant(Snap.Fresh()));
        }

        [Fact]
        public void CookFood_Fires_WithFarmButNoKitchen_ThenSatisfied()
        {
            var r = new CookFoodRule();

            var farmNoKitchen = Snap.Fresh().With(Prefabs.FarmTile);
            Assert.True(r.IsRelevant(farmNoKitchen));
            Assert.False(r.IsSatisfied(farmNoKitchen));

            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.FarmTile).With(Prefabs.MicrobeMusher)));
            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.FarmTile).With(Prefabs.ElectricGrill)));
        }

        // ---- water.sieve (gap-filler) --------------------------------------

        [Fact]
        public void WaterSieve_Irrelevant_OnOuthouseOnlyBase()
        {
            // Outhouses make polluted DIRT, not polluted water — nothing to sieve.
            var r = new WaterSieveRule();
            Assert.False(r.IsRelevant(Snap.Fresh().With(Prefabs.Outhouse)));
        }

        [Fact]
        public void WaterSieve_Fires_WithFlushToiletButNoSieve_ThenSatisfied()
        {
            var r = new WaterSieveRule();

            var flushNoSieve = Snap.Fresh().With(Prefabs.FlushToilet);
            Assert.True(r.IsRelevant(flushNoSieve));
            Assert.False(r.IsSatisfied(flushNoSieve));

            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.FlushToilet).With(Prefabs.WaterSieve)));
        }

        // ---- sanitation.lavatory (gap-filler) ------------------------------

        [Fact]
        public void Lavatory_Irrelevant_BeforeResearchOrWithFlushToilet()
        {
            var r = new LavatoryUpgradeRule();
            // Outhouse but no research station yet -> too early to nag the upgrade.
            Assert.False(r.IsRelevant(Snap.Fresh().With(Prefabs.Outhouse)));
            // Already has a flush toilet -> no longer outhouse-only.
            Assert.False(r.IsRelevant(Snap.Fresh().With(Prefabs.ResearchCenter)
                                                  .With(Prefabs.Outhouse).With(Prefabs.FlushToilet)));
        }

        [Fact]
        public void Lavatory_Fires_OnEstablishedOuthouseOnlyBase_ThenSatisfied()
        {
            var r = new LavatoryUpgradeRule();

            var outhouseOnly = Snap.Fresh().With(Prefabs.ResearchCenter).With(Prefabs.Outhouse);
            Assert.True(r.IsRelevant(outhouseOnly));
            Assert.False(r.IsSatisfied(outhouseOnly));

            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.Outhouse).With(Prefabs.FlushToilet)));
        }

        // ---- power.basic (brownout / no battery) ---------------------------

        [Fact]
        public void PowerBasic_Irrelevant_WhenPowerUnknown()
        {
            // Fail-soft: if the circuit probe didn't run, don't guess.
            var r = new PowerBrownoutRule();
            Assert.False(r.IsRelevant(Snap.Fresh())); // PowerKnown defaults false
        }

        [Fact]
        public void PowerBasic_Fires_WithNoBattery_OrDrainedBattery()
        {
            var r = new PowerBrownoutRule();

            var noBattery = Snap.Fresh().WithPower(generatedW: 400, consumedW: 600, hasBattery: false);
            Assert.True(r.IsRelevant(noBattery));
            Assert.False(r.IsSatisfied(noBattery));

            var drained = Snap.Fresh().WithPower(400, 600, batteryFraction: 0.05f);
            Assert.False(r.IsSatisfied(drained));

            var healthy = Snap.Fresh().WithPower(800, 600, batteryFraction: 0.9f);
            Assert.True(r.IsSatisfied(healthy)); // comfortable buffer -> solved
        }

        [Fact]
        public void PowerBasic_Urgency_RampsAsBatteryDrains()
        {
            var r = new PowerBrownoutRule();
            var def = Kb.Library.Get(r.Id);

            int low = r.Urgency(Snap.Fresh().WithPower(400, 600, batteryFraction: 0.02f), def);
            int near = r.Urgency(Snap.Fresh().WithPower(400, 600, batteryFraction: 0.18f), def);
            Assert.True(low > near, $"emptier battery should be more urgent ({low} vs {near})");
            Assert.True(near >= def.UrgencyBase);
        }

        // ---- morale.basics (stress) ----------------------------------------

        [Fact]
        public void Morale_Fires_OnHighStress_ClearsWhenLow()
        {
            var r = new MoraleBasicsRule();

            var stressed = Snap.Fresh().WithStress(55f);
            Assert.True(r.IsRelevant(stressed));
            Assert.False(r.IsSatisfied(stressed));

            Assert.True(r.IsSatisfied(Snap.Fresh().WithStress(12f)));
            Assert.False(r.IsRelevant(Snap.Fresh())); // StressKnown false -> quiet
        }

        // ---- heat.awareness ------------------------------------------------

        [Fact]
        public void Heat_Fires_WhenBaseWarm_ClearsWhenCool()
        {
            var r = new HeatAwarenessRule();

            var warm = Snap.Fresh().WithHeat(310f); // ~37 C
            Assert.True(r.IsRelevant(warm));
            Assert.False(r.IsSatisfied(warm));

            Assert.True(r.IsSatisfied(Snap.Fresh().WithHeat(298f))); // ~25 C -> fine
            Assert.False(r.IsRelevant(Snap.Fresh()));                // HeatKnown false -> quiet
        }

        [Fact]
        public void Heat_Urgency_RampsWithTemperature()
        {
            var r = new HeatAwarenessRule();
            var def = Kb.Library.Get(r.Id);
            int mild = r.Urgency(Snap.Fresh().WithHeat(305f), def);
            int hot = r.Urgency(Snap.Fresh().WithHeat(316f), def);
            Assert.True(hot > mild, $"hotter base should be more urgent ({hot} vs {mild})");
        }

        // ---- industry.steel / industry.plastic -----------------------------

        [Fact]
        public void Steel_Fires_WithRefineryAndNoSteel_ThenSatisfied()
        {
            var r = new SteelRule();

            var refineryNoSteel = Snap.Fresh().With(Prefabs.MetalRefinery);
            Assert.True(r.IsRelevant(refineryNoSteel));
            Assert.False(r.IsSatisfied(refineryNoSteel));

            // No refinery -> not relevant (won't nag a pre-industrial base).
            Assert.False(r.IsRelevant(Snap.Fresh()));

            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.MetalRefinery)
                                                  .WithResource(Elements.Steel, 500)));
        }

        [Fact]
        public void Plastic_Fires_WhenIndustrial_SatisfiedByPressOrStock()
        {
            var r = new PlasticRule();

            var needsPlastic = Snap.Fresh().With(Prefabs.MetalRefinery);
            Assert.True(r.IsRelevant(needsPlastic));
            Assert.False(r.IsSatisfied(needsPlastic));

            // Pre-industrial base (no refinery) -> not relevant.
            Assert.False(r.IsRelevant(Snap.Fresh()));

            // A Polymer Press OR a plastic stockpile (e.g. a Drecko rancher) solves it.
            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.MetalRefinery).With(Prefabs.PolymerPress)));
            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.MetalRefinery)
                                                  .WithResource(Elements.Plastic, 300)));
        }

        // ---- suits.atmo (guide-derived) ------------------------------------

        [Fact]
        public void AtmoSuits_Fires_WithRefineryAndNoSuits_ThenSatisfied()
        {
            var r = new AtmoSuitsRule();

            var refineryNoSuits = Snap.Fresh().With(Prefabs.MetalRefinery);
            Assert.True(r.IsRelevant(refineryNoSuits));
            Assert.False(r.IsSatisfied(refineryNoSuits));

            // Pre-industrial base -> not relevant.
            Assert.False(r.IsRelevant(Snap.Fresh()));

            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.MetalRefinery).With(Prefabs.AtmoSuitDock)));
            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.MetalRefinery).With(Prefabs.SuitFabricator)));
        }

        // ---- ranching.coal (guide-derived) ---------------------------------

        [Fact]
        public void Ranching_Fires_OnCoalWithoutRanch_ThenSatisfied()
        {
            var r = new RanchingRule();

            var coalNoRanch = Snap.Fresh().With(Prefabs.CoalGenerator);
            Assert.True(r.IsRelevant(coalNoRanch));
            Assert.False(r.IsSatisfied(coalNoRanch));

            // No coal burning -> nothing to make renewable.
            Assert.False(r.IsRelevant(Snap.Fresh()));

            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.CoalGenerator).With(Prefabs.GroomingStation)));
            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.CoalGenerator).With(Prefabs.CritterFeeder)));
        }

        // ---- power.petroleum (guide-derived) -------------------------------

        [Fact]
        public void Petroleum_Fires_OnIndustrialCoalOnly_ThenSatisfied()
        {
            var r = new PetroleumPowerRule();

            var coalOnly = Snap.Fresh().With(Prefabs.MetalRefinery).With(Prefabs.CoalGenerator);
            Assert.True(r.IsRelevant(coalOnly));
            Assert.False(r.IsSatisfied(coalOnly));

            // No refinery yet -> too early.
            Assert.False(r.IsRelevant(Snap.Fresh().With(Prefabs.CoalGenerator)));

            Assert.True(r.IsSatisfied(Snap.Fresh().With(Prefabs.MetalRefinery)
                                                  .With(Prefabs.CoalGenerator).With(Prefabs.PetroleumGenerator)));
        }

        // ---- cooling.aquatuner (guide-derived, heat-probe) -----------------

        [Fact]
        public void AquaTuner_Fires_WhenHotWithMaterials_ThenSatisfied()
        {
            var r = new AquaTunerRule();

            var hotWithSteel = Snap.Fresh().WithHeat(312f).WithResource(Elements.Steel, 400);
            Assert.True(r.IsRelevant(hotWithSteel));
            Assert.False(r.IsSatisfied(hotWithSteel));

            // Hot but no materials yet -> heat.awareness territory, not the AquaTuner build.
            Assert.False(r.IsRelevant(Snap.Fresh().WithHeat(312f)));

            // Hot, has materials, already has an AquaTuner -> solved.
            Assert.True(r.IsSatisfied(Snap.Fresh().WithHeat(312f)
                                          .WithResource(Elements.Steel, 400).With(Prefabs.AquaTuner)));

            // Cool base -> not relevant even with materials.
            Assert.False(r.IsRelevant(Snap.Fresh().WithHeat(298f).WithResource(Elements.Steel, 400)));
        }
    }
}
