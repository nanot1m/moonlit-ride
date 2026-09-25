import * as T from 'three';
// A bounded pool: collecting indefinitely never allocates more particle systems.
export class CollectionEffects {
 constructor(scene,{glowTexture,labelTexture,reducedMotion=false}){
  this.reducedMotion=reducedMotion;this.pool=[];
  for(let k=0;k<6;k++){
   const positions=new Float32Array(28*3),geometry=new T.BufferGeometry();geometry.setAttribute('position',new T.BufferAttribute(positions,3).setUsage(T.DynamicDrawUsage));
   const material=new T.PointsMaterial({color:'#ffe398',map:glowTexture,size:.24,transparent:true,blending:T.AdditiveBlending,depthWrite:false});
   const sparks=new T.Points(geometry,material);sparks.frustumCulled=false;scene.add(sparks);
   const halo=new T.Sprite(new T.SpriteMaterial({map:glowTexture,color:'#ffc65c',transparent:true,blending:T.AdditiveBlending,depthWrite:false}));scene.add(halo);
   const label=new T.Sprite(new T.SpriteMaterial({map:labelTexture,transparent:true,depthWrite:false,depthTest:false}));label.scale.set(1.1,.55,1);label.renderOrder=10;scene.add(label);
   const velocities=Array.from({length:28},(_,i)=>{const angle=i*2.39996,spread=.6+(i%5)*.18;return new T.Vector3(Math.cos(angle)*spread,.3+((i*7)%13)/9,Math.sin(angle)*spread)});
   this.pool.push({sparks,halo,label,positions,velocities,origin:new T.Vector3(),age:2,active:false});
  }
  this.reset();
 }
 burst(position){const e=this.pool.find(e=>!e.active)||this.pool.reduce((a,b)=>a.age>b.age?a:b);e.origin.copy(position);e.age=0;e.active=true;this.draw(e);}
 draw(e){const t=e.age,u=t/1.05,fade=Math.max(0,1-u);e.sparks.visible=e.active&&!this.reducedMotion;e.halo.visible=e.label.visible=e.active;
  for(let i=0;i<28;i++){const v=e.velocities[i],expansion=(1-Math.exp(-t*5))*.85;e.positions.set([e.origin.x+v.x*expansion,e.origin.y+v.y*expansion+t*.35,e.origin.z+v.z*expansion],i*3);}
  e.sparks.geometry.attributes.position.needsUpdate=true;e.sparks.material.opacity=fade*fade;e.sparks.material.size=.17+fade*.14;
  e.halo.position.copy(e.origin);e.halo.material.opacity=fade*fade*(this.reducedMotion?.25:.65);e.halo.scale.setScalar(this.reducedMotion?1.2:1+t*3.2);
  e.label.position.copy(e.origin);e.label.position.y+=.5+(this.reducedMotion?0:t*.9);e.label.material.opacity=Math.min(1,fade*3);
 }
 update(dt){for(const e of this.pool)if(e.active){e.age+=dt;if(e.age>=1.05)e.active=false;this.draw(e);}}
 reset(){for(const e of this.pool){e.active=false;e.age=2;e.sparks.visible=e.halo.visible=e.label.visible=false;}}
}
