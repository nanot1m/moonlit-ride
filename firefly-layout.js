import {ROUTE_LENGTH,DESCENT_LENGTH} from './route.js';
export const ROW_COUNT=6,ROW_SPACING=3,ROWS_PER_DESCENT=5;
export function firefliesForChunk(start,length=24){
 const end=start+length,items=[],lo=Math.floor(-end/ROUTE_LENGTH)-1,hi=Math.floor(-start/ROUTE_LENGTH)+1;
 for(let loop=lo;loop<=hi;loop++){
  for(let row=0;row<ROWS_PER_DESCENT;row++){
   const first=(row+1)*DESCENT_LENGTH/(ROWS_PER_DESCENT+1)-7.5;
   for(let index=0;index<ROW_COUNT;index++){const progress=loop*ROUTE_LENGTH+first+index*ROW_SPACING,z=-progress;
    if(z>=start&&z<end)items.push({z,lane:0,finishRow:true,rowId:`${loop}:${row}`,index,rowEnd:loop*ROUTE_LENGTH+first+15});
   }
  }
  const z=-(loop*ROUTE_LENGTH+DESCENT_LENGTH-2);if(z>=start&&z<end)items.push({z,lane:0,booster:true});
 }
 const z=start+length/2,phase=((-z%ROUTE_LENGTH)+ROUTE_LENGTH)%ROUTE_LENGTH;
 if(phase>DESCENT_LENGTH+3)items.push({z,lane:Math.sin(start/24*7)*3});
 return items.sort((a,b)=>b.z-a.z);
}
export class RowTracker {
 constructor(){this.rows=new Map();}
 collect(p){if(!p.rowId)return false;let row=this.rows.get(p.rowId);if(!row){row={seen:new Set(),end:p.rowEnd,awarded:false};this.rows.set(p.rowId,row);}row.seen.add(p.index);if(row.seen.size===ROW_COUNT&&!row.awarded){row.awarded=true;return true;}return false;}
 prune(progress){for(const [id,row]of this.rows)if(progress>row.end+10)this.rows.delete(id);}
 reset(){this.rows.clear();}
}
