# Boss Level — Design Document

**Author:** Itai Muntner
**Course:** Advanced Unity — Final Assignment
**Engine:** Unity 6000.0.41f1 (Unity 6 LTS), Universal Render Pipeline 17.0.4, 2D
**Target build:** WebGL

---

## 1. Concept

A single-screen boss fight in the style of *Cuphead*'s ground battles. The player
occupies the left two-thirds of a fixed, non-scrolling arena and fights a large
boss anchored on the right. The player can run, jump, double jump, dash through
damage, and fire a projectile. The boss cycles through telegraphed attacks drawn
from a pool that grows in size and severity across three phases, each phase
triggered by the boss's remaining health.

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
| Shader | One hand-written HLSL shader serving hit flash, telegraph tint, phase tint and death dissolve | §10 |
| VFX | Built-in Particle System bursts on fire, impact, phase change, death | §10 |
| Tweens | DOTween across UI transitions, health bar drain, telegraphs, fades | §9, §11 |
| Audio | `SoundEvent` assets, pooled emitters, crossfading music service | §10 |
| Player mechanic to control | Run, jump (variable height), double jump, dash with invulnerability, shoot | §6 |
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
| `BossLevel.Editor` | Editor-only build tooling (§13) |
| `BossLevel.Tests` | EditMode unit tests |
| `BossLevel.TestSupport` | Test doubles, excluded from builds |

The split exists for one concrete reason: an assembly definition cannot
reference Unity's predefined `Assembly-CSharp`, so unit-testing any gameplay
code requires the gameplay code to live in its own assembly. It is not an
attempt at layered architecture — at this scope that would be ceremony.

`BossLevel.TestSupport` exists because of a narrower rule. A test assembly is
editor-only, and Unity refuses to `AddComponent` a type from an editor
assembly — so any test double that must be a real component cannot live beside
the tests. It is a normal runtime assembly kept out of player builds by the
`UNITY_INCLUDE_TESTS` define constraint instead, which keeps test-only types out
of `BossLevel.Runtime` without making them editor scripts.

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
│   ├── Combat/                  IDamageable, ITarget, Health, Projectile,
│   │                            ProjectilePool, VolcanoHazard, VolcanoPool,
│   │                            Minion, MinionPool
│   ├── UI/                      BossHealthBar, PlayerHealthView,
│   │                            PhaseBanner, EndScreen, LoadingScreen
│   ├── Audio/                   SoundEvent, SoundEmitter, AudioService,
│   │                            SceneMusic
│   ├── Feel/                    SpriteEffects, DamageFeedback, DeathDissolve,
│   │                            MinionFeedback, HitStop, CameraShake,
│   │                            VfxBurst, VfxPool
│   ├── Common/                  Pool, IPoolable, PersistentSingleton
│   └── Editor/                  WebGlBuildTool
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

### Migration from the starter project

The repository began as boilerplate under `Assets/BBB/` — `MonoSingleton`,
`MonoPool`, `IPoolable`, `PlayerMovement` and `PassThroughPlatform` — carried
over from earlier projects. The concepts were kept and every implementation
replaced; §15 records why, one file at a time.

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
| `PlayerMotor` | Translates intent into physics. Owns grounding, jumping, double jumping and dashing. |
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

### Dash and double jump

The player has two escapes beyond running: a **double jump**, and a **dash** —
a short, fast, uncontrollable burst that passes through damage.

The dash is the more important of the two. Without it the only defensive skill
in the fight is being somewhere else in advance, so a well-read telegraph is
worth nothing more than a badly-read one, and retreating is always the safest
play. A dash with invulnerability makes reading a telegraph *correctly* pay:
the player can hold ground, take the risk, and come out the far side of an
attack that positioning alone could not have answered.

It is deliberately short and cannot be steered once started. Committing to it
has to be a decision rather than a means of travel, and one dash per trip
through the air stops it being chained into flight.

**Invulnerability is counted, not flagged.** Two systems want it at once — the
dash, and the frames granted by being hit — and neither knows about the other.
With a boolean, whichever finished first would strip the protection the other
was still relying on, producing a rare unfair death that is almost impossible to
reproduce deliberately. `Health` therefore counts holders, and both systems
hold and release rather than set and clear.

### Platforms: removed

The arena had one-way platforms, using `PlatformEffector2D` for
jump-through-from-below and per-pair `IgnoreCollision` on the player for
dropping down.

