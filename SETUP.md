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
| Jury-rig wild upgrade (5 tech) | Q | Left shoulder |
| Ride: throttle / brake / back up | W / S | Left stick ↑ / ↓ |
| Ride: steer | A / D | Left stick ← / → |
| Toggle camera (third ↔ first person) | V | Right stick click |
| Pause / resume | Esc or P | Start |
| Restart (after win/lose) | R or Enter | — |

> **Riding tip:** while mounted, W/S/A/D drives **Buttercup** (momentum + speed-
> sensitive steering), but your **mouse aim stays free** — you can blast aliens
> sideways at full gallop. That's the intended cattle-defense power move.

---

## Testing checklist (Packet 2.3 — alien tech upgrades)

- [ ] **Start-of-wave hint:** a toast explains "[Q] jury-rig a wild upgrade — 5 tech a roll".
- [ ] **Broke roll:** pressing Q with < 5 tech shows "Need 5 tech..." and spends nothing.
- [ ] **Wild roll:** with ≥ 5 tech, Q deducts 5 and lands a random upgrade with a flavor toast.
- [ ] **Extra Shells:** reserve jumps +12 instantly (ammo counter updates).
- [ ] **Greased Lightnin':** reload visibly ~2× faster while the timer runs.
- [ ] **Hair Trigger:** you can pump shells noticeably faster while active.
- [ ] **Boomstick Rounds:** pellet impacts detonate — a blast ring at the impact point damages nearby enemies (never you, the horse, or cows).
- [ ] **Slot timers:** each timed upgrade appears under the tech tally counting down live (e.g. "BOOMSTICK ROUNDS 12s").
- [ ] **Stacking:** rolling the same upgrade again shows "×2", extends its clock, and Boomstick's ring grows.
- [ ] **Moonshine Timer:** with buffs running, every countdown jumps +8 s; with nothing running you get the sad-whiff toast.
- [ ] **Expiry:** when a timer hits 0 the effect stops (reload/fire rate return to normal) and the slot line disappears.
- [ ] **Pause:** upgrade timers freeze while paused, resume after.

## Testing checklist (Packet 2.2 — heavies, war saucers & health bars)

- [ ] **Health bars:** the first hit on any enemy pops a floating bar above it (green), which shades amber → red as you keep shooting; it always faces the camera and vanishes on death.
- [ ] **Hit flash:** every pellet impact flashes the enemy white for a blink — hits read clearly at night.
- [ ] **Large Alien:** big ember-orange hunter; slower than a Medium but its swipes hurt (~14).
- [ ] **Brute telegraph:** the moss-green giant crouches and bulks out for ~0.8 s before slamming — it's rooted while winding up.
- [ ] **Dodging the slam:** sprint out of the ring during the crouch → shockwave expands, you take nothing; stand inside → ~30 damage.
- [ ] **War Saucer support fire:** while hovering/beaming, it lobs slow pink plasma bolts at you; strafing sideways dodges them.
- [ ] **Weak point:** shoot the saucer's glowing dome — its health bar visibly drops ~2.5× faster than dish hits.
- [ ] **Bolt cleanup:** bolts fizzle on the ground and never pile up.
- [ ] **Squash vs smash:** shooting a winding-up Brute flashes it white but doesn't interrupt or distort the crouch.
- [ ] **Wave completion:** all five enemy types must die (6 Little + 3 Medium + 2 Large + 1 Brute + 1 War Saucer).

## Testing checklist (Packet 2.1 — enemies & tech)

