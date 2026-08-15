# Boss Level — Design Document

**Author:** Itai Muntner
**Course:** Advanced Unity — Final Assignment
**Engine:** Unity 6000.0.41f1 (Unity 6 LTS), Universal Render Pipeline 17.0.4, 2D
**Target build:** WebGL

---

## 1. Concept

A single-screen boss fight in the style of *Cuphead*'s ground battles. The player
occupies the left two-thirds of a fixed, non-scrolling arena and fights a large
boss anchored on the right. The player can run, jump, drop through one-way
platforms, and fire a projectile. The boss cycles through telegraphed attacks
drawn from a pool that grows in size and severity across three phases, each
phase triggered by the boss's remaining health.

The fight ends in one of two states: the player's health reaches zero (lose), or
the boss's health reaches zero (win). Both lead to an end screen offering a
retry.

### Why this shape

A single boss encounter is a deliberately small surface that still exercises
nearly every system a full game needs: state machines, data-driven content,
object pooling, UI, scene loading, and game feel. That makes it a good vehicle
for demonstrating architecture, which is what this assignment is graded on.

---

## 2. Requirements traceability

The assignment lists required subject matter and a grading breakdown. This
table maps each requirement to the system that satisfies it, so nothing is left
implicit.

| Requirement | Implementation | Section |
|---|---|---|
| Loading code, scalable | `SceneLoader` service + `SceneCatalog` asset, async load with progress | §5 |
| AI, Phases | `BossPhaseMachine` + `AttackSelector` + attack coroutines | §7 |
| Shader | One Shader Graph serving hit flash, phase tint, and death dissolve | §10 |
| VFX | Built-in Particle System bursts on fire, impact, phase change, death | §10 |
| Tweens | DOTween across UI transitions, health bar drain, telegraphs, fades | §9, §11 |
| Player mechanic to control | Run, jump (variable height), drop-through, shoot | §6 |
| UI | Boss health bar, player health, phase banner, end screens, loading screen | §9 |
| End scenario — win / lose | `GameStateMachine` with `Won` and `Lost` states | §5 |
| Clean code | Conventions in §12, enforced throughout | §12 |
| Documentation and planning | This document, committed to the repository | — |
| Build for web | WebGL, constraints noted in §14 | §14 |

### Grading weights and their consequence

| Weight | Criterion |
|---|---|
| 40% | Readability — the reader should easily understand the code |
| 30% | Quality: clean, scalable, evident thought and planning |
| 10% | Documentation and planning, committed to git |
| 10% | Build for web or mobile |
| 10% | Game feel — good, fun, juicy |

Seventy percent of the grade is a person reading this source and finding it
clear. That has a specific architectural consequence, and it is worth stating
plainly because it explains choices that might otherwise look under-engineered:

**Where extensibility and obviousness conflict, this project chooses
obviousness.** Abstraction that a reader has to decode costs more in
readability than it gains in scalability at this scope. Extension points are
noted in comments rather than built speculatively. Files are kept short and
single-purpose. There is no dependency-injection container, no event-bus
indirection, and no deep inheritance chain, because at the scale of one boss
fight each of those would obscure more than it enables.

---

## 3. Architecture at a glance

```
                        ┌──────────────────┐
   Bootstrap scene ───► │  GameBootstrap   │  creates persistent services
                        └────────┬─────────┘
                                 │
                        ┌────────▼─────────┐
                        │   SceneLoader    │  async load + progress + fade
                        └────────┬─────────┘
                                 │
   ┌─────────────────────────────▼──────────────────────────────┐
   │                     BossLevel scene                        │
   │                                                            │
   │   GameStateMachine ──► Intro → Fighting → Won | Lost       │
   │          │                                                 │
   │          ├── PlayerInputReader → PlayerMotor               │
   │          │                     → PlayerShooter → Pool      │
   │          │                     → Health                    │
   │          │                                                 │
   │          ├── BossController ──► BossPhaseMachine           │
   │          │        │             AttackSelector             │
   │          │        └───────────► BossAttack (ScriptableObject)
   │          │                          reads BossContext      │
   │          │                                                 │
   │          └── UI ◄── events from Health / PhaseMachine      │
   └────────────────────────────────────────────────────────────┘
```

