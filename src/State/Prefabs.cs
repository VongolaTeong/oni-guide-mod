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

        // Research
        public const string ResearchCenter = "ResearchCenter";                 // Research Station (tier 1)
        public const string AdvancedResearchCenter = "AdvancedResearchCenter"; // Super Computer (tier 2)
        public const string CosmicResearchCenter = "CosmicResearchCenter";     // space research

        // Power (reserved for later rules)
        public const string ManualGenerator = "ManualGenerator";
        public const string CoalGenerator = "Generator";

        // ---- Convenience groupings used by rules ----
        public static readonly string[] OxygenSources =
            { OxygenDiffuser, Electrolyzer, RustDeoxidizer, AlgaeTerrarium };

        public static readonly string[] Toilets =
            { Outhouse, FlushToilet };

        public static readonly string[] Farms =
            { PlanterBox, FarmTile, HydroponicFarm };
    }
}
