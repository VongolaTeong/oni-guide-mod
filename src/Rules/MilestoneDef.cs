using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace NextStepGuide.Rules
{
    /// <summary>
    /// One milestone loaded from milestones.yaml — the CONTENT/TUNING layer.
    ///
    /// Every property carries an explicit [YamlMember(Alias=...)] mapping to the
    /// YAML key. We deliberately avoid a naming convention so the SAME source
    /// compiles against both the game-bundled YamlDotNet (production) and the
    /// NuGet YamlDotNet (tests), whose naming-convention helpers differ.
    ///
    /// Detection (IsRelevant/IsSatisfied/urgency scaling) lives in a matching
    /// C# IRule keyed by <see cref="Id"/>; this object supplies the words,
    /// urgency baseline, gating (status/dlc/depends_on) and ordering priors.
    /// </summary>
    public sealed class MilestoneDef
    {
        [YamlMember(Alias = "id")] public string Id { get; set; }
        [YamlMember(Alias = "category")] public string Category { get; set; }
        [YamlMember(Alias = "tier")] public string Tier { get; set; }
        [YamlMember(Alias = "title")] public string Title { get; set; }
        [YamlMember(Alias = "why")] public string Why { get; set; }

        /// <summary>[min, max] cycle band — a soft prior for ordering only.</summary>
        [YamlMember(Alias = "soft_cycle")] public List<int> SoftCycle { get; set; }

        [YamlMember(Alias = "urgency_base")] public int UrgencyBase { get; set; }

        [YamlMember(Alias = "trigger")] public TriggerDef Trigger { get; set; }
        [YamlMember(Alias = "satisfied_when")] public string SatisfiedWhen { get; set; }
        [YamlMember(Alias = "depends_on")] public List<string> DependsOn { get; set; }

        /// <summary>any | base | spacedout</summary>
        [YamlMember(Alias = "dlc")] public string Dlc { get; set; } = "any";

        /// <summary>active | draft (draft rules are skipped by the engine).</summary>
        [YamlMember(Alias = "status")] public string Status { get; set; } = "active";

        [YamlMember(Alias = "source")] public string Source { get; set; }
        [YamlMember(Alias = "notes")] public string Notes { get; set; }

        // ---- Derived helpers (not from YAML) ----
        [YamlIgnore] public int SoftCycleMin => (SoftCycle != null && SoftCycle.Count > 0) ? SoftCycle[0] : 0;
        [YamlIgnore] public int SoftCycleMax => (SoftCycle != null && SoftCycle.Count > 1) ? SoftCycle[1] : int.MaxValue;

        [YamlIgnore]
        public bool IsActive =>
            string.Equals(Status, "active", System.StringComparison.OrdinalIgnoreCase);

        [YamlIgnore] public RuleCategory CategoryEnum => RuleEnumParse.Category(Category);
        [YamlIgnore] public RuleTier TierEnum => RuleEnumParse.Tier(Tier);
    }

    public sealed class TriggerDef
    {
        [YamlMember(Alias = "when")] public string When { get; set; }
        [YamlMember(Alias = "state_hints")] public List<string> StateHints { get; set; }
    }
}
