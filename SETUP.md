# Hillbilly Alien Shooter — Setup & Run Guide (Packet 1.1)

Everything you need to go from an empty machine to blasting aliens on the farm.

- **Engine:** Unity **6 LTS** (6000.x)
- **Render pipeline:** **URP** (Universal Render Pipeline)
- **Input:** **New Input System** (built in code — no asset wiring needed)
- **Target:** PC / Standalone (mouse + keyboard, gamepad-friendly)

---

## 1. Create the Unity project

1. Install **Unity 6 LTS** via Unity Hub (any 6000.x LTS).
2. In Unity Hub → **New Project** → template **Universal 3D (URP)** → name it
   `HillbillyAlienShooter`.
3. Open it once so Unity generates the full project (this creates `ProjectSettings/`,
   `Packages/`, etc.), then close Unity.

## 2. Add the game code

This repo contains the `Assets/_Project` folder (all scripts) and this guide.

- Copy the repo's **`Assets/_Project`** folder into your new project's **`Assets/`**
  folder (so you end up with `.../HillbillyAlienShooter/Assets/_Project/...`).
- Also copy the repo's **`.gitignore`** to the project root if you want to version it.

> Tip: You can also just clone this repo *inside* the generated project and move the
> folder — whatever's easiest. The only thing Unity cares about is that
> `Assets/_Project/Scripts/...` exists under the project's `Assets/`.

## 3. Enable the Input System package

The scripts use `UnityEngine.InputSystem`, so:

1. **Window ▸ Package Manager** → install **Input System** (com.unity.inputsystem)
   if it isn't already there.
2. **Edit ▸ Project Settings ▸ Player ▸ Active Input Handling** → set to
   **Both** (or **Input System Package (New)**).
3. Unity will ask to **restart** — do it.

> If you skip this, the code still compiles but the player won't respond to input.

## 4. Build the farm (one click)

With the project open and scripts compiled:

- Menu bar → **Tools ▸ Hillbilly ▸ Build Farm Scene** → confirm.

This generates and saves `Assets/_Project/Scenes/Farm.unity` with:
- Night-lit ground, barn, perimeter fence, a ring of trees
- The hillbilly (first-person controller + shotgun + camera)
- A herd of 6 cows
- `GameManager`, `WaveSpawner`, and a self-building `HUD`
- ScriptableObject data assets in `Assets/_Project/Data/`
  (`WeaponData_Shotgun`, `EnemyData_LittleAlien`, `WaveData_Wave1`), wired in automatically

It also adds the scene to **Build Settings** so the in-game restart works.

## 5. Play

Press **Play**. After a short delay the wave banner reads **“WAVE 1 — YEE-HAW!”**
and aliens shamble in from the tree line to rustle your cattle.

### Controls
| Action | Keyboard/Mouse | Gamepad |
|---|---|---|
| Move (on foot) | WASD / Arrows | Left stick |
| Look / aim | Mouse | Right stick |
| Fire shotgun | Left mouse | Right trigger |
| Reload | R | West button (X/□) |
| Interact (mount/dismount horse) | E | North button (Y/△) |
| Whistle (horse: follow ↔ stay) | H | D-pad up |
| Ride: throttle / brake / back up | W / S | Left stick ↑ / ↓ |
| Ride: steer | A / D | Left stick ← / → |
| Restart (after win/lose) | R or Enter | — |

> **Riding tip:** while mounted, W/S/A/D drives **Buttercup** (momentum + speed-
> sensitive steering), but your **mouse aim stays free** — you can blast aliens
> sideways at full gallop. That's the intended cattle-defense power move.

---

## Testing checklist (Packet 1.2 — horse)

- [ ] **Finding:** Buttercup stands by the barn; walking within ~3 m shows "[E] Ride Buttercup".
- [ ] **Mounting:** E snaps you into the saddle; the prompt flips to "[E] Hop off Buttercup".
- [ ] **Riding:** W accelerates to a gallop (~2× foot speed), S brakes then backs up, A/D steers — tight turns at a walk, wide arcs at a gallop.
- [ ] **Shooting while riding:** mouse aim stays free while W drives the horse; you can fire in any direction mid-gallop.
- [ ] **Dismount:** E drops you beside the horse (never inside a fence/barn).
- [ ] **Follow:** after dismounting, Buttercup trots behind you and gallops to catch up when far; HUD shows "Buttercup: followin' you".
- [ ] **Whistle:** H toggles follow/stay from anywhere; from Stay/Idle she comes to you.
- [ ] **Teleport failsafe:** leave her > 45 m behind — she reappears near you ("knows a shortcut").
- [ ] **Terrain:** riding up/over the new hills works; aliens and wandering cows follow the hill surface instead of clipping.
- [ ] **Collision:** the horse can't gallop through fences, trees, or the barn.
- [ ] **End-of-round:** win/lose freezes horse + prompts; no mounting from the game-over screen.

