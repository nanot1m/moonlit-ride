# Moonlit Ride

## Continue on another computer

Clone this repository, install Node.js 22+, then run `npm ci`, `npm test`, and `npm start`. Open http://127.0.0.1:8765. In Codex, open the cloned folder and ask it to read `HANDOFF.md` before continuing.

Run `npm run build:pages` before committing updates; GitHub Pages serves `docs/` from `main`.


A standalone, stylized 3D browser-game prototype inspired by the supplied coastal cycling reference. Includes an endless winding waterfront, a bicycle and rider, illuminated houses, boats, lanterns, trees, and collectible fireflies. Geometry is procedural; this is an initial stylized prototype, not a reproduction of the reference's detailed art.

## Visual and audio upgrade

The route now climbs and descends through rolling coastal hills, with grade affecting riding speed. Added mountain silhouettes, animated water shading, textured paving and plaster, bloom, detailed windows and shutters, awnings, pine trees, patterned rider clothing, smoother leg animation, and bicycle lighting.

An original generative ambient score starts after clicking Let’s ride. Music has a mute toggle and volume slider, and softens when paused. Audio is synthesized locally with Web Audio; no external recordings are used.

This remains stylized procedural artwork, not AAA production assets.

## Handling and speed update

The bike uses a fixed 120 Hz simulation with pedal power, gravity, aerodynamic drag, rolling resistance, braking to a full stop, smoothed bicycle steering, turn lean, and forgiving road-edge contact. Easy pedaling remains automatic; hold W/up for more power, S/down to brake, or Shift to coast. Touch controls include pedal and brake buttons.

Speed cues include a lower, closer chase camera, a gradually widening field of view, subtle peripheral streaks, wheel rotation matched to distance, independent pedaling cadence, and speed-dependent wind sound. The existing music toggle and volume also control wind. Reduced-motion system preferences disable camera roll, vibration, FOV expansion, and streaks.

Eight automated checks cover slope forces, stopping downhill, stationary steering, road-edge containment, pedaling power, matching results at 30 and 144 FPS, wheel travel, and starting uphill. Browser start and runtime checks passed. This is forgiving arcade bicycle physics, not a full tire/suspension simulator.

## Cloth and hair motion

The skirt is now a simulated cloth grid with a pinned waistband, structural and diagonal constraints, flexible folds, wind flutter, inertia, and a basic lower-body collision envelope. Its blue hem follows the same simulated vertices. The braid uses a constrained chain with gravity, damping, wind, torso collision, and acceleration/turn response. Both simulations run at the same fixed timestep as the bicycle, freeze on pause, and reset with the ride.

This is lightweight position-based secondary motion, with approximate body collision rather than full cloth self-collision. Stability checks cover extreme speed/braking, pinned attachments, bounded stretch, settling, and reset; browser rendering and runtime checks passed.

## Firefly collection feedback

Pickups now emit golden sparkles, a soft glow, a floating +1, a warm rider-light pulse, a highlighted counter, and a short two-tone chime with gentle pitch variation. Audio follows the existing mute and volume controls. Effects freeze on pause, clear on restart, and use a fixed pool to keep memory bounded. Reduced-motion settings replace particles and expansion with a gentle glow and static label.

Effect checks cover pooling, finite particle positions, expiration, reset, and reduced motion. Browser load reported no errors.

## Steadier handling and longer descents

Steering sensitivity and maximum lean are reduced. Lean follows the sustained steering input rather than rapid yaw acceleration, so releasing a turn no longer makes the rider flop in the opposite direction. The camera horizon remains level.

Each 720 m route profile has a 480 m descent and a 240 m return climb, with smooth transitions. Automatic pedaling includes climbing assistance to retain momentum; braking and coasting remain unassisted. Regression checks cover upright settling, absence of opposite lean on release, climbing pace, downhill proportion, and smooth hill transitions.

## Downhill firefly rows and bonuses

Each 480 m descent has five evenly distributed rows of six fireflies, spaced 3 m apart within each row. Every pickup earns one score; completing all six in a row adds five bonus score, a floating ROW COMPLETE +5, and a chord-matched musical flourish. Incomplete rows earn no bonus, and completed rows cannot award twice. The earlier dense end-of-descent row has been removed. A booster remains just before the climb.

The rider gradually tucks forward by up to about ten degrees as speed rises. Head and braid follow the upper body; arms stay connected to the fixed handlebars. The posture is visual and does not change drag or steering.

## Pre-climb booster and musical pickups

A larger mint-green firefly with an orbiting ring appears at route position 478 m, before each climb. It adds 10 m/s immediately (up to 30 m/s) and supplies a nine-second thrust envelope that smoothly decays. Normal drag then brings speed back toward cruising; braking cancels the boost. A countdown appears while active. Pause freezes the timer; restart clears it.

Pickup notes are now selected from the soundtrack chord at their scheduled audio time, including across chord changes. Ordinary pickups play a soft two-note phrase; the booster plays a four-note arpeggio. Both share the score’s synth, reverb, and volume/mute controls. Automated checks cover boost onset, taper, braking, spawn placement, and chord membership.

## Run

From this folder, run:

    python3 -m http.server 8765

Then open http://localhost:8765 in a browser with WebGL support. Internet access is not required; Three.js is included locally. Opening index.html directly as a file will not load JavaScript modules in most browsers.

## Controls

- Click Let's ride to begin. Cruising is automatic.
- A / D or left / right arrows: steer.
- W / up: pedal harder; S / down: brake to a stop; Shift: coast.
- Space or Pause: pause / resume.
- R or the circular arrow: restart.
- Touch arrows: steer, pedal harder, and brake on narrow screens.

There is no timer or failure state. Pass through golden fireflies to collect them. Progress is session-only.

## Files

- index.html: interface and styles
- game.js: editable procedural world, rider, controls, and animation
- secondary-motion.js: cloth and braid simulation
- secondary-motion.test.js: stability and attachment checks
- physics.js: fixed-step bicycle dynamics
- physics.test.js: handling regression checks
- music.js: original generative score and wind
- bundle.js: ready-to-run bundled game
- package.json: build dependencies and scripts
- vendor/three.module.js: Three.js r170, MIT license in vendor/LICENSE

Browser verification covered rendering, starting, changing elevation, distance progression, firefly collection, music on/off, pause, and restart. No browser errors or warnings were reported. Build and syntax checks passed. Audio controls and scheduling were verified; subjective listening quality, touch hardware, and long-session performance have not been independently tested.

## Rebuild after editing

Run `npm install`, then `npm run build`. Serve the folder again as described above.

Run `npm test` for physics checks.
