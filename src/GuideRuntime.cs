using System;
using System.Collections.Generic;
using System.IO;
using NextStepGuide.Config;
using NextStepGuide.Rules;
using NextStepGuide.State;
using PeterHan.PLib.Options;
using UnityEngine;

namespace NextStepGuide
{
    /// <summary>
    /// Process-wide glue between the game-touching StateReader and the pure
    /// RuleEngine. Owns the loaded library + engine, the user settings, and the
    /// latest evaluation result. The UI renders <see cref="Latest"/>; it never
    /// reads game state itself.
    /// </summary>
    public static class GuideRuntime
    {
        public static MilestoneLibrary Library { get; private set; }
        public static RuleEngine Engine { get; private set; }
        public static ColonySnapshot LastSnapshot { get; private set; }
        public static IReadOnlyList<Recommendation> Latest { get; private set; } = new List<Recommendation>();

        /// <summary>Live user settings (persisted via PLib).</summary>
        public static GuideSettings Settings { get; private set; } = new GuideSettings();

        /// <summary>Bumped on every successful Recompute so the UI can skip redraws.</summary>
        public static int Version { get; private set; }

        private static bool _initialised;

        /// <summary>Load settings + the knowledge base + wire the engine. Idempotent.</summary>
        public static void EnsureInitialised()
        {
            if (_initialised) return;
            _initialised = true;

            try
            {
                Settings = POptions.ReadSettings<GuideSettings>() ?? new GuideSettings();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{ModEntry.Prefix} failed to read settings, using defaults: {e}");
                Settings = new GuideSettings();
            }

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
                Latest = Engine.Evaluate(snap, Settings.DismissedSet(),
                                         Settings.MutedCategorySet(), Settings.MaxTips);
                Version++;

                if (logToConsole) LogResult(snap, Latest);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{ModEntry.Prefix} Recompute failed: {e}");
            }
        }

        /// <summary>Hide a tip permanently (until reset). Persists + re-evaluates now.</summary>
        public static void Dismiss(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (Settings.DismissedIds == null) Settings.DismissedIds = new List<string>();
            if (!Settings.DismissedIds.Contains(id))
            {
                Settings.DismissedIds.Add(id);
                PersistSettings();
                Recompute(logToConsole: false);
            }
        }

        /// <summary>Bring back all dismissed tips. Persists + re-evaluates now.</summary>
        public static void ResetDismissed()
        {
            if (Settings.DismissedIds != null && Settings.DismissedIds.Count > 0)
            {
                Settings.DismissedIds.Clear();
                PersistSettings();
                Recompute(logToConsole: false);
            }
        }

        /// <summary>Adopt edited option values from the Options dialog (keeps dismissed state).</summary>
        public static void ApplyOptionChanges(GuideSettings edited)
        {
            if (edited == null) return;
            Settings.ShowWhy = edited.ShowWhy;
            Settings.MaxTips = edited.MaxTips;
            Settings.RefreshIntervalSeconds = edited.RefreshIntervalSeconds;
            Settings.MuteOxygen = edited.MuteOxygen;
            Settings.MutePower = edited.MutePower;
            Settings.MuteFood = edited.MuteFood;
            Settings.MuteWater = edited.MuteWater;
            Settings.MuteSanitation = edited.MuteSanitation;
            Settings.MuteHeat = edited.MuteHeat;
            Settings.MuteMorale = edited.MuteMorale;
            Settings.MuteResearch = edited.MuteResearch;
            Settings.MuteIndustry = edited.MuteIndustry;
            Settings.MuteExploration = edited.MuteExploration;
            Settings.MuteSpace = edited.MuteSpace;
            Settings.MuteDupes = edited.MuteDupes;
            Recompute(logToConsole: false);
        }

        private static void PersistSettings()
        {
            try { POptions.WriteSettings(Settings); }
            catch (Exception e) { Debug.LogWarning($"{ModEntry.Prefix} failed to save settings: {e}"); }
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