- [ ] **Weaving scouts:** Little Aliens sway side-to-side on approach — noticeably harder to hit than 1.x.
- [ ] **Medium hunters:** violet, ~1.3× bigger, faster; they curve out to your side before charging; two at once come from *different* sides.
- [ ] **Light attacks:** a Medium in melee range lands quick small hits (HP ticks down ~6 at a time).
- [ ] **Saucer arrives:** after the ground spawns, one dish UFO glides in at altitude with rim lights + belly glow.
- [ ] **Air abduction:** the saucer parks over a cow and a wide cone beam lifts it — faster than ground rustlers.
- [ ] **Shooting it down:** the saucer dips when hit; on death it spins, crashes, and drops tech at the crash site.
- [ ] **Tech drops:** some dead aliens leave glowing cyan shards (bobbing, spinning, lit).
- [ ] **Magnet collect:** walk or ride within ~3 m — the shard flies to you; "ALIEN TECH" (top-right) increments (+3 from a saucer).
- [ ] **Wave completion:** the wave only completes once ground aliens **and** the saucer are dead.
- [ ] **Restart:** tech counter resets to 0 on restart.

## Testing checklist (Packet 1.3 — camera & controls)

- [ ] **Third person default:** game starts over-the-shoulder; the hillbilly capsule and shotgun tracers are visible.
- [ ] **Camera toggle:** V smoothly slides between third and first person (both on foot and mounted).
- [ ] **Mounted framing:** mounting zooms the camera out so Buttercup fits; FOV widens slightly at full gallop.
- [ ] **Camera collision:** back up against the barn/fence — the camera pulls in instead of clipping through walls.
- [ ] **Crosshair truth:** what's under the crosshair is what gets hit, in both camera modes.
- [ ] **Pause:** Esc (or P) freezes everything (aliens, beams, cows mid-lift), unlocks the cursor, shows the menu.
- [ ] **Resume:** button or Esc/P again — cursor re-locks, action continues exactly where it froze.
- [ ] **Settings:** sensitivity slider changes look speed live; Invert Y flips; both survive a restart (PlayerPrefs).
- [ ] **Restart from pause:** reloads the wave with timescale restored.
- [ ] **No pause-fire:** clicking Resume never fires the shotgun.
- [ ] **WebGL (if built):** first click captures the mouse; Esc in-browser auto-pauses; Quit button absent.

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
- **UpgradeData_*** — five wild-pool upgrades: effect amount, duration, max stacks,
  explosion damage, pool weight, and the toast flavor line.

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
Core/      GameState, GameEvents (static event bus), GameManager (win/lose/pause), IInteractable
Combat/    IDamageable, DamageInfo, Health, WeakPoint (dome bonus damage), HitFlash
Player/    PlayerInputHandler (New Input System), PlayerController (+mount/dismount),
           PlayerInteraction (proximity prompts), CameraRig (third/first person), PlayerHealth
Horse/     HorseController (Idle/Follow/Stay/Mounted state machine + riding physics)
Weapons/   Shotgun (hitscan spread + ammo/reload + explosive rounds — works on horseback),
           WeaponUpgradeController (Q = wild roll, stacking buff timers)
Enemies/   AlienEnemy (Rustler/Hunter roles, Swipe/Smash attacks), UfoEnemy (abduction +
           support fire), PlasmaBolt, EnemyRegistry (shared alive count)
Pickups/   TechPickup (magnet-collect tech shards → TechInventory)
Effects/   ShockwaveFx (Brute slam ring)
Cattle/    Cattle (abduction meter + terrain-aware wander)      [namespace: Livestock]
Waves/     WaveSpawner (drip-spawn one wave, role-aware)
Data/      WeaponData, EnemyData (roles + attack styles), WaveData, HorseData,
           UpgradeData (wild pool entries) — all ScriptableObjects
UI/        HUDController, PauseMenu, EnemyHealthBar (all self-building, event-driven)
Utils/     LowPolyFactory (all placeholder primitives), GameLayers, GroundSnap
Editor/    FarmSceneBuilder (scene generator), WebGLBuilder (browser builds — see docs/WEBGL.md)
```

> **Upgrading from Packet 1.1?** After pulling the new code, re-run
> **Tools ▸ Hillbilly ▸ Build Farm Scene** — the saved scene predates the horse,
> hills, and interaction system, and a rebuild wires them all in.

**Key idea:** systems talk through `GameEvents`, not directly to each other, and all
placeholder art comes from one `LowPolyFactory` — so swapping in real low-poly models
or a polished TMP HUD later touches almost nothing else.
