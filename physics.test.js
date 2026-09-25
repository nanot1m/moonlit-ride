import assert from 'node:assert/strict';
import {createRide,stepRide,FIXED_STEP} from './physics.js';
function simulate(seconds,input,road,initial={}){const s=Object.assign(createRide(),initial);for(let t=0;t<Math.round(seconds/FIXED_STEP);t++)stepRide(s,input,road);return s;}
const flat=simulate(10,{coast:true},{dy:0},{speed:10});
const downhill=simulate(10,{coast:true},{dy:.08},{speed:10});
const uphill=simulate(10,{coast:true},{dy:-.08},{speed:10});
assert(downhill.speed>flat.speed&&flat.speed>uphill.speed,'Gravity must affect coasting in the correct direction');
const stopped=simulate(8,{brake:true},{dy:.12},{speed:18});assert.equal(stopped.speed,0,'Brakes must stop and hold on a downhill');
const stationary=simulate(5,{steer:1,brake:true},{dy:0});assert.equal(stationary.lateral,0,'Cannot slide sideways at rest');
const edge=simulate(40,{steer:1,pedal:true},{dy:.08});assert(Math.abs(edge.lateral)<=4.15&&Number.isFinite(edge.speed),'Sustained steering stays within road bounds');
const cruise=simulate(20,{},{}),sprint=simulate(20,{pedal:true},{});assert(sprint.speed>cruise.speed+2,'Hard pedaling should have a meaningful effect');
function framed(hz){let accumulator=0,s=createRide();for(let frame=0;frame<hz*20;frame++){accumulator+=1/hz;while(accumulator+1e-12>=FIXED_STEP){stepRide(s,{pedal:true,steer:.2},{dy:.04,dx:.2});accumulator-=FIXED_STEP;}}return s;}
const a=framed(30),b=framed(144);assert(Math.abs(a.distance-b.distance)<1e-6&&Math.abs(a.lateral-b.lateral)<1e-6,'Frame rate must not alter dynamics');
const straight=simulate(5,{coast:true},{},{speed:10});assert(Math.abs(straight.wheelAngle*.52-straight.distance)<1e-8,'Wheel rotation must match traveled distance');
assert(simulate(15,{},{dy:-.13}).distance>5,'Easy pedaling must start on the steepest route hills');
console.log('8 physics checks passed: slopes, braking, stationary steering, edges, pedaling, frame rates, wheel travel, hill starts.');
// A released turn should settle upright without flicking into the opposite lean.
const turn=createRide();turn.speed=12;for(let i=0;i<60;i++)stepRide(turn,{steer:1},{});
assert(turn.lean<0&&Math.abs(turn.lean)<=.19);
for(let i=0;i<360;i++){stepRide(turn,{},{});assert(turn.lean<=.00001,'Releasing steering must not counter-flop');}
assert(Math.abs(turn.lean)<.002,'Turn settles upright');
const assisted=simulate(20,{},{dy:-.15},{speed:8});assert(assisted.speed>7,'Uphill assist preserves cruising momentum');
console.log('Steady turning and assisted-climb checks passed.');
