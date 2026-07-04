# Progress Report — Packet 1.2: Player Movement & Horse

**Status:** ✅ Code complete, ready for in-editor verification
**Engine:** Unity 6 LTS · URP · New Input System · PC/Standalone

## Roadmap items delivered

| Roadmap bullet | Status | Where |
|---|---|---|
| Horse finding + mounting | ✅ | `HorseController` (IInteractable) + `PlayerInteraction` prompt system |
| Horse riding physics (speed boost, turning) | ✅ | `HorseController.TickMounted` / `ApplyMotion` + `HorseData` |
| Shooting while riding | ✅ | Free-look decoupled from horse heading; `Shotgun` needed **zero changes** |
| "Bring horse with you" (follow / until alien bag) | ✅ | Follow/Stay states + whistle toggle + catch-up gallop + teleport failsafe |
| Basic collision + terrain following on farm | ✅ | CharacterController on horse, hills via `BuildHill`, `GroundSnap` for aliens, terrain-aware cattle wander |

## What was built

### New scripts
- **`Horse/HorseController.cs`** — the heart of the packet. State machine (`Idle / Follow / Stay / Mounted`) over a shared momentum core: speed eases toward a target with separate acceleration/braking/coast rates, and yaw rate lerps from 140°/s at a stand to 65°/s at full gallop. Follow mode throttles by facing error (no full-speed drifting into turns) and teleports if abandoned > 45 m. Implements `IInteractable` for mount/dismount with a clear-spot capsule check.
- **`Player/PlayerInteraction.cs`** — timer-based non-alloc overlap scan → nearest usable `IInteractable` → HUD prompt via event → Interact on E. Deliberately proximity-based (not crosshair-raycast) so mounting feels forgiving.
- **`Core/IInteractable.cs`** — tiny contract reused by every future interactable (tech pickups 2.3, ship boarding 3.2).
- **`Data/HorseData.cs`** — full riding/follow tuning + cosmetics in one SO (`HorseData_Buttercup`).
- **`Utils/GameLayers.cs`** — numeric ground layer (3) + mask; no TagManager setup needed.
- **`Utils/GroundSnap.cs`** — LateUpdate raycast ground-hugging for kinematic AI (aliens).

### Modified
- **`PlayerController`** — `MountTo(seat)` / `DismountTo(pos)`: parents the body to the saddle, disables the CharacterController, keeps `HandleLook` running while mounted and skips `HandleMove` (the horse consumes `MoveInput` instead). Camera pivot pattern from 1.1 paid off — riding required no camera work.
- **`PlayerInputHandler`** — new `Interact` (E / north button) and `Whistle` (H / d-pad up) actions, same code-built pattern.
- **`GameEvents`** — `InteractPromptChanged`, `HorseStateChanged` (+ ResetAll coverage).
- **`HUDController`** — centre-screen interaction prompt + horse status line under the HP readout.
- **`Cattle`** — wander now raycasts terrain height so the abduction lift starts from the true surface.
- **`LowPolyFactory`** — `BuildHorse` (17-primitive chestnut mare with saddle, blaze, mane, hooves + seat transform), `BuildHill` (buried squashed sphere = walkable dome), ground-layer assignment, `GroundSnap` on aliens, `PlayerInteraction` on the player.
- **`FarmSceneBuilder`** — creates `HorseData_Buttercup.asset`, places Buttercup by the barn facing the pasture, scatters three rolling hills away from the herd.

## Design decisions & rationale

1. **Aim decoupled from heading.** While mounted, A/D steers the horse and the mouse steers *you*. This is what makes mounted combat a upgrade instead of a compromise — circle-strafing a rustler cluster at a gallop is the fantasy.
2. **First-person retained for this packet.** The roadmap puts the third-person camera in 1.3. Because the camera rides a dedicated pivot, that swap stays cheap.
3. **Whistle as a state toggle, not a summon-to-point.** Simple, readable, and the exact seam where the roadmap's "alien bag" carry/summon mechanic will plug in during Phase 2.
4. **Proximity interaction over crosshair raycast.** Nobody wants to pixel-hunt a horse. The scanner runs every 0.15 s with a fixed non-alloc buffer — effectively free.
5. **Hills as buried ellipsoids.** Slopes stay under CharacterController limits by construction, colliders are exact, zero terrain-system overhead at this stage.

## Bugs prevented during development (worth knowing)

- **Parenting vs `transform.root`:** while riding, the player's root *is* the horse — the interaction scanner now compares against the player transform directly, otherwise the dismount prompt would never appear.
- **Game-over interaction leak:** `PlayerInteraction` gates on `GameState.Playing`, so you can't mount from the end screen; prompts clear on state change.
- **Dismount into geometry:** dismount tries left → right → behind with a capsule clearance check before falling back to "drop on the saddle and let gravity sort it".

## Known limitations / notes for next packets

- **Horse is invulnerable** (aliens ignore her). Intentional for now — "Buttercup is too stubborn to die." Revisit if she should kite melee aliens in 2.x.
- **First-person saddle view** clips slightly through the mane at extreme downward pitch; disappears with the 1.3 third-person camera.
- **No riding animations** — legs are static (squash/stretch juice budgeted for Packet 4.3). Speed feel comes entirely from physics right now.
- **Hills vs trees:** a treeline tree can straddle a hill edge and sink its trunk base; cosmetic only, placeholder art.
- **One horse assumed** — the whistle handler polls the single player's input; multiple horses would all respond. Fine until stretch-goal cosmetics/co-op.

## Performance notes

- Interaction scan: `OverlapSphereNonAlloc` (16-collider buffer) at ~6.7 Hz — negligible.
- GroundSnap: one raycast per alien per frame against a 2-collider ground mask — trivially cheap at wave scale (≤ ~20 aliens).
- No new per-frame allocations introduced.

## How to verify

`SETUP.md` → "Testing checklist (Packet 1.2 — horse)". Highlights: mount by the barn, gallop the fence line shooting sideways, dismount, whistle her to stay, sprint away, whistle again, watch her catch up over a hill.

## Suggested next packet

**Packet 1.3 — Camera & Controls Polish:** third-person follow camera (biggest feel upgrade now that riding exists), migrate input to a serialized `.inputactions` asset with rebinding, pause menu + settings stub. Alternatively jump to 2.1 (Medium aliens + UFO beams) if you'd rather grow combat first — the codebase is ready for either.
