using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace NextStepGuide.Rules
{
    /// <summary>
    /// Parses milestones.yaml into MilestoneDefs and indexes them by id.
    /// Pure: takes YAML text in, gives data out — no game references, so tests
    /// load the same file off disk while the mod loads it from an embedded
    /// resource (see GuideRuntime).
    /// </summary>
    public sealed class MilestoneLibrary
    {
        private readonly Dictionary<string, MilestoneDef> _byId =
            new Dictionary<string, MilestoneDef>(StringComparer.Ordinal);

        public IReadOnlyList<MilestoneDef> All { get; }

        private MilestoneLibrary(List<MilestoneDef> defs)
        {
            All = defs;
            foreach (var d in defs)
            {
                if (d?.Id != null) _byId[d.Id] = d;
            }
        }

        public MilestoneDef Get(string id)
            => (id != null && _byId.TryGetValue(id, out var d)) ? d : null;

        public bool Has(string id) => id != null && _byId.ContainsKey(id);

        public int Count => _byId.Count;

        /// <summary>Parse YAML text. Throws on malformed YAML; callers fail-soft.</summary>
        public static MilestoneLibrary Load(string yamlText)
        {
            if (string.IsNullOrWhiteSpace(yamlText))
                return new MilestoneLibrary(new List<MilestoneDef>());

            // No naming convention: property->key mapping is via explicit
            // [YamlMember(Alias=...)] on MilestoneDef, so this compiles the same
            // against the game-bundled and NuGet YamlDotNet versions.
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties() // tolerate meta:, source:, notes: etc.
                .Build();

            var file = deserializer.Deserialize<MilestonesFile>(yamlText);
            var list = file?.Milestones ?? new List<MilestoneDef>();
            return new MilestoneLibrary(list);
        }

        /// <summary>Root document shape: we only care about the milestones list.</summary>
        private sealed class MilestonesFile
        {
            [YamlMember(Alias = "milestones")]
            public List<MilestoneDef> Milestones { get; set; }
        }
    }
}
