using NextStepGuide.State;

namespace NextStepGuide.Rules
{
    /// <summary>
    /// The DETECTION half of a piece of advice. The CONTENT half (title, why,
    /// urgency baseline, gating) lives in the matching MilestoneDef, paired by
    /// <see cref="Id"/>. Rules are pure: they read only a ColonySnapshot.
    /// </summary>
    public interface IRule
    {
        /// <summary>Stable id; must match a milestones.yaml entry's id.</summary>
        string Id { get; }

        /// <summary>Cheap gate: should this advice even be considered right now?</summary>
        bool IsRelevant(ColonySnapshot s);

        /// <summary>Is the problem already solved? (don't show solved problems)</summary>
        bool IsSatisfied(ColonySnapshot s);

        /// <summary>
        /// 0..100 urgency. Default = the milestone's baseline; override to scale
        /// with severity (e.g. ramp up as a resource approaches depletion).
        /// </summary>
        int Urgency(ColonySnapshot s, MilestoneDef def);
    }

    /// <summary>Convenience base: urgency defaults to the milestone baseline.</summary>
    public abstract class RuleBase : IRule
    {
        public abstract string Id { get; }
        public abstract bool IsRelevant(ColonySnapshot s);
        public abstract bool IsSatisfied(ColonySnapshot s);

        public virtual int Urgency(ColonySnapshot s, MilestoneDef def)
            => def?.UrgencyBase ?? 0;

        /// <summary>Linearly map t in [0,1] onto [lo,hi] and clamp.</summary>
        protected static int Scale(float t, int lo, int hi)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            int v = lo + (int)System.Math.Round((hi - lo) * t);
            return v < 1 ? 1 : (v > 100 ? 100 : v);
        }
    }
}
