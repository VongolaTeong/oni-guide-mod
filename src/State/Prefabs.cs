namespace NextStepGuide.State
{
    /// <summary>
    /// Verified building prefab ids (= KPrefabID.PrefabID().Name). These drift
    /// between game updates — all values below were confirmed present in
    /// Assembly-CSharp.dll for build U59-737790 on 2026-06-21.
    ///
    /// GOTCHA: the in-game "Oxygen Diffuser" prefab id is "MineralDeoxidizer",
    /// NOT "OxygenDiffuser" (which does not exist). Re-verify after major updates.
    /// </summary>
    public static class Prefabs
    {
        // Oxygen generation
        public const string OxygenDiffuser = "MineralDeoxidizer"; // the algae "Oxygen Diffuser"
        public const string Electrolyzer = "Electrolyzer";
        public const string RustDeoxidizer = "RustDeoxidizer";
        public const string AlgaeTerrarium = "AlgaeHabitat";

        // Sanitation
        public const string Outhouse = "Outhouse";
        public const string FlushToilet = "FlushToilet";

        // Food / farming
        public const string PlanterBox = "PlanterBox";
        public const string FarmTile = "FarmTile";
        public const string HydroponicFarm = "HydroponicFarm";

        // Cooking (raw crops -> proper meals)
        public const string MicrobeMusher = "MicrobeMusher";
        public const string ElectricGrill = "CookingStation";               // "Electric Grill"
        public const string GourmetCookingStation = "GourmetCookingStation"; // "Gas Range"

        // Water
        public const string WaterSieve = "WaterPurifier";                   // "Water Sieve"
        public const string WashBasin = "WashBasin";

        // Research
        public const string ResearchCenter = "ResearchCenter";                 // Research Station (tier 1)
        public const string AdvancedResearchCenter = "AdvancedResearchCenter"; // Super Computer (tier 2)
        public const string CosmicResearchCenter = "CosmicResearchCenter";     // space research

        // Power
        // GOTCHA: the Coal Generator's prefab id is "Generator" (not "CoalGenerator"),
        // and the Natural Gas Generator's is "MethaneGenerator". Verified U59-737790.
        public const string ManualGenerator = "ManualGenerator";       // hand-cranked
        public const string CoalGenerator = "Generator";
        public const string WoodGasGenerator = "WoodGasGenerator";     // Wood Burner
        public const string HydrogenGenerator = "HydrogenGenerator";
        public const string NaturalGasGenerator = "MethaneGenerator";  // "Natural Gas Generator"
        public const string PetroleumGenerator = "PetroleumGenerator";
        public const string SteamTurbine = "SteamTurbine";
        public const string SolarPanel = "SolarPanel";

        // Industry
        public const string MetalRefinery = "MetalRefinery";
        public const string PolymerPress = "Polymerizer";              // "Polymer Press"

        // Atmo suits
        public const string AtmoSuitDock = "SuitLocker";               // "Atmo Suit Dock"
        public const string SuitCheckpoint = "SuitMarker";             // "Exosuit Checkpoint"
        public const string SuitFabricator = "SuitFabricator";         // "Exosuit Forge"

        // Ranching
        public const string GroomingStation = "RanchStation";          // "Grooming Station"
        public const string CritterFeeder = "CreatureFeeder";          // "Critter Feeder"
        public const string EggIncubator = "EggIncubator";

        // Cooling
        // GOTCHA: the AquaTuner's prefab id is "LiquidConditioner" (and the gas
        // "Thermo Regulator" is "AirConditioner"). Verified U59-737790.
        public const string AquaTuner = "LiquidConditioner";

        // ---- Convenience groupings used by rules ----
        public static readonly string[] OxygenSources =
            { OxygenDiffuser, Electrolyzer, RustDeoxidizer, AlgaeTerrarium };

        public static readonly string[] Toilets =
            { Outhouse, FlushToilet };

        public static readonly string[] Farms =
            { PlanterBox, FarmTile, HydroponicFarm };

        public static readonly string[] CookingStations =
            { MicrobeMusher, ElectricGrill, GourmetCookingStation };

        /// <summary>
        /// Any power source that runs without a dupe hand-cranking it — i.e. the
        /// player has automated power. Used to decide both "move off Manual
        /// Generators" and "the base has real power, ready for a Metal Refinery".
        /// Deliberately excludes <see cref="ManualGenerator"/>.
        /// </summary>
        public static readonly string[] AutomatedGenerators =
            { CoalGenerator, WoodGasGenerator, HydrogenGenerator, NaturalGasGenerator,
              PetroleumGenerator, SteamTurbine, SolarPanel };

        /// <summary>Denser, longer-lasting fuel power than coal (mid/late game).</summary>
        public static readonly string[] DenserGenerators =
            { NaturalGasGenerator, PetroleumGenerator, HydrogenGenerator };

        /// <summary>Any part of an atmo-suit setup (dock / checkpoint / forge).</summary>
        public static readonly string[] AtmoSuitStations =
            { AtmoSuitDock, SuitCheckpoint, SuitFabricator };

        /// <summary>Buildings that indicate an active critter ranch.</summary>
        public static readonly string[] RanchBuildings =
            { GroomingStation, CritterFeeder, EggIncubator };
    }
}
