using System;
using System.Collections.Generic;

namespace NextStepGuide.State
{
    /// <summary>
    /// A pure-data snapshot of everything a rule might need to reason about the
    /// colony. CONTAINS NO GAME / UNITY REFERENCES — this is the seam that lets
    /// the rules engine and its tests run without the game.
    ///
    /// Every group of fields carries a `...Known` flag. The StateReader sets it
    /// false when a probe fails (fail-soft); rules must treat unknown data as
    /// "can't tell" and stay quiet rather than firing on zeros.
    /// </summary>
    public sealed class ColonySnapshot
    {
        // ---- Time -----------------------------------------------------------
        public int Cycle;
        public bool CycleKnown;

        // ---- Duplicants -----------------------------------------------------
        public int LiveDuplicants;
        public bool DuplicantsKnown;

        // ---- Environment ----------------------------------------------------
        /// <summary>True when the Spaced Out! expansion is active.</summary>
        public bool IsDlcActive;

        // ---- Buildings (prefab id -> count) ---------------------------------
        // Keyed by KPrefabID.PrefabID().Name, e.g. "MineralDeoxidizer".
        public Dictionary<string, int> BuildingCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        public bool BuildingsKnown;

        // ---- Resources (element tag name -> accessible kg) ------------------
        // Keyed by element tag name, e.g. "Algae", "Water". Accessible amount
        // on the active world (unreachable stock excluded).
        public Dictionary<string, float> ResourceKg = new Dictionary<string, float>(StringComparer.Ordinal);
        public bool ResourcesKnown;

        // ---- Power (reserved for later phases; default unknown) -------------
        public float PowerGeneratedW;
        public float PowerConsumedW;
        public float BatteryChargeFraction; // 0..1
        public bool PowerKnown;

        // =====================================================================
        // Convenience accessors used by rules (null-safe, allocation-free).
        // =====================================================================

        /// <summary>Total count across any of the given building prefab ids.</summary>
        public int Buildings(params string[] prefabIds)
        {
            if (BuildingCounts == null || prefabIds == null) return 0;
            int total = 0;
            foreach (var id in prefabIds)
            {
                if (id != null && BuildingCounts.TryGetValue(id, out int c)) total += c;
            }
            return total;
        }

        /// <summary>True if at least one of the given building prefab ids exists.</summary>
        public bool HasAnyBuilding(params string[] prefabIds) => Buildings(prefabIds) > 0;

        /// <summary>Accessible mass (kg) of an element by tag name, or 0 if unknown.</summary>
        public float Resource(string tagName)
        {
            if (ResourceKg != null && tagName != null && ResourceKg.TryGetValue(tagName, out float kg))
                return kg;
            return 0f;
        }
    }
}
