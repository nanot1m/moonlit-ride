import assert from 'node:assert/strict';
import {firefliesForChunk,RowTracker} from './firefly-layout.js';
import {ROUTE_LENGTH,DESCENT_LENGTH} from './route.js';
const items=[];for(let start=-ROUTE_LENGTH*3;start<0;start+=24)items.push(...firefliesForChunk(start));
const grouped=Map.groupBy(items.filter(p=>p.rowId),p=>p.rowId);assert.equal(grouped.size,15);
for(const row of grouped.values()){row.sort((a,b)=>b.z-a.z);assert.equal(row.length,6);assert(row.every(p=>p.lane===0));for(let i=1;i<6;i++)assert.equal(row[i-1].z-row[i].z,3);}
assert.equal(new Set(items.map(p=>p.z)).size,items.length);
const tracker=new RowTracker(),row=[...grouped.values()][0];for(const p of row.slice(0,5))assert(!tracker.collect(p));assert(!tracker.collect(row[0]),'Duplicates do not complete a row');assert(tracker.collect(row[5]));assert(!tracker.collect(row[5]),'Bonus only once');tracker.prune(row[0].rowEnd+11);assert.equal(tracker.rows.size,0);
console.log('Rows passed: five per descent, six pickups each, spacing, uniqueness, complete-only bonus, no duplicate bonus, cleanup.');
