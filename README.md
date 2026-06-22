# Next Step Guide — an *Oxygen Not Included* mod

> A good ONI player looking over your shoulder.

**Next Step Guide** adds a small, state-aware checklist to *Oxygen Not Included* that tells you
**what to prioritise next — and why — based on your colony's live state.** It's **advisory only**:
it never builds, automates, or touches your save. It just points.

ONI's early game is self-directing (you need oxygen, a toilet, food, power, and the game more or
less screams when one is missing). After that it goes quiet, and a lot of players with a stable base
stall out — not sure whether the next move is cooling, water security, atmo suits, ranching, refined
metal, or research. This mod fills that gap with a prioritised list of nudges like:

> **Cook your food in a kitchen** — Raw crops give few calories and low morale; an Electric Grill
> turns them into proper meals.

Each tip is (1) detected from real game state, (2) explained with a one-line *why*, (3) colour-coded
by urgency, and (4) dismissable. Wrong or nagging advice is worse than none, so it biases toward
**high precision over coverage**.

---

## What it looks like

A collapsible panel pinned to the side of the HUD shows the top few recommendations, each with an
urgency colour chip (red crisis → amber pressing → blue progress → grey polish), a title, and an
optional one-line reason. You can dismiss individual tips, mute whole categories, and tune how many
show — all from the in-game mod options.

## How it works (two layers)

- **`milestones.yaml`** — the *content/knowledge* layer: every piece of advice with its title,
  *why*, urgency, category, tier, and gating. Editing advice text or tuning is a YAML change, no
  recompile of logic needed.
- **C# rules** (`src/Rules/Definitions/`) — the *detection* layer: one tiny class per milestone that
  reads a pure-data `ColonySnapshot` and decides whether the advice is relevant / already handled,
  and how urgent it is.

The `StateReader` is the **only** code that touches the live game; every probe is `try/catch`-wrapped
and fails soft (one broken probe hides one tip, never the HUD). Because `ColonySnapshot` has no game
references, the entire rules engine is unit-testable without launching ONI.

Currently **19 of 33 milestones** have live detection (survival → mid-game). See [ROADMAP.md](ROADMAP.md)
for what's done and what's next, and [CLAUDE.md](CLAUDE.md) for the full design spec.

---

## Setting up locally

### Prerequisites

| Need | Notes |
|---|---|
| **Oxygen Not Included** installed | The build references the game's `Managed/*.dll` assemblies. Built/verified against build **U59-737790** (Unity 6000.3.5f2). |
| **.NET SDK** | A modern SDK (tested with **9.0.302**). The `net48` reference assemblies come from NuGet, so **no admin targeting-pack install is required**. |
| Git | To clone the repo. |

You do **not** need Visual Studio — everything works headless via `dotnet build` / `dotnet test`.
PLib and the YAML/Harmony dependencies are pulled from NuGet or the game's own `Managed` folder.

### 1. Clone

```bash
git clone <repo-url> oni-guide-mod
cd oni-guide-mod
```

### 2. Point the build at your game (only if it's not at the default Steam path)

The build looks for ONI at the default Windows Steam location
(`C:\Program Files (x86)\Steam\steamapps\common\OxygenNotIncluded`). If yours lives elsewhere, copy
the template and edit it — the `.user` copy is git-ignored so your machine paths are never committed:

```bash
cp Directory.Build.props.default Directory.Build.props.user
```

Then set `<GameDir>` (and, if your deploy target differs, `<ModDeployFolder>`) in
`Directory.Build.props.user`.

### 3. Build (and auto-deploy for testing)

```bash
dotnet build -c Release
```

This compiles, uses **ILRepack** to merge PLib into a single `NextStepGuide.dll`, and **auto-deploys**
the DLL + manifests into ONI's local **Dev** mods folder. Mods placed there are *not* auto-disabled
on a crash, which is ideal for iteration.

To build without deploying:

```bash
dotnet build -c Release -p:SkipDeploy=true
```

The Dev mods folder is:

| OS | Path |
|---|---|
| Windows | `%USERPROFILE%\Documents\Klei\OxygenNotIncluded\mods\Dev\NextStepGuide\` |
| Linux | `~/.config/unity3d/Klei/OxygenNotIncluded/mods/Dev/NextStepGuide/` |
| macOS | `~/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Dev/NextStepGuide/` |

> **Note:** ONI loads mod DLLs at startup, so after deploying you must **fully restart the game**
> (not just reload a save) to pick up a new build. Close ONI before building, or the deploy step may
> fail to overwrite the in-use DLL.

### 4. Run it in-game

1. Launch ONI → **Mods** → enable **Next Step Guide** → restart when prompted.
2. Load or start a colony. The guide panel appears on the HUD; tune it under **Mods → Options**.
3. Logs are written to `Player.log` (Windows:
   `%USERPROFILE%\AppData\LocalLow\Klei\Oxygen Not Included\Player.log`). Everything from this mod is
   prefixed `[NextStepGuide]`, so it's easy to grep.

### 5. Run the tests

The rules engine is pure data, so the test suite needs no game install and runs fast:

```bash
cd test
dotnet test
```

---

## Project layout

```
NextStepGuide/
├─ milestones.yaml          ← the advice knowledge base (content/tuning)
├─ src/
│  ├─ ModEntry.cs           ← UserMod2.OnLoad: init PLib, register options + patches
│  ├─ State/                ← ColonySnapshot (pure data) + StateReader (the only game-facing code)
│  ├─ Rules/                ← IRule, RuleEngine, MilestoneDef/Library, and Definitions/ (one file per tier)
│  ├─ UI/                   ← the HUD panel
│  └─ Config/               ← PLib options + persisted settings
├─ test/                    ← xUnit suite over the rules engine (no game needed)
├─ CLAUDE.md                ← full build spec & design rationale
└─ ROADMAP.md               ← phase status and what's next
```

### Adding a new tip

1. Add a milestone block to `milestones.yaml` (or flip an existing one's `status: draft` → `active`).
2. Add a small `IRule` class under `src/Rules/Definitions/` keyed by the same `id`, implementing the
   relevance / satisfied / urgency checks against `ColonySnapshot`.
3. Register it in `src/Rules/RuleRegistry.cs`.
4. Add fixtures in `test/` (one snapshot where it fires, one where it's satisfied, one where it's
   irrelevant) and run `dotnet test`.

**Design law:** progression advice gates on **structure** (have you built the next step?), never on a
current resource buffer — a stockpile only *lowers urgency*, it never hides the tip. Survival gaps are
the exception: those gate on a genuine missing fundamental. See [CLAUDE.md](CLAUDE.md) §6 for the full
rationale.

> ⚠️ Internal ONI class/prefab names drift between game updates. Always verify a symbol against the
> decompiled `Assembly-CSharp.dll` before relying on it (e.g. the "Oxygen Diffuser" prefab is
> `MineralDeoxidizer`, the AquaTuner is `LiquidConditioner`). Verified ids live in `src/State/Prefabs.cs`.

---

## Status & compatibility

- Works **with and without** the Spaced Out! DLC (`supportedContent: ALL`).
- **Save-safe:** no save-data writes, no building/automation changes — freely add or remove mid-colony.
- Built against ONI **U59-737790**; a major game update may need a symbol re-verify pass.

This is a work in progress — see [ROADMAP.md](ROADMAP.md) for the current phase.
