// Position-based cloth and braid simulation in rider-local space, metres and seconds.
const clamp=(n,a,b)=>Math.max(a,Math.min(b,n));
const point=(x,y,z,pinned=false)=>({x,y,z,px:x,py:y,pz:z,rx:x,ry:y,rz:z,pinned});
function link(a,b){return {a,b,length:Math.hypot(a.x-b.x,a.y-b.y,a.z-b.z)};}
function satisfy(l,stiffness=1){const {a,b}=l,dx=b.x-a.x,dy=b.y-a.y,dz=b.z-a.z,d=Math.hypot(dx,dy,dz)||1;const correction=(d-l.length)/d*stiffness;const weight=a.pinned||b.pinned?1:.5;if(!a.pinned){a.x+=dx*correction*weight;a.y+=dy*correction*weight;a.z+=dz*correction*weight;}if(!b.pinned){b.x-=dx*correction*weight;b.y-=dy*correction*weight;b.z-=dz*correction*weight;}}
function integrate(p,dt,fx,fy,fz,damping){if(p.pinned)return;const x=p.x,y=p.y,z=p.z;p.x+=(p.x-p.px)*damping+fx*dt*dt;p.y+=(p.y-p.py)*damping+fy*dt*dt;p.z+=(p.z-p.pz)*damping+fz*dt*dt;p.px=x;p.py=y;p.pz=z;}
export function createSecondaryMotion(){
 const segments=40,rows=10,cloth=[],links=[],hair=[],hairLinks=[];
 for(let j=0;j<=rows;j++){const t=j/rows;for(let i=0;i<segments;i++){const a=i/segments*Math.PI*2,r=(.235+.435*t)*(1+Math.sin(a*12)*.025*t);cloth.push(point(Math.sin(a)*r,.475-t*.95,Math.cos(a)*r,j===0));}}
 const at=(j,i)=>cloth[j*segments+(i+segments)%segments];
 for(let j=0;j<=rows;j++)for(let i=0;i<segments;i++){links.push(link(at(j,i),at(j,i+1)));if(j<rows){links.push(link(at(j,i),at(j+1,i)),link(at(j,i),at(j+1,i+1)),link(at(j,i+1),at(j+1,i)));}if(j<rows-1)links.push(link(at(j,i),at(j+2,i)));}
 for(let i=0;i<16;i++){hair.push(point(0,2.65-i*.073,.26+i*.047,i===0));if(i)hairLinks.push(link(hair[i-1],hair[i]));}
 let time=0;
 return {segments,rows,cloth,hair,links,hairLinks,
 reset(){time=0;for(const p of [...cloth,...hair]){p.x=p.px=p.rx;p.y=p.py=p.ry;p.z=p.pz=p.rz;}},
 step(dt,{speed=0,acceleration=0,lean=0,distance=0}={}){
 time+=dt;const air=Math.min(speed*speed*.012,5),side=clamp(Math.tan(lean)*9.81,-5,5),inertia=clamp(acceleration,-5,3);
 for(let j=1;j<=rows;j++){const t=j/rows;for(let i=0;i<segments;i++){const p=at(j,i),angle=i/segments*Math.PI*2;const flutter=Math.sin(time*(5+speed*.25)-j*.7+angle*3)*air*.24*t;integrate(p,dt,side+Math.sin(angle)*flutter+(p.rx-p.x)*18,-4.5+(p.ry-p.y)*26+Math.sin(distance*7)*Math.min(speed*.07,.7)*t,air*t+inertia*.45+(p.rz-p.z)*16+Math.cos(angle)*flutter,.975);}}
 for(const p of hair)integrate(p,dt,side*.8+Math.sin(time*5+p.ry*3)*air*.07,-9.81,air*1.6+inertia*.7,.984);
 // Repeated constraints keep the waist pinned, fabric coherent, and braid inextensible.
 for(let iteration=0;iteration<7;iteration++){
 for(const l of links)satisfy(l,.85);
 for(const p of cloth){if(p.pinned)continue;const t=(.475-p.ry)/.95;const radius=Math.hypot(p.x,p.z),min=(.235+.435*t)*.76;if(radius<min){p.x*=min/(radius||1);p.z*=min/(radius||1);}p.y=clamp(p.y,-.59,.475);}
 for(const l of hairLinks)satisfy(l);
 for(const p of hair){if(p.pinned)continue;const x=p.x/.46,y=(p.y-2.08)/.64,z=(p.z-.15)/.37;const radius=Math.hypot(x,y,z);if(radius<1){p.x=x/radius*.46;p.y=2.08+y/radius*.64;p.z=.15+z/radius*.37;}p.z=Math.max(p.z,.19);}
 }
 }};
}
