# Continue development

Moonlit Ride is a standalone Three.js browser cycling game. The intended mood is a colorful European coastal village at blue hour, with warm windows, water, hills, and a blue-haired cyclist in a patterned skirt. Assets are procedural; the original visual reference is not included.

## Current accepted behavior

- Relaxed automatic pedaling; W/up pedals harder, S/down brakes, Shift coasts, A/D or arrows steer. Space pauses; R resets. Touch buttons are included.
- 720 m repeating terrain profile: 480 m descending, 240 m assisted climb. Stable steering and a level camera horizon are deliberate user preferences.
- Five evenly spaced rows of six fireflies per descent. One score per pickup, plus five for collecting a complete row. Row bonuses trigger a floating label and chord-matched musical flourish.
- A mint-green booster at 478 m, before the climb, adds speed immediately and thrust that tapers over nine seconds. Braking cancels it.
- Position-based skirt and braid dynamics. Rider tucks forward with speed; hands stay on the handlebars.
- Synthesized ambient music, wind, and pickup chimes; shared mute/volume. Pickup notes follow the current chord.

## Source map

- game.js: scene, procedural world, rendering, controls, rider, integration
- physics.js: fixed 120 Hz arcade bicycle dynamics
- route.js / firefly-layout.js: terrain profile, pickups, row tracking
- secondary-motion.js: cloth and braid constraints
- music.js / score.js: audio engine and harmony
- collection-effects.js: pooled pickup feedback
- index.html: HUD and responsive styles

## Development

Install Node.js 22 or newer. Run npm ci, npm test, and npm start. Open http://127.0.0.1:8765.
After changes run npm run build:pages, commit source and generated files, then push main. GitHub Pages serves docs/.

The original Codex conversation is not part of this Git repository. This document preserves implementation context so a new task can continue from the same state. Known limits: procedural prototype art, approximate cloth collision without self-collision, no saved player progress, and limited device-performance testing.
