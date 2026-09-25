export const BEAT=.54;
export const CHORDS=[[50,57,62,65,69],[46,53,58,62,65],[53,60,65,69,72],[48,55,60,64,67]];
export function chordAt(seconds){return CHORDS[Math.floor(Math.max(0,seconds)/(BEAT*16))%CHORDS.length];}
export function pickupNotes(seconds,count,booster=false){return Array.from({length:booster?4:2},(_,i)=>{const offset=i*BEAT/4;const chord=chordAt(seconds+offset);return {offset,note:chord[((count-1)+i*2)%chord.length]+12};});}