They were removed after play testing, and the reason is worth recording. Every
attack the boss had travelled from the boss to the player, so a platform was
total cover against the entire fight — a player who stood behind one could not
be hit by anything. That is a failure of attack *variety* rather than of
placement, and it was addressed twice over: with the volcanic vents (§7), which
do not travel at all, and by removing the geometry that made hiding possible.

What the platforms contributed to mobility is now covered by the double jump and
the dash, which are answers the player controls rather than terrain that happens
to be in the right place.

## 7. Boss

### Two nested state machines

**Phase machine (outer).** Three phases, selected by remaining health
percentage. Defaults: Phase 1 above 66%, Phase 2 from 66% to 33%, Phase 3 below
33%. Thresholds are data on the phase assets, not constants in code.

When health crosses a threshold, the boss **does not interrupt its current
attack**. It finishes, then runs a transition sequence:

```
boss becomes invulnerable
phase-change VFX + shader tint + camera shake
PhaseBanner announces the new phase        (DOTween)
brief pause
resume with the new phase's attack set
```

**Whatever is already in the arena stays there.** Clearing it would make a
transition a guaranteed moment of safety; leaving it means the player still has
to survive the previous phase's parting shots during a window where the boss
cannot be punished for them. The arena is only swept when the fight *ends* —
there, a stray shot from a boss that is already dead would turn a win into a
draw.

The first phase is announced the same way as the rest, when the fight opens.
Phase one is where the fight *starts* rather than something it changes into, so
without that the player would be told about every phase except the one they
begin in.

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
| Telegraph | The attack's **own** tell; no damage yet | The fairness contract — the player must be able to react |
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

### Each attack telegraphs as itself

The controller owns *when* the warning happens; the attack owns *what it looks
like*, carried as a `TelegraphCue` — colour, pulse count, per-axis swell, and
shudder strength.

This matters more than it first appears. A single shared tell for every attack
reduces the telegraph to a countdown: the player learns that *something* is
coming but not what, so the only available response is to keep moving and hope.
Distinct tells turn the warning into information — the player can begin moving
in the right direction before the first projectile exists. That is the
difference between a fight that is hard and one that is merely fast.

The **phase transition uses a cue deliberately unlike any attack's**: white
rather than a hue, several rapid pulses rather than one swell, and a shudder. A
change of rules that looks like another wind-up is not read as a change of rules
at all.

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

### What makes the boss look intelligent

A boss that cycles attacks at random and fires at where the player currently
stands is not a fight — it is a sprinkler. Every shot misses a moving player,
so the strongest strategy becomes standing still and trading damage, which is
the least interesting thing the player can do. Two mechanisms fix that, and
neither is a difficulty number.

**Leading the target.** `BossContext` estimates where the player will be when a
shot arrives: guess the flight time from the current distance, move the player
by it, re-measure, twice. The `AimLead` value blends between the player's
present position and that prediction, and it is **set per phase** — an early
phase aims sloppily and can be walked away from, a late phase aims where the
player is going. That makes the boss visibly learn to read the player rather
than merely firing faster, which is a far more interesting form of escalation.

**Judging the situation.** Each attack answers `Suitability(context)` from 0 to
1, given what the player is doing right now. The selector draws two candidates
from the shuffle bag and uses the better one, returning the other to the bag so
variety is preserved.

| Situation | What the boss reaches for |
|---|---|
| Standing still, trading shots | `AimedBurst`, `Rain` — pinpoint, punishing |
| Running | `SpreadShot` — covers the escape routes |
| Airborne | `Sweep` — a committed arc cannot be steered out of |
| Grounded | `Slam` — and never while the player is already in the air |

The boss reads the player through an `ITarget` interface exposing position,
velocity and footing, rather than referencing the player type — so the boss
depends on what a target *exposes*, not on how the player is built.

The important property is that **the answer to camping is no longer "the boss
happens to miss"**. It is that camping invites the attacks that do not miss.

### Cover, and the attack shape that defeats it

Play testing found a second, structural gap: every attack listed above travels
*from the boss to the player*, so a single platform in between defeats all of
them at once. A player who found that spot was safe from the entire fight.

Adding more projectile attacks would not have helped — they share the flaw. Two
different **shapes** were added instead, and the platforms were removed as well
(§6).

