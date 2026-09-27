// Independent oracle: C# tests consume reference results from the accepted JS dynamics.
import {mkdirSync, writeFileSync} from 'node:fs';
import {createRide, stepRide, activateBoost} from '../physics.js';
const cases = [];
for (const dy of [-.12, 0, .12]) {
  for (const mode of ['auto', 'pedal', 'brake', 'coast', 'turn', 'boost']) {
    const s = createRide(); s.speed = 9;
    const input = {pedal: mode === 'pedal', brake: mode === 'brake', coast: mode === 'coast', steer: mode === 'turn' ? .7 : 0};
    if (mode === 'boost') activateBoost(s);
    for (let i = 0; i < 1200; i++) stepRide(s, input, {dx: .2, dy, curvature: .001});
    cases.push({name: `${mode}/${dy}`, slope: dy, mode, speed: s.speed, progress: s.progress, lateral: s.lateral, lean: s.lean, distance: s.distance, boost: s.boostRemaining});
  }
}
const directory = new URL('../Unity/Assets/MoonlitRide/Editor/Fixtures/', import.meta.url);
mkdirSync(directory, {recursive: true});
writeFileSync(new URL('RideParity.json', directory), JSON.stringify({cases}, null, 2) + '\n');
console.log(`Exported ${cases.length} JavaScript reference cases for Unity.`);