Dependencies point inward and downward. UI observes gameplay through C# events
and never reaches back into it. Gameplay never references UI types.

### Assemblies

Three assembly definitions:

| Assembly | Contents |
|---|---|
| `BossLevel.Runtime` | All gameplay code |
| `BossLevel.Editor` | Editor-only setup tooling (§13) |
| `BossLevel.Tests` | EditMode unit tests |

The split exists for one concrete reason: an assembly definition cannot
reference Unity's predefined `Assembly-CSharp`, so unit-testing any gameplay
code requires the gameplay code to live in its own assembly. It is not an
attempt at layered architecture — at this scope that would be ceremony.

---

## 4. Project structure

```
Assets/_Project/
├── Scenes/
│   ├── Bootstrap.unity          entry point, no gameplay
│   ├── MainMenu.unity           title, play button
│   └── BossLevel.unity          the fight
├── Scripts/
│   ├── App/                     GameBootstrap, SceneLoader, SceneCatalog,
│   │                            GameStateMachine
│   ├── Player/                  PlayerInputReader, PlayerMotor,
│   │                            PlayerShooter, PlayerDamageResponse
│   ├── Boss/                    BossController, BossPhaseMachine,
│   │                            AttackSelector, BossContext
│   │   ├── Attacks/             BossAttack + one file per concrete attack
│   │   └── Data/                BossDefinition, BossPhase
│   ├── Combat/                  IDamageable, Health, Projectile,
│   │                            ProjectilePool
│   ├── UI/                      BossHealthBar, PlayerHealthView,
│   │                            PhaseBanner, EndScreen, LoadingScreen
│   ├── Feel/                    HitStop, CameraShake, SpriteFlash
│   └── Common/                  Pool, IPoolable, PersistentSingleton
├── Data/                        ScriptableObject assets
│   ├── Boss/                    FlowerBoss.asset, Phase1..3.asset
│   └── Attacks/                 one asset per tuned attack variant
├── Prefabs/   Art/   Shaders/   VFX/   Settings/
└── Tests/EditMode/              AttackSelectorTests, PhaseMachineTests,
                                 HealthTests
```

Folder names state their contents in plain language. A reader looking for the
boss's attack logic should find it in one guess.

**Namespaces mirror folders**: `BossLevel.Boss.Attacks`, `BossLevel.Combat`, and
so on.

### Migration from the current repository

The repository currently contains starter boilerplate under `Assets/BBB/` —
`MonoSingleton`, `MonoPool`, `IPoolable`, `PlayerMovement`, and
`PassThroughPlatform` — carried over from earlier projects. The concepts are
kept; the implementations are replaced. Rationale for each rewrite is in §15.

---

## 5. Application flow and loading

### Scenes

The game has three scenes. `Bootstrap` is nearly empty: it holds
`GameBootstrap`, which creates the persistent services and immediately requests
the main menu. Splitting the entry point from the menu means services have
exactly one creation site and one lifetime, rather than being created lazily
from wherever they happen to be touched first.

### SceneLoader

`SceneLoader` is the single way any scene is loaded. It is a persistent
singleton created by `GameBootstrap`.

```
LoadAsync(SceneId id)
    fade the loading screen in            (DOTween)
    begin SceneManager.LoadSceneAsync     (allowSceneActivation = false)
    drive the progress bar from operation.progress
    wait until progress >= 0.9 AND a minimum display time has elapsed
    allowSceneActivation = true
    fade the loading screen out
```

Two details worth the lines they cost. Holding `allowSceneActivation = false`
until a minimum display time has passed prevents the loading screen from
flashing on and off for fast loads, which reads as a glitch. And Unity's async
progress caps at `0.9` until activation is permitted, so the progress bar is
remapped from `0..0.9` to `0..1` rather than appearing to stall at 90%.

### Scalability

Scenes are identified by a `SceneId` enum and resolved through a `SceneCatalog`
ScriptableObject that maps each id to a scene name. Adding a scene means adding
an enum entry and a row in the catalog asset; no loading code changes.

