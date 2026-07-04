# Progress Report — Packet 1.1: Project Setup & Core Loop

**Status:** ✅ Code complete, ready for in-editor verification
**Engine:** Unity 6 LTS · URP · New Input System · PC/Standalone

## Roadmap items delivered

| Roadmap bullet | Status | Where |
|---|---|---|
| Initialize project, low-poly scene (ground, fences, barn, trees) | ✅ | `LowPolyFactory`, `FarmSceneBuilder` |
| Basic player controller (WASD + mouse look) | ✅ | `PlayerController`, `PlayerInputHandler` |
| Shotgun weapon with basic shoot + reload | ✅ | `Shotgun` + `WeaponData` |
| Simple wave spawner (1 wave) | ✅ | `WaveSpawner` + `WaveData` |
| Cattle entities with health + abduction beam logic | ✅ | `Cattle`, `AlienEnemy` beam |
| Cattle Saved vs Taken UI counter | ✅ | `HUDController` |
| Win/lose conditions for a single wave | ✅ | `GameManager` |

## Architecture decisions

- **Static `GameEvents` bus** decouples gameplay from UI/flow. Payloads are
  primitives/`GameObject` only, so `Core` never depends on gameplay types.
- **ScriptableObjects** (`WeaponData`, `EnemyData`, `WaveData`) drive tuning, each
  with a `CreateDefault()` fallback so components run even with nothing assigned.
- **`LowPolyFactory`** is the single source of truth for placeholder art, shared by
  the runtime spawner and the editor builder — they can't drift.
- **`FarmSceneBuilder`** (editor menu) assembles the whole scene + data assets and
  wires references, so setup is two clicks instead of manual placement.
- **Input built in code** (New Input System) — no `.inputactions` asset or codegen to
  misconfigure now; trivially swappable to an asset + rebinding UI in Packet 1.3.

## Deliberately deferred (future packets)

- Third-person camera & horse riding → **1.2 / 1.3**
- `.inputactions` asset + rebinding UI, pause menu → **1.3**
- Enemy variety, tech drops, UFO beams → **2.x**
- Multiple escalating waves + rest periods → **3.1**
- TMP HUD polish, real VFX/particles, audio → **4.x**

## Known limitations at this stage

- Aliens have no mutual avoidance (can clump on a cow). Fine for 1.1; NavMesh/steering later.
- First-person for now (shotgun aims from camera). Camera rig is on a pivot so 1.3 can
  pull it back to third-person without touching control math.
- Placeholder shrink-poof death & code-drawn beams stand in for real VFX (Packet 4.3).
- Runtime-created materials aren't pooled/shared per-instance (negligible at this scale).

## How to verify

See `SETUP.md` → "Testing checklist". Short version: build the scene, press Play,
defend the herd, confirm win/lose/restart all fire.
