# Crazy Diamond

A single-screen boss fight in the style of *Cuphead*'s ground battles, built in
Unity 6 with the Universal Render Pipeline.

Final assignment for the Advanced Unity course.
**Author:** Itai Muntner

---

## Links

| | |
|---|---|
| ▶ **Play in your browser** | [Crazy Diamond on itch.io](https://itaimuntner.itch.io/crazy-diamond) |
| 🎬 **Gameplay video** | [Watch the fight](https://www.youtube.com/watch?v=Bg_HPXVisnk) |
| 🧭 **Code review video** | [Walkthrough of the architecture](https://www.youtube.com/watch?v=msbyML4nfTM) |
| 📄 **Design document** | [Documentation/DESIGN.md](Documentation/DESIGN.md) |

---

## The fight

You occupy the left of a fixed arena and fight a boss anchored on the right. It
escalates through three phases as its health falls, gaining attacks and losing
the patience it showed you in the first one.

### Controls

| Action | Key |
|---|---|
| Move | **A / D** or **Left / Right Arrows** |
| Jump (and double jump) | **W** or **Up Arrow** |
| Dash | **Left Shift** |
| Shoot | **Left Mouse** or **Space** |
| Pause | **P** or **Escape** |

The dash passes through damage. It is short, cannot be steered once started, and
you get one per trip through the air — reading a telegraph correctly and dashing
*through* an attack is the strongest thing you can do, and the main reason the
fight rewards nerve over caution.

> Playing in the browser: click the game once before using the keyboard. WebGL
> only receives key presses while its canvas has focus.

### What the boss does

Seven attacks, chosen to demand different answers rather than to be seven ways
of firing forwards:

| Attack | What it asks of you |
|---|---|
| Spread shot | Punishes standing still — move to an edge of the fan |
| Aimed burst | Re-aims between shots, so it tracks: keep moving |
| Sweep | Locks its aim — a pattern to cross, pick a side |
| Rain | Falls from overhead, so cornering stops being safe |
| Slam | Shockwaves along the floor — jump or dash them |
| Eruption | Vents open underfoot and erupt upwards — leave the ground you are on |
| Summon | Minions that hunt you until killed, competing for your shots |

Every attack announces itself first, with its own colour, motion and sound. That
warning is a promise: the fight is difficult but it never surprises you, and a
phase change is signalled differently again so a change of rules never reads as
just another wind-up.

### Why it feels like it is paying attention

Two mechanisms, and neither of them is a difficulty number.

**It leads its shots.** The boss predicts where you will be when a projectile
arrives rather than firing at where you are — and it leads *further* in later
phases, so it visibly learns to read you instead of merely firing faster.

**It chooses.** Each attack scores how well it suits the situation right now,
and the boss draws two candidates and uses the better one. Standing still invites
the pinpoint attacks; running invites the ones that cover ground; a shockwave
along the floor is never sent at someone already in the air.

The point of both is the same. Standing still and trading shots is the least
interesting thing a player can do, and it stopped being viable not because the
boss hits harder, but because holding still invites the attacks that do not miss.

---

## How it is built

The full reasoning — including the decisions that play-testing reversed — is in
**[the design document](Documentation/DESIGN.md)**. The short version:

- **The boss is data, not code.** Attacks and phases are ScriptableObject assets.
  The controller owns the rhythm — warn, strike, recover, pause — and the assets
  own what happens inside it. "Phase three is harder" is two multipliers, and a
  harder spread shot is a duplicated asset rather than a new class.
- **One fairness contract, stated once.** Telegraph and recovery are sequenced by
  the controller rather than by each attack, so every attack ever added gives the
  player a warning beforehand and a window to punish afterwards.
- **One pool, five users.** The same `Pool<T>` backs projectiles, minions,
  volcanic vents, particle bursts and audio sources. It is a plain C# class
  rather than a component, because Unity cannot attach generic MonoBehaviours.
- **Gameplay never references the interface.** Every view observes C# events, so
  the fight runs correctly in a scene with no canvas in it at all.
- **47 EditMode tests**, aimed squarely at the things that fail *silently* — a
  phase quietly skipped by one large hit, a pool that stops pooling, a boss whose
  judgement is inverted. None of those look like bugs while playing. The game
  just feels worse.

---

## Running it

Open the project in **Unity 6000.0.41f1** and play from
`Assets/_Project/Scenes/Bootstrap.unity`.

Playing `BossLevel.unity` directly also works while iterating on the fight — the
retry button falls back to reloading in place when the persistent services do
not exist.

### Tests

**Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All.**

### Building for the web

**Boss Level ▸ Build WebGL.** It applies the required player settings, refuses to
build unless `Bootstrap` is the first scene, and writes the output to `docs/`.

---

## Built with

Unity 6000.0.41f1 · URP 17.0.4 (2D Renderer) · Input System 1.13.1 ·
DOTween · Unity Test Framework