The enum is deliberate. A raw string API (`Load("BossLevel")`) is more
"scalable" in the sense that it needs no code edit at all, but it moves failures
from compile time to runtime and gives the reader nothing to autocomplete
against. The enum plus catalog keeps call sites type-safe and readable while
keeping the scene *names* as data.

### GameStateMachine

Within `BossLevel`, a small state machine governs the encounter:

| State | Meaning |
|---|---|
| `Intro` | Camera settle and boss entrance; player input disabled |
| `Fighting` | Normal play |
| `Won` | Boss died; boss death sequence, then win screen |
| `Lost` | Player died; death sequence, then lose screen |

It owns nothing but the transitions and the events announcing them. UI and
input enablement subscribe.

---

## 6. Player

Four small components rather than one controller class, each with a single
responsibility. This costs a few extra files and buys a reader who can find
"how does jumping work" without scrolling past shooting code.

| Component | Responsibility |
|---|---|
| `PlayerInputReader` | The only place input is read. Wraps the Input System actions and exposes intent as properties and events. |
| `PlayerMotor` | Translates intent into physics. Owns grounding, jumping, drop-through. |
| `PlayerShooter` | Fire rate, muzzle position, requests projectiles from the pool. |
| `PlayerDamageResponse` | Invulnerability frames, hit flash, knockback. Listens to `Health`. |

Isolating input in `PlayerInputReader` matters beyond tidiness: it is what lets
`GameStateMachine` disable control during the intro and end states by toggling
one object, and it removes every scattered `Input.GetKeyDown` call from the
codebase.

### Movement feel

The current starter code applies physics from `Update()`, which runs at render
rate and produces frame-rate-dependent movement. `PlayerMotor` reads input in
`Update` (necessary — button-down edges are only true for one frame) and applies
it in `FixedUpdate`.

Four standard affordances, all cheap and all responsible for the difference
between "responsive" and "sluggish":

- **Variable jump height** — releasing the jump button early cuts upward
  velocity, so a tap is a hop and a hold is a full jump.
- **Asymmetric gravity** — falling uses a higher gravity multiplier than rising.
  Makes the arc feel weighty without making the jump feel slow.
- **Coyote time** (~0.10s) — a jump input is still accepted shortly after
  walking off a ledge.
- **Jump buffering** (~0.10s) — a jump pressed slightly before landing fires on
  touchdown instead of being dropped.

Jumping sets vertical velocity directly rather than applying an impulse. An
impulse adds to existing velocity, so a jump taken while already falling is
weaker than one from rest — an inconsistency the player feels as unreliability.

### One-way platforms

Jump-through-from-below is handled by `PlatformEffector2D`, configured on the
platforms in the scene. No code required.

**Surface Arc is set to 160°, not the default 180°.** The arc is centred on the
effector's up direction and decides which contact normals count as solid
surface. At 180° the arc spans exactly −90° to +90°, so a contact with the
*side* of a platform — whose normal is precisely horizontal — sits on the
boundary and is treated as surface, which makes the platform's sides solid and
stops the player dead in mid-air. Narrowing to 160° excludes horizontal normals
while costing only 10° at each corner of the landing surface, which is
irrelevant for landing on top.

The platform colliders must also have **Used By Effector** enabled, or the
effector has no effect at all.

Drop-through is code, and it lives on the **player**, not the platform. The
player detects what it is standing on and asks that platform to ignore it:

```
on drop-through input, while grounded:
    find the platform beneath via raycast
    Physics2D.IgnoreCollision(playerCollider, platformCollider, true)
    restore once the player's feet clear the platform's top edge
    (with a timeout as a safety net, not as the primary condition)
```

Two departures from the starter implementation, both deliberate. The starter
disabled the platform's collider outright, which turns the platform off for
*every* object in the scene, not just the dropping player;
`Physics2D.IgnoreCollision` affects only that one pair. And the starter restored
on a fixed 0.5s timer, which can re-enable the collider while the player is
still overlapping it — the position check is the correct primary condition, with
the timer demoted to a safety net so a missed check cannot leave the player
falling through the world forever.

