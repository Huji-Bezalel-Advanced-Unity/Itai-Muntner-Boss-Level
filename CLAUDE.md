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

**Milestone 7 (Shell) — written, not yet compiled.** Ten files.

`App/`: `SceneId` (enum), `SceneCatalog` (SO mapping id → scene name),
`SceneLoader` (persistent singleton, async load with held activation and a
minimum display time), `GameBootstrap`.

`UI/`: `LoadingScreen`, `MainMenu`, `BossHealthBar` (fill + delayed chip),
`PlayerHealthView` (discrete hearts, one per hit point), `PhaseBanner`,
`EndScreen`.

`BossLevel.Runtime.asmdef` gained `UnityEngine.UI` and `Unity.TextMeshPro`
references. **These are the most likely compile failure in this drop** — if
either assembly name is wrong the whole assembly fails.

UI components hide themselves via `CanvasGroup` alpha rather than by
deactivating the GameObject, so their `Awake` is guaranteed to have run before
anything asks them to show. Deactivated-at-start UI is a recurring lifecycle
trap; do not "fix" it by unticking the object.

**Scenes, canvases and the catalog asset do not exist yet** — this milestone is
mostly editor work for the user. Bootstrap and MainMenu scenes must be created
and added to Build Settings alongside BossLevel.

**Boss AI and balance pass — written, not yet compiled.** Prompted by play
testing: the fight ended in seconds, the boss's attacks almost always missed, and
standing still while holding fire was therefore the strongest strategy.

- `Combat/ITarget` — position, velocity, footing. `PlayerMotor` implements it;
  the boss resolves it via `GetComponent<ITarget>()` on the player transform, so
  Boss does not depend on the Player type.
- `BossContext` — predictive aiming (`AimPoint`, `AimAngle`), `TargetMobility`,
  `TargetIsGrounded`, and a settable `AimLead` driven from the active phase.
- `BossAttack.Suitability(context)` — each attack scores how well it fits the
  current situation. `AttackSelector.Next(context)` draws two candidates and
  uses the better, returning the other to the bag so variety survives.
- `BossPhase.AimLead` — escalation now includes the boss aiming better, not just
  faster. **Existing phase assets deserialise this as 0** (no lead); it must be
  set by hand.
- Fixes: loading bar floors progress on elapsed time so it animates on instant
  loads; quit button exits play mode in the editor; `BossDefinition.OnValidate`
  warns on non-descending thresholds.
- `AttackSuitabilityTests` (6) and `StubTarget` in TestSupport.

**Cover pass — done.** Line of sight, and an attack shape that does not travel.

**Platforms removed, dash and minions added — written, not yet compiled.** The
user removed the platforms entirely (they were total cover against every attack)
and asked for a double jump, a dash, minions, and a volcano-style eruption.

- **Platforms and all drop-through code are gone.** `PlayerInputReader` no
  longer reads `Crouch`; dash is bound to the template's **`Sprint`** action
  (Left Shift).
- `PlayerMotor` gained `airJumps` (double jump) and a dash: short, unsteerable,
  one per trip through the air, granting invulnerability. The file is over the
  200-line guideline on purpose — running, jumping and dashing all write the
  same rigidbody in the same fixed step, and splitting them would mean separate
  components racing to set velocity.
- **`Health` invulnerability is now counted, not a bool.** `HoldInvulnerability`
  / `ReleaseInvulnerability`. The dash and the hit frames can both be active,
  and with a flag whichever ended first stripped the other's protection.
  `IsInvulnerable` is now read-only — do not reintroduce a setter.
- `Combat/VolcanoHazard` + `VolcanoPool` **replace** `GroundHazard` +
  `HazardPool`. Ground marker warns for 2s+, then a column rises and damages
  only the part that has actually risen.
- `Combat/Minion` + `MinionPool` — slow pursuers with their own `Health`, which
  burst on contact. `MinionPool.ActiveCount` feeds the summon attack's
  suitability so the arena fills to a pressure and stops.
- `Boss/Attacks/SummonMinionsAttack`; `EruptionAttack` rewritten for vents.
- `BossContext` constructor now takes `VolcanoPool` and `MinionPool`.

Three concrete pools (`ProjectilePool`, `VolcanoPool`, `MinionPool`) duplicate
each other's shape deliberately — a shared generic base cannot be a
MonoBehaviour, so it would have to be non-generic and cast at every call site.

