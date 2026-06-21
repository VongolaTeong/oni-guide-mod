using System;
using System.Collections.Generic;
using System.Linq;
using NextStepGuide.State;

namespace NextStepGuide.Rules
{
    /// <summary>
    /// Joins the DETECTION layer (IRules) to the CONTENT layer (MilestoneDefs),
    /// evaluates everything against a snapshot, and returns a small, ordered,
    /// de-duplicated list of recommendations.
    ///
    /// Pure: depends only on ColonySnapshot + the loaded library, so it is fully
    /// unit-testable without the game.
    /// </summary>
    public sealed class RuleEngine
    {
        private readonly MilestoneLibrary _library;
        private readonly Dictionary<string, IRule> _rulesById;

        /// <summary>Collapse to at most one recommendation per category.</summary>
        public bool CollapseByCategory { get; set; } = true;

        public RuleEngine(IEnumerable<IRule> rules, MilestoneLibrary library)
        {
            _library = library;
            _rulesById = new Dictionary<string, IRule>(StringComparer.Ordinal);
            if (rules != null)
            {
                foreach (var r in rules)
                    if (r?.Id != null) _rulesById[r.Id] = r;
            }
        }

        public IReadOnlyDictionary<string, IRule> Rules => _rulesById;

        public List<Recommendation> Evaluate(
            ColonySnapshot snapshot,
            ISet<string> dismissed = null,
            int maxResults = 4)
        {
            var fired = new List<Recommendation>();
            if (snapshot == null || _library == null) return fired;

            foreach (var rule in _rulesById.Values)
            {
                var def = _library.Get(rule.Id);
                if (def == null || !def.IsActive) continue;                  // unknown/draft
                if (dismissed != null && dismissed.Contains(rule.Id)) continue;
                if (!DlcAllows(def, snapshot)) continue;
                if (!DependenciesMet(def, snapshot)) continue;

                if (!Safe(() => rule.IsRelevant(snapshot))) continue;
                if (Safe(() => rule.IsSatisfied(snapshot))) continue;        // solved → hide

                int urgency = Clamp(SafeInt(() => rule.Urgency(snapshot, def), def.UrgencyBase));

                fired.Add(new Recommendation
                {
                    Id = def.Id,
                    Category = def.CategoryEnum,
                    Tier = def.TierEnum,
                    Title = def.Title,
                    Why = def.Why,
                    Urgency = urgency,
                    SoftCycleMin = def.SoftCycleMin,
                });
            }

            // Order: urgency desc, then survival-first tier, then earlier cycle band.
            fired.Sort((a, b) =>
            {
                int c = b.Urgency.CompareTo(a.Urgency);
                if (c != 0) return c;
                c = RuleEnumParse.TierRank(a.Tier).CompareTo(RuleEnumParse.TierRank(b.Tier));
                if (c != 0) return c;
                c = a.SoftCycleMin.CompareTo(b.SoftCycleMin);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Id, b.Id); // stable
            });

            var result = CollapseByCategory ? CollapsePerCategory(fired) : fired;
            if (maxResults > 0 && result.Count > maxResults)
                result = result.GetRange(0, maxResults);
            return result;
        }

        // ---- gating helpers -------------------------------------------------

        private static bool DlcAllows(MilestoneDef def, ColonySnapshot s)
        {
            switch ((def.Dlc ?? "any").ToLowerInvariant())
            {
                case "spacedout": return s.IsDlcActive;
                case "base": return !s.IsDlcActive;
                default: return true; // "any"
            }
        }

        /// <summary>
        /// A prerequisite blocks a rule only while it is an active, unsolved
        /// problem. If we have no detector for it, or it's already satisfied, or
        /// it doesn't currently apply, it's treated as met (non-blocking).
        /// </summary>
        private bool DependenciesMet(MilestoneDef def, ColonySnapshot s)
        {
            if (def.DependsOn == null) return true;
            foreach (var depId in def.DependsOn)
            {
                if (!_rulesById.TryGetValue(depId, out var dep)) continue; // no detector → ignore
                bool relevant = Safe(() => dep.IsRelevant(s));
                if (!relevant) continue;
                bool satisfied = Safe(() => dep.IsSatisfied(s));
                if (!satisfied) return false; // prerequisite still an open problem
            }
            return true;
        }

        private static List<Recommendation> CollapsePerCategory(List<Recommendation> ordered)
        {
            var seen = new HashSet<RuleCategory>();
            var outList = new List<Recommendation>();
            foreach (var r in ordered) // already sorted, so first per category is most urgent
            {
                if (seen.Add(r.Category)) outList.Add(r);
            }
            return outList;
        }

        private static int Clamp(int v) => v < 1 ? 1 : (v > 100 ? 100 : v);

        // Fail-soft wrappers: a throwing rule degrades to "not firing", never crashes.
        private static bool Safe(Func<bool> f)
        {
            try { return f(); } catch { return false; }
        }

        private static int SafeInt(Func<int> f, int fallback)
        {
            try { return f(); } catch { return fallback; }
        }
    }
}