**`EruptionAttack`** opens volcanic vents on the ground beneath the player,
which erupt straight upwards after their own long warning. A vent does not
travel, so distance and geometry are both irrelevant to it, and a column going
straight up cannot be jumped over — it asks whether the player will give up the
ground they are standing on, which is the question worth asking of someone who
has settled somewhere they like. The warning is over two seconds, far more than
any projectile gives, which is what makes an otherwise undodgeable attack fair:
the player is never surprised by it, only caught still by it.

`VolcanoHazard` resolves damage by repeated overlap queries against the part of
the column that has actually risen, rather than by a trigger collider. A collider
would have to cope with the player already standing inside it when it activates —
the normal case here — and enter events do not fire for something already there.
Checking only the risen part also makes the column read as travelling upwards
rather than simply appearing.

The eruption's motion is driven by **three animation curves** — height, width and
opacity across the eruption — rather than by a single linear scale. A column that
grows and vanishes at a constant rate reads as a rectangle being resized, because
fire does not move at a constant speed. Bursting past full height and falling
back, flaring wide at the base before narrowing, swaying with a slight lean, and
guttering out is what makes the same primitive read as something alive. Being
curves, all of it is tunable by eye rather than by editing constants.

**`SummonMinionsAttack`** is the only attack that leaves something behind.
Everything else resolves and is gone, so the fight is a series of separate
problems; minions turn it into a situation the player is managing, because
ignoring one costs them the arena a piece at a time. It is also the only attack
that competes for the player's *shots* — time spent clearing minions is time the
boss is not taking damage, which is a more interesting cost than simply dealing
more damage would be. The boss reads how many are already alive and stops
summoning once the arena is busy, because a crowd is noise rather than pressure.

**Line of sight** survives both: `BossContext.LineOfSightFactor` discounts any
attack that has to cross the arena when something solid is in the way, so the
boss does not empty its repertoire into an obstacle. With the platforms gone it
rarely triggers, but it is what keeps the mechanism correct if cover is ever
reintroduced.

`SlamAttack` additionally checks the player's *height*, not just whether their
feet are down: being airborne or standing on anything raised puts them off the
floor the shockwave travels along.

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
| `EruptionAttack` | Volcanic vents that open underfoot and erupt straight up |
| `SummonMinionsAttack` | Small enemies that hunt the player until killed |

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
| `BossHealthBar` | Tweened fill plus a delayed "chip" bar trailing behind it, so the size of each hit is legible rather than merely its result. Phase boundaries are shown by static marker images placed at the thresholds — the bar itself stays one continuous fill, which is simpler than three segments and reads the same. |
| `PlayerHealthView` | Discrete hearts; punch-scale tween on loss. |
| `PhaseBanner` | Slides in on phase change, holds, slides out. |
| `EndScreen` | Win and lose variants; staggered fade-in, retry button. |
| `LoadingScreen` | Progress bar driven by `SceneLoader`; fade in and out. |
| `PauseMenu` | Freezes the fight on P or Escape; continue, restart, quit. |

All views are read-only observers. They subscribe to C# events on `Health`,
`BossPhaseMachine`, and `GameStateMachine`, and hold no references back into
gameplay. Gameplay code contains no reference to any UI type, which means the
fight is fully functional in a scene with no canvas — useful when testing.

`PauseMenu` is the exception that proves the rule, because pausing is not a view
of anything — it acts. It sets the time scale to zero, which stops everything at
once: physics, coroutines, projectiles mid-flight, a vent halfway through its
warning. That works only because **all of gameplay runs on scaled time and all of
the interface does not** — the same division that lets a damage flash animate
through a hit stop.

Two details there are load-bearing:

- **Player input is switched off as well as frozen.** A zero time scale does not
  stop `Update` running, so a dash begun while paused would start and never reach
  its end time, stranding the player mid-dash on resume.
- **`HitStop` is told to stand down before pausing.** It owns the time scale
  while it runs and restores it on an *unscaled* wait, so a freeze in progress
  would set time back to one and quietly un-pause the game — intermittently,
  which is the worst kind of bug to be handed.

Pause input is read directly rather than through `PlayerInputReader`, the one
deliberate exception to routing input through a single place: pausing has to keep
working exactly when gameplay input has been taken away, so it cannot depend on
the component that gets disabled to take it away.

---

## 10. Shader, VFX, audio, and feel

### Shader

One shader, `Boss Level/Sprite Effects`, applied to the boss and player sprites:

