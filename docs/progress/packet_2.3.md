# Progress Report — Packet 2.3: Alien Tech Upgrades System

**Status:** ✅ Code complete, ready for in-editor verification
**Engine:** Unity 6 LTS · URP · New Input System · PC/Standalone + WebGL

## Roadmap items delivered

| Roadmap bullet | Status | Where |
|---|---|---|
| Pickup system for tech drops | ✅ (2.1) + closed the loop | `TechInventory.TrySpend` finally has a spender |
| Shotgun upgrade slots: extra ammo, faster reload, explosive bullets, rapid fire, duration extenders, stacking | ✅ | 5 `UpgradeData` assets + `WeaponUpgradeController` + live `Shotgun` hooks |
| Temporary power-up UI + timers | ✅ | HUD slot list with locally-ticked countdowns + acquisition toasts |
| Wild upgrade discovery (random pool) | ✅ | Q spends 5 tech on a weighted roll from the pool |

## What was built

### The jury-rig loop
Press **Q** (gamepad LB) mid-fight → `TechInventory.TrySpend(5)` → weighted
random roll from the wild pool → toast announces the result in hillbilly.
Rolling is deliberately a *gamble*: you never pick, and Moonshine Timer with no
buffs running whiffs with a sad toast. That tension (roll now, or bank tech and
roll during the saucer?) is the packet's fun.

### The pool (all data — `UpgradeData` SOs)
| Upgrade | Type | Effect |
|---|---|---|
| Extra Shells | instant | +12 reserve (weighted slightly common) |
| Greased Lightnin' | 20 s | reload time × 0.45 |
| Boomstick Rounds | 15 s | pellet impacts detonate (r 2.2 m, +10 dmg AoE, +25% radius/stack) |
| Hair Trigger | 15 s | fire cooldown × 0.45 |
| Moonshine Timer | instant | +8 s to every running upgrade (weighted rare) |

### Live stat modification (no asset mutation)
`WeaponUpgradeController` owns the active-buff list (remaining time + stacks);
the `Shotgun` *queries multipliers at the moment of use* — cooldown on each
trigger pull, reload time when the reload starts. WeaponData assets are never
mutated, so expiry is automatic and restarts are always clean.

**Stacking:** re-rolling an active type adds its full duration and +1 stack
(capped at the asset's `maxStacks`). Multipliers stay flat per stack (only the
clock grows — ×0.45² would have been silly); Boomstick's radius grows +25% per
stack so stacking it still *feels* bigger.

**Explosive rounds:** pellet hits are gathered per shot and detonated **once at
the impact centroid** — one clean blast per trigger pull instead of 8 pellet
micro-explosions (fairer, cheaper, reads better). AoE victims are collected in
a reused HashSet (once per enemy per shot), and the player/horse/cattle are
explicitly excluded — it's an upgrade, not a foot-gun. The Brute's shockwave
ring doubles as the blast VFX.

### UI
- **Slot list** under the tech tally: `BOOMSTICK ROUNDS x2  12s`, ticking down
  every frame. The HUD mirrors state locally from `UpgradeChanged` /
  `UpgradeExpired` events and counts down on its own — the event bus stays
  primitives-only and there's no per-frame cross-system polling.
- **Toasts** center-low for every roll: flavor lines, stack announcements, and
  the "Need 5 tech!" rejection. One-time hint toast at wave start.

## Design decisions & rationale

1. **Roll, don't shop.** A pick-from-menu upgrade shop mid-wave would stop the
   action dead. One button + randomness keeps it arcade and makes tech feel
   like slot-machine quarters. (A between-waves shop can still slot into 3.1's
   rest periods if wanted — `TrySpend` doesn't care who calls it.)
2. **Query-based buffs over stat mutation.** The shotgun asking "what's my
   multiplier right now?" means no restore-on-expiry bookkeeping, no corrupted
   WeaponData assets, and pause Just Works (timers tick on scaled time).
3. **HUD-local countdowns.** Events fire only on change; the HUD extrapolates.
   Keeps the bus quiet and the countdown smooth.

## Known limitations / notes

- The pool is shotgun-focused by design; horse/player upgrades (Phase 5
  cosmetics/stretch) would be new `UpgradeType`s.
- Explosion excludes cattle implicitly (no `IDamageable`) and player/horse
  explicitly — but a future destructible fence WOULD take blast damage, which
  is probably correct and fun.
- Upgrade toasts and the wave banner can overlap visually if a wave starts
  mid-toast; cosmetic, revisit in 4.3's UI polish.
- No sound on roll/expiry yet (4.2).

## Performance notes

- Rolls are rare; per-frame cost is a handful of timer decrements.
- Explosion path: one `OverlapSphereNonAlloc` (24-collider buffer) + a reused
  HashSet per shot — zero steady-state allocations.

## How to verify

**Re-run `Tools ▸ Hillbilly ▸ Build Farm Scene` after pulling** (creates the 5
upgrade assets and wires the pool into the player). Then `SETUP.md` → "Testing
checklist (Packet 2.3)". The money moment: stacked Boomstick ×2 + Hair Trigger,
galloping the fence line, every trigger pull blooming blast rings through a
scout pack.

## Suggested next packet

**Packet 3.1 — Farm Wave System**: multiple escalating waves with rest periods
(the natural home for "bank tech between waves"), increasing variety/count, and
the progression gate (enough cattle saved → the mothership appears). The
spawner was built to accept a list of `WaveData` from day one.
