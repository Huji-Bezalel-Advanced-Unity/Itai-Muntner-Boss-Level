# Boss Level

A single-screen boss fight in the style of *Cuphead*'s ground battles, built in
Unity 6 with the Universal Render Pipeline.

Final assignment for the Advanced Unity course.
**Author:** Itai Muntner

> **Play it:** _add the GitHub Pages link here once the build is published_

---

## The fight

You occupy the left of a fixed arena and fight a boss anchored on the right. It
escalates through three phases as its health falls, gaining attacks and losing
the patience it showed you in the first one.

### Controls

| Action | Key |
|---|---|
| Move | **A / D** or **← →** |
| Jump (and double jump) | **Space** |
| Dash | **Left Shift** |
| Shoot | **Left Mouse** or **Enter** |

The dash passes through damage. It is short, cannot be steered once started, and
you get one per trip through the air — reading a telegraph correctly and dashing
through the attack is the strongest thing you can do.

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

The boss **leads its shots**, predicting where you will be when they arrive, and
it leads them further in later phases. It also **chooses**: each attack scores
how well it fits what you are doing right now, so camping invites the pinpoint
attacks and running invites the ones that cover ground.

---

## Running it

Open the project in **Unity 6000.0.41f1** and play from
`Assets/_Project/Scenes/Bootstrap.unity`.

Playing `BossLevel.unity` directly also works for iterating on the fight — the
end screen's retry button falls back to reloading in place when the persistent
services do not exist.

### Tests

**Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All.**

The suites cover the rules that fail silently rather than loudly: health and
invulnerability, the object pool actually reusing instances, attack selection
never repeating back to back, phase thresholds never skipping a phase, and the
boss's judgement about which attack suits which situation.

### Building for the web

**Boss Level ▸ Build WebGL.** It applies the required player settings, checks
that `Bootstrap` is the first scene, builds into `docs/`, and writes the
`.nojekyll` marker GitHub Pages needs.

To publish: commit `docs/`, then in the repository's **Settings ▸ Pages**, set
the source to this branch with the **`/docs`** folder.

---

## Documentation

**[`Documentation/DESIGN.md`](Documentation/DESIGN.md)** is the design document:
architecture, the boss's state machines and AI, the data model, coding
conventions, testing strategy, and the reasoning behind the choices — including
the ones that were reversed after play testing, and why.

`CLAUDE.md` is working context for AI-assisted development on this repository.

---

## Built with

Unity 6000.0.41f1 · URP 17.0.4 (2D Renderer) · Input System 1.13.1 ·
DOTween · Unity Test Framework