| Property | Use |
|---|---|
| `_FlashAmount` / `_FlashColour` | White-out on a hit; driven by a short tween |
| `_TintAmount` / `_TintColour` | The attack wind-up tell |
| `_PhaseTint` | Persistent colour shift as the phases escalate |
| `_DissolveAmount` | Death, burned away over about a second |

**Serving all of them from one shader is what makes them compose.** Before this
existed, the damage flash and the attack telegraph both wrote
`SpriteRenderer.color`, so whichever ran second erased the other and a hit
landing during a wind-up did not read at all. As separate properties applied in
a defined order — phase tint, then telegraph, then flash — the boss can flash
white while still glowing with whatever it is about to do.

**Written by hand in HLSL rather than in Shader Graph.** A `.shadergraph` file
is generated JSON: it cannot be read, reviewed, or sensibly diffed, and on a
project graded largely on readability that is a poor trade for a effect that
fits on one page. The dissolve uses procedural value noise rather than a texture,
so it depends on no authored asset that could go missing.

`Feel/SpriteEffects` is the only thing that writes those properties, and it does
so through a `MaterialPropertyBlock` so each sprite holds its own values without
instantiating a copy of the material.

### VFX

Built-in Particle System (Shuriken) throughout: muzzle flash on fire, impact
burst on projectile hit, a radial burst on phase transition, and a death
explosion.

**Deliberately not VFX Graph.** VFX Graph requires compute shader support, which
**WebGL does not provide**. Effects built in it would silently do nothing in the
submitted build.

### Audio

Audio reuses three patterns the project already leans on rather than inventing a
fourth, which is most of why it is small.

**`SoundEvent` is a ScriptableObject**, exactly as `BossAttack` is. A sound is
mostly numbers — which clip, how loud, how far the pitch wanders — and those want
tuning while the game runs. It also puts a layer between the code and the clip,
so replacing a placeholder with a real recording is a change to one asset rather
than to whichever component happened to reference it.

A `SoundEvent` can hold **several clips and a pitch range**, and picks between
them. Repetition is what makes game audio grating, and a weapon firing four times
a second is this project's worst offender; random selection and a little pitch
wander is most of the cure.

**`AudioService` is a `PersistentSingleton`**, like `SceneLoader`, because music
has to survive the scene changes it plays across — a track restarting every time
the player pressed retry would make the game feel like it was stuttering rather
than continuing. Requesting a track already playing is a no-op, so the menu and
the fight can share one without it ever restarting.

**Sound effects use `Pool<T>`** — the same pool as projectiles, minions and
particles. Sounds arrive in bursts at the busiest moments, which is exactly when
allocating an `AudioSource` per shot is least affordable.

Two details worth their lines. A `SoundEvent` carries a **minimum interval**, but
the record of when it last played lives in the service, not on the asset — the
interval is configuration and the timestamp is runtime state, and the rule about
ScriptableObjects never holding state applies as much here as anywhere. And every
duration in the audio layer uses **unscaled time**, because audio is unaffected by
`Time.timeScale`, so a hit stop must not hold an emitter that has already finished
playing.

Call sites read as `jumpSound.Play()` rather than as a call into the service, so
they say *what* should be heard and not how it gets played. When no service
exists — a gameplay scene played directly rather than through Bootstrap — sounds
are silently skipped rather than logging, which would otherwise drown the console
on every shot.

**`LoopingSound` is the counterpart** for anything held rather than fired once.
A pooled emitter releases itself on a timer, which is exactly wrong for a sound
that should last an unknown length of time, so a loop owns its own
`AudioSource`. Its fades are not decoration: starting a looping clip at full
volume clicks and cutting it dead clicks louder, and for something that starts
and stops several times a second while the player taps, that is the difference
between a weapon and a fault.

Firing uses both, because firing sounds like two different things. A tap is a
single shot; a held trigger is a continuous roar that retriggering one short clip
cannot imitate — overlapping copies of the same sample phase against each other
and become a rattle. So `PlayerShooter` plays the single-shot clip per shot while
tapping, and hands over to the loop once the button has been held past a brief
threshold, silencing the per-shot clip while it does.

**Attacks carry their own sounds**, played by `BossController` alongside the
telegraph for the same reason the timing lives there: stated once, an attack
written later cannot forget it. A distinct sound per attack is worth more than
the visual tell in one specific way — the player can be looking anywhere, and
sound reaches them regardless. That matters most for the attacks that do not
originate at the boss, which is why the volcanic vent carries its own rumble as
well.

