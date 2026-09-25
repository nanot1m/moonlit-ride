import {CHORDS,BEAT,pickupNotes} from './score.js';
// Original generative score: a slow, non-repeating arrangement over four chords.
// Synthesized locally after a user gesture. No external audio or copyrighted recordings.
export function createMusic(){
 let ctx,master,wet,windGain,windFilter,timer,next=0,origin=0,step=0,enabled=false,level=.35,paused=false;
 const chords=CHORDS;
 const freq=n=>440*2**((n-69)/12);
 function note(n,t,d,gain,type='sine',pan=0){const osc=ctx.createOscillator(),amp=ctx.createGain(),st=ctx.createStereoPanner();osc.type=type;osc.frequency.value=freq(n);st.pan.value=pan;amp.gain.setValueAtTime(0,t);amp.gain.linearRampToValueAtTime(gain,t+.06);amp.gain.exponentialRampToValueAtTime(.0001,t+d);osc.connect(amp);amp.connect(st);st.connect(master);st.connect(wet);osc.start(t);osc.stop(t+d+.1);}
 function schedule(){while(next<ctx.currentTime+.4){const chord=chords[Math.floor(step/16)%4];if(step%16===0){chord.slice(0,4).forEach((n,i)=>note(n,next,9,.022,'sine',(i-1.5)*.35));note(chord[0]-12,next,8,.035);}
 if(step%2===0){const pattern=[2,4,3,1,4,2,3,4];const n=chord[pattern[(step/2)%8]]+12;note(n,next,3.8,.045,'sine',Math.sin(step)*.5);note(n+12,next,1.1,.008,'sine',-.25);}
 if(step%8===5)note(chord[3]+12,next,4,.02,'triangle',.35);
 next+=BEAT;step++;}}
 async function init(){if(!ctx){ctx=new AudioContext();master=ctx.createGain();master.gain.value=0;master.connect(ctx.destination);
const noise=ctx.createBuffer(1,ctx.sampleRate*2,ctx.sampleRate),noiseData=noise.getChannelData(0);for(let i=0;i<noiseData.length;i++)noiseData[i]=Math.random()*2-1;const wind=ctx.createBufferSource();wind.buffer=noise;wind.loop=true;windFilter=ctx.createBiquadFilter();windFilter.type='lowpass';windFilter.frequency.value=300;windGain=ctx.createGain();windGain.gain.value=0;wind.connect(windFilter);windFilter.connect(windGain);windGain.connect(master);wind.start();wet=ctx.createConvolver();const impulse=ctx.createBuffer(2,ctx.sampleRate*3,ctx.sampleRate);for(let c=0;c<2;c++){const data=impulse.getChannelData(c);for(let i=0;i<data.length;i++)data[i]=(Math.random()*2-1)*Math.pow(1-i/data.length,3)*.35;}wet.buffer=impulse;const reverb=ctx.createGain();reverb.gain.value=.32;wet.connect(reverb);reverb.connect(master);next=ctx.currentTime+.1;origin=next;timer=setInterval(schedule,100);schedule();}await ctx.resume();}
 function update(){if(master)master.gain.setTargetAtTime(enabled?level*(paused?.3:1):0,ctx.currentTime,.35);}
 return {bonus(){if(!ctx||!enabled)return;const t=ctx.currentTime;for(const [i,p]of pickupNotes(t-origin,1,true).entries())note(p.note+(i===3?12:0),t+p.offset,1.4,.075,'sine',i%2?.2:-.2);},collect(count,booster=false){if(!ctx||!enabled)return;const t=ctx.currentTime;for(const [i,p] of pickupNotes(t-origin,count,booster).entries())note(p.note,t+p.offset,booster?1.2:.8,i===0?.085:.045,'sine',i%2?.12:-.12);},setSpeed(speed){if(windGain){const amount=Math.min(speed/20,1);windGain.gain.setTargetAtTime(amount*amount*.12,ctx.currentTime,.2);windFilter.frequency.setTargetAtTime(250+amount*1000,ctx.currentTime,.2)}},get enabled(){return enabled},async start(){await init();enabled=true;update()},async toggle(){await init();enabled=!enabled;update()},volume(v){level=v;update()},setPaused(v){if(paused!==v){paused=v;update()}}};
}
