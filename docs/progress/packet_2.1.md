# Progress Report — Packet 2.1: Little & Medium Aliens

**Status:** ✅ Code complete, ready for in-editor verification
**Engine:** Unity 6 LTS · URP · New Input System · PC/Standalone + WebGL

## Roadmap items delivered

| Roadmap bullet | Status | Where |
|---|---|---|
| Little Alien: scout behavior, low health, annoying paths | ✅ | Weave system in `AlienEnemy.MoveTowards` driven by `EnemyData.weaveAmplitude/Frequency` |
| Medium Alien: faster movement, light attack, simple AI (chase or flank) | ✅ | `EnemyRole.Hunter` + `HuntPlayerFlanking` (side-offset approach → committed charge → quick light melee) |
| Basic alien death + drop system (tech pickups) | ✅ | `techDropChance`/`techAmount` on death → `TechPickup` (magnet-collect) → `TechInventory` → HUD tally |
| Abduction beam from UFOs to cattle | ✅ | `UfoEnemy` — hovers to the nearest cow, cone beam from the air, must be shot down |

## What was built

### Role-driven enemies (one class, three archetypes)
`EnemyData` gained an `EnemyRole` enum (**Rustler / Hunter / Saucer**) plus role
tuning blocks. The factory dispatches (`BuildEnemy`) and the spawner stays
completely generic — **Medium Aliens are 90% data**: a violet tint, 1.3× body
scale, faster speed, and the Hunter branch.

- **Weave ("annoying paths")** — approach targets get a lateral sine offset
  (per-alien random phase so squads don't sway in sync). Littles weave hard;
  Mediums keep a light jink even while flanking; anything with amplitude 0
  walks straight. One mechanism, all tuned in data.
- **Flanking** — each Hunter rolls a side once, then aims at
  `player + perpendicular × flankOffset` until inside `flankCloseRange`, where
  it charges straight and swings quick light melee hits (6 dmg / 0.8 s).
  Two hunters naturally pincer from opposite sides half the time — cheap code,
  emergent menace.

### Scout Saucer (`UfoEnemy`)
The first airborne threat: cruises at 9 m with an ominous bob and banking,
parks above the nearest cow, and pours a **cone beam** (narrow at the belly,
flooding wide at the ground) that abducts *faster than ground rustlers*.
It can't be body-blocked — the only counterplay is shooting it down, so it
functions as a priority-target puzzle in the middle of a brawl. Hits make it
dip and jolt; death is a spin-out crash that always pays 3 tech. Weak points
and support fire are reserved for Packet 2.2 as planned.

### Tech drop pipeline
- `TechPickup` — glowing cyan shard (two interlocked cubes + a point light that
  earns its keep on the night farm). Bobs in place; within ~3 m it magnets to
  the player and collects — tuned so a gallop drive-by scoops drops without
  dismounting. Distance-based, zero physics plumbing.
- `TechInventory` (static, event-raising) with `Add`/`TrySpend` — 2.3's upgrade
  system already has its spending API waiting.
- HUD: "ALIEN TECH n" top-right via the existing event bus.

### Infrastructure
- **`EnemyRegistry`** replaces `AlienEnemy.ActiveCount` — with two enemy classes,
  wave completion needed a shared alive-counter. WaveSpawner now waits on it;
  GameManager resets it (and `TechInventory`) on scene load.
- `Cattle.FindNearest(pos)` static — cow targeting shared by ground aliens and
  saucers instead of two private copies.

## Balance snapshot (Wave 1 = 8 Little + 4 Medium + 1 Saucer)

Deliberately spicier than 1.x: Mediums force you to fight *while* defending,
and the saucer punishes tunnel vision. If it's too hot, drop Medium count to 2
or delay the saucer (reorder the spawn list) in `WaveData_Wave1`.

## Known limitations / notes for next packets

- Hunters ignore cattle entirely (by design — role clarity). If all cattle die
  the wave devolves into a pure brawl, which is fine.
- The saucer's beam ignores line-of-sight (it's 9 m up; irrelevant on the farm,
  may matter on the alien ship in 3.2).
- No enemy health bars yet — that's explicitly Packet 2.2, and the saucer's
  120 HP is where their absence starts to be felt.
- Rebuilding the scene **overwrites Wave 1's composition** (so new enemy types
  flow into existing projects); manual WaveData tuning survives until the next
  rebuild. Called out in the builder comment and README.
- Tech currently accumulates with nothing to spend it on — 2.3 closes the loop.

## Performance notes

- Weave/flank: pure arithmetic, no allocations.
- Pickups: one distance check per pickup per frame; point lights per pickup are
  fine at typical drop counts (≤ ~8 alive at once), worth batching if 2.3
  multiplies drop rates.
- UFO: one cow scan per frame (list walk over ≤ 6 cows) — trivial.

## How to verify

`SETUP.md` → "Testing checklist (Packet 2.1 — enemies & tech)". **Re-run
`Tools ▸ Hillbilly ▸ Build Farm Scene` after pulling** — it creates the two new
EnemyData assets and refreshes Wave 1's composition.

## Suggested next packet

**Packet 2.2 — Large Aliens, Brutes & UFOs**: Large tanky melee, the Brute
smash, combat saucers with support fire + weak points, and enemy health bars +
hit reactions. The role system means Larges/Brutes are mostly new data + one
attack behaviour; health bars get their own small world-space UI system.
