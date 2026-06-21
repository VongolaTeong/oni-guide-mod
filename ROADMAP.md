# ROADMAP — Next Step Guide (ONI mod)

Status of the **Next Step Guide** mod: a state-aware checklist that tells you what to
prioritise next in Oxygen Not Included, and why. Advisory only — it never builds anything.

See [CLAUDE.md](CLAUDE.md) for the full build spec and [milestones.yaml](milestones.yaml) for
the advice knowledge base.

**Legend:** ✅ done · 🔜 next · 🧊 planned · 💡 idea/backlog

Last updated: 2026-06-21 · Built against ONI **U59-737790** (Unity 6000.3.5f2, .NET 4.8).

---

## At a glance

| Phase | Goal | Status |
|---|---|---|
| 0 | Scaffold: builds, merges PLib, loads in-game, logs | ✅ |
| 1 | State layer: `ColonySnapshot` + `StateReader` (fail-soft) | ✅ |
| 2 | Rules engine + 5 rules + YAML knowledge base (tested) | ✅ |
| 3 | On-screen panel (collapsible, colour-coded) | ✅ |
| 4 | Dismiss per tip + options menu | 🔜 |
| 5 | Rule depth + the heavier state probes | 🧊 |
| 6 | Polish, heat/space rules, Workshop release | 🧊 |

**29 unit tests** pass (`cd test && dotnet test`); the build auto-deploys to the Klei Dev
mods folder (`dotnet build -c Release`).

---

## ✅ Done

### Phase 0 — Scaffold & toolchain
- `net48` SDK-style project; framework refs via NuGet (no admin targeting-pack install).
- **PLib 4.24** merged in via ILRepack (`ILRepack.targets`); YamlDotNet uses the game's
  bundled copy (adding the NuGet package collides — see CLAUDE.md §2a).
- `ModEntry : UserMod2` initialises PLib + applies Harmony patches; logs under
  `[NextStepGuide]`.
- `dotnet build -c Release` auto-deploys the merged DLL + manifests to the Dev mods folder.
- Verified the real environment and corrected the spec's guesses (Unity 6 not 2022.3, net48
  not net471, Oxygen Diffuser prefab = `MineralDeoxidizer`).

### Phase 1 — State layer
- `ColonySnapshot` — pure data, **no game references** (so rules/tests run without ONI).
- `StateReader` — the only layer that touches game singletons; **every probe is try/catch'd
  with a `…Known` flag** (one broken probe hides one rule, never the HUD). Reads: cycle,
  live duplicants, DLC, building counts (via `Components.BuildingCompletes`), and accessible
  resource amounts (via `ClusterManager…worldInventory.GetAmount`).
- `Throttle` keeps the heavy work off the per-frame hot path (default every 3s, skipped while
  paused).

### Phase 2 — Rules engine + knowledge base
- `milestones.yaml` = the **content/tuning** layer (titles, why, urgency, gating); C# `IRule`
  classes = the **detection** layer, paired by id. `RuleEngine` joins them, sorts by urgency,
  collapses per category, returns the top N.
- 5 high-precision rules live: `oxygen.source`, `sanitation.toilet`, `food.basic_farm`,
  `research.early_stations`, `oxygen.electrolyzer` (SPOM transition with urgency scaling).
- **Design law:** progression advice gates on **structure** (did you build the next step?),
  never on current **stock** — resource levels only scale urgency. So a tip persists until you
  actually build the next step; a big finite-resource buffer just lowers its priority.
- 29 xUnit tests cover every rule (fires/satisfied/irrelevant), engine ordering/dedupe/
  gating/fail-soft, urgency scaling, and that the real YAML parses and stays consistent with
  the code.

### Phase 3 — On-screen panel
- Collapsible card pinned to the left-centre of the HUD; click to collapse/expand.
- One self-sizing rich-text block (raw Unity `Image`+`VerticalLayoutGroup`+`ContentSizeFitter`
  +`TextMeshProUGUI`) on its own `Canvas`/`GraphicRaycaster` so it draws above and clicks
  independently of the native HUD. Colour chip per tip by urgency band. Fail-soft: any UI
  exception logs and the panel goes quiet, never taking the HUD down.

---

## 🔜 Next — Phase 4: Dismiss & options
- Per-tip **✕ dismiss** → adds the rule id to a dismissed set; "reset dismissed" action.
- **PLib options menu**: refresh interval, max tips shown (N), show-why inline toggle,
  per-category mutes (some players never want morale/ranching nags).
- Persist settings + dismissed ids via PLib's options/config.
- Panel placement: make the corner/offset a setting (left-centre is the current default).

---

## 🧊 Planned

### Phase 5 — Rule depth + heavier probes
Grow the rule set across the tiers in `milestones.yaml` (most have `status: draft` until the
StateReader can detect them). The valuable mid-game "what now?" rules live here.

Deferred state probes that unblock whole groups of rules (see CLAUDE.md §5 `🔜` rows):
- **Power** — generated/consumed W, battery %. Needs reflection over `CircuitManager`'s
  private circuit list (no public enumerator). Unblocks `power.brownout`, `power.coal`, etc.
- **Calories** — total stored calories (the `RationTracker` is a *consumption* tracker, not a
  stockpile). Unblocks food-buffer rules.
- **Stress / morale** — average across duplicants. Unblocks `morale.*`, `dupes.skills`.
- **Heat** — sample temperatures at a handful of representative buildings (not the whole map).
  Unblocks `heat.awareness`, `cooling.aquatuner`, `base.industrial_brick`.
- **Rooms/ranches** — detect a Stable + critter type for `ranching.coal`, room queries.
- Cache the expensive building-count probe more aggressively than the cheap numeric probes.
- Tune urgency bands against real saves (a fresh base, a stalled cycle-80 base, a mature
  cycle-300 base).

### Phase 6 — Polish & publish
- Heat/hotspot detection; DLC-gated space rules; decor/skill polish rules.
- Steam Workshop release + a GitHub release with the raw DLL as a fallback.
- "Re-verify symbols after major game updates" maintenance pass.

---

## 💡 Backlog / ideas (not committed)
- Click a tip to open the relevant build menu / wiki hint, or ping the relevant area.
- Hysteresis on urgency so tips near a threshold don't flicker between bands.
- Severity-aware *why* text (e.g. "algae ~3 cycles left at current draw").
- "Why is this hidden?" debug view listing satisfied/irrelevant rules.
- Localization of titles/why lines.
- Snooze a tip for N cycles instead of permanent dismiss.
- Optional sound/notification when a new **Crisis**-band tip appears.

---

## Known limitations / non-goals
- **Advisory only** — never builds, automates, or writes save data (stays freely add/removable).
- Out of scope: pathfinding hints, "build it here" overlays, re-tutorialising the early game,
  multiplayer.
- Internal ONI symbols drift between updates; rules fail soft, but a major game update may need
  a symbol re-verify pass (CLAUDE.md §2a/§5).
