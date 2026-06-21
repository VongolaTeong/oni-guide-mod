using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NextStepGuide.Rules;
using NextStepGuide.State;
using UnityEngine;

namespace NextStepGuide
{
    /// <summary>
    /// Process-wide glue between the game-touching StateReader and the pure
    /// RuleEngine. Owns the loaded library + engine and the latest evaluation
    /// result. The UI (later phases) renders <see cref="Latest"/>; it never
    /// reads game state itself.
    /// </summary>
    public static class GuideRuntime
    {
        public static MilestoneLibrary Library { get; private set; }
        public static RuleEngine Engine { get; private set; }
        public static ColonySnapshot LastSnapshot { get; private set; }
        public static IReadOnlyList<Recommendation> Latest { get; private set; } = new List<Recommendation>();

        /// <summary>Bumped on every successful Recompute so the UI can skip redraws.</summary>
        public static int Version { get; private set; }

        // Phase 4 will persist these via PLib options; defaults for now.
        public static readonly HashSet<string> Dismissed = new HashSet<string>(StringComparer.Ordinal);
        public static int MaxResults = 4;

        private static bool _initialised;

        /// <summary>Load the knowledge base + wire the engine. Safe to call repeatedly.</summary>
        public static void EnsureInitialised()
        {
            if (_initialised) return;
            _initialised = true;

            try
            {
                string yaml = LoadEmbeddedMilestones();
                Library = MilestoneLibrary.Load(yaml);
                Engine = new RuleEngine(RuleRegistry.CreateAll(), Library);
                Debug.Log($"{ModEntry.Prefix} knowledge base ready: " +
                          $"{Library.Count} milestones, {Engine.Rules.Count} detection rules.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{ModEntry.Prefix} failed to initialise rules engine: {e}");
            }
        }

        /// <summary>Read live state, evaluate, store the result. Fail-soft.</summary>
        public static void Recompute(bool logToConsole)
        {
            if (Engine == null) return;
            try
            {
                var snap = StateReader.BuildSnapshot();
                LastSnapshot = snap;
                Latest = Engine.Evaluate(snap, Dismissed, MaxResults);
                Version++;

                if (logToConsole) LogResult(snap, Latest);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{ModEntry.Prefix} Recompute failed: {e}");
            }
        }

        private static void LogResult(ColonySnapshot s, IReadOnlyList<Recommendation> recs)
        {
            int buildings = s.BuildingsKnown ? s.BuildingCounts.Count : -1;
            Debug.Log($"{ModEntry.Prefix} cycle={(s.CycleKnown ? s.Cycle.ToString() : "?")} " +
                      $"dupes={(s.DuplicantsKnown ? s.LiveDuplicants.ToString() : "?")} " +
                      $"dlc={s.IsDlcActive} buildingTypes={buildings} " +
                      $"algae={s.Resource(Elements.Algae):0}kg -> {recs.Count} recommendation(s):");
            for (int i = 0; i < recs.Count; i++)
                Debug.Log($"{ModEntry.Prefix}   {i + 1}. {recs[i]}");
        }

        private static string LoadEmbeddedMilestones()
        {
            var asm = typeof(GuideRuntime).Assembly;

            // Primary logical name (set in the csproj EmbeddedResource).
            const string primary = "NextStepGuide.milestones.yaml";
            string name = primary;

            var names = asm.GetManifestResourceNames();
            if (Array.IndexOf(names, primary) < 0)
            {
                // Fall back to any resource ending in milestones.yaml.
                name = null;
                foreach (var n in names)
                {
                    if (n.EndsWith("milestones.yaml", StringComparison.OrdinalIgnoreCase))
                    {
                        name = n;
                        break;
                    }
                }
                if (name == null)
                    throw new FileNotFoundException("Embedded milestones.yaml not found in assembly.");
            }

            using (Stream stream = asm.GetManifestResourceStream(name))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}
