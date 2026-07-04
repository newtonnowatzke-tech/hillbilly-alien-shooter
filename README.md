# 🛸 Hillbilly Alien Shooter 🤠

**A colorful low-poly action shooter where you play as a no-nonsense hillbilly defending your farm from alien cattle rustlers.**

The skies light up with glowing saucers in the dead of night. Greedy little green varmints are beaming up your livestock — and they've picked the wrong redneck to mess with. Grab your trusty shotgun, whistle for your horse, and blast every invader back to the stars.

> *Yee-haw! Them aliens ain't ready for this kind of country justice.*

---

## 📋 Table of Contents

- [About the Game](#-about-the-game)
- [Play in Your Browser](#-play-in-your-browser-webgl)
- [Current Features](#-current-features)
- [Development Status](#-development-status)
- [Quickstart](#-quickstart)
- [Controls](#-controls)
- [Gameplay Guide](#-gameplay-guide)
- [Project Structure](#-project-structure)
- [Architecture](#-architecture)
- [Tuning & Balancing](#-tuning--balancing)
- [Roadmap](#-roadmap)
- [Tech Stack](#-tech-stack)

---

## 🎮 About the Game

| | |
|---|---|
| **Genre** | Wave-based defense / action shooter |
| **Perspective** | Third-person over-the-shoulder (V toggles first-person) |
| **Art style** | Bright, saturated low-poly; night-time farm palette (think *Sneaky Sasquatch* meets *Pokémon Stadium*) |
| **Target playtime** | ~1 h 20 min full campaign |
| **Progression** | Farm ▸ Alien Ship ▸ Alien Homeworld ▸ **Alien King** boss |
| **Engine** | Unity 6 LTS (6000.x), URP, New Input System |
| **Platform** | PC / Standalone (gamepad-friendly; mobile possible later) |

You defend your herd across escalating waves of alien rustlers — Little scouts, faster Mediums, tough Larges, tanky Brutes, and dish-shaped UFOs with abduction beams. Alien tech drops power up your shotgun. Save enough cattle and you take the fight to *them*: board the mothership, invade the homeworld, and give the Alien King a double-barrel diplomacy lesson.

## 🌐 Play in Your Browser (WebGL)

The repo ships a full WebGL pipeline — see **[docs/WEBGL.md](docs/WEBGL.md)**:

- **Instant local playtest:** in Unity, **Tools ▸ Hillbilly ▸ Build WebGL** → serve `build/WebGL/` with any static server (or drag-drop the zip onto itch.io).
- **Auto-published build:** every merge to `main` builds and deploys to GitHub Pages at **<https://newtonnowatzke-tech.github.io/hillbilly-alien-shooter/>** — after a one-time setup (enable Pages + add your free Unity license secrets; ~10 min, fully documented in the guide).

Browser quirks are pre-handled: click once to capture the mouse, Esc auto-pauses (browsers reserve it to release the cursor), P also pauses, and Quit is hidden.

## ✨ Current Features

### Core loop (Packet 1.1)
- 🌙 **Night-time low-poly farm** — barn, perimeter fences, trees, moonlight + warm barn glow, distance fog
- 🔫 **Hitscan shotgun** — 8-pellet spread, magazine + reserve ammo, timed reload with auto-reload on empty click, tracer FX
- 👽 **Little Alien scouts** — hunt the nearest cow, latch on an abduction beam, and melee you once the cattle are gone; squash-and-stretch hit reactions
- 🐄 **Rescuable cattle** — cows lift skyward while beamed; shoot the alien off and they *settle back down* — saving a cow mid-lift is a real play
- 🌊 **Wave system** — drip-spawned wave from the treeline with win (wave cleared, cattle alive) / lose (you die or the herd's gone) and instant restart
- 📊 **Event-driven HUD** — HP, Cattle Saved/Rustled tally, shells, reload state, wave banner, end screens

### Horse riding (Packet 1.2)
- 🐴 **Buttercup** — your loyal low-poly steed, found waiting by the barn
- 🏇 **Momentum riding physics** — analog throttle, braking, back-up, and speed-sensitive steering (pivots at a stand, wide arcs at a gallop, ~2× foot speed)
- 🔫🐎 **Shoot while riding** — mouse aim stays fully independent of the horse's heading: gun down rustlers *sideways at full gallop*
- 🎯 **Interaction system** — walk up, press **E**; proximity prompts on the HUD (reused for pickups & levers in later packets)
- 📢 **Whistle mechanic** — **H** toggles *follow* ↔ *stay* from anywhere; she gallops to catch up, and "knows a shortcut" (teleports) if truly left behind
- ⛰️ **Rolling terrain** — gentle hills, with CharacterController physics for player + horse and ground-snapping for aliens & wandering cattle

### Enemy variety & alien tech (Packet 2.1)
- 👾 **Little Alien scouts** now weave "annoying paths" toward your cows — lead your buckshot
- 👹 **Medium Aliens** — bigger, faster violet hunters that *flank* you from the sides before charging in with quick light swipes (pairs naturally pincer)
- 🛸 **Scout Saucers** — dish-shaped UFOs with rim lights and a belly glow that hover over the herd and beam cows up *from the air*; can't be body-blocked, must be shot down; spin-crash death
- 💎 **Alien tech drops** — dead invaders drop glowing tech shards that magnet-collect as you ride past; the HUD tallies them (the Packet 2.3 upgrade system spends them; saucers always pay out 3)

### Camera, pause & browser builds (Packet 1.3)
- 🎥 **Third-person camera** — over-the-shoulder follow cam with collision (never clips through the barn), smooth zoom-out while mounted, and a gallop FOV kick; **V** toggles back to first person any time
- ⏸️ **Pause menu** — Esc/P/Start freezes the action mid-abduction; Resume / Settings / Restart / Quit
- 🎛️ **Settings stub** — mouse sensitivity, invert Y, and master volume, persisted between sessions
- 🌐 **WebGL pipeline** — one-click local browser builds + a GitHub Actions workflow that auto-deploys every merge to GitHub Pages ([guide](docs/WEBGL.md))

## 🚧 Development Status

Development proceeds in self-contained **work packets** (see the [roadmap](docs/hillbilly_alien_shooter_roadmap.md)).

| Phase | Packet | Contents | Status |
|---|---|---|---|
| **1 — Foundation** | 1.1 | Project setup, core loop, shotgun, cattle, first wave | ✅ Done |
| | 1.2 | Horse finding/mounting, riding physics, shoot-while-riding, follow/stay | ✅ Done |
| | 1.3 | Third-person camera, pause menu + settings, WebGL builds | ✅ Done |
| **2 — Combat & Enemies** | 2.1 | Little & Medium aliens, tech drops, UFO abduction beams | ✅ Done |
| | 2.2 | Large aliens, Brutes, combat UFOs, health bars | ⬜ Next |
| | 2.3 | Alien tech upgrade system (ammo, reload, explosive, rapid fire…) | ⬜ |
| **3 — Progression** | 3.1 | Escalating farm waves, rest periods, progression gate | ⬜ |
| | 3.2 | Alien ship boarding + space transition | ⬜ |
| | 3.3 | Alien homeworld level | ⬜ |
| | 3.4 | Alien King boss + finale | ⬜ |
| **4 — Polish** | 4.1–4.4 | Cutscenes, audio, VFX/UI polish, balancing & save system | ⬜ |
| **5 — Stretch** | — | Cosmetics, local co-op, achievements | ⬜ |

Per-packet progress reports live in [`docs/progress/`](docs/progress/).

## 🚀 Quickstart

Full instructions (with troubleshooting) in **[SETUP.md](SETUP.md)** — short version:

1. **Unity 6 LTS** project from the **Universal 3D (URP)** template.
2. Drop this repo's **`Assets/_Project`** into your project's `Assets/`.
3. Install the **Input System** package; set *Active Input Handling* → **Both**; restart.
4. Menu: **Tools ▸ Hillbilly ▸ Build Farm Scene** — one click generates the entire playable scene *and* all data assets, fully wired.
5. Press **Play**. Defend them cows.

> Already had the Packet 1.1 scene? **Re-run the scene builder** after pulling — it regenerates `Farm.unity` with the horse, hills, and interaction system.

## 🕹️ Controls

| Action | Keyboard / Mouse | Gamepad |
|---|---|---|
| Move (on foot) | WASD / Arrows | Left stick |
| Look / aim | Mouse | Right stick |
| Fire shotgun | Left mouse button | Right trigger |
| Reload | R | X / □ (West) |
| Interact — mount/dismount | E | Y / △ (North) |
| Whistle — follow ↔ stay | H | D-pad up |
| Ride: throttle / brake–reverse | W / S | Left stick ↑ / ↓ |
| Ride: steer | A / D | Left stick ← / → |
| Toggle camera (third ↔ first person) | V | Right stick click |
| Pause / resume | Esc or P | Start |
| Restart after win/lose | R or Enter | — |

## 📖 Gameplay Guide

**The herd is everything.** Aliens spawn at the treeline and beeline for your nearest cow. A latched abduction beam lifts the cow skyward — but shooting the alien off lets the cow drift back down unharmed. Lose every cow (or your life) and it's over; clear the wave with at least one cow standing and the farm holds.

**Buttercup wins fights.** On foot you're quick; on horseback you're *cavalry*. Mount up (E by the barn), hold W to gallop, and swing your aim freely — horse heading and gun aim are independent, so circling a cluster of rustlers while pouring buckshot into them is the signature move. Dismount to fight tight corners; she'll follow. Whistle (H) to park her somewhere safe or call her back.

**Watch your shells.** Six in the tube, thirty in the bag. Reloads are slow enough to be a decision — top up between skirmishes, not during them.

**Know your varmints.** Little scouts weave for the cows — lead them. Violet Mediums hunt *you* from the flanks — don't get pincered while lining up a cow rescue. And when a saucer drifts overhead, drop everything: its air-beam rustles cattle faster than anything on the ground, and it only comes down when you shoot it down (it always drops a fat tech payout).

**Grab the glow.** Dead invaders drop shimmering alien tech — ride close and it flies to you. Stockpile it; upgrades are coming (Packet 2.3).

## 🗂️ Project Structure

```
Assets/_Project/
├── Scenes/                Farm.unity            (generated by the scene builder)
├── Data/                  *.asset               (generated ScriptableObject instances)
└── Scripts/
    ├── Core/              GameState, GameEvents (event bus), GameManager, IInteractable
    ├── Combat/            IDamageable, DamageInfo, Health
    ├── Player/            PlayerInputHandler, PlayerController, PlayerInteraction,
    │                      CameraRig (3rd/1st person), PlayerHealth
    ├── Horse/             HorseController      (Idle/Follow/Stay/Mounted + riding physics)
    ├── Weapons/           Shotgun              (hitscan pellets, ammo, reload)
    ├── Enemies/           AlienEnemy (rustler/hunter roles), UfoEnemy, EnemyRegistry
    ├── Pickups/           TechPickup           (magnet-collect alien tech)
    ├── Cattle/            Cattle               (abduction meter, registry, tallies)
    ├── Waves/             WaveSpawner
    ├── Data/              WeaponData, EnemyData, WaveData, HorseData   (SO definitions)
    ├── UI/                HUDController, PauseMenu   (self-building, event-driven)
    ├── Utils/             LowPolyFactory, GameLayers, GroundSnap
    └── Editor/            FarmSceneBuilder, WebGLBuilder   (Tools ▸ Hillbilly ▸ …)
Packages/, ProjectSettings/                      (CI-only Unity project scaffolding)
.github/workflows/                               (WebGL build & Pages deploy, license helper)
docs/
├── hillbilly_alien_shooter_roadmap.md           (full development roadmap)
├── WEBGL.md                                     (browser build & deploy guide)
└── progress/                                    (per-packet progress reports)
SETUP.md                                         (detailed setup, testing & troubleshooting)
```

## 🏛️ Architecture

Designed for **fast packet-by-packet growth** without rework:

- **Static event bus (`GameEvents`)** — gameplay systems *raise* events (`CattleCountsChanged`, `AmmoChanged`, `HorseStateChanged`…); the HUD, GameManager, and future audio/score systems *subscribe*. No system references another directly, so adding a listener never touches gameplay code. Payloads are primitives only, keeping `Core` dependency-free.
- **ScriptableObject data** — every tunable lives in an asset (`WeaponData`, `EnemyData`, `WaveData`, `HorseData`), each with a `CreateDefault()` runtime fallback so components function even unconfigured. Designers balance in the Inspector; the Packet 2.3 upgrade system will mutate these at runtime.
- **Interface-driven combat** — the shotgun only knows `IDamageable`; aliens, future destructibles, and UFO weak points all plug into the same contract via the shared `Health` component. `DamageInfo` carries hit point/direction/force so knockback, headshots, and reaction VFX can be added without signature churn.
- **`IInteractable` + proximity scanner** — the horse is the first consumer; tech pickups (2.3) and the ship-boarding trigger (3.2) reuse the same prompt pipeline.
- **One art factory (`LowPolyFactory`)** — every placeholder (cow, alien, horse, barn, hills…) is built from primitives in exactly one place, shared by the runtime spawner and the editor scene builder. Swapping in real low-poly models later is a factory-only change.
- **One-click scene builder** — `FarmSceneBuilder` regenerates the whole playable scene deterministically, creating and wiring the SO assets. No manual scene fiddling, no broken references, reproducible setups for every packet.
- **Restart-safe statics** — registries and tallies (`Cattle.Alive`, `AlienEnemy.ActiveCount`) are reset by the `GameManager` on scene load, so fast play-mode (no domain reload) and in-game restarts behave identically.

## 🎛️ Tuning & Balancing

All live in `Assets/_Project/Data/` — tweak in the Inspector, no code:

| Asset | What you control |
|---|---|
| `WeaponData_Shotgun` | pellet damage/count, spread cone, range, knockback, mag size, reserve, fire cooldown, reload time |
| `EnemyData_LittleAlien` | health, speed, weave amplitude/frequency ("annoying paths"), grab range, abduction rate, melee, tech drop chance, tint |
| `EnemyData_MediumAlien` | hunter stats: flank offset/close range, light-attack damage & cooldown, body scale, drop chance |
| `EnemyData_ScoutSaucer` | hover height & bob, beam lock radius, air abduction rate, hull HP, guaranteed tech payout |
| `WaveData_Wave1` | spawn list (enemy × count — currently 8 Little + 4 Medium + 1 Saucer), start delay, spawn interval |
| `HorseData_Buttercup` | gallop/walk/reverse speeds, acceleration, braking, turn rates (standing vs gallop), follow/gallop/teleport distances, colors |
| `GameManager` (scene) | cattle needed to win |
| `WaveSpawner` (scene) | spawn ring radius (visualized as a gizmo), optional spawn points |

## 🗺️ Roadmap

The full plan — five phases from foundation to stretch goals — lives in
[`docs/hillbilly_alien_shooter_roadmap.md`](docs/hillbilly_alien_shooter_roadmap.md).
The short arc: polish the farm loop, grow the bestiary (Mediums ▸ Larges ▸ Brutes ▸ UFOs), add alien-tech shotgun upgrades, escalate waves until the mothership shows up, then take the fight through the ship to the homeworld and the **Alien King** — with cutscenes, a hillbilly/sci-fi soundtrack, and a save system to tie it together.

## 🔧 Tech Stack

- **Unity 6 LTS (6000.x)** — Universal Render Pipeline (URP)
- **New Input System** — actions built in code (zero asset wiring); keyboard/mouse + gamepad out of the box
- **uGUI** runtime-built HUD (TextMeshPro polish scheduled for Packet 4.3)
- **No third-party dependencies** — every placeholder asset is generated from Unity primitives

---

*Built packet-by-packet with Claude. Progress reports for every packet live in [`docs/progress/`](docs/progress/).*

**Now git along, little dogies — them cows ain't gonna save themselves.** 🐄🛸