Each attack has **two** slots, and the distinction matters: `TelegraphSound`
plays as the wind-up begins and is the *warning*, while `AttackSound` plays as
the attack lands and is the *event*. An anticipation sound in the second slot
arrives too late to be acted on, and an impact in the first gives the wrong
information entirely.

**The two endings sound deliberately opposite.** `OutcomeAudio` cuts the music on
victory and lets one sound land in the silence, which is far more emphatic than
layering it over a track still playing; defeat replaces the music instead,
because a loss should sit with the player rather than being punctuated and
released. Retrying restarts the fight's own theme from the top, so an attempt
never opens halfway through the previous one.

**A full theme belongs in the music slot, not the sound one.** Music is a single
source the next scene crossfades away; a sound effect is fire-and-forget and
plays to its end wherever the player has got to by then. The service also cuts
every effect still playing when a scene loads, because a sound belongs to the
scene that made it — without that, anything started just before a transition
follows the player into the next screen.

### Feel

The 10% for juice is cheap to earn and worth doing last, once the fight works.

- **`HitStop`** — a freeze of about 45ms on a hit. The cheapest effect in the
  project and one of the most effective: a hit with no pause reads as a number
  changing, while the same hit followed by a moment of stillness reads as
  weight. Everything in it runs on unscaled time, because waiting on scaled time
  while time is stopped is a coroutine that never resumes.
- **`CameraShake`** — a nudge on ordinary damage, and the rest saved for the two
  moments that change the fight: a phase turning over and the boss dying. If
  every hit shakes the screen hard then none of them does.
- **`DamageFeedback`** — gathers flash, freeze, shake and burst into one place,
  because feedback belongs to the thing *being* hit rather than to whatever hit
  it. A projectile should not have to know whether its target shakes the screen.
- **`DeathDissolve`** — a boss that vanishes on its last hit ends the fight
  without marking it.
- **`VfxBurst` / `VfxPool`** — pooled one-shot particle bursts, using the
  built-in Particle System (§10, VFX).

Every tween here runs unscaled, so effects keep animating through the hit stop
that accompanies them rather than freezing at full white until time resumes.

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
  without breaking encapsulation. The one exception is a plain serializable data
  struct such as `TelegraphCue`, where Unity serializes fields and not
  properties, so public fields are the only way to expose it in the Inspector.
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
| `HealthTests` | Damage clamps at zero; death fires once and only once; **invulnerability survives until every holder releases it** |
| `PoolTests` | Instances are reused rather than quietly recreated; a double return cannot hand the same instance out twice |
| `AttackSelectorTests` | No immediate repeats, including across a bag refill; duplicate entries weight correctly |
| `BossPhaseMachineTests` | Correct phase at each boundary; **no phase is skipped when one hit crosses two thresholds**; healing cannot rewind |
| `AttackSuitabilityTests` | The boss prefers the right attack for the situation, and every score stays in range |

Each of these covers something that fails *silently*. A pool that stops pooling
still works; a boss whose judgement is inverted still attacks. Nothing about
either looks like a bug while playing — the game simply feels worse, which is
the hardest kind of problem to trace back to a cause.

The phase-skip case is the clearest example: it only appears when a single hit
happens to cross two thresholds, so it can survive any amount of play testing
and then rob the player of a transition on the one run that matters.

### Editor tooling

```
Boss Level ▸ Build WebGL            validate, configure, build into docs/
Boss Level ▸ Apply WebGL Settings   the player settings alone, without a build
```

The plan originally called for tooling that generated the layers, the data
assets and the test scene as well. That was written when the expectation was
that scenes and assets would be assembled blind, and it turned out to be solving
a problem that did not arise: the assets were authored by hand in the editor far
faster than a generator could have been specified, and a generator for a
one-boss project would have been more code than the thing it generated.

The build tooling earned its place for the opposite reason. A WebGL build fails
in ways that are invisible until it is hosted — a compression format the server
cannot describe, a first scene that is not the bootstrap — so having those
settings applied identically every time, and reviewable as source, is worth more
than a checklist someone has to remember.

---

## 14. Build

**Target: WebGL.** Chosen over mobile because it earns the same 10% while
keeping keyboard input — a mobile build would require designing touch controls
for move, jump, dash and shoot, which the rubric does not reward.

