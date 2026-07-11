# Progress Report — Packet 3.1: Farm Wave System

**Status:** ✅ Code complete, ready for in-editor verification
**Engine:** Unity 6 LTS · URP · New Input System · PC/Standalone + WebGL

## Roadmap items delivered

| Roadmap bullet | Status | Where |
|---|---|---|
| Multiple escalating waves on farm | ✅ | `WaveSpawner` campaign loop + 5 authored `WaveData` assets |
| Increasing enemy variety + count | ✅ | Escalation curve: scouts → hunters → saucers/heavies → Brutes → armada; intervals 1.3 s → 0.8 s |
| Wave complete triggers + brief rest periods | ✅ | `WaveCompleted`/`RestStarted` events, 12 s breather with heal + restock + jury-rig prompt |
| Progression gate: enough cattle saved → alien ship appears | ✅ | `GameManager` gate (≥ 3 cows) → `MothershipSummoned` + the descent set piece |

## What was built

### The campaign loop (`WaveSpawner` rewrite)
One coroutine now runs the whole night: announce wave *i of N* → drip-spawn →
wait for `EnemyRegistry` to hit zero → announce the clear → 12 s rest → next.
After the final wave it raises `CampaignCompleted` and lets the GameManager
decide what that means. Spawning stops cold on win/lose, so nothing piles onto
the end screens. Wave events grew a `totalWaves` arg (`Action<int,int>`).

### The escalation (authored data, refreshed each scene rebuild)
| # | Name | Composition | Interval |
|---|---|---|---|
| 1 | First Contact | 6 Little | 1.3 s |
| 2 | Rustle Up | 8 Little + 3 Medium | 1.15 s |
| 3 | Saucer Season | 8 Little + 4 Medium + 2 Large + Scout Saucer | 1.0 s |
| 4 | Heavy Metal | 6 Little + 4 Medium + 3 Large + Brute + Scout Saucer | 0.95 s |
| 5 | The Whole Dang Armada | 8 Little + 5 Medium + 3 Large + 2 Brutes + War Saucer + Scout Saucer | 0.8 s |

### Rest periods that matter
Between waves: **+20 HP** (PlayerHealth listens to `WaveCompleted`), **+8
reserve shells** "from the truck" (Shotgun listens, with a toast), and the HUD
counts the breather down with a *jury-rig now* prompt — 2.3's upgrade gamble
finally has its natural home. Everything works during the rest: reload,
reposition, whistle, roll.

### The progression gate
Clearing all five waves = **Won**. But the *ending* depends on the herd:
- **≥ 3 cows saved** → `MothershipSummoned` + `LowPolyFactory.BuildMothership`:
  a colossal 18-unit gunmetal saucer with double light rings and a sickly green
  farm-wide glow **descends from y=60 and looms** (`MothershipFx`). End screen:
  "THE MOTHERSHIP DESCENDS… TO BE CONTINUED (Packet 3.2)".
- **1–2 cows** → "FARM DEFENDED!" but the varmints got away — the hint tells
  the player exactly what to do better. Both endings show saved/rustled stats.

The gate makes cattle defense the *strategic* layer across all five waves: you
can win sloppy, but you can't *progress* sloppy.

## Design decisions & rationale

1. **Spawner reports, GameManager decides.** The spawner knows waves; it does
   not know what winning means. `CampaignCompleted` is a fact, the gate is a
   rule — 3.2/3.3 levels can reuse the spawner with different rules untouched.
2. **Rest perks via listeners.** Heal and restock are one subscription each on
   the components that own those stats — the spawner stays ignorant of the
   player, and both perks are tunable serialized fields.
3. **Mothership as pure set piece.** No collider, no health — it's a promise,
   not a fight. 3.2 attaches the boarding interaction to it.
4. **Losing early kills the faucet.** `StopAllCoroutines` on Won/Lost — a small
   thing that keeps end screens clean and restarts snappy.

## Balance snapshot

Full run ≈ 8–10 minutes. Tech economy across 5 waves supports roughly 5–8
jury-rig rolls; wave 5 without at least one damage-relevant buff (Boomstick /
Hair Trigger) is *spicy*. The herd realistically ends around 3–5 cows for a
careful player — the gate (3) should be reachable-but-not-free. Playtest reads
welcome: rest length (12 s), heal (+20), restock (+8) are all one-field tweaks.

## Known limitations / notes

- The mothership appears on the *win screen* rather than as an interactive
  sequence — intentional; boarding (cinematic + interior level) is 3.2's whole
  packet.
- No between-wave save point — save/load is 4.4.
- Wave names exist in data but aren't shown on the HUD yet (banner shows
  numbers); easy 4.3 flourish to add them.
- Difficulty is authored, not adaptive; fine for the target 1h20 arc.

## Performance notes

Nothing new per-frame; the campaign is the same spawner cadence 5×. Peak alive
count (~wave 5) is ≈ 20 enemies + bars + bolts — still trivial for the target.

## How to verify

**Re-run `Tools ▸ Hillbilly ▸ Build Farm Scene` after pulling** (authors
Wave1–5 assets and wires the campaign list). Then `SETUP.md` → "Testing
checklist (Packet 3.1)". Fast gate test: set Cattle To Summon Mothership to 1
on the GameManager, clear the run, watch the big one come down.

## Suggested next packet

**Packet 3.2 — Alien Ship Boarding & Space Transition**: interact with the
landed/looming mothership → boarding cinematic trigger → simple ship-interior
level (new environment from the factory) → adjusted enemy behaviour inside.
The `IInteractable` prompt system and the multi-scene flow both exist already.