Placing the logic on the player also means input is read once per frame instead
of once per platform per frame.

**No platform component is needed at all.** A platform is droppable exactly when
it is one-way, and what makes it one-way is its `PlatformEffector2D`. The motor
therefore tests the surface it is standing on for that component rather than
asking a marker script or a dedicated layer. One less file, one less thing to
remember to attach, and no way for the two to disagree.

---

## 7. Boss

### Two nested state machines

**Phase machine (outer).** Three phases, selected by remaining health
percentage. Defaults: Phase 1 above 66%, Phase 2 from 66% to 33%, Phase 3 below
33%. Thresholds are data on the phase assets, not constants in code.

When health crosses a threshold, the boss **does not interrupt its current
attack**. It finishes, then runs a transition sequence:

```
boss becomes invulnerable
existing projectiles are cleared from the arena
phase-change VFX + shader tint + camera shake
PhaseBanner announces the new phase        (DOTween)
brief pause
resume with the new phase's attack set
```

This is not decoration. It gives the player a breath, telegraphs that the rules
just changed, and prevents the distinctly unfair feeling of dying to an attack
that was cancelled halfway through.

`BossPhaseMachine` deals in **thresholds and indices only** and knows nothing
about what a phase contains, which keeps it plain C# and testable in
milliseconds without creating a single asset. It **advances at most one phase
per call**: a single large hit can cross two thresholds at once, and the naive
check would jump from phase one to phase three, skipping a transition the player
was owed. The controller advances in a loop, so both transitions play in order.
It also never moves backwards, so healing the boss cannot rewind the fight.

**Attack machine (inner).** Every attack runs the same four beats:

| Beat | What happens | Purpose |
|---|---|---|
| Telegraph | Distinct pose and tell; no damage yet | The fairness contract — the player must be able to react |
| Active | Projectiles spawn, hitboxes go live | The threat |
| Recovery | Boss committed and unable to act | The player's damage window |
| Idle | Cooldown before the next selection | Pacing |

**The loop lives in `BossController`, not in the individual attacks.** Each
attack asset carries its telegraph and recovery durations as data, and the
controller sequences them:

```csharp
yield return Telegraph(attack);        // scaled by current phase
yield return attack.Execute(context);  // the attack's own coroutine
yield return Recovery(attack);         // scaled by current phase
yield return Cooldown(currentPhase);
```

Two payoffs. The fairness contract is stated once in one readable place instead
of being reimplemented — and eventually forgotten — in each attack. And
difficulty scaling falls out of the architecture: "phase 3 is harder" is
expressible as shorter telegraph and recovery multipliers on the phase asset,
applied uniformly to any attack, rather than hand-authored per attack.

### Attack selection

Pure random selection will roll the same attack three times in a row and read as
broken. `AttackSelector` uses a **shuffle bag**: it fills a list with the
current phase's attacks (an attack may be listed twice to weight it), shuffles,
draws until empty, then refills. This guarantees variety while staying
unpredictable.

One extra constraint: the first draw from a refilled bag may not equal the last
draw of the previous bag, which closes the one seam where a repeat can still
occur.

`AttackSelector` is a plain C# class with no Unity dependencies, which makes it
directly unit-testable (§13).

---

## 8. Data model

Boss content is authored as ScriptableObject assets rather than hardcoded
values. The reason is iteration speed: an attack is mostly *numbers* — bullet
count, arc width, windup duration, projectile speed — and tuning those in the
Inspector at runtime is the difference between a fight tuned twice and one
tuned thirty times. Tuning is where a boss fight becomes good.

```
BossDefinition (asset)
 ├─ maxHealth
 └─ phases: [ BossPhase, BossPhase, BossPhase ]

BossPhase (asset)
 ├─ healthThresholdPercent
 ├─ attacks: [ BossAttack, ... ]        may repeat an entry to weight it
 ├─ cooldownRange (min, max)
 ├─ telegraphMultiplier                 < 1 in later phases = less warning
 └─ recoveryMultiplier                  < 1 in later phases = smaller window

BossAttack (abstract asset)
 ├─ displayName
 ├─ telegraphDuration
 ├─ recoveryDuration
 └─ abstract IEnumerator Execute(BossContext context)
```

