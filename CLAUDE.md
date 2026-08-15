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
- **DOTween shortcuts need the `DOTween.Modules` assembly reference — it is now
  in place.** `DOTween.dll` is precompiled and auto-referenced, so the core API
  always worked, but the shortcut extensions (`DOColor`, `DOScale`, `DOFade`,
  `DOAnchorPos`, …) ship as loose `.cs` files that Unity compiles into a
  separate assembly. `DOTween.Modules.asmdef` has been generated and added to
  `BossLevel.Runtime`'s references, so the shortcuts are usable. If they ever
  stop resolving, that reference is the first thing to check.
- **A kinematic `Rigidbody2D` only reports contacts against dynamic bodies**
  unless `useFullKinematicContacts` is enabled. A kinematic projectile without
  it sails straight through static targets and geometry, silently. `Projectile`
  sets it in `Reset()`.
- **`PlatformEffector2D` Surface Arc must be below 180°** (160 is used here).
  At the default 180 the arc spans exactly ±90° from up, so a side contact —
  normal exactly horizontal — sits on the boundary and counts as solid surface,
  making platform sides block the player in mid-air. The platform's collider
  also needs **Used By Effector** enabled or the effector does nothing.
- **A `MonoBehaviour` defined in an editor-only assembly cannot be added with
  `AddComponent`** — it fails with *"Can't add script behaviour X because it is
  an editor script."* An EditMode test assembly has
  `"includePlatforms": ["Editor"]`, so any test double that must be a real
  component belongs in `BossLevel.TestSupport` instead: a normal runtime
  assembly kept out of player builds by the `UNITY_INCLUDE_TESTS` define
  constraint. Testing a component that already lives in `BossLevel.Runtime`
  (such as `Health`) is fine and needs none of this.

---

## Starter code status

`Assets/BBB/` held boilerplate the author carried between projects. **The author
confirmed it is not meaningful and can be freely renamed, rewritten, or
deleted**, along with the folder hierarchy. Concepts are kept; implementations
are replaced. Per-file rationale in `Documentation/DESIGN.md` §15.

`MonoSingleton`, `MonoPool`, and the old `IPoolable` are gone, replaced by
`Common/PersistentSingleton`, `Common/Pool<T>`, and `Common/IPoolable`.

`PlayerMovement.cs` and `PassThroughPlatform.cs` were **moved, not rewritten**,
and still carry their original `BBB.Scripts.*` namespaces. This is deliberate:
their `.meta` GUIDs are referenced by the `BossLevel` scene, so keeping them
intact leaves the greybox working until Milestone 2 replaces them properly.
They are interim, not examples of the project's conventions.

The `BossLevel` scene greybox and its four `PlatformEffector2D` platforms are
worth keeping.

---

## Open questions

- ~~"Survivor.io boss phase" scope~~ — **resolved.** The instructor's intent is
  *scale of deliverable*, not genre. The Cuphead-style side-scroller stands.
- ~~DOTween shortcut extensions unreachable~~ — **resolved.** The user generated
  `DOTween.Modules.asmdef` and it is referenced by `BossLevel.Runtime`.

---

## Current state

**Milestone 1 (Foundation) — done, verified.** 18 EditMode tests green on the
Unity machine.

- Restructured into `Assets/_Project/`
- `BossLevel.Runtime`, `BossLevel.Tests`, `BossLevel.TestSupport` assemblies
- `Common/`: `IPoolable`, `Pool<T>`, `PersistentSingleton<T>`
- `Combat/`: `IDamageable`, `Health`
- Tests: `HealthTests` (10), `PoolTests` (8), `PoolDummy` in TestSupport

No `BossLevel.Editor` assembly yet — it arrives with the setup tooling.

**Milestone 2 (Player) — written, not yet compiled.** `PlayerInputReader` and
`PlayerMotor`. `PlayerMovement` and `PassThroughPlatform` deleted.

