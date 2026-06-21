namespace NextStepGuide.Rules
{
    /// <summary>
    /// What the panel shows for one piece of advice. Built by the engine from a
    /// MilestoneDef (content) plus the rule's computed urgency (detection).
    /// </summary>
    public sealed class Recommendation
    {
        public string Id;
        public RuleCategory Category;
        public RuleTier Tier;
        public string Title;
        public string Why;
        public int Urgency;       // 1..100
        public string Wiki;       // optional building/wiki name hint (text only)

        /// <summary>Soft cycle band min, used only for stable tie-breaking.</summary>
        public int SoftCycleMin;

        public UrgencyBand Band => RuleEnumParse.Band(Urgency);

        public override string ToString()
            => $"[{Urgency:000} {Band}] {Category}/{Tier} {Id} :: {Title} — {Why}";
    }
}
