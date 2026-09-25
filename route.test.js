import assert from 'node:assert/strict';
import {elevation,slope,ROUTE_LENGTH,DESCENT_LENGTH} from './route.js';
assert.equal(DESCENT_LENGTH,480);assert.equal(ROUTE_LENGTH,720);
for(const p of [0,DESCENT_LENGTH,ROUTE_LENGTH]){assert(Math.abs(elevation(-p-.001)-elevation(-p+.001))<.001);assert(Math.abs(slope(-p-.001)-slope(-p+.001))<.001);}
console.log('Shorter route and smooth transition checks passed.');