**Milestone 8 (Polish) — written, not yet compiled.**

- `Shaders/SpriteEffects.shader` — hand-written URP 2D unlit sprite shader
  (`LightMode = Universal2D`) with `_FlashAmount`/`_FlashColour`,
  `_TintAmount`/`_TintColour`, `_PhaseTint`, `_DissolveAmount`. Procedural value
  noise for the dissolve, so no texture asset is required. **Hand-written HLSL,
  not Shader Graph** — a `.shadergraph` is generated JSON and cannot be authored
  from here or reviewed by a grader.
- `Feel/SpriteEffects` — the only writer of those properties, via
  `MaterialPropertyBlock`. **If the properties appear to do nothing in play, the
  fallback is `_renderer.material` instead of a property block** (SRP Batcher
  interaction).
- `Feel/HitStop`, `Feel/CameraShake`, `Feel/VfxBurst`, `Feel/VfxPool`.
- `Feel/DamageFeedback` **replaces `SpriteFlash`** — flash, hit stop, shake and
  VFX burst, all driven from `Health.Damaged`. Everything but the flash is
  optional so the same component suits the boss and a minion.
- `Feel/DeathDissolve` — shake, burst, then dissolve on `Health.Died`.
- `BossTelegraph` now tints through `SpriteEffects` instead of
  `SpriteRenderer.color`, **which resolves the long-standing conflict** where a
  hit during a wind-up was overwritten and never showed.
- `BossPhase.Tint` (with `OnValidate` repair, since Unity zero-fills it to
  transparent) applied by `BossController`, plus a hard camera shake on phase
  change.

All feel tweens use `SetUpdate(true)`; on scaled time they would freeze during
the hit stop they accompany.

Two follow-up fixes after play testing:

- **Never declare `_MainTex_ST` (or `_TexelSize`) in `UnityPerMaterial`** in a 2D
  sprite shader. Sprite scale/offset is per-renderer data, and declaring it makes
  the material incompatible with the 2D SRP Batcher — Unity warns and disables
  batching for every renderer using it. Sprite UVs arrive correct, so there is
  nothing to transform.
- **`VfxBurst` configures its entire particle system in `Awake`** rather than
  trusting the prefab. Particle System defaults suit 3D — the default Cone shape
  fires along +Z, into the screen — so an unconfigured system in a 2D scene looks
  broken rather than plain. It still needs a **material assigned**, because
  Unity's built-in particle material renders magenta under URP; it logs a warning
  if that is missing.

**Milestone 9 (Build) — written, not yet run.** The build order is complete.

- `Scripts/Editor/BossLevel.Editor.asmdef` + `WebGlBuildTool` —
  *Boss Level ▸ Build WebGL* and *Boss Level ▸ Apply WebGL Settings*. Validates
  that `Bootstrap` is the first enabled scene, applies the player settings,
  builds into `docs/`, and writes `.nojekyll`.
- **Gzip + `decompressionFallback = true` is the critical setting.** GitHub Pages
  cannot send the `Content-Encoding` header a compressed Unity build otherwise
  needs, so without the fallback the build works locally and fails once hosted.
- `README.md` written (the empty extensionless `README` is gone); design doc
  brought in line with what was actually built.

Remaining for the user: run the build, commit `docs/`, enable Pages
(Settings ▸ Pages ▸ this branch, `/docs`), and paste the link into `README.md`.

**Audio and presentation pass — written, not yet compiled.** Requested after
Milestone 9 was drafted; the build is still the last step.

- `Audio/SoundEvent` (SO: clips, volume, pitch range, spatial blend, minimum
  interval), `Audio/SoundEmitter` (pooled `AudioSource`), `Audio/AudioService`
  (`PersistentSingleton`, `Pool<SoundEmitter>`, crossfading music),
  `Audio/SceneMusic` (per-scene track request).
- **The minimum-interval timestamp lives in the service, not on the asset** —
  interval is config, last-played is state. Same rule as `BossAttack`.
- Sound hooks: `PlayerShooter` (shoot), `PlayerMotor` (jump, dash),
  `DamageFeedback` (hit), `ButtonFeedback` (hover, click).
