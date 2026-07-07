# Progress Report — Packet 2.2: Large Aliens, Brutes & UFOs

**Status:** ✅ Code complete, ready for in-editor verification
**Engine:** Unity 6 LTS · URP · New Input System · PC/Standalone + WebGL

## Roadmap items delivered

| Roadmap bullet | Status | Where |
|---|---|---|
| Large Alien: tougher, medium damage attacks | ✅ | `EnemyData_LargeAlien` — **pure data** on the Hunter role (1.7×, 90 HP, 14 dmg / 1.4 s) |
| Brute: tanky, slow, high damage smash | ✅ | `AttackStyle.Smash` + `SmashRoutine` (crouch telegraph → AoE slam + `ShockwaveFx` ring) |
| Dish UFOs: hover, support fire, abduction beams, weak points | ✅ | `UfoEnemy.HandleSupportFire` + `PlasmaBolt`; `WeakPoint` on the dome (2.5×) |
| Enemy health bars + hit reactions (low-poly squash/stretch) | ✅ | `EnemyHealthBar` billboard bars + `HitFlash` white blink (on top of 1.1's squash & the saucer dip) |

## What was built

### The heavies
- **Large Alien** shipped with *zero new code* — the payoff of 2.1's role system.
  It's an asset: Hunter role, 1.7× scale, ember-orange, 90 HP, medium swipes,
  shallower flank (it bullies more than it circles).
- **Brute** added exactly one new mechanic: `AttackStyle.Smash`. In melee range
  it roots itself, crouches and bulks out for `smashWindup` seconds (the dodge
  window — readable at a glance), then slams: an expanding `ShockwaveFx` ring
  marks the AoE and anyone still inside `smashRadius` eats 30 damage. Shooting
  a winding-up Brute flashes it but deliberately doesn't interrupt the
  telegraph (the squash hit-react is suppressed during the wind-up so the two
  scale animations never fight).

### Combat saucers
- **War Saucer**: the scout's kit plus **support fire** — slow, glowing pink
  plasma bolts lobbed at the player every 2 s while it hovers/beams. Bolts are
  deliberately dodgeable (area denial, not sniping), fizzle on the dirt, and
  self-clean after 5 s. Support fire is enabled purely by data
  (`projectileDamage > 0`), so the Scout Saucer remains unarmed.
- **Weak points**: the glowing dome now carries its own collider + a
  `WeakPoint` component that forwards 2.5× damage to the hull `Health`. The
  dish hitbox was reshaped from a fat sphere to a flat box specifically so the
  dome pokes out and dome shots actually land on the dome collider — with the
  old sphere, the weak point would have been unhittable.

### Hit feedback suite
- **`EnemyHealthBar`** — world-space billboard bar (two unlit quads, colliders
  stripped so they never eat pellets). Hidden until first damage, fill shades
  green → amber → red, hides on death. Deliberately *not* parented to the enemy
  (squash reactions scale the enemy root — a parented bar would squash with
  it); it follows and billboards in LateUpdate and cleans itself up on despawn.
- **`HitFlash`** — every renderer under an enemy blinks white for 0.07 s on
  damage. Works with the factory's per-instance materials, skips
  LineRenderers (beams), and restores colors on disable so corpses never
  freeze white.

## Design decisions & rationale

1. **Brute telegraph over reaction-time damage.** The wind-up is the whole
   fight: it converts the Brute from a stat check into a positioning test, and
   it reads in silhouette (crouch + bulk) even in low light.
2. **Weak point via collider forwarding.** The shotgun resolves `IDamageable`
   from the collider it hit, so a child collider + forwarding component needed
   no weapon changes at all — and the same pattern gives 3.4's Alien King
   multi-phase weak spots for free.
3. **Bolts are distance-checked, not physical.** Consistent with the codebase's
   physics-light style; they can't push things, tunnel, or collide with the
   horse by accident.
4. **Health bars outside the enemy hierarchy.** Costs one manual follow per
   frame, buys immunity from every scale-based hit reaction now and later.

## Balance snapshot (Wave 1 = 6 Little + 3 Medium + 2 Large + 1 Brute + 1 War Saucer)

A real gauntlet now (~13 enemies, 3 archetype tiers + air support). The Brute
is intentionally a "kite it while managing everything else" problem, and the
War Saucer's bolts punish standing still. If it's too much, swap the War Saucer
back to the Scout (`WaveData_Wave1`) or drop the Larges to 1.

## Known limitations / notes

- Plasma bolts pass through the barn/trees (no world collision) — acceptable at
  farm scale, revisit for the ship interior in 3.2.
- The Brute's slam doesn't knock the player back (no impulse system on the
  CharacterController yet); the `DamageInfo.Force` field is already populated
  for when we add one.
- Health-bar quads use `Sprites/Default` (unlit, always visible) — they don't
  fade with distance; fine at farm scale.
- Weak-point damage isn't visually distinct yet (same flash) — a crit-flash /
  damage-number pass belongs to 4.3 polish.

## Performance notes

- Health bars: one transform follow + billboard per damaged enemy per frame; no
  canvas, no raycasting.
- HitFlash caches materials once at spawn; flashing is two color writes.
- Bolts: ≤ a handful alive at once, one distance check each. All trivial.

## How to verify

**Re-run `Tools ▸ Hillbilly ▸ Build Farm Scene` after pulling** (creates the
three new EnemyData assets, re-authors Wave 1). Then `SETUP.md` → "Testing
checklist (Packet 2.2)". The money moment: a Brute crouching as you gallop
clear, its shockwave ripping across the grass behind you while you pop the war
saucer's dome mid-jump off a hill.

## Suggested next packet

**Packet 2.3 — Alien Tech Upgrades System**: spend the tech that's been piling
up — pickup-driven shotgun upgrade slots (extra ammo, faster reload, explosive
rounds, rapid fire, duration extenders, stacking), temporary power-up UI with
timers, and the wild-upgrade random pool. `TechInventory.TrySpend` and
`WeaponData` were built for this moment.
