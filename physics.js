// SI units, fixed-step arcade bicycle dynamics. The road provides gentle heading assist.
export const FIXED_STEP = 1 / 120;
const clamp=(x,a,b)=>Math.max(a,Math.min(b,x));
export function createRide(){return {progress:0,distance:0,lateral:0,speed:0,heading:0,steering:0,lean:0,wheelAngle:0,cadence:0,acceleration:0,contact:false,boostRemaining:0};}
export function activateBoost(s){s.boostRemaining=9;s.speed=Math.min(30,s.speed+10);}
export function stepRide(s,input,road,dt=FIXED_STEP){
 const dx=road.dx||0,dy=road.dy||0,metric=Math.hypot(1,dx,dy);
 const brake=!!input.brake,coast=!!input.coast;
 if(brake)s.boostRemaining=0;else s.boostRemaining=Math.max(0,s.boostRemaining-dt);
 const boostForce=4*Math.pow(s.boostRemaining/9,2);
 const power=brake||coast?0:input.pedal?620:230;
 const drive=Math.min(input.pedal?3.1:1.7,power/(85*Math.max(s.speed,1.4)));
 const gravity=9.81*dy/metric;
 const resistance=.065+.0035*s.speed*s.speed;
 const braking=brake?5.5:0;
 // Gentle climbing assistance preserves the relaxed pace, but never overrides braking or coasting.
 const climbAssist=!brake&&!coast&&dy<0?Math.max(0,-gravity)*.9+Math.max(0,7.5-s.speed)*.22:0;
 s.acceleration=drive+gravity+climbAssist+boostForce-resistance-braking;
 const previousSpeed=s.speed;s.speed=clamp(s.speed+s.acceleration*dt,0,32);
 // A stationary brake holds on hills. Coasting never rolls the bike backwards.
 if(brake&&s.speed<.08)s.speed=0;
 const steerLimit=.24/(1+s.speed*.075);
 s.steering+=(clamp(input.steer||0,-1,1)*steerLimit-s.steering)*(1-Math.exp(-dt*6));
 const yawRate=s.speed/1.1*Math.tan(s.steering)-s.heading*(3.6+s.speed*.16);
 s.heading=clamp(s.heading+yawRate*dt,-.42,.42);
 if(s.speed<.1)s.heading*=Math.exp(-dt*7);
 const travel=(previousSpeed+s.speed)*.5*dt;
 s.lateral+=s.speed*Math.sin(s.heading)*dt;
 s.contact=Math.abs(s.lateral)>4.15;
 if(s.contact){s.lateral=clamp(s.lateral,-4.15,4.15);s.speed*=Math.exp(-dt*5);if(Math.sign(s.heading)===Math.sign(s.lateral))s.heading*=Math.exp(-dt*18);}
 s.progress+=travel*Math.cos(s.heading)/metric;s.distance+=travel;
 // Visual lean follows the sustained turn, not transient yaw acceleration: no release counter-flop.
 const turnLean=s.steering*Math.min(s.speed*.13,1.3)+(road.curvature||0)*s.speed*s.speed*.018;
 const leanTarget=-clamp(turnLean,-.19,.19);
 s.lean+=(leanTarget-s.lean)*(1-Math.exp(-dt*4.5));
 s.wheelAngle+=travel/.52;
 if(power>0&&s.speed>.1)s.cadence+=Math.min(10,3+s.speed*.42)*dt;
 return s;
}