Input binds against the existing `InputSystem_Actions` asset through a
serialized `InputActionAsset` field plus `FindAction`, because the asset has
`generateWrapperCode: 0`; this avoids requiring an editor step before the code
compiles. Actions used: `Move`, `Jump`, `Attack`, and `Crouch` (reused as
drop-through).

Drop-through has **no platform component**. A surface is droppable exactly when
it has a `PlatformEffector2D`, so `PlayerMotor` tests for that rather than using
a marker script or a dedicated layer.

DOTween is confirmed present as a plain DLL at
`Assets/Plugins/Demigiant/DOTween/DOTween.dll` with no `.asmdef`. Because
`BossLevel.Runtime` sets `"overrideReferences": false`, it is auto-referenced —
**no asmdef change is needed to use DOTween.**

**Milestone 3 (Combat loop) — written, not yet compiled.** `Combat/Projectile`,
`Combat/ProjectilePool`, `Player/PlayerShooter`, `Feel/SpriteFlash`, and a
`Facing` property added to `PlayerMotor` so shots follow the way the player
faces.

`Projectile` filters what it may hit with a serialized `LayerMask` rather than
the physics collision matrix. That is deliberate for now: it needs no new
layers, so it works in the existing scene. The layer-and-matrix version arrives
with the `Boss Level ▸ Configure Project` editor tooling.

`SpriteFlash` is a placeholder driving `SpriteRenderer.color`; the final version
drives `_FlashAmount` on the Shader Graph material (Milestone 8).

**Milestone 4 (Boss skeleton) — done, verified.** The four-beat loop plays and
the fight works.

Known wart: `BossTelegraph` and `SpriteFlash` both write `SpriteRenderer.color`,
so a hit landing during a telegraph is overwritten by the telegraph tween. The
Milestone 8 shader resolves this properly, which is exactly why the design gives
`_FlashAmount` and `_PhaseTint` separate shader properties.

**Milestone 5 (Data layer) — written, not yet compiled.**

- `Boss/BossContext` — the world handed to an attack, with `Fire`, `FireAtAngle`,
  `FireFrom`, and `AngleToPlayer` helpers so attacks read as their own shape
  rather than as trigonometry plus pool plumbing.
- `Boss/Attacks/BossAttack` — abstract ScriptableObject holding display name,
  telegraph and recovery durations, and `IEnumerator Execute(BossContext)`.
- Five concrete attacks: `SpreadShot`, `AimedBurst`, `Sweep`, `Rain`, `Slam`.
- `Boss/AttackSelector` — shuffle bag, plain C#, injectable random.
- `BossController` refactored to a list of attacks plus the selector; the
  hardcoded spread is gone.
- `AttackSelectorTests` — 7 cases, including no back-to-back repeats across 50
  seeds and 200 draws each.

All five attack assets are authored and in `_Project/Data/Attacks/`.

**Milestone 6 (Phases) — written, not yet compiled.**

- `Boss/Data/BossPhase` — attacks, cooldown range, telegraph and recovery
  multipliers, health threshold.
- `Boss/Data/BossDefinition` — max health plus an ordered phase list.
- `Boss/BossPhaseMachine` — thresholds and indices only, no Unity dependency.
  Advances one phase per call and never backwards.
- `BossController` — now driven by a `BossDefinition`; its `attacks` list and
  `cooldownRange` are gone, replaced by whatever the current phase says. Runs
  the transition between attacks, never during one. Gained `StopFighting()`
  and a `PhaseChanged` event.
- `Player/PlayerDamageResponse` — invulnerability frames plus a blink.
- `App/GameStateMachine` — Intro → Fighting → Won | Lost, toggling
  `PlayerInputReader` to take control away at the two moments it should be gone.
- `BossPhaseMachineTests` — 10 cases, including the phase-skip case.

**Phase and boss assets do not exist yet.** The user authors them from
*Assets ▸ Create ▸ Boss Level ▸ Phase* and *▸ Boss Definition*.

**Next: Milestone 7 (Shell)** — Bootstrap scene, `SceneLoader`, main menu,
loading screen, health bars, phase banner, end screens, per
`Documentation/DESIGN.md` §16.