Concrete attacks are separate C# classes, each in its own file, each a
`[CreateAssetMenu]` ScriptableObject. Planned set:

| Attack | Behaviour |
|---|---|
| `SpreadShotAttack` | Fan of projectiles across a configurable arc |
| `AimedBurstAttack` | Short burst fired at the player's current position |
| `SweepAttack` | Rotating stream that sweeps across the arena |
| `SlamAttack` | Ground slam producing a shockwave the player must jump |
| `RainAttack` | Projectiles falling from above, forcing lateral movement |

Escalation across phases is largely achieved by duplicating an asset and
retuning it — `SpreadShot` and `SpreadShot_Hard` are the same C# class with
different data. Phase 2 "adding the slam" is a drag-and-drop into a list, not a
code change.

### The one rule that governs ScriptableObject use here

**A ScriptableObject asset is a single shared instance. It holds configuration
only; it never holds mutable runtime state.** Writing to a field on an asset at
runtime persists that value between play sessions in the editor and shares it
across every user of the asset — a genuinely confusing class of bug.

This is why `Execute` is a **coroutine**. A coroutine's local variables live in
the compiler-generated iterator object created per call, not on the
ScriptableObject, so each invocation gets its own state for free. The
alternative — a separate runner object per execution — would be safe too, but it
adds a class and an indirection for no gain here.

Everything an attack needs from the world arrives through `BossContext`: the
boss transform, the projectile pool, the player transform, and the active
phase's multipliers. Attacks read from it and never store references to it.

---

## 9. UI

| View | Behaviour |
|---|---|
| `BossHealthBar` | Three visually distinct segments matching the phases, so the player can see a transition coming. Drains with a tweened fill plus a delayed "chip" bar for damage legibility. |
| `PlayerHealthView` | Discrete hearts; punch-scale tween on loss. |
| `PhaseBanner` | Slides in on phase change, holds, slides out. |
| `EndScreen` | Win and lose variants; staggered fade-in, retry button. |
| `LoadingScreen` | Progress bar driven by `SceneLoader`; fade in and out. |

All views are read-only observers. They subscribe to C# events on `Health`,
`BossPhaseMachine`, and `GameStateMachine`, and hold no references back into
gameplay. Gameplay code contains no reference to any UI type, which means the
fight is fully functional in a scene with no canvas — useful when testing.

---

## 10. Shader, VFX, and feel

### Shader

One Shader Graph, `SpriteEffects`, applied to the boss and player sprites,
exposing three properties:

| Property | Use |
|---|---|
| `_FlashAmount` | White-out on hit; driven by a short tween |
| `_PhaseTint` | Colour shift as phases escalate |
| `_DissolveAmount` | Death dissolve, driven over ~1s on defeat |

Serving three effects from one shader keeps the material count low and the
graph comprehensible, rather than three near-identical graphs.

### VFX

Built-in Particle System (Shuriken) throughout: muzzle flash on fire, impact
burst on projectile hit, a radial burst on phase transition, and a death
explosion.

**Deliberately not VFX Graph.** VFX Graph requires compute shader support, which
**WebGL does not provide**. Effects built in it would silently do nothing in the
submitted build.

### Feel

The 10% for juice is cheap to earn and worth doing last, once the fight works.

- **Hit stop** — a very brief freeze on impactful hits, run on unscaled time so
  it cannot deadlock itself.
- **Camera shake** — small on player hits, large on boss phase change and death.
- **Damage flash** — via `_FlashAmount` above.
- **Squash and stretch** on jump and land, via tween.

---

## 11. Third-party dependencies

| Package | Purpose |
|---|---|
| DOTween (Demigiant, free) | All tweening: UI, telegraphs, health bar, fades |

Installed from the Asset Store into `Assets/Plugins/Demigiant` and committed, so
the repository clones and builds without extra setup steps.

