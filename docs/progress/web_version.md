# Progress Report — Standalone Web Version (no Unity)

**Status:** ✅ Built, runtime-verified in Chromium, ready to play
**Tech:** Plain HTML5 + Canvas 2D + vanilla JS — one file, zero dependencies

## Why this exists

The Unity project is the "real" 3D game, but a Unity build can only be produced
by Unity (locally or via a cloud CI that needs a Unity license). To let the game
be **played with no Unity involvement at all** — no editor, no account, no
license — this is a from-scratch browser port that runs by simply opening
`web/index.html`. It's the play-anywhere companion to the Unity version.

## What's in it (feature parity with the farm game)

- **Top-down twin-stick-style shooter** on a scrolling night farm (barn, fence
  border, tree ring, grass detail, vignette + camera shake).
- **Hillbilly + shotgun** — hitscan 8-pellet spread with per-pellet ray/enemy
  intersection, tracers, muzzle flash, magazine/reserve ammo, timed reload
  (auto-reload on empty).
- **Buttercup the horse** — trots after you, press **E** to mount for a big
  speed boost; shoot freely while riding.
- **Cattle** — wander, get beamed up (rise + fade), and settle back down if you
  shoot the alien off. Lose them all and it's over.
- **Six enemy archetypes** — Little (weaves to cows), Medium (flanking hunter),
  Large (tanky hunter), Brute (telegraphed AoE ground slam), Scout Saucer
  (air abduction beam, must be shot down), War Saucer (also lobs plasma bolts).
  Per-enemy health bars, white hit-flash, squash/death bursts.
- **Five-wave campaign** with 12 s rest periods (heal +20, restock +8, jury-rig
  prompt), escalating exactly like the Unity `WaveData` sequence.
- **Alien tech** drops that magnet-collect; **Q** spends 5 tech on a wild
  upgrade roll (Extra Shells, Greased Lightnin', Boomstick Rounds, Hair Trigger,
  Moonshine Timer) with stacking and live HUD timers.
- **Progression gate** — finish with ≥ 3 cows and the **mothership descends**;
  fewer and the varmints get away. Full end screen with stats + restart.
- **Mobile support** — virtual move joystick (left), hold-to-fire with auto-aim
  (right), and on-screen Reload / Tech / Horse buttons. Responsive to any
  screen, retina-aware.

## Delivery

- **`web/index.html`** — the whole game, committed to the repo.
- **`.github/workflows/pages-deploy.yml`** — static GitHub Pages deploy on every
  push to `main` (no Unity, no secrets). One-time: enable Pages → GitHub Actions.
- The Unity `webgl-deploy.yml` was switched to **manual-only** so the two don't
  both fight over the Pages site.

## Verification

- `node --check` on the extracted script: clean.
- Launched in headless Chromium (Playwright): loads, starts, spawns the herd,
  enters Wave 1, spawns enemies, player takes input and fires — **zero console
  or page errors** across a simulated play session. Menu + gameplay screenshots
  captured and look correct.

## Known limitations / next steps

- It's 2D top-down (vs. the Unity 3D). Deliberate — it's the zero-dependency,
  runs-anywhere build.
- Audio not wired yet (matches the Unity project's state; audio is Phase 4).
- The mothership "boarding" is an end-screen set piece, same as Unity 3.1.
- Balance mirrors the Unity campaign; tune constants at the top of the enemy /
  wave tables in `web/index.html`.

Future roadmap packets can be mirrored into this file as they land, keeping the
web version tracking the Unity one.
