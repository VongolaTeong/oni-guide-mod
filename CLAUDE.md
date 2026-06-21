# CLAUDE.md — Oxygen Not Included "Next Step" Guidance Mod

This file is the build spec and working context for implementing an ONI mod that tells the player **what to prioritise next**, based on the
live state of their colony.

---

## 0. The problem this mod solves

ONI's early game is self-directing: you need oxygen, a toilet/wash loop, food, and power,
and the game more or less screams when one is missing. After that, the game goes quiet.
Players who have a stable base but no clear objective stall out — they don't know whether
the next priority is cooling, water security, atmo suits, ranching, refined metal, or
research, and the game offers no opinion.

This mod adds a **prioritised, state-aware checklist panel**: a small list of "you should
probably do X next, because Y" recommendations that update as the colony changes. It is
advisory, not automated — it never builds anything, it just points.

Design north star: **a good ONI player looking over your shoulder.** Every recommendation
must be (1) detected from real game state, (2) explained with a one-line *why*, and
(3) dismissable. Wrong or nagging advice is worse than no advice, so bias toward high
precision over coverage.

---

## 1. Scope and assumptions

- **Target game:** current retail ONI, **Spaced Out! DLC assumed active** but the mod must
  not crash on the base game. DLC-specific rules are gated behind a DLC check (see §6).
- **MVP is a single side panel** with a prioritised list. No new buildings, no automation,
  no save-data changes. This keeps the mod save-safe (can be added/removed without
  corrupting colonies).
- **Out of scope (for now):** pathfinding hints, "build it here" overlays, tutorialising the
  early game (the game already does that), multiplayer.
- The rules engine is **data-driven and additive** so new advice can be added without
  touching the UI or state layer.

---

## 2. Tech stack & hard constraints