- `UI/ButtonFeedback` — hover scale, press punch, sounds. `UI/SceneFadeIn` —
  in-scene fade from black so a scene opens the same way however it was entered,
  including the direct-play retry fallback.
- `EndScreen` gained a panel scale-in alongside its fade.
- `MinionPool` now raises `Spawned` / `Despawned` **events**; `Feel/MinionFeedback`
  listens and plays bursts and sounds. Done as events specifically to avoid
  `Combat` depending on `Feel`, which already depends on `Combat`.
- `Minion` scales up on spawn (`OnSpawn`) and restores scale on despawn.

`AudioService` must go in the **Bootstrap** scene beside `SceneLoader`.
`AudioService.Awake` returns early if `Instance != this`, so a duplicate does not
build a pool it will never use.

**Sustained fire and boss attack sounds — written, not yet compiled.**

- `Audio/LoopingSound` — owns its own `AudioSource` (a pooled emitter
  self-releases on a timer, wrong for a held sound). Fades in and out because
  starting or cutting a loop abruptly clicks. `IsPlaying` tracks *intent*, not
  `AudioSource.isPlaying`, which stays true during a fade-out.
- `PlayerShooter` plays `singleShotSound` per shot while tapping and hands over
  to `sustainedFire` after `sustainDelay`, silencing the per-shot clip. Stops the
  loop in `OnDisable`. The old `shootSound` field is preserved via
  `[FormerlySerializedAs]`, so the existing assignment survives the rename.
- `BossAttack` gained `TelegraphSound` and `AttackSound`, played by
  `BossController` alongside the telegraph — stated once so a later attack cannot
  omit them.
- `BossController` gained `phaseChangeSound` and `defeatedSound`;
  `VolcanoHazard` gained `warningSound` and `eruptSound`.
- `AudioService` exposes `EffectsVolume` and `EffectsGroup` so self-owned
  sources mix alongside pooled ones.

**Outcome audio and a fluid eruption — written, not yet compiled.**

- `Audio/OutcomeAudio` — listens to `GameStateMachine.StateChanged`. Victory
  stops the music and plays a sound into the silence; defeat swaps in its own
  music. Separate from `EndScreen` so sound and drawing stay independent.
- `AudioService.PlayMusic(clip, restartIfAlreadyPlaying)`; `SceneMusic` exposes
  the flag, defaulting to **true** so a retry opens the fight's theme from the
  top.
- `VolcanoHazard` reworked: `eruptionDuration` plus **three `AnimationCurve`s**
  (height, width, alpha) replace `riseDuration`/`activeDuration`, with sway and
  tilt. **Existing prefab values for the old fields are gone and need retuning.**
  `OnValidate` fills empty curves — a newly added `AnimationCurve` deserialises
  with no keys and evaluates to zero, which would render the column invisible.
- `VolcanoPool` raises `Opened` / `Erupted`; `Feel/VolcanoFeedback` plays bursts
  and a camera shake. Events again, because a prefab cannot reference a scene
  object.

**Pause menu and two bug fixes — written, not yet compiled.**

- **`SpriteEffects` now calls `GetPropertyBlock` before every `SetPropertyBlock`.**
  A `SpriteRenderer` supplies its sprite texture through its own property block,
  and `SetPropertyBlock` *replaces* rather than merges — so writing without
  reading first discarded the texture binding and rendered an untextured quad.
  Symptom was minion sprites flickering between their real shape and a rectangle.
  **Never call `SetPropertyBlock` on a `SpriteRenderer` without reading first.**
- `AudioService` tracks playing emitters and calls `StopAllEffects()` on
  `SceneManager.sceneLoaded`. The service outlives every scene, so a long
  one-shot otherwise followed the player into the next screen.
- `OutcomeAudio` gained `victoryMusic` — a full theme belongs in the music slot,
  which the next scene crossfades away; a sound effect plays to its end regardless.
- `UI/PauseMenu` — P or Escape, `Time.timeScale = 0`, continue / restart / quit.
  Also disables `PlayerInputReader` (a zero time scale does not stop `Update`, so
  a dash started while paused would never end) and calls the new `HitStop.Cancel()`
  (a freeze finishing would restore time and un-pause the game by itself).
  Restores the time scale before any scene load, or the next scene opens frozen.
  Reads the keyboard directly — the one deliberate exception to routing input
  through `PlayerInputReader`, since pause must work when that is disabled.