**A note on which half of DOTween is reachable.** `DOTween.dll` is a precompiled
assembly and is auto-referenced by `BossLevel.Runtime`, so the core API —
`DOTween.To`, `DOTween.Sequence`, `Tween`, `Sequence` — is always available.
DOTween's *shortcut extensions* (`DOColor`, `DOScale`, `DOFade`, `DOAnchorPos`)
are shipped as loose scripts under `Assets/Plugins`, which Unity compiles into a
predefined assembly; an assembly definition cannot reference predefined
assemblies, so those shortcuts are invisible to project code.

Tween code therefore uses `DOTween.To(getter, setter, endValue, duration)`. The
alternative — generating `DOTween.Modules.asmdef` from DOTween's utility panel
and referencing it — is worth taking before the UI work, where the shortcuts are
most valuable.

---

## 12. Coding conventions

Stated here so they are visibly intentional rather than incidental.

- **Naming.** `PascalCase` for types, methods, properties, and constants;
  `_camelCase` for private fields; `camelCase` for locals and parameters. Names
  say what a thing *is* or *does* — no abbreviations that need decoding.
- **Fields.** `[SerializeField] private` rather than `public`. Inspector-visible
  without breaking encapsulation.
- **Documentation.** An XML `<summary>` on every public type and on any member
  whose purpose is not obvious from its name. Comments explain *why*, not *what*
  — the code already says what.
- **File size.** One public type per file, named for the file. A file past
  roughly 200 lines is a signal to split.
- **Magic numbers.** Tunable values are serialized fields or ScriptableObject
  data. Genuinely fixed values are named constants.
- **Update loops.** Physics in `FixedUpdate`, input edges and rendering in
  `Update`. No physics writes from `Update`.
- **Null and lifetime.** Cached component references resolved in `Awake`;
  subscriptions in `OnEnable`, unsubscriptions in `OnDisable`, always paired.

---

## 13. Testing and editor tooling

### Unit tests (EditMode)

The genuinely rule-based logic is written as plain C# so it can be tested
without entering play mode:

| Suite | Asserts |
|---|---|
| `AttackSelectorTests` | No immediate repeats; bag refills correctly; every attack in a phase is eventually drawn |
| `PhaseMachineTests` | Correct phase at each boundary; **no phase is skipped when a single large hit crosses two thresholds**; transitions fire exactly once |
| `HealthTests` | Damage clamps at zero; death event fires once and only once; damage after death is ignored |

The phase-skip case is the one most likely to ship broken, which is precisely
why it is written down as a test rather than trusted to play-testing.

### Editor tooling

Because this project is authored on a machine separate from the Unity editor,
editor-side setup is automated rather than performed by hand:

```
Boss Level ▸ 1. Configure Project      layers + physics collision matrix
Boss Level ▸ 2. Generate Data Assets   boss, phase, and attack assets, tuned
Boss Level ▸ 3. Build Test Scene       arena, platforms, player, boss
```

This is re-runnable, self-documenting, and removes a long manual checklist from
the setup path.

---

## 14. Build

**Target: WebGL.** Chosen over mobile because it earns the same 10% while
keeping keyboard input — a mobile build would require designing touch controls
for move, jump, drop-through, and shoot, which the rubric does not reward.

Constraints carried through the design:

- No VFX Graph and no compute shaders (unsupported on WebGL).
- Compression set to Gzip or Brotli, with the hosting server configured to match.
- Texture sizes and audio import settings kept modest to hold download size down.
- Build output hosted via GitHub Pages so the submission can include a live
  playable link alongside the repository.

---

## 15. Rewrites of existing starter code

The repository's pre-existing scripts are boilerplate carried between projects.
Each is replaced, for reasons recorded here.

**`MonoPool<T>` → `Pool<T>` + `ProjectilePool`.** The starter pool was a generic
`MonoBehaviour`. Unity cannot attach generic MonoBehaviours through the
Inspector, so its serialized `prefab`, `poolParent`, and `initialPoolSize`
fields could never be assigned — and the singleton base's `AddComponent`
fallback would have produced a pool with a null prefab that throws on first use.

