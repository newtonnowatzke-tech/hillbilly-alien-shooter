# Progress Report — Packet 1.3: Camera & Controls Polish (+ WebGL)

**Status:** ✅ Code complete, ready for in-editor verification
**Engine:** Unity 6 LTS · URP · New Input System · PC/Standalone + **WebGL**

## Roadmap items delivered

| Roadmap bullet | Status | Where |
|---|---|---|
| Third-person camera that follows player/horse nicely | ✅ | `CameraRig` — collision-aware orbit on the existing pivot, mounted zoom-out, gallop FOV kick, V toggles first person |
| Input system (mobile-friendly if wanted later) | ✅ | Settings surface (sensitivity/invert Y) + everything action-based; touch bindings can be added to the same actions later |
| Basic pause menu + settings stub | ✅ | `PauseMenu` — Esc/P/Start, Resume/Settings/Restart/Quit, persisted settings |
| **Bonus (user request): WebGL playtest version** | ✅ | `WebGLBuilder` + GitHub Actions → GitHub Pages pipeline + `docs/WEBGL.md` |

## What was built

### Third-person camera (`Player/CameraRig.cs`)
The 1.1 decision to hang the camera off a head-height **pivot** paid out in full:
the rig only slides the camera along a local offset, keeping the pivot's
orientation. Consequences:
- **Aim stays truthful** — same crosshair math in both modes; the shotgun now
  raycasts from the pivot (not the camera) so third person can't shoot things
  *behind* the player's back.
- **First ↔ third person toggle (V)** is just an offset lerp — it even animates.
- **Collision**: a sphere-cast from pivot to desired camera position pulls the
  camera in front of barns/fences/hills; the player's own hierarchy and any
  horse are ignored (crucial: while mounted, the player's root *is* the horse).
- **Feel**: mounted zooms out 1.35×, FOV widens up to +9° with gallop speed.

### Pause menu + settings (`UI/PauseMenu.cs`, `GameManager`)
- `GameManager` gains `Pause/Resume/TogglePause` (timescale 0 ↔ 1, `Paused`
  state) with timescale hygiene on scene load/restart.
- All gameplay already froze correctly via `Time.deltaTime`/coroutines, and the
  existing GameState gating meant the player, horse, and interaction system
  needed **zero changes** to pause. Only the shotgun needed a state gate so a
  Resume click can't discharge it.
- Self-building UI (buttons, sliders, cycling Invert-Y toggle) with an
  auto-created `EventSystem` + `InputSystemUIInputModule`.
- Settings persist in PlayerPrefs and apply live: mouse sensitivity → input
  handler, invert Y → look math, master volume → `AudioListener` (ready for the
  4.2 audio packet).

### WebGL pipeline (`Editor/WebGLBuilder.cs`, workflows, `docs/WEBGL.md`)
Two paths, one build core:
- **Local:** `Tools ▸ Hillbilly ▸ Build WebGL` → `build/WebGL/`, with
  serve-locally and itch.io instructions.
- **CI:** `.github/workflows/webgl-deploy.yml` builds with GameCI and deploys
  to **GitHub Pages** on every push to `main` (plus manual dispatch). Requires
  a one-time free Unity license secret setup — fully documented, including a
  helper workflow that generates the activation file if the local `.ulf` is
  missing.

The clever bit: this repo is **scripts-only**, so the CI builder self-heals a
fresh project — it force-enables the New Input System (fresh projects default
to legacy input), creates/assigns a URP pipeline asset when none exists (else:
pink materials), regenerates the farm scene headlessly, and builds with
**Gzip + decompression fallback** so the output runs on any static host with no
header configuration. `Packages/manifest.json` + `ProjectVersion.txt` pin the
CI editor; they don't affect local projects.

Browser quirks handled in game code:
- First click captures the pointer (browsers require a gesture); if the lock is
  lost mid-play (Esc, alt-tab), the game **auto-pauses** instead of flailing.
- **P** pauses too, since browsers eat Esc for pointer-lock release.
- Quit button hidden on WebGL.

## Design decisions & rationale

1. **No Cinemachine.** A bespoke ~150-line rig keeps the zero-dependency rule,
   and this game needs exactly one camera behaviour. Cinemachine earns its
   weight when you need many blended virtual cameras — revisit for cutscenes (4.1).
2. **Aim from pivot, render from camera.** Standard third-person-shooter split;
   prevents between-camera-and-player hits with a one-line aim-source change.
3. **Settings stub stayed a stub.** Roadmap says stub: three useful settings,
   persisted, done. Rebinding UI and a `.inputactions` asset migration are
   deliberately deferred until a packet actually needs them.
4. **Auto-pause on pointer-lock loss.** Turns a browser limitation into correct
   behaviour for free (alt-tab pauses on desktop too).

## Known limitations / notes

- **CI needs your license secrets before the workflow goes green** — the
  workflow fails fast with a pointer to `docs/WEBGL.md` until then. Local
  builds need nothing.
- The pinned CI editor (6000.0.34f1) is arbitrary within 6000.0.x — bump
  `ProjectSettings/ProjectVersion.txt` freely; no serialized assets depend on it.
- Third-person exposes placeholder art more (you now *see* the hillbilly
  capsule); cosmetic upgrade lands with the art pass (4.3).
- WebGL performance is comfortably fine at this scale (one scene, ~20 actors,
  primitive meshes); worth re-profiling when UFOs and particles arrive (2.2/4.3).

## Performance notes

- Camera: one sphere-cast per frame (8-hit non-alloc buffer) — negligible.
- Pause menu UI exists once, toggled by `SetActive` — no per-frame cost hidden.
- FOV/offset lerps use unscaled time so the camera stays alive while paused
  (menu feels responsive) without advancing gameplay.

## How to verify

`SETUP.md` → "Testing checklist (Packet 1.3 — camera & controls)", and
`docs/WEBGL.md` for the browser build. Fun check: gallop at the barn, watch the
camera tuck in; pause mid-abduction and watch the cow hang frozen in the beam.

## Suggested next packet

**Packet 2.1 — Little & Medium Aliens**: enemy variety (Medium chase/flank AI),
the death→tech-drop pipeline, and UFO abduction beams — the foundation the
whole upgrade system (2.3) builds on. The `EnemyData` SO + factory pattern
means Mediums are mostly data + one new behaviour branch.
