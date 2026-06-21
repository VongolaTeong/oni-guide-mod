using UnityEngine;

namespace NextStepGuide.State
{
    /// <summary>
    /// The ONLY layer allowed to touch live game singletons. Builds a pure
    /// ColonySnapshot. EVERY probe is wrapped in try/catch and sets a `...Known`
    /// flag on failure rather than throwing — one broken probe degrades one rule,
    /// never the HUD.
    /// </summary>
    public static class StateReader
    {
        public static ColonySnapshot BuildSnapshot()
        {
            var s = new ColonySnapshot();

            // ---- Cycle ----
            try
            {
                s.Cycle = GameClock.Instance.GetCycle();
                s.CycleKnown = true;
            }
            catch { s.CycleKnown = false; }

            // ---- Duplicants ----
            try
            {
                s.LiveDuplicants = Components.LiveMinionIdentities.Count;
                s.DuplicantsKnown = true;
            }
            catch { s.DuplicantsKnown = false; }

            // ---- DLC ----
            try { s.IsDlcActive = DlcManager.IsExpansion1Active(); }
            catch { s.IsDlcActive = false; }

            // ---- Buildings (the expensive probe) ----
            try
            {
                ReadBuildingCounts(s);
                s.BuildingsKnown = true;
            }
            catch { s.BuildingsKnown = false; }

            // ---- Resources ----
            try
            {
                ReadResources(s);
                s.ResourcesKnown = true;
            }
            catch { s.ResourcesKnown = false; }

            return s;
        }

        private static void ReadBuildingCounts(ColonySnapshot s)
        {
            var dict = s.BuildingCounts;
            dict.Clear();

            var completes = Components.BuildingCompletes;
            int n = completes.Count;
            for (int i = 0; i < n; i++)
            {
                var bc = completes[i];
                if (bc == null) continue;

                var kpid = bc.GetComponent<KPrefabID>();
                if (kpid == null) continue;

                string id = kpid.PrefabID().Name;
                if (string.IsNullOrEmpty(id)) continue;

                dict.TryGetValue(id, out int c);
                dict[id] = c + 1;
            }
        }

        private static void ReadResources(ColonySnapshot s)
        {
            var world = ClusterManager.Instance != null ? ClusterManager.Instance.activeWorld : null;
            var inventory = world != null ? world.worldInventory : null;
            if (inventory == null) return;

            foreach (var name in Elements.Tracked)
            {
                try
                {
                    // Element primary tags equal new Tag(elementName); accessible
                    // amount only (unreachable stock isn't actionable advice).
                    s.ResourceKg[name] = inventory.GetAmount(new Tag(name), false);
                }
                catch { /* skip this element, keep the rest */ }
            }
        }
    }
}
