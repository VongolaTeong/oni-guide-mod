namespace NextStepGuide.State
{
    /// <summary>
    /// Element tag names used as keys in <see cref="ColonySnapshot.ResourceKg"/>.
    /// These equal SimHashes.&lt;X&gt;.CreateTag().Name; the StateReader and the
    /// rules must agree on them, so they live here in one place.
    /// </summary>
    public static class Elements
    {
        public const string Algae = "Algae";
        public const string Water = "Water";
        public const string DirtyWater = "DirtyWater"; // "Polluted Water"
        public const string Dirt = "Dirt";
        public const string Oxygen = "Oxygen";

        /// <summary>Tags the StateReader probes each snapshot (kept small for perf).</summary>
        public static readonly string[] Tracked = { Algae, Water, DirtyWater, Dirt };
    }
}
