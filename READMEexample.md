# 2D Action Platformer - Slay The Demons – Hollow Knight -Style Boss Fight

[![Unity Version](https://img.shields.io/badge/Unity-6000.0.41f1-yellow?logo=unity&logoColor=white)](https://unity.com/)
[![License: CC Assets](https://img.shields.io/badge/Assets-Free%20CC--BY--ITCH.IO-blue)](https://itch.io/)
[![MIT License](https://img.shields.io/badge/Code-License%3A%20MIT-green.svg)](LICENSE)

> Developed as part of a **Computer Science & Game Development** program.  
> A technical demo inspired by *Hollow Knight*, focused on modular FSM logic, combat feedback, and reactive AI behavior.

---

## 📋 Table of Contents
1. [🎮 Gameplay](#-gameplay)
2. [🧠 Features](#-features)
3. [🕹️ Controls](#️-controls)
4. [🔧 Architecture Overview](#-architecture-overview)
5. [📈 State Machines (UML)](#-state-machines-uml)
6. [📦 Assets & Credits](#-assets--credits)
7. [📘 Code Explanation (Technical Walkthrough)](#-code-explanation-technical-walkthrough)
8. [📎 Download & Play](#-download--play)

---

## 🎮 Gameplay

> A short video showing boss combat, dash / jump mechanics and rally healing:

[[Watch Gameplay on YouTube]](https://youtu.be/j95xNAh2Vno)

A fast-paced 2D boss-fight prototype with jump-dash-attack flow, rally mechanic, enemy knockbacks and a responsive camera system. All attacks are buffered. Boss actions follow a weighted-random FSM sequence. Built entirely in Unity.

---

## 🧠 Features

- ⚔️ **Player Combat System** (buffered attack, dash, jump, rally)
- 🧠 **Boss AI FSM** – 3 phases: Glide, Slam, Fireball
- 🧱 **Modular Architecture**: AttackBox / HitBox / Health separation
- 💥 **Hit Feedback**: Popups, particles, camera shake, sound
- 🎥 **Adaptive Follow Camera**: zone clamping + fall anticipation
- 🎛️ **UI & Game State Management**: PlayerUI, Pause, Victory
- 🧪 **Debug Overlay**: shows boss state & coroutine activity
- 🌀 **Coroutines** for deterministic attack sequencing

---

## 🕹️ Controls

| Action   | Keyboard     | Description                   |
|----------|--------------|-------------------------------|
| Move     | A / D        | Walk left / right             |
| Attack   | Left Click   | Directional attack            |
| Jump     | Space        | Jump (buffered, forgiving)    |
| Dash     | Right Click  | Dash (ground/air, cooldown)   |
| Attack   | W / S        | Attack Up / Down (for aiming) |
| Help     | ESC          | In-game "How to Play" panel   |

---

## 🔧 Architecture Overview

| System           | File                 | Description                                     |
|------------------|----------------------|-------------------------------------------------|
| **Player Logic** | `Player.cs`          | Movement, FSM, combat, knockback, death        |
| **UI**           | `PlayerUI.cs`        | Hearts, rally, break animation, pause icon     |
| **Sound**        | `PlayerSound.cs`     | All player SFX – step, hit, attack, rally      |
| **Boss Logic**   | `BossCore.cs`        | FSM-based behavior using coroutines            |
| **Boss Overlay** | `BossDebugOverlay.cs`| Real-time debug panel in-game                  |
| **Enemies**      | `Enemy.cs`           | Wander / recoil / stagger / death              |
| **Camera**       | `FollowCamera.cs`    | Damped follow, zone limits, fall offset        |
| **Zone**         | `CameraZone.cs`      | Trigger-based camera bounding                  |
| **Damage Logic** | `AttackBox.cs` / `HitBox.cs` | Event-driven hit-detection system      |
| **Health**       | `Health.cs`          | Shared health system for all entities          |
| **Game State**   | `GameManager.cs`     | Victory, respawn, checkpoints, fade transitions|

<p align="center">
<img width="706" height="960" alt="Full_Game_Architecture_UML" src="https://github.com/user-attachments/assets/d61a75f0-c293-426e-ba04-a67f2cc49807" />
</p>

---

## 📈 State Machines (UML)

> This image visualizes all FSM transitions in gameplay logic:

<p align="center">
<img width="768" height="914" alt="All_Characters_StateMachine" src="https://github.com/user-attachments/assets/434b01b1-b879-4f7a-9460-d43d26e3b762" />
</p>

---

## 📦 Assets & Credits

| Type        | Source                    |
|-------------|---------------------------|
| Pixel Art   | Free Packs on [itch.io](https://itch.io) |
| SFX         | OpenGameArt & itch CC0    |
| Engine      | Unity 2D (URP)            |
| Inspiration | 🎮 *Hollow Knight* by Team Cherry |

> All third-party content is licensed for non-commercial, educational use.

---

# 📘 Code Explanation (Technical Walkthrough)

> Want to understand how it works under the hood?

🎓 Here's a full technical explanation of the FSMs, coroutines, architecture & design patterns used in the project:

[[Watch Code Explanation]](https://youtu.be/sTUOQ4XW-48)

## 📎 Download & Play

> 🕹️ Available now on Itch.io:  
> [👉 Click here to play](https://osherx.itch.io/slay-the-demons)

> Or clone the repo and run via Unity Hub:
```bash
git clone https://github.com/...


#