| Concern | Decision |
|---|---|
| Language | C#, **.NET Framework 4.8** (VERIFIED — Unity 6 / U59 moved the game off 4.7.1; see §2a) |
| Patching | **HarmonyLib 2.x** (game ships **0Harmony.dll 2.4.2.0**; we reference it directly from Managed, never copy it) |
| Mod base class | `KMod.UserMod2` with a `static OnLoad()` entry point |
| UI / options | **PLib 4.24.0** (`PeterHan.PLib`), merged in via **ILRepack** (targets net48/netstandard2.1) |
| Game assemblies | Reference DLLs from the install's `OxygenNotIncluded_Data/Managed` folder |
| Unity | **6000.3.5f2** (VERIFIED from `Player.log`; the spec's original 2022.3.x guess was wrong) |
| IDE | Visual Studio 2022 or Rider; agent works headless via `dotnet build`/`dotnet test` |

### 2a. VERIFIED environment & toolchain facts (build U59-737790, 2026-06-21)

These were confirmed against the live install and supersede earlier guesses in this file:

- **Target framework is `net48`**, not net471. PLib 4.24.0 ships only `net48`/`netstandard2.1`,
  which is the tell that the game's scripting runtime moved to .NET Framework 4.8. Build with
  the `Microsoft.NETFramework.ReferenceAssemblies.net48` NuGet package — no admin targeting-pack
  install needed for headless `dotnet build`.
- **The game already bundles YamlDotNet** inside `Assembly-CSharp-firstpass.dll`. Do **NOT** add
  the `YamlDotNet` NuGet package to the mod project — it causes CS0433 type-collision errors.
  Compile/run against the bundled copy; map YAML keys with explicit `[YamlMember(Alias=...)]`
  (no naming convention) so the same source also compiles against NuGet YamlDotNet in the tests.
- **ILRepack** is driven by a project-level `ILRepack.targets` (its presence suppresses the
  `ILRepack.Lib.MSBuild.Task` default auto-merge). We merge **only PLib** (internalized) and set
  `LibraryPath` to include the game's Managed folder so PLib's Newtonsoft/Unity refs resolve
  there without being merged.
- Build/test/deploy: `dotnet build -c Release` (auto-deploys to the Klei Dev mods folder via the
  `DeployToDevMods` target; pass `-p:SkipDeploy=true` to skip). `cd test && dotnet test` runs the
  pure rule/engine/loader suite with no game dependency.

### Critical constraint — verify APIs against the real assemblies
Internal ONI class/method names (`Game`, `Research`, `WorldInventory`, `RationTracker`,
`CircuitManager`, etc.) **change between game updates**. The API references in §5 below are a
starting map, **not gospel**. Before relying on any symbol:

1. Reference the actual `Managed/*.dll` in the `.csproj`.
2. Decompile `Assembly-CSharp.dll` (dotPeek or ILSpy) and confirm the exact member name,
   signature, and namespace.
3. Prefer `AccessTools`/reflection-tolerant access for anything fragile, and **fail soft**:
   if a state probe throws, that rule reports "unknown" and is hidden, never crashing the HUD.

The canonical modding reference is **Cairath's "Meep, Modding, and You" wiki**
(`github.com/Cairath/Oxygen-Not-Included-Modding/wiki`). Read its Introduction +
Harmony pages before writing patches.

---

## 3. Dev environment setup (do this first)

1. Locate the game: `…/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/Managed`.
2. Create `Directory.Build.props.user` (copied from `…default`) pointing `GameLibsFolder`
   at that Managed path, so the build can resolve `Assembly-CSharp.dll`,
   `UnityEngine.CoreModule.dll`, `0Harmony.dll`, etc. Do **not** commit machine-specific paths.
3. Build tooling: a modern .NET SDK (tested with 9.0.302). The `net48` reference assemblies
   come from the `Microsoft.NETFramework.ReferenceAssemblies.net48` NuGet package, so **no
   admin targeting-pack install is required** (the old `winget … DeveloperPack 4.7.1` step is
   obsolete; we target 4.8 — see §2a).
4. Dev mod folder (mods that crash here are *not* auto-disabled, ideal for iteration):
   - Windows: `%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Dev\NextStepGuide\`
   - Linux: `~/.config/unity3d/Klei/OxygenNotIncluded/mods/Dev/NextStepGuide/`
   - Mac: `~/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Dev/NextStepGuide/`
5. Build output: ILRepack-merged `NextStepGuide.dll` + `mod.yaml` + `mod_info.yaml` copied
   into that folder.
6. Logs: `Player.log` (path varies by OS; on Windows `%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\Player.log`).
   Use `Debug.Log("[NextStepGuide] …")` liberally and grep the log.

---

## 4. Project structure

```
NextStepGuide/
├─ CLAUDE.md                      ← this file
├─ NextStepGuide.csproj           ← targets net48, ILRepack merges PLib (see ILRepack.targets)
├─ Directory.Build.props.default  ← game-path resolution (committed)
├─ mod.yaml                       ← title, description, staticID
├─ mod_info.yaml                  ← supportedContent (ALL / vanilla / dlc), minimumSupportedBuild
├─ src/
│  ├─ ModEntry.cs                 ← UserMod2.OnLoad: PUtil.InitLibrary, register options + patches
│  ├─ Config/
│  │  └─ GuideSettings.cs         ← PLib [Option] config (refresh rate, dismissed-ids, verbosity)
│  ├─ State/
│  │  ├─ ColonySnapshot.cs        ← plain struct: all the numbers a rule might need
│  │  └─ StateReader.cs           ← builds a ColonySnapshot from game singletons (fail-soft)
│  ├─ Rules/
│  │  ├─ IRule.cs                 ← rule contract (see §6)
│  │  ├─ Recommendation.cs        ← output DTO (id, title, why, urgency, category, wikiHint)
│  │  ├─ RuleEngine.cs            ← evaluate all rules vs snapshot, sort, dedupe, top-N
│  │  └─ Definitions/             ← one file per tier; rules are small + testable
│  │     ├─ SurvivalRules.cs
│  │     ├─ StabilizationRules.cs
│  │     ├─ InfrastructureRules.cs
│  │     └─ EndgameRules.cs
│  ├─ UI/
│  │  ├─ GuidePanel.cs            ← the side panel (PLib PPanel/PScrollPane), collapsible
│  │  └─ GuidePanelPatch.cs       ← Harmony patch that injects the panel into the HUD
│  └─ Util/
│     └─ Throttle.cs              ← run StateReader at most every N seconds, off the hot path
└─ test/                          ← see §9 (rules are tested against fake snapshots)
```

---

## 5. State layer — what to read and roughly where

`StateReader.BuildSnapshot()` runs on a throttle (default every ~3 real seconds, or once
per in-game cycle tick) and populates a `ColonySnapshot`. **Every probe is wrapped in
try/catch; a failed probe sets a `bool …Known = false` flag rather than throwing.**

Approximate source map (CONFIRM each against decompiled source — names drift):

Legend: ✅ verified in code against build U59-737790 · 🔜 not wired up yet (planned).

| Snapshot field | Source | Status |
|---|---|---|
| `Cycle` | `GameClock.Instance.GetCycle()` | ✅ |
| `LiveDuplicants` | `Components.LiveMinionIdentities.Count` | ✅ |
| `BuildingCounts[prefabId]` | iterate `Components.BuildingCompletes` (a `Cmps<BuildingComplete>`); key by `GetComponent<KPrefabID>().PrefabID().Name`. This is the registry that makes building-count cheap — no grid scan needed. | ✅ |
| `ResourceKg[tag]` | `ClusterManager.Instance.activeWorld.worldInventory.GetAmount(new Tag(name), false)` — accessible mass only. Element tag == `new Tag("Algae")` etc. (equals `SimHashes.X.CreateTag()`). | ✅ |
| `IsDlcActive` | `DlcManager.IsExpansion1Active()` | ✅ |
| `PowerGeneratedW/ConsumedW`, `BatteryChargeFraction` | `Game.Instance.circuitManager` — `GetWattsGeneratedByCircuit` / `GetWattsUsedByCircuit` / `GetJoulesAvailableOnCircuit` / `GetMinBatteryPercentFullOnCircuit` **per circuit**. NOTE: no public circuit-count enumerator — iterating circuits needs reflection on the private circuit list. Deferred to Phase 5. | 🔜 |
| `CaloriesAvailable` | ⚠️ `RationTracker` is a **consumption** tracker (`GetAmountConsumed`), NOT total stored calories. For "available calories" sum `Edible` components or read the resources panel source. | 🔜 |
| `AvgStress`, `AvgMorale` | iterate `Components.LiveMinionIdentities`, read `Db.Get().Amounts.Stress` via `AmountInstance` | 🔜 |
| `WorstRoomTemperatureK` / hotspots | sample temperatures near a handful of representative buildings (not the whole map) | 🔜 |

⚠️ **Prefab-id gotcha:** the "Oxygen Diffuser" building's prefab id is **`MineralDeoxidizer`**,
not `OxygenDiffuser` (which does not exist). All verified prefab ids live in
`src/State/Prefabs.cs` with a re-verify-after-update note. Element tag names live in
`src/State/Elements.cs`.

`ColonySnapshot` is a **pure data class with no game references**, so the rules engine and
its tests never touch Unity. The StateReader is the *only* layer allowed to call game APIs,
and every probe is try/catch-wrapped with a `…Known` flag (fail-soft).

---

## 6. Rules engine

### How milestones.yaml and the C# rules fit together (AS BUILT)

`milestones.yaml` and this section describe the same rules from two angles, reconciled like so:

- **`milestones.yaml` = the CONTENT/TUNING layer.** It owns `title`, `why`, `urgency_base`,
  `category`, `tier`, `soft_cycle`, `dlc`, `status`, and `depends_on`. Editing advice text,
  urgency, or gating = editing YAML, no recompile of logic needed. Loaded into `MilestoneDef`
  (`src/Rules/MilestoneDef.cs`) via the game-bundled YamlDotNet.
- **C# `IRule` = the DETECTION layer.** Each rule is a tiny class keyed by the same `id` that
  implements `IsRelevant` / `IsSatisfied` / `Urgency(snapshot, def)` against a `ColonySnapshot`.
  Detection can't be data-driven because `state_hints` are prose, not executable.
- **`RuleEngine` joins them by id** and assembles the `Recommendation` from the `MilestoneDef`
  (words/urgency baseline) plus the rule's computed urgency. A rule with no matching active
  milestone — or a milestone with `status: draft` — is skipped. The test
  `EveryRegisteredRule_HasMatchingActiveMilestone` enforces this pairing.

So the contract below is real, but `Category`/`Build()` live on the `MilestoneDef`/engine rather
than the rule; rules stay pure detection. `Urgency` takes the `def` so it can scale off the
baseline. The actual interface is in `src/Rules/IRule.cs`.

### Contract (original sketch)

```csharp
public interface IRule {
    string Id { get; }                 // stable, used for dismiss + tests, e.g. "cooling.aquatuner"
    RuleCategory Category { get; }      // Oxygen, Power, Food, Water, Heat, Morale, Research, Industry, Space
    // Should this advice even be considered right now? (cheap gate)
    bool IsRelevant(in ColonySnapshot s);
    // If relevant, is it already satisfied? (don't show solved problems)
    bool IsSatisfied(in ColonySnapshot s);
    // 0..100; higher = more urgent. May be dynamic (e.g. scales with how negative O2 is).
    int Urgency(in ColonySnapshot s);
    Recommendation Build(in ColonySnapshot s); // title + one-line why + optional wiki hint
}
```

`RuleEngine.Evaluate(snapshot)`:
1. For each rule: skip if `!IsRelevant` or `IsSatisfied` or id is in the user's dismissed set.
2. Compute `Urgency`, build `Recommendation`.
3. Sort by urgency desc, then by a fixed category tiebreak (survival categories first).
4. Collapse near-duplicates (e.g. two food rules → show the most urgent one).
5. Return top **N** (config, default 4) — never wall-of-text the player.

### Urgency banding (keep it legible)
- **90–100 Crisis** (red): active death spiral — O2 net-negative, food running out this cycle,
  power browning out, mass stress breakdowns.
- **60–89 Pressing** (amber): will become a crisis soon — algae nearly depleted, water trending
  down, base heating past comfort, no atmo suits before entering hot/hazard biomes.
- **30–59 Progress** (blue): the "what now?" sweet spot — unlock refined metal, start ranching,
  build cooling loop, set up plastic, expand research tier.
- **1–29 Polish** (grey): decor, skill specialisation, logistics tidy-ups.

The **30–59 band is the whole reason this mod exists** — invest the most rule-writing effort
there, because that's where players stall.

### Progression gates on STRUCTURE, not STOCK (core rule-writing law)

A **progression** rule (move to the next tier: renewable oxygen, refined metal, steel, plastic,
cooling, sustainable power…) must decide `IsRelevant`/`IsSatisfied` from **structural/capability
state** — *have you built the next-step thing yet?* — and must **never** gate visibility on a
current consumable buffer. Resource levels feed `Urgency` only.

- ✅ `IsRelevant = on algae diffusers AND no electrolyzer built`; `Urgency` ramps as algae falls.
- ❌ `IsRelevant = … AND algae < 600` — this *hides the advice exactly when the player has
  breathing room to act on it*, which defeats the "what now?" purpose and teaches nothing about
  progression. A finite-resource buffer should **lower priority, not silence the tip**.

So a tip persists until the next step is actually built; a comfortable stockpile just pushes it
down the list (Progress band) instead of off it. The one exception is **survival** rules, which
*do* gate on a genuine missing fundamental (no oxygen source at all, no toilet, etc.).
Implemented in `ElectrolyzerTransitionRule`; mirrored in `milestones.yaml`'s PHILOSOPHY header.

### Representative rule set (starter — expand from here)

Write these as concrete `IRule`s. This is a seed list, not the full tree; once the engine
works, mine the ONI wiki + your own experience to grow each tier.

**Survival (gate: low cycle or missing fundamentals)**
- `oxygen.source.missing` — no deox/electrolysis and O2 trending down → Crisis.
- `sanitation.toilet.loop` — outhouse but no wash basin / no lavatory upgrade path → Pressing.
- `food.negative` — calorie delta negative and < ~2 cycles buffer → Crisis/Pressing by buffer.
- `power.brownout` — consumed > generated and batteries < 20% → Crisis.
- `research.station.tier2` — have power+materials but no Super Computer / next research bench → Progress.

**Stabilization (gate: stable survival, cycle ~20–60)**
- `oxygen.algae.depletion` — relying on algae deox and Algae stock < threshold and no
  electrolyzer SPOM → Pressing ("transition to electrolysis before algae runs out").
- `water.security` — net water consumption positive and no sieve/vent source secured → Pressing.
- `heat.base.warming` — base ambient climbing past ~30–35 °C with no cooling → Pressing.
- `suits.atmo` — research/refined metal available, hazard biomes adjacent, no atmo suit dock → Progress.
- `morale.ceiling` — required skill morale > available morale for several dupes → Progress.

**Infrastructure (gate: cycle ~60+, suits/cooling beginning)**
- `metal.refinery` — no Metal Refinery and refined metal stock low → Progress.
- `cooling.aquatuner` — have plastic/steel + heat problem, no AquaTuner loop → Progress.
- `ranching.coal` — burning coal with no Hatch ranch (coal sustainability risk) → Progress.
- `plastic.production` — DLC/oil accessible, no plastic source (drecko or polymer press) → Progress.
- `industrial.brick.isolation` — high-heat machines inside livable base → Polish/Progress.

**Endgame (gate: cycle ~150+, refined industry online)**
- `power.sustainable` — still on coal/manual at high cycle, no petroleum/NG/steam loop → Progress.
- `space.access` — no telescope/scanner (vanilla) or no rocket platform (DLC) → Progress.
- `food.sustainable` — still on basic crops, no high-quality food chain → Polish.

Each rule's `Build()` returns a `Recommendation` like:
```
title: "Move off algae oxygen"
why:   "Algae is down to ~150 kg and you have no electrolyzer. Build a SPOM before it runs out."
wiki:  "Oxygen Diffuser" / "Electrolyzer"   (just a name hint shown as text; no external calls)
urgency: 74 (amber)
```

---

## 7. UI

MVP: a **collapsible panel pinned to a screen edge** (top-right under the resource list is a
natural home), built with PLib (`PPanel` / `PScrollPane` / labels), injected via a Harmony
patch on an appropriate HUD screen's init (verify the right host screen against decompiled
source — candidates include the in-game `ScreenManager`/main HUD root).

Requirements:
- Header showing colony cycle + a collapse/expand toggle. Collapsed = just a small badge with
  the count + highest urgency colour, so it's never in the way.
- Each row: urgency colour chip, title, and the *why* line on hover or always-visible (config).
- A small **✕ dismiss** per row → adds the rule id to the dismissed set in `GuideSettings`
  (persisted via PLib options). Add a "reset dismissed" button in mod options.
- Refresh from the latest `ColonySnapshot`; **never** read game state on the UI thread —
  the UI only renders the last `RuleEngine.Evaluate` result.
- Respect pause: still display, just don't thrash recompute while paused.

Options (PLib `[Option]`): refresh interval, max recommendations shown (N), show-why-inline
toggle, and per-category mute toggles (some players never want morale nags).

---

## 8. Performance & safety

- StateReader runs on a throttle (`Util/Throttle`), default ≥3s between full snapshots, and the
  expensive `BuildingCount` probe is cached/invalidated less often than the cheap numeric probes.
- All game access is fail-soft: one broken probe degrades one rule, never the HUD.
- No save-game writes, no building/automation changes → mod is freely add/removable mid-colony.
- Guard every patch with a clear `[NextStepGuide]` log line on load so failures are greppable.

---

## 9. Testing

Because `ColonySnapshot` is pure data, the rules engine is unit-testable **without the game**:

- A small xUnit/NUnit project references only `Rules/` + `State/ColonySnapshot.cs`.
- For each rule, write fixtures: a snapshot where it should fire, one where it's satisfied,
  one where it's irrelevant. Assert `IsRelevant`/`IsSatisfied`/`Urgency` bands.
- Add an engine test: given a crafted snapshot, assert the top-N ordering and dedupe behaviour.
- The game-touching layers (StateReader, UI, patches) are validated manually in a Dev-folder
  install against a few save files at different cycle counts (a fresh base, a stalled
  cycle-80 base, a mature cycle-300 base). Keep 2–3 such saves as manual test fixtures.

This split is the key quality lever: the *judgement* of the mod lives in pure-data rules you
can test fast and iterate on; only the thin reading/rendering shell needs the running game.

---

## 10. Distribution

- `mod_info.yaml`: set `supportedContent: ALL` (works with and without DLC), and a sensible
  `minimumSupportedBuild`.
- Test as a **Local** mod first; publish to Steam Workshop only once stable. Note Steam can
  serve stale mod versions — keep a GitHub release with the raw DLL as a fallback.
- Don't ship against a single game build and forget it: ONI updates break internal APIs, so
  budget for "re-verify symbols after major game updates" maintenance.

---

## 11. Phased build order (ship something playable each phase)

**Phase 0 — Scaffold.** `.csproj` (net471) referencing Managed DLLs, ILRepack merging PLib,
`ModEntry : UserMod2` with `OnLoad` → `PUtil.InitLibrary` + a single Harmony patch that logs
"loaded". Confirm it appears in the in-game mods list and logs on load. *Nothing visible yet.*

**Phase 1 — State spike.** Implement `ColonySnapshot` + `StateReader` for the cheap fields
only (cycle, dupes, power gen/consume, calories, a few resource tags). Log the snapshot every
few seconds. Verify numbers match the in-game HUD across two save files.

**Phase 2 — Engine + 5 rules.** `IRule`, `RuleEngine`, and 5 high-confidence survival/
stabilization rules. Log the top-N recommendations. No UI yet — prove the advice is *right*
by reading the log against known colony situations.

**Phase 3 — Panel.** Minimal PLib panel rendering the engine output, collapsible, pinned to
the HUD. Read-only, no dismiss yet. This is the first genuinely useful build.

**Phase 4 — Dismiss + options.** Per-row dismiss persisted via PLib options; refresh-rate,
N, per-category mute settings; reset-dismissed button.

**Phase 5 — Rule depth.** Expand to the full §6 tiers, add the expensive building-count
probes (cached), tune urgency bands against the cycle-80 and cycle-300 test saves. This is
the long tail where the mod's quality actually lives.

**Phase 6 — Polish + publish.** Heat/hotspot detection, DLC-gated space rules, decor/skill
polish rules, Workshop release + GitHub fallback.

---

## 12. Open questions to resolve early (don't block, but decide)

- Exact host screen for injecting the panel (verify against decompiled HUD).
- Building-count strategy: per-type `Components` registries vs a maintained tag index — pick
  the cheapest that's reliable, since it's the perf-sensitive probe.
- How to estimate "heat problem" cheaply without sampling the whole map (likely: watch a
  handful of representative buildings' temperatures, not every tile).
- DLC method names for `DlcManager` checks — confirm current signatures.

---

### Working agreement for the agent
- Treat §5 API names as **hypotheses to verify against the decompiled `Assembly-CSharp.dll`**,
  not facts. When a symbol doesn't exist, search the decompiled source for the nearest match
  and update this file's table.
- Keep all game access inside `StateReader`; keep rules pure and tested.
- Ship each phase as a working build; prefer 5 correct rules over 30 flaky ones.
- Log under the `[NextStepGuide]` prefix and fail soft everywhere.