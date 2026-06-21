using System.Collections.Generic;
using NextStepGuide.Rules;
using Xunit;

namespace NextStepGuide.Tests
{
    /// <summary>
    /// Validates the real milestones.yaml parses cleanly and stays consistent
    /// with the code — these tests are the guardrail for editing the knowledge
    /// base, which is meant to be done without touching C#.
    /// </summary>
    public class MilestoneLibraryTests
    {
        [Fact]
        public void RealFile_ParsesManyMilestones()
        {
            Assert.True(Kb.Library.Count >= 25,
                $"expected the full knowledge base, got {Kb.Library.Count}");
        }

        [Fact]
        public void EveryMilestone_HasIdTitleWhy_AndSaneUrgency()
        {
            foreach (var m in Kb.Library.All)
            {
                Assert.False(string.IsNullOrWhiteSpace(m.Id), "milestone with empty id");
                Assert.False(string.IsNullOrWhiteSpace(m.Title), $"{m.Id}: empty title");
                Assert.False(string.IsNullOrWhiteSpace(m.Why), $"{m.Id}: empty why");
                Assert.InRange(m.UrgencyBase, 1, 100);
            }
        }

        [Fact]
        public void MilestoneIds_AreUnique()
        {
            var seen = new HashSet<string>();
            foreach (var m in Kb.Library.All)
                Assert.True(seen.Add(m.Id), $"duplicate milestone id: {m.Id}");
        }

        [Fact]
        public void SoftCycle_WhenPresent_HasMinMax()
        {
            foreach (var m in Kb.Library.All)
            {
                if (m.SoftCycle == null) continue;
                Assert.Equal(2, m.SoftCycle.Count);
                Assert.True(m.SoftCycleMin <= m.SoftCycleMax, $"{m.Id}: soft_cycle min>max");
            }
        }

        [Fact]
        public void CategoryAndTier_ParseToKnownEnums()
        {
            foreach (var m in Kb.Library.All)
            {
                Assert.True(m.CategoryEnum != RuleCategory.Unknown, $"{m.Id}: unknown category '{m.Category}'");
                Assert.True(m.TierEnum != RuleTier.Unknown, $"{m.Id}: unknown tier '{m.Tier}'");
            }
        }

        [Fact]
        public void EveryRegisteredRule_HasMatchingActiveMilestone()
        {
            foreach (var rule in RuleRegistry.CreateAll())
            {
                var def = Kb.Library.Get(rule.Id);
                Assert.True(def != null, $"rule '{rule.Id}' has no milestones.yaml entry");
                Assert.True(def.IsActive, $"rule '{rule.Id}' maps to a non-active milestone");
            }
        }

        [Fact]
        public void DependsOn_ReferencesExistingMilestones()
        {
            foreach (var m in Kb.Library.All)
            {
                if (m.DependsOn == null) continue;
                foreach (var dep in m.DependsOn)
                    Assert.True(Kb.Library.Has(dep), $"{m.Id}: depends_on unknown id '{dep}'");
            }
        }
    }
}
