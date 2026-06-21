using System;
using System.IO;
using NextStepGuide.Rules;
using NextStepGuide.State;

namespace NextStepGuide.Tests
{
    /// <summary>Fluent helpers for building ColonySnapshots in tests.</summary>
    internal static class Snap
    {
        /// <summary>A known, stable base with N dupes and no buildings/resources yet.</summary>
        public static ColonySnapshot Fresh(int dupes = 3, int cycle = 5)
            => new ColonySnapshot
            {
                Cycle = cycle, CycleKnown = true,
                LiveDuplicants = dupes, DuplicantsKnown = true,
                BuildingsKnown = true,
                ResourcesKnown = true,
                IsDlcActive = true,
            };

        public static ColonySnapshot With(this ColonySnapshot s, string prefabId, int count = 1)
        {
            s.BuildingCounts[prefabId] = count;
            return s;
        }

        public static ColonySnapshot WithResource(this ColonySnapshot s, string tag, float kg)
        {
            s.ResourceKg[tag] = kg;
            return s;
        }
    }

    /// <summary>Loads the real milestones.yaml once for the whole test run.</summary>
    internal static class Kb
    {
        public static readonly MilestoneLibrary Library = Load();

        private static MilestoneLibrary Load()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "milestones.yaml");
            return MilestoneLibrary.Load(File.ReadAllText(path));
        }

        public static RuleEngine Engine() => new RuleEngine(RuleRegistry.CreateAll(), Library);
    }
}
