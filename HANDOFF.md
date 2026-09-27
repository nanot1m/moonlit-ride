# Continue development

## Unity migration (2026-09-25)

The native project is in `Unity/`, targeting Unity 6000.3.17f1 and Windows with the built-in renderer. Read `Unity/README.md` for setup, architecture, commands, and known differences. `Moonlit Ride > Validate and build Windows` runs C# checks and builds the player. `scripts/export-unity-fixtures.mjs` exports reference physics results from the accepted browser code for C# parity checks. Continue new Unity work in that directory; keep the browser game usable. The Unity scene bootstraps procedural content at runtime. Local branch: `unity-port`.

The visual follow-up restores houses on the right and sea on the left using an X reflection at the render boundary. Rider art is authored in Blender (`art/MoonlitRider.blend`); Unity imports explicit-axis mesh data into native assets. `RiderDynamics` uses Unity Cloth for the dress and 12 Rigidbody/ConfigurableJoint bones for the skinned braid, with explicit WindZone-to-force coupling. Native physics and the ride now share 120 Hz FixedUpdate; pause uses Time.timeScale. Do not restore the old custom cloth as the active runtime. Smoke checks cover orientation, native wind response, attachments, and high-speed/stop stability.

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

Latest: anatomical CC0 MakeHuman head (see THIRD_PARTY_ASSETS.md); two-bone cycling IK; acceleration/boost forward tuck pivoting at waistband; IK thigh Cloth collisions; continuous curbs; WaterReflection planar camera; warmer local lantern lighting.

User priority: realistic body silhouette from behind, not facial detail. AnatomicalRider replaces primitive body renderers with the full CC0 MakeHuman clothed mesh and original weights reduced to 14 bones. Include metacarpals in hand weights; lowerleg endpoints must span both original twist segments. Acceleration screenshot resets secondary physics after instant posing.

Latest reference direction: blue hair/braid, black loose speckled blouse, white/cobalt skirt, warm promenade and saturated blue coast. Rider includes cadence sway and hand grip. Cloth now has welded circumference (cylindrical UV0 plus UV1 height); never reintroduce duplicate seam particles. Check ordinary-speed cloth motion and preview frames, not only high-wind tests.

Hair follow-up: replaced hemispherical bowl silhouette with a fitted scalp, raised temples and a gathered nape continuous with the braid. HairDetail is rendered again using fine front-to-back curved locks and temple wisps; the separate attachment ball is removed.

Reference clothing follow-up: full-length speckled black sleeves, softened blouse volume, wider simulated skirt with dense cobalt rosette borders alternating with leafy sprays, fuller braid and cleaner cobalt locks. DressFabric remains deterministically authored in art/build_rider.py; no generated AI texture was used (service quota unavailable).

Silhouette follow-up: anatomical body width reduced 14% and depth 10%, slimmer blouse allowance, matching shoulder/hip IK anchors and smaller cloth envelopes. AnatomicalLeggings uses skin material to expose the existing anatomical legs. Dress waistband is an ellipse (0.190 x 0.145 m) centered at z=0.105 to fit the actual waist instead of the old displaced circular opening. Head neck mesh extends inside the collar. Preserve welded skirt topology.

Latest Unity follow-up: Quaternius CC0 textured nature meshes replace cone/ball trees. NatureAssets preserves FBX root transforms; Geometry.Combine handles all material submeshes. NatureImporter enables readable meshes for runtime combining. CoastalWorld.Shore/BankHeight create a continuous irregular bank, with grounded vegetation/rocks and boats beyond the shoreline. Water adds a shallow-water tint and foam along the matching shore equation. Nature source/license details are in THIRD_PARTY_ASSETS.md.

Unity speed now intentionally differs from browser parity: 80 km/h automatic cruise, 90 while pedalling, 140 while boosted. Speed is still measured in m/s internally. Step selects fastRide; StepOnRoad retains the original default for existing browser parity fixtures. New tests cover cruise on a climb, reaching 140, cap and downhill braking. Native braid bodies use no interpolation so their render positions share the rider's fixed-step pose at 140 km/h. Runtime smoke stresses the new maximum speed.

District follow-up: CoastalDistricts.cs owns three 240 m zones per 720 m route loop: Market, Residential, Tourist. Market buildings have display windows, produce crates and striped awnings; lower residential houses have gardens, fences, balconies and garden plazas; tourist hotels have balconies, roof terraces and cafe plazas. Geometry remains combined per material and streamed in the existing 23 chunks. Two tourist chunks have piers, connected by stairs through a cleared/cut bank corridor, with shaped sailboats, fishing launches and runabouts. DistrictAt and HasPier are covered by PortChecks. Smoke captures District-Market/Residential/Tourist/Marina views and still runs the 140 km/h secondary-motion checks. Boat props are static moored scenery.

Bicycle follow-up: CityBicycle replaces the primitive bike with an adapted CC BY 3.0 Poly by Google city bicycle. Source, attribution and changes are listed in THIRD_PARTY_ASSETS.md; Windows builds now copy that notice beside the executable. art/build_bicycle.py rebuilds five FBX parts from SourceParts.blend. Frame/Steering use the source shape; wheels, crossed spokes, drivetrain, carrier and grips were rebuilt. Preserve imported FBX child transforms under explicit runtime pivots. Steering moves the fork/front wheel and IK hand targets together. Cadence drives the crank and level pedals; CityBicycle.Foot supplies the matching 0.21 m circular foot path. Wheel rotation still follows travel. The entire bicycle/rider origin was lowered 0.11 m so tires meet the road. Source model and editable adapted Blender asset are preserved.

Web publication follow-up: Unity is now the main GitHub Pages edition, with the original Three.js build retained at /classic/. Build with ProjectSetup.BuildWebGL (-buildTarget WebGL), then npm run build:pages and npm run package:unity-pages. Release files use gzip decompression fallback and hashed names. RideAudio delegates to a Web Audio bridge on WebGL; other platforms retain the native synth. Preserve link.xml's indirect CreatePrimitive collider types. Windows and WebGL builds both pass PortChecks; npm test additionally checks the browser audio bridge. Development caches and local player builds remain ignored; only the deployable docs/ web output is tracked.

Coastal refinement: alternating left/right firefly rows now require steering; tests verify all rows are reachable at 90/140 km/h. Route.Center has modulated tighter curves and analytic derivatives. Waterfront(z) eases the road down to 1.1 m for a close-water promenade; keep Water.shader's center/shore formulas synchronized. Sparse residential buildings, side streets with dropped curbs and set-back cottages break up the facades. Lighting is distance-faded instead of chunk-switched; selected windows have aperture/mullion cookies plus dynamic shadow maps (closed buildings still have no interior rooms). Planar reflection sampling now filters ripples and fades borders. The head is scaled to .86, acceleration tuck is stronger, and low-speed pedalling animates standing with shared pelvis/skirt offsets and IK contact. Runtime smoke uses steering for pickup collection and waits for skinning before pose captures.

Soft cel pass: Geometry, CityBicycle and NatureAssets now use MoonlitRide/Painted; Fabric, Blouse, Foliage and CoastalGround share PaintedLighting.cginc. Three soft light bands retain continuous attenuation/cookies/shadows; foliage saturation and bloom are reduced. CoastalBloom requests DepthNormals and adds a restrained blue-grey contour fading out with distance. Water and sky remain smoothly shaded. Preserve the always-included Painted shader when building WebGL.
