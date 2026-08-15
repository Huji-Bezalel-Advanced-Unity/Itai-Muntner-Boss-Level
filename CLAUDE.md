# CLAUDE.md — working context for this repository

Read this first. It records the constraints and decisions that are expensive to
re-derive. Full design detail lives in `Documentation/DESIGN.md`; this file is
the operating context around it.

---

## What this is

A Cuphead-style single-screen boss fight, built as the **final assignment for an
Advanced Unity course** (Hebrew University / Bezalel). The repository is
`Huji-Bezalel-Advanced-Unity/Itai-Muntner-Boss-Level`.

The player fights one boss on a fixed, non-scrolling arena with one-way
platforms. The boss escalates through three health-gated phases.

---

## Critical: this is a two-machine workflow

**Claude Code runs on a Windows machine that does NOT have Unity installed. The
Unity editor lives on a separate computer.** The two are connected only through
this git repository.

Consequences that shape everything:

- **Nothing written here can be compiled or run before it is committed.** Code
  correctness comes from care in authoring, not from a green build. Say so
  plainly rather than implying anything has been verified.
- **Editor-only work cannot be done from here** — creating ScriptableObject
  assets, scene hierarchies, prefabs, layers, the physics collision matrix.
  Automate it with `[MenuItem]` editor scripts under `Scripts/Editor/` so the
  Unity-side setup is a menu click, not a manual checklist.
- **Sequence work to compile early and often.** Prefer small, verifiable drops
  over large ones; every compile error costs a round trip through a human.
- Local clone on the Claude machine: `C:\Users\imuntner\Documents\BossFight`
- Commit identity is set locally to `Itai Muntner
  <itai.muntner@mail.huji.ac.il>` to match existing history. Do not change it.

---

## Environment

| | |
|---|---|
| Unity | 6000.0.41f1 (Unity 6 LTS) |
| Render pipeline | URP 17.0.4, 2D renderer |
| Input | Input System 1.13.1. `activeInputHandler: 2` (Both) — legacy `Input` still compiles, but **new code uses the Input System only** |
| Test framework | 1.4.6 (EditMode tests) |
| Tweening | DOTween (Demigiant, free) — Asset Store install, committed to the repo |
| Build target | **WebGL** |

---

## The grading rubric drives the architecture

| Weight | Criterion |
|---|---|
| **40%** | Readability — "I should easily understand the code" |
| **30%** | Quality: clean, scalable, evident thought and planning |
| 10% | Documentation and planning, committed to git |
| 10% | Build for web/mobile (5% if PC zip) |
| 10% | Game feel — good, fun, juicy |

**Roughly 70% of the grade is a human reading the source and finding it clear.**
Game feel is worth only 10%.

**Therefore: where extensibility and obviousness conflict, choose obviousness.**
Abstraction the reader must decode costs more than it gains at this scope. No DI
container, no event-bus indirection, no deep inheritance. Note extension points
in comments rather than building them speculatively. Short, single-purpose
files.

This is a real correction to default instincts — an early draft of the plan
proposed a four-layer assembly split, which was over-engineered for this brief.

Required subject matter that must visibly appear in the project: scalable
loading code, AI with phases, a shader, VFX, tweens, a player mechanic, UI, and
win/lose end states.

---

## Conventions

- `PascalCase` types/methods/properties/constants; `_camelCase` private fields;
  `camelCase` locals and parameters.
- `[SerializeField] private`, never `public` fields.
- XML `<summary>` on every public type and any non-obvious member. Comments
  explain **why**, not what.
- One public type per file, named for the file. Past ~200 lines, split.
- Physics writes in `FixedUpdate`; input edges in `Update`. Never physics from
  `Update`.
- Subscriptions in `OnEnable`, unsubscriptions in `OnDisable`, always paired.
- Namespaces mirror folders: `BossLevel.Boss.Attacks`, `BossLevel.Combat`, …
- Tunable numbers are serialized fields or ScriptableObject data, not literals.

---

## Key decisions already made (do not relitigate)

- **Attacks are `ScriptableObject`s exposing `IEnumerator Execute(BossContext)`.**
  Coroutine locals live in the compiler-generated iterator, not on the asset, so
  each execution gets isolated state for free.
- **ScriptableObjects hold configuration only, never mutable runtime state.** An
  asset is a single shared instance; writing to it at runtime persists in the
  editor and leaks across users.
- **The telegraph → active → recovery → idle loop lives in `BossController`,**
  not in individual attacks. Stated once; phase difficulty is then just
  telegraph/recovery multipliers on the phase asset.
- **Phase transitions never interrupt an in-flight attack.** Finish, then run
  the transition. Also guarantees a phase cannot be skipped by one large hit.
- **Attack selection is a shuffle bag**, not pure random, with no repeat across
  a refill boundary.
- **Pools: `Pool<T>` is a plain C# generic class** owned by a small concrete
  `MonoBehaviour`. Unity cannot attach generic MonoBehaviours, so the starter
  `MonoPool<T> : MonoSingleton<MonoPool<T>>` could never have its serialized
  fields assigned.
- **Singletons are narrowed** to genuinely global persistent services
  (`SceneLoader`). Everything else is wired through the Inspector.
- **WebGL over mobile** — same 10%, and mobile would force touch controls the
  rubric does not reward.

---

## Gotchas that have already cost time

- **VFX Graph does not work on WebGL** (no compute shader support). Use the
  built-in Particle System (Shuriken). Effects built in VFX Graph would silently
  do nothing in the submitted build.
- **Unity cannot attach generic `MonoBehaviour`s** via the Inspector, so their
  `[SerializeField]` values can never be assigned. Keep generics in plain C#
  classes, or add a concrete non-generic subclass.
- **`MonoBehaviour` already defines `Reset()`** — Unity calls it in the *editor*
  when a component is added or reset. An `IPoolable.Reset()` would fire at
  authoring time. Interface uses `OnSpawn()` / `OnDespawn()` instead.
- **`FindObjectOfType` is deprecated in Unity 6** → `FindFirstObjectByType`.
- **A `MonoBehaviour`'s protected constructor prevents nothing** — Unity creates
  components via `AddComponent`, never via a constructor. Duplicate handling
  belongs in `Awake`.
- **`LoadSceneAsync` progress caps at 0.9** while `allowSceneActivation` is
  false. Remap `0..0.9` to `0..1` or the bar appears to stall.

---

## Starter code status

`Assets/BBB/` contains boilerplate the author carried between projects —
`MonoSingleton`, `MonoPool`, `IPoolable`, `PlayerMovement`, `PassThroughPlatform`.
**The author has confirmed it is not meaningful and can be freely renamed,
rewritten, or deleted**, along with the folder hierarchy. Concepts are kept;
implementations are replaced. Per-file rationale in `Documentation/DESIGN.md`
§15.

The `BossLevel` scene greybox and its four `PlatformEffector2D` platforms are
worth keeping.

---

## Open questions

- The assignment describes scope as "Survivor.io boss phase." This project reads
  that as *scale of deliverable*, not *genre to copy*, and builds a Cuphead-style
  side-scroller. **To be confirmed with the instructor.**
- DOTween is not yet installed; it must be imported from the Asset Store on the
  Unity machine before any tween code will compile.

---

## Current state

Design document written. No production code written yet. Next milestone is
Foundation: restructure into `Assets/_Project/`, add assembly definitions,
replace the starter scripts. See `Documentation/DESIGN.md` §16 for the full
build order.