Constraints carried through the design:

- **No VFX Graph and no compute shaders.** VFX Graph requires compute support,
  which WebGL does not have; effects built in it would silently do nothing in
  the submitted build. All particles use the built-in Particle System.
- **Gzip compression with the decompression fallback enabled.** The fallback is
  the important half: a static host such as GitHub Pages cannot send the
  `Content-Encoding` header that compressed Unity builds normally rely on, so
  without it the loader fails outright and the page shows only an error. This
  is the single most common reason a WebGL build works locally and not once
  hosted.
- **Managed stripping set to High, exceptions limited to explicitly thrown.**
  Download size is the whole player experience on the web — nobody waits.
- **Data caching on**, so a second visit does not download the build again.

`Boss Level ▸ Build WebGL` applies all of the above, refuses to build if
`Bootstrap` is not the first scene, outputs into `docs/`, and writes the
`.nojekyll` marker that stops GitHub Pages running the build through Jekyll —
which would skip Unity's underscore-prefixed files.

Publishing is then committing `docs/` and pointing **Settings ▸ Pages** at this
branch with the `/docs` folder, so the submission carries a live playable link
alongside the source.

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

**`PassThroughPlatform` → deleted, and then the platforms themselves.** It was
first replaced by drop-through logic on `PlayerMotor` — per-pair
`IgnoreCollision` instead of disabling the collider globally, position-based
restore instead of a fixed timer, and input read once on the player rather than
once per platform per frame. Play testing then showed the platforms themselves
were the problem (§6), so both they and that code were removed. The mobility
they offered is now the double jump and the dash.

---

## 16. Build order

Each milestone ends in a playable state, so there is always something to
demonstrate and always a small surface to debug.

| # | Milestone | Done when | |
|---|---|---|---|
| 1 | Foundation | Project restructured, conventions in place, assemblies and tests running | ✅ |
| 2 | Player | Movement feels good in an empty greybox arena | ✅ |
| 3 | Combat loop | Pooled projectile damages a dummy target; health responds | ✅ |
| 4 | Boss skeleton | One hardcoded attack running the full telegraph → active → recovery → idle loop | ✅ |
| 5 | Data layer | That attack converted to a ScriptableObject; the rest authored as assets | ✅ |
| 6 | Phases | Thresholds, transition sequence, shuffle-bag selection, win and lose | ✅ |
| 7 | Shell | Bootstrap, loading, menu, health bars, phase banner, end screens | ✅ |
| 8 | Polish | Shader, VFX, hit stop, shake, tween pass | ✅ |
| 9 | Build | WebGL build, hosting, final documentation pass | ✅ |

Milestone 5 deliberately followed milestone 4: the data abstraction was built
*after* one concrete attack existed, so it was shaped by a real case rather than
by a guess about one. That paid off — `BossAttack` needed no revision when the
remaining six attacks were written against it.

### What play testing changed

The plan survived contact reasonably well, but three things only became visible
once the fight was playable, and each is recorded where it belongs rather than
quietly patched:

- **The boss could not hit a moving player** (§7). It fired at where the player
  stood, so standing still was optimal — the least interesting strategy in the
  game. Fixed with predictive aiming and situational attack selection, not with
  difficulty numbers.
- **Platforms were total cover** (§6). Every attack travelled from the boss to
  the player, so one platform defeated all of them. Fixed twice: with attack
  shapes that do not travel, and by removing the platforms.
- **The player's toolkit was too thin** (§6). One verb and no defensive skill
  meant a well-read telegraph was worth no more than a badly-read one. The dash
  is the answer, and it changed the feel of the fight more than any boss change
  did.

---

## 17. Resolved questions

- **"Boss Level (scope of Survivor.io boss phase)"** — read as *scale of
  deliverable* rather than *genre to copy*, and confirmed with the instructor.
  The Cuphead-style side-scrolling encounter satisfies every listed requirement,
  including "player mechanic to control", which Survivor.io's auto-attack
  notably would not.
- **Shader Graph or hand-written shader** — hand-written. A `.shadergraph` is
  generated JSON, which cannot be read or reviewed, and on a project graded
  largely on readable source that is a poor trade for an effect that fits on one
  page (§10).
- **Web or mobile build** — WebGL. The same 10% either way, and mobile would have
  required designing touch controls for move, jump, dash and shoot, which the
  rubric does not reward (§14).
