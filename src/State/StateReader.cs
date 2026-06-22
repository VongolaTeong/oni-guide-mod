using System.Collections;
using System.Reflection;
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

            // ---- Power (reflection over CircuitManager's private circuit list) ----
            try { ReadPower(s); }
            catch { s.PowerKnown = false; }

            // ---- Morale (average duplicant stress) ----
            try { ReadStress(s); }
            catch { s.StressKnown = false; }

            // ---- Heat (temperature where dupes are) ----
            try { ReadHeat(s); }
            catch { s.HeatKnown = false; }

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

        // CircuitManager has no public circuit enumerator, so we reflect its
        // private `circuitInfo` list just to learn the circuit count, then query
        // each circuit id through the public getters. Cached so we resolve the
        // FieldInfo once.
        private static FieldInfo _circuitInfoField;

        private static void ReadPower(ColonySnapshot s)
        {
            var cm = Game.Instance != null ? Game.Instance.circuitManager : null;
            if (cm == null) return;

            if (_circuitInfoField == null)
                _circuitInfoField = typeof(CircuitManager).GetField(
                    "circuitInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            var list = _circuitInfoField != null ? _circuitInfoField.GetValue(cm) as ICollection : null;
            int count = list != null ? list.Count : 0;

            float gen = 0f, used = 0f, minBattery = 1f;
            bool hasBattery = false;
            for (ushort id = 0; id < count; id++)
            {
                gen += cm.GetWattsGeneratedByCircuit(id);
                used += cm.GetWattsUsedByCircuit(id);
                if (cm.HasBatteries(id))
                {
                    hasBattery = true;
                    float frac = cm.GetMinBatteryPercentFullOnCircuit(id) / 100f;
                    if (frac < minBattery) minBattery = frac;
                }
            }

            s.PowerGeneratedW = gen;
            s.PowerConsumedW = used;
            s.PowerHasBattery = hasBattery;
            s.BatteryChargeFraction = hasBattery ? minBattery : 1f;
            s.PowerKnown = true;
        }

        private static void ReadStress(ColonySnapshot s)
        {
            var stress = Db.Get().Amounts.Stress;
            var minions = Components.LiveMinionIdentities;
            int n = minions.Count;
            if (n <= 0) return; // no dupes -> no stress advice (leave Known false)

            float total = 0f;
            int counted = 0;
            for (int i = 0; i < n; i++)
            {
                var m = minions[i];
                if (m == null) continue;
                var inst = stress.Lookup(m);
                if (inst == null) continue;
                total += inst.value;
                counted++;
            }
            if (counted == 0) return;

            s.AvgStress = total / counted;
            s.StressKnown = true;
        }

        private static void ReadHeat(ColonySnapshot s)
        {
            var minions = Components.LiveMinionIdentities;
            int n = minions.Count;
            if (n <= 0) return;

            // Sample the temperature at each dupe's cell — that's where heat
            // actually affects the colony, and it avoids being skewed by the
            // intentionally-hot machines elsewhere on the map.
            float total = 0f, max = 0f;
            int counted = 0;
            for (int i = 0; i < n; i++)
            {
                var m = minions[i];
                if (m == null) continue;
                int cell = Grid.PosToCell(m.gameObject);
                if (!Grid.IsValidCell(cell)) continue;
                float t = Grid.Temperature[cell];
                if (t <= 1f) continue; // skip obviously-bad reads (vacuum/uninit)
                total += t;
                if (t > max) max = t;
                counted++;
            }
            if (counted == 0) return;

            s.AvgBaseTempK = total / counted;
            s.MaxBaseTempK = max;
            s.HeatKnown = true;
        }
    }
}
