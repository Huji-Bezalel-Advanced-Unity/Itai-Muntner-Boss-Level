# Rectangle Hate

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
| Move | **A / D** or **Left / Right Arrows** |
| Jump (and double jump) | **W** or **Up Arrow** |
| Dash | **Left Shift** |
| Shoot | **Left Mouse** or **Space** |

The dash passes through damage. It is short, cannot be steered once started, and
you get one per trip through the air - reading a telegraph correctly and dashing
through the attack is the strongest thing you can do.

### What the boss does

Seven attacks, chosen to demand different answers rather than to be seven ways
of firing forwards:

| Attack | What it asks of you |
|---|---|
| Spread shot | Punishes standing still - move to an edge of the fan |
| Aimed burst | Re-aims between shots, so it tracks: keep moving |
| Sweep | Locks its aim - a pattern to cross, pick a side |
| Rain | Falls from overhead, so cornering stops being safe |
| Slam | Shockwaves along the floor - jump or dash them |
| Eruption | Vents open underfoot and erupt upwards - leave the ground you are on |
| Summon | Minions that hunt you until killed, competing for your shots |

The boss **leads its shots**, predicting where you will be when they arrive, and
it leads them further in later phases. It also **chooses**: each attack scores
how well it fits what you are doing right now, so camping invites the pinpoint
attacks and running invites the ones that cover ground.

---

## Running it

Open the project in **Unity 6000.0.41f1** and play from
`Assets/_Project/Scenes/Bootstrap.unity`.

Playing `BossLevel.unity` directly also works for iterating on the fight - the
end screen's retry button falls back to reloading in place when the persistent
services do not exist.

---

## Built with

Unity 6000.0.41f1 · URP 17.0.4 (2D Renderer) · Input System 1.13.1 ·
DOTween · Unity Test Framework