## Testing checklist (Packet 1.1)

- [ ] **Movement & look:** WASD moves relative to where you look; mouse turns you; cursor is locked/hidden.
- [ ] **Shooting:** LMB fires a spread of tracer pellets; ammo counter drops `6 → 0`.
- [ ] **Reload:** at 0 ammo it auto-reloads; pressing R reloads early; “RELOADIN’…” shows; reserve drops.
- [ ] **Aliens spawn:** they appear at the map edge and head for the nearest cow.
- [ ] **Abduction:** an alien in range beams a cow; the cow rises and spins; “RUSTLED” count climbs if it completes.
- [ ] **Saving a cow:** shoot the beaming alien off — the cow settles back down (progress drains).
- [ ] **Enemy death:** enough pellets kill an alien (squash react → shrink-poof).
- [ ] **HUD:** HP, “CATTLE SAVED / RUSTLED”, and shells all update live.
- [ ] **Win:** clear every alien with ≥1 cow left → **“FARM DEFENDED!”**
- [ ] **Lose (cattle):** let all cows get rustled → **“THEM ALIENS GOT YER CATTLE…”**
- [ ] **Lose (death):** let aliens melee you to 0 HP (happens once no cattle remain) → same lose screen.
- [ ] **Restart:** press R on the end screen → scene reloads fresh.

---

## Tuning (no code required)

Select the assets in `Assets/_Project/Data/` and tweak in the Inspector:

- **WeaponData_Shotgun** — damage, pellet count, spread, mag size, reload time, fire rate.
- **EnemyData_LittleAlien** — health, move speed, grab range, abduction rate, melee.
- **WaveData_Wave1** — enemy count, spawn interval, start delay.
- **HorseData_Buttercup** — gallop speed, acceleration/braking, turn rates, follow
  distances, teleport failsafe distance, coat/mane/saddle colors.

The `GameManager` has **Cattle Needed To Win** (default 1). The `WaveSpawner` has a
**Spawn Ring Radius** and a gizmo showing where aliens come from.

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| Player doesn't move | Set **Active Input Handling** to Both (Step 3) and restart Unity. |
| Everything is magenta/pink | URP not active. Ensure you used the URP template, or assign a URP asset in **Project Settings ▸ Graphics**. |
| No camera / black screen | Re-run **Build Farm Scene**, or make sure the player's `PlayerCamera` is tagged `MainCamera`. |
| “Scene couldn't be loaded … not in Build Settings” on restart | Re-run the builder (it registers the scene), or add `Farm.unity` via **File ▸ Build Settings ▸ Add Open Scenes**. |
| HUD text missing | The HUD uses the built-in `LegacyRuntime.ttf`; it should always resolve. If not, any OS font fallback kicks in automatically. |

---

## Architecture at a glance

```
Core/      GameState, GameEvents (static event bus), GameManager (win/lose), IInteractable
Combat/    IDamageable, DamageInfo, Health (shared HP)
Player/    PlayerInputHandler (New Input System), PlayerController (+mount/dismount),
           PlayerInteraction (proximity prompts), PlayerHealth
Horse/     HorseController (Idle/Follow/Stay/Mounted state machine + riding physics)
Weapons/   Shotgun (hitscan spread + ammo/reload — works on horseback)
Enemies/   AlienEnemy (hunt cow → beam → melee fallback)
Cattle/    Cattle (abduction meter + terrain-aware wander)      [namespace: Livestock]
Waves/     WaveSpawner (drip-spawn one wave)
Data/      WeaponData, EnemyData, WaveData, HorseData (ScriptableObjects)
UI/        HUDController (self-building, event-driven)
Utils/     LowPolyFactory (all placeholder primitives), GameLayers, GroundSnap
Editor/    FarmSceneBuilder (the one-click scene generator)
```

> **Upgrading from Packet 1.1?** After pulling the new code, re-run
> **Tools ▸ Hillbilly ▸ Build Farm Scene** — the saved scene predates the horse,
> hills, and interaction system, and a rebuild wires them all in.

**Key idea:** systems talk through `GameEvents`, not directly to each other, and all
placeholder art comes from one `LowPolyFactory` — so swapping in real low-poly models
or a polished TMP HUD later touches almost nothing else.
