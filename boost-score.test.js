import assert from 'node:assert/strict';
import {createRide,activateBoost,stepRide} from './physics.js';
import {firefliesForChunk} from './firefly-layout.js';
import {chordAt,pickupNotes,BEAT} from './score.js';
const ride=createRide();ride.speed=12;activateBoost(ride);assert.equal(ride.speed,22);assert.equal(ride.boostRemaining,9);
let peak=ride.speed;for(let i=0;i<1081;i++){stepRide(ride,{}, {dy:-.1});peak=Math.max(peak,ride.speed);}
assert.equal(ride.boostRemaining,0);assert(ride.speed<peak,'Boost must fade back from peak speed');
activateBoost(ride);stepRide(ride,{brake:true},{});assert.equal(ride.boostRemaining,0,'Braking cancels boost');
let items=[];for(let n=-90;n<0;n++)items.push(...firefliesForChunk(n*24));const boosters=items.filter(x=>x.booster);assert.equal(boosters.length,3);assert(boosters.every(x=>(-x.z)%720===478&&x.lane===0));
for(const t of [0,8.6,17.2,25.9,34.56,70])for(const booster of [false,true]){for(const p of pickupNotes(t,8,booster))assert(chordAt(t+p.offset).includes(p.note-12),'Chime must fit chord at its scheduled time');}
console.log('Boost and score checks passed: kick, taper, braking override, one booster per climb, chord-matched notes across transitions.');