The replacement splits the generic from the component: `Pool<T>` is a **plain C#
generic class** with no Unity component involvement, owned by a small concrete
`MonoBehaviour` (`ProjectilePool`) that holds the serialized configuration. The
generic-component problem disappears entirely.

`Pool<T>` is still not *pure* C# — it calls `Object.Instantiate` and toggles
GameObject activation — so testing it needs a real component, supplied by the
`BossLevel.TestSupport` assembly. It could be made engine-free by taking a
factory delegate and moving activation into `IPoolable`, but that trades an
indirection the reader must follow for purity the project does not need.

**`IPoolable.Reset()` → `OnSpawn()` / `OnDespawn()`.** `MonoBehaviour` already
defines `Reset()`, which Unity invokes **in the editor** when a component is
added or reset from the context menu. Any pooled MonoBehaviour implementing the
old interface would have had its pool-reset logic fired at authoring time. The
two-method replacement removes the collision and gives a clean place to stop
coroutines and clear trails on return.

**`MonoSingleton<T>` → `PersistentSingleton<T>`.** The starter used
`FindObjectOfType`, deprecated in Unity 6 in favour of `FindFirstObjectByType`.
Its `protected` constructor was documented as preventing extra instances but
does nothing — Unity creates MonoBehaviours via `AddComponent`, never via a
constructor — so real duplicate handling moves into `Awake`. The
create-on-demand path in the property getter is removed: services are created
explicitly by `GameBootstrap`, which gives them one creation site and one
lifetime instead of appearing wherever they are first touched.

Singleton use is also narrowed. Only genuinely global, persistent services
(`SceneLoader`) are singletons. Pools and gameplay objects are referenced
through the Inspector, which keeps their dependencies visible.

**`PlayerMovement` → `PlayerInputReader` + `PlayerMotor` + `PlayerShooter`.**
Rationale in §6: physics moved out of `Update`, velocity-set jumping instead of
impulse, feel affordances added, input centralised, and the ground check's
`groundCheck.position - groundCheck.localScale / 2` — which mixes a world
position with a local scale and breaks if the player is rescaled — replaced with
an explicit serialized offset and radius.

**`PassThroughPlatform` → deleted, its job absorbed by `PlayerMotor`.**
Rationale in §6: per-pair `IgnoreCollision` instead of disabling the collider
globally, position-based restore instead of a fixed timer, and input read once
on the player instead of once per platform per frame. No replacement component
is needed, because the `PlatformEffector2D` already identifies a droppable
surface.

---

## 16. Build order

Each milestone ends in a playable state, so there is always something to
demonstrate and always a small surface to debug.

| # | Milestone | Done when |
|---|---|---|
| 1 | Foundation | Project restructured, conventions in place, assemblies and tests running |
| 2 | Player | Move, jump, drop-through feel good in an empty greybox arena |
| 3 | Combat loop | Pooled projectile damages a dummy target; health bar responds |
| 4 | Boss skeleton | One hardcoded attack running the full telegraph → active → recovery → idle loop |
| 5 | Data layer | That attack converted to a ScriptableObject; attacks 2–5 authored as assets |
| 6 | Phases | Thresholds, transition sequence, shuffle-bag selection, win and lose |
| 7 | Shell | Bootstrap, loading, menu, end screens |
| 8 | Polish | Shader, VFX, hit stop, shake, tween pass |
| 9 | Build | WebGL build, hosting, final documentation pass |

Milestone 5 deliberately follows milestone 4: the data abstraction is built
*after* one concrete attack exists, so it is shaped by a real case rather than
by a guess about one.

Milestone 8 is the one most likely to be skipped, because by then the fight
works and polish can feel like it is not progress. It is 10% of the grade and
most of the perceived quality.

---

## 17. Open questions

- The assignment describes the scope as "Survivor.io boss phase." This project
  reads that as *scale of deliverable* rather than *genre to copy*, and
  implements a Cuphead-style side-scrolling encounter, which satisfies every
  listed requirement including "player mechanic to control." To be confirmed
  with the instructor.
