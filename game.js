import * as T from 'three';
import {EffectComposer} from 'three/addons/postprocessing/EffectComposer.js';
import {RenderPass} from 'three/addons/postprocessing/RenderPass.js';
import {UnrealBloomPass} from 'three/addons/postprocessing/UnrealBloomPass.js';
import {OutputPass} from 'three/addons/postprocessing/OutputPass.js';
import {createMusic} from './music.js';
import {createRide,stepRide,FIXED_STEP,activateBoost} from './physics.js';
import {createSecondaryMotion} from './secondary-motion.js';
import {CollectionEffects} from './collection-effects.js';
import {elevation,slope} from './route.js';
import {firefliesForChunk,RowTracker} from './firefly-layout.js';
const scene=new T.Scene();scene.background=new T.Color('#1c315b');scene.fog=new T.FogExp2('#455273',.0038);
const renderer=new T.WebGLRenderer({antialias:true});renderer.setSize(innerWidth,innerHeight);renderer.setPixelRatio(Math.min(devicePixelRatio,2));renderer.shadowMap.enabled=true;renderer.shadowMap.type=T.PCFSoftShadowMap;renderer.toneMapping=T.ACESFilmicToneMapping;renderer.toneMappingExposure=1.05;document.body.prepend(renderer.domElement);
const camera=new T.PerspectiveCamera(62,innerWidth/innerHeight,.1,650);scene.add(new T.HemisphereLight('#93b8f4','#423b55',1.25));const sun=new T.DirectionalLight('#ffe0af',1.7);sun.position.set(-25,50,20);sun.castShadow=true;sun.shadow.mapSize.set(2048,2048);Object.assign(sun.shadow.camera,{left:-65,right:65,top:65,bottom:-65,far:180});sun.shadow.bias=-.0005;scene.add(sun,sun.target);
const mat=(c,extra={})=>new T.MeshStandardMaterial({color:c,roughness:.8,...extra});const roadMat=mat('#b9a490'),wallMat=mat('#c4b199'),trim=mat('#f8dda6'),roof=mat('#853f32'),dark=mat('#17364b'),gold=mat('#cc9a45',{metalness:.65,roughness:.3}),glow=mat('#ffe3a0',{emissive:'#ffbc49',emissiveIntensity:1.7});
function box(w,h,d,m,x=0,y=0,z=0,parent=scene){const o=new T.Mesh(new T.BoxGeometry(w,h,d),m);o.position.set(x,y,z);o.castShadow=true;o.receiveShadow=true;parent.add(o);return o}
function sphere(r,m,x,y,z,parent=scene,sx=1,sy=1,sz=1){const o=new T.Mesh(new T.SphereGeometry(r,12,10),m);o.position.set(x,y,z);o.scale.set(sx,sy,sz);o.castShadow=true;parent.add(o);return o}
function rod(a,b,r,m,parent){const d=new T.Vector3().subVectors(b,a);const o=new T.Mesh(new T.CylinderGeometry(r,r,d.length(),8),m);o.position.copy(a).add(b).multiplyScalar(.5);o.quaternion.setFromUnitVectors(new T.Vector3(0,1,0),d.normalize());parent.add(o);return o}const v=(x,y,z)=>new T.Vector3(x,y,z);
function center(z){return Math.sin(z*.012)*19+Math.sin(z*.028)*5}
const composer=new EffectComposer(renderer);composer.addPass(new RenderPass(scene,camera));composer.addPass(new UnrealBloomPass(new T.Vector2(innerWidth,innerHeight),.28,.45,1.15));composer.addPass(new OutputPass());
// Deterministic surface textures keep the world continuous without network assets.
function texture(size,draw){const c=document.createElement('canvas');c.width=c.height=size;draw(c.getContext('2d'),size);const t=new T.CanvasTexture(c);t.wrapS=t.wrapT=T.RepeatWrapping;t.colorSpace=T.SRGBColorSpace;t.anisotropy=renderer.capabilities.getMaxAnisotropy();return t;}
const paving=texture(512,(c,n)=>{c.fillStyle='#84756b';c.fillRect(0,0,n,n);for(let y=0;y<16;y++)for(let x=-1;x<9;x++){let q=105+Math.random()*40;c.fillStyle=`rgb(${q+26},${q+17},${q+8})`;c.beginPath();c.roundRect(x*64+(y%2)*32+2,y*32+2,60,28,5);c.fill();for(let j=0;j<30;j++){c.fillStyle='#ffffff08';c.fillRect(x*64+(y%2)*32+Math.random()*60,y*32+Math.random()*28,2,2)}}});paving.repeat.set(2,5);roadMat.map=paving;roadMat.bumpMap=paving;roadMat.bumpScale=.055;roadMat.roughness=.65;roadMat.color.set('#c3b6ae');
const plaster=texture(128,(c,n)=>{c.fillStyle='#eee9df';c.fillRect(0,0,n,n);for(let i=0;i<5000;i++){c.fillStyle=Math.random()>.5?'#fff2':'#48352210';c.fillRect(Math.random()*n,Math.random()*n,1,1)}});
const glowTexture=texture(128,(c,n)=>{const g=c.createRadialGradient(n/2,n/2,0,n/2,n/2,n/2);g.addColorStop(0,'#fff8df');g.addColorStop(.1,'#ffd17aaa');g.addColorStop(.35,'#ffb24935');g.addColorStop(1,'#ffae3800');c.fillStyle=g;c.fillRect(0,0,n,n)});
const haloMaterial=new T.SpriteMaterial({map:glowTexture,color:'#ffc775',transparent:true,blending:T.AdditiveBlending,depthWrite:false});
function halo(parent,x,y,z,size){const o=new T.Sprite(haloMaterial);o.position.set(x,y,z);o.scale.set(size,size,1);parent.add(o);return o;}

const sea=new T.ShaderMaterial({uniforms:{time:{value:0}},vertexShader:`varying vec3 world; void main(){vec4 p=modelMatrix*vec4(position,1.);world=p.xyz;gl_Position=projectionMatrix*viewMatrix*p;}`,fragmentShader:`uniform float time;varying vec3 world;
void main(){vec2 p=world.xz;float waves=sin(p.x*.54+p.y*.23+time*.7)*sin(p.y*.8-p.x*.11-time*.5);float fine=sin(p.x*2.5+sin(p.y*.5)+time)*sin(p.y*3.1-time);float shimmer=pow(max(0.,waves*.65+fine*.35),8.);float band=exp(-pow((p.x+75.+sin(p.y*.03)*14.)/28.,2.));vec3 col=mix(vec3(.018,.07,.15),vec3(.07,.19,.32),waves*.2+.5);col+=vec3(.5,.56,.68)*shimmer*band*.7;col+=vec3(.06,.11,.16)*pow(max(0.,fine),14.);gl_FragColor=vec4(col,1.);}`});
const ocean=new T.Mesh(new T.PlaneGeometry(2000,2400),sea);ocean.rotation.x=-Math.PI/2;ocean.position.set(-450,-1,0);scene.add(ocean);
const sky=new T.Mesh(new T.SphereGeometry(950,48,24),new T.ShaderMaterial({side:T.BackSide,depthWrite:false,uniforms:{},vertexShader:`varying vec3 pos;void main(){pos=position;gl_Position=projectionMatrix*modelViewMatrix*vec4(position,1.);}`,fragmentShader:`varying vec3 pos;void main(){float h=normalize(pos).y;vec3 col=mix(vec3(.48,.37,.43),vec3(.065,.13,.3),smoothstep(-.06,.4,h));float cloud=sin(pos.x*.018+sin(pos.z*.013)*2.)*sin(pos.z*.02+pos.y*.03);col+=vec3(.12,.1,.16)*smoothstep(.25,.8,cloud)*smoothstep(.01,.18,h)*(1.-smoothstep(.25,.6,h));gl_FragColor=vec4(col,1.);}`}));scene.add(sky);
// Layered mountainous headlands across the bay.
const mountains=new T.Group();scene.add(mountains);
for(let layer=0;layer<3;layer++){const verts=[],indices=[];const count=110;for(let i=0;i<=count;i++){const x=-750+i*8;const h=25+layer*12+Math.sin(i*.11+layer)*19+Math.sin(i*.27+layer)*9+Math.sin(i*.59)*4;verts.push(x,-2,-330-layer*100,x,h,-330-layer*100);}for(let i=0;i<count;i++){const a=i*2;indices.push(a,a+1,a+2,a+1,a+3,a+2)}const geom=new T.BufferGeometry();geom.setAttribute('position',new T.Float32BufferAttribute(verts,3));geom.setIndex(indices);geom.computeVertexNormals();const ridge=new T.Mesh(geom,new T.MeshBasicMaterial({color:['#25364e','#34435e','#44516b'][layer],side:T.DoubleSide}));mountains.add(ridge);}
const moon=sphere(8,mat('#ffedd0',{emissive:'#fff3c4',emissiveIntensity:1}),-190,125,-480);
const stars=new T.BufferGeometry();const pts=[];for(let i=0;i<700;i++)pts.push((Math.random()-.5)*1200,100+Math.random()*240,-500+Math.random()*1000);stars.setAttribute('position',new T.Float32BufferAttribute(pts,3));const starfield=new T.Points(stars,new T.PointsMaterial({color:'#e0e6ff',size:.65,transparent:true,opacity:.65}));scene.add(starfield);
const moonHalo=halo(scene,-190,125,-480,45);
const reflectionMat=new T.MeshBasicMaterial({color:'#ffc977',transparent:true,opacity:.24,depthWrite:false});
const terrainMat=mat('#384d45',{map:plaster,bumpMap:plaster,bumpScale:.3});
const chunks=[];const colors=['#f0c279','#d58c76','#688ccc','#d7c8a8','#90b6a9','#e3ac75'];const leaves=mat('#2e6656'),flowers=mat('#ed725e');
function building(g,x,z,i){const w=5+(i%3),h=8+(i%5)*1.7,d=7;const facade=mat(colors[((i%6)+6)%6],{map:plaster,bumpMap:plaster,bumpScale:.045});box(w,h,d,facade,x,h/2,z,g);box(w+.4,.3,d+.4,trim,x,h,z,g);
for(const side of [-1,1])box(.16,h,.18,trim,x+side*(w/2-.14),h/2,z+d/2+.1,g);
for(let level=3.25;level<h;level+=2.7)box(w+.14,.1,d+.14,trim,x,level,z,g);
for(let yy=2;yy<h-1;yy+=2.7)for(let zz of [-1.8,1.8]){box(.16,1.8,1.28,trim,x-w/2-.1,yy,z+zz,g);box(.18,1.42,.9,glow,x-w/2-.15,yy,z+zz,g);box(.2,1.48,.055,dark,x-w/2-.2,yy,z+zz,g);box(.2,.06,.96,dark,x-w/2-.2,yy,z+zz,g);for(const sign of [-1,1])box(.18,1.65,.32,leaves,x-w/2-.16,yy,z+zz+sign*.7,g);if(yy<3){box(.55,.28,1.5,roof,x-w/2-.25,yy-.94,z+zz,g);for(let f=0;f<5;f++)sphere(.13,flowers,x-w/2-.35,yy-.71,z+zz-.55+f*.27,g);}}
const awning=box(1.4,.12,2.5,mat(i%2?'#354f66':'#8c5363'),x-w/2-.5,2.5,z,g);awning.rotation.z=.16;
const top=new T.Mesh(new T.ConeGeometry(w*.78,3.8,4),roof);top.rotation.y=Math.PI/4;top.scale.z=d/w;top.position.set(x,h+1.8,z);top.castShadow=true;g.add(top);box(.7,2,.8,wallMat,x+1,h+2.9,z,g);
for(let yy=2;yy<h-1;yy+=2.7){for(let xx=-w/2+1;xx<w/2-.5;xx+=1.8){box(1.1,1.7,.12,trim,x+xx,yy,z+d/2+.06,g);box(.78,1.35,.16,glow,x+xx,yy,z+d/2+.14,g);box(.08,1.4,.18,dark,x+xx,yy,z+d/2+.24,g);box(.85,.08,.18,dark,x+xx,yy,z+d/2+.24,g)}box(.15,1.5,1,glow,x-w/2-.09,yy,z+1.3,g);box(.15,1.5,1,glow,x-w/2-.09,yy,z-1.3,g)}box(1.3,2.3,.2,dark,x,.95,z+d/2+.12,g);box(w+.5,.2,d+.5,trim,x,.15,z,g);}
function lamp(g,x,z){rod(v(x,0,z),v(x,5.3,z),.085,dark,g);box(.7,.9,.7,glow,x,5.3,z,g);box(.95,.12,.95,dark,x,5.8,z,g);const cap=new T.Mesh(new T.ConeGeometry(.7,.5,4),dark);cap.position.set(x,6.1,z);cap.rotation.y=Math.PI/4;g.add(cap);const light=new T.PointLight('#ffbb58',25,12,2);light.position.set(x,4.8,z);g.add(light);halo(g,x,5.35,z,3.5);}
const sharedMaterials=new Set([roadMat,wallMat,trim,roof,dark,gold,glow,reflectionMat,leaves,flowers,terrainMat,haloMaterial]);
function disposeChunk(c){scene.remove(c.g);const materials=new Set();c.g.traverse(o=>{if(o.geometry)o.geometry.dispose();if(o.material&&!sharedMaterials.has(o.material))materials.add(o.material)});materials.forEach(m=>m.dispose());}
function makeChunk(n){const g=new T.Group();scene.add(g);const start=n*24;const points=[];for(let j=0;j<=12;j++){let z=start+j*2;points.push(center(z)-5,elevation(z),z,center(z)+5,elevation(z),z)}const geo=new T.BufferGeometry();geo.setAttribute('position',new T.Float32BufferAttribute(points,3));const ix=[];for(let j=0;j<12;j++){let k=j*2;ix.push(k,k+2,k+1,k+1,k+2,k+3)}geo.setIndex(ix);const uv=[];for(let j=0;j<=12;j++)uv.push(0,j/12,1,j/12);geo.setAttribute('uv',new T.Float32BufferAttribute(uv,2));geo.computeVertexNormals();const r=new T.Mesh(geo,roadMat);r.receiveShadow=true;g.add(r);
for(let j=0;j<24;j+=3){const z=start+j,x=center(z);box(1.2,.3,3.1,trim,x-5.5,-.04,z+1.5,g);box(2,.3,3.1,wallMat,x+6,-.04,z+1.5,g);box(.14,1.25,.14,dark,x-5.7,.65,z,g);const next=center(z+3);rod(v(x-5.7,1.15,z),v(next-5.7,1.15,z+3),.055,gold,g);}
lamp(g,center(start+4)-4.8,start+4);
for(let k=0;k<22;k++){const rz=start+4+(Math.random()-.5)*3;const rx=center(start+4)-7-k*.55;const patch=new T.Mesh(new T.PlaneGeometry(.22+Math.random()*.6,.1+Math.random()*.5),reflectionMat);patch.rotation.x=-Math.PI/2;patch.position.set(rx,-.97,rz);g.add(patch);}
if(n%3===0){const tz=start+19,tx=center(tz)-8;rod(v(tx,-1,tz),v(tx,4,tz),.22,roof,g);for(let k=0;k<10;k++)sphere(1.3,leaves,tx+Math.sin(k*2.4)*1.2,3+Math.sin(k)*.8,tz+Math.cos(k*2.4),g);}
for(let j=0;j<3;j++){let z=start+j*8;building(g,center(z)+10,z,n*3+j+10000)}
let z=start+15,x=center(z)-6.3;box(1.8,.65,1.8,mat('#af6e49'),x,.3,z,g);for(let j=0;j<6;j++)sphere(.45,flowers,x+(Math.sin(j*2)*.5),.85,z+Math.cos(j*2)*.5,g);
if(n%2===0){let z=start+13,x=center(z)-15;const boat=sphere(1,mat('#f0dfb4'),x,-.35,z,g,1.3,.6,3.3);rod(v(x,0,z),v(x,7,z),.04,trim,g);box(.8,.25,2,mat('#49658d'),x,.05,z,g);}
const pickups=firefliesForChunk(start).map(({z,lane,finishRow,booster=false,rowId,index,rowEnd})=>{const fire=sphere(booster?.3:finishRow?.17:.14,booster?mat('#9cfff1',{emissive:'#37efcf',emissiveIntensity:2.5}):glow,center(z)+lane,1.5,z,g);const fireHalo=halo(g,fire.position.x,1.5,z,finishRow?1.4:1.2);let ring=null;if(booster){fireHalo.material=haloMaterial.clone();fireHalo.material.color.set('#67ffe4');fireHalo.scale.set(2.3,2.3,1);ring=new T.Mesh(new T.TorusGeometry(.48,.035,8,32),mat('#adffef',{emissive:'#47f6cf',emissiveIntensity:2}));ring.position.copy(fire.position);g.add(ring);}return {fire,fireHalo,ring,booster,rowId,index,rowEnd,taken:false,lane};});
// Elevate roadside scenery with the route; water and moored boats remain at sea level.
for(const o of g.children){if(o===r||o.material===reflectionMat)continue;const ox=o.position.x,oz=o.position.z;if(ox>center(oz)-10)o.position.y+=elevation(oz);}
// Continuous terraced coast and rolling inland hills.
const terrainPoints=[],terrainUV=[],terrainIndices=[];const columns=[-22,-10,-6.2,5.1,8,18,35,60,100,170];
for(let j=0;j<=12;j++){const tz=start+j*2;for(let k=0;k<columns.length;k++){const off=columns[k];let y=off< -15?-2:off< -8?elevation(tz)-4:off<6?elevation(tz)-.3:elevation(tz)+Math.max(0,off-13)*.36+(off>18?Math.sin(tz*.024+off*.055)*off*.16:0);terrainPoints.push(center(tz)+off,y,tz);terrainUV.push(k/9,j/12);}}
for(let j=0;j<12;j++)for(let k=0;k<9;k++){const a=j*10+k;terrainIndices.push(a,a+10,a+1,a+1,a+10,a+11)}const tg=new T.BufferGeometry();tg.setAttribute('position',new T.Float32BufferAttribute(terrainPoints,3));tg.setAttribute('uv',new T.Float32BufferAttribute(terrainUV,2));tg.setIndex(terrainIndices);tg.computeVertexNormals();const tm=new T.Mesh(tg,terrainMat);tm.receiveShadow=true;g.add(tm);
for(let j=0;j<4;j++){const tz=start+3+j*6,tx=center(tz)+23+Math.sin(n+j)*4;const base=elevation(tz)+4;rod(v(tx,base,tz),v(tx,base+5,tz),.19,roof,g);for(let k=0;k<3;k++){const crown=new T.Mesh(new T.ConeGeometry(1.5-k*.3,3.6,9),leaves);crown.position.set(tx,base+3+k*1.3,tz);crown.castShadow=true;g.add(crown);}}
return {g,n,pickups};}
for(let n=-19;n<4;n++)chunks.push(makeChunk(n));
// A bicycle and rider, modelled in local coordinates, facing negative Z.
const bike=new T.Group();scene.add(bike);const rubber=mat('#202632'),steel=mat('#e9b35b',{metalness:.7}),cloth=mat('#e5e1e7'),blue=mat('#355ccd'),jacket=mat('#192b49');const wheels=[];
for(const z of [-.83,.83]){const w=new T.Group();w.position.set(0,.56,z);const tire=new T.Mesh(new T.TorusGeometry(.52,.055,8,32),rubber);tire.rotation.y=Math.PI/2;w.add(tire);for(let i=0;i<12;i++){const a=i*Math.PI/6;rod(v(0,0,0),v(0,Math.sin(a)*.49,Math.cos(a)*.49),.008,trim,w)}bike.add(w);wheels.push(w)}
const A=v(0,.57,.83),B=v(0,.62,0),C=v(0,1.3,.28),D=v(0,1.35,-.62),E=v(0,.57,-.83);for(const [a,b]of[[A,B],[B,C],[A,C],[C,D],[D,B],[D,E]])rod(a,b,.04,steel,bike);box(.34,.09,.42,jacket,0,1.43,.25,bike);rod(D,v(0,1.68,-.65),.04,steel,bike);rod(v(-.4,1.68,-.65),v(.4,1.68,-.65),.035,steel,bike);
const fabric=texture(512,(c,n)=>{c.fillStyle='#eae4d8';c.fillRect(0,0,n,n);c.strokeStyle='#315a9f';c.lineWidth=5;for(let y=0;y<n;y+=75){for(let x=0;x<n;x+=48){c.beginPath();c.ellipse(x,y,15,25,.6,0,Math.PI*2);c.stroke();c.beginPath();c.ellipse(x+20,y+30,8,17,-.6,0,Math.PI*2);c.stroke();}}});cloth.map=fabric;cloth.side=T.DoubleSide;
const secondary=createSecondaryMotion();
const skirtGeo=new T.BufferGeometry(),clothVertices=new Float32Array((secondary.segments+1)*(secondary.rows+1)*3),clothUV=[],clothIndices=[];
for(let j=0;j<=secondary.rows;j++)for(let i=0;i<=secondary.segments;i++){clothUV.push(i/secondary.segments,1-j/secondary.rows);if(j<secondary.rows&&i<secondary.segments){const a=j*(secondary.segments+1)+i,b=a+secondary.segments+1;clothIndices.push(a,b,a+1,a+1,b,b+1);}}
skirtGeo.setAttribute('position',new T.BufferAttribute(clothVertices,3).setUsage(T.DynamicDrawUsage));skirtGeo.setAttribute('uv',new T.Float32BufferAttribute(clothUV,2));skirtGeo.setIndex(clothIndices);
const skirt=new T.Mesh(skirtGeo,cloth);skirt.position.set(0,1.38,.22);skirt.castShadow=true;skirt.receiveShadow=true;skirt.frustumCulled=false;bike.add(skirt);
// The blue hem shares the simulated bottom ring so it cannot detach from the fabric.
const hemGeometry=new T.BufferGeometry(),hemVertices=new Float32Array((secondary.segments+1)*2*3),hemIndices=[];
for(let i=0;i<secondary.segments;i++){const a=i*2;hemIndices.push(a,a+1,a+2,a+2,a+1,a+3);}hemGeometry.setAttribute('position',new T.BufferAttribute(hemVertices,3).setUsage(T.DynamicDrawUsage));hemGeometry.setIndex(hemIndices);
const hemMaterial=blue.clone();hemMaterial.side=T.DoubleSide;const hem=new T.Mesh(hemGeometry,hemMaterial);hem.position.copy(skirt.position);hem.castShadow=true;hem.frustumCulled=false;bike.add(hem);
const torso=sphere(.42,jacket,0,2.05,.15,bike,1,1.4,.7),head=sphere(.27,blue,0,2.78,.02,bike,1,1.15,1);let tuck=0;
function tuckPosition(p){const y=p.y-1.5,z=p.z-.2,c=Math.cos(tuck),s=Math.sin(tuck);return v(p.x,1.5+y*c-z*s,.2+y*s+z*c);}

const braid=[];const hairHighlight=mat('#517df0',{roughness:.48});
for(let i=0;i<secondary.hair.length;i++){const r=.095-i*.003;const left=sphere(r,blue,0,0,0,bike,.85,1.05,.85),right=sphere(r,hairHighlight,0,0,0,bike,.8,1,.8);braid.push({left,right});}
const hairTie=sphere(.06,gold,0,0,0,bike,1,.6,1);
function updateSecondaryMeshes(){for(let j=0;j<=secondary.rows;j++)for(let i=0;i<=secondary.segments;i++){const p=secondary.cloth[j*secondary.segments+i%secondary.segments],k=(j*(secondary.segments+1)+i)*3;clothVertices.set([p.x,p.y,p.z],k);}
skirtGeo.attributes.position.needsUpdate=true;skirtGeo.computeVertexNormals();
for(let i=0;i<=secondary.segments;i++){const bottom=secondary.cloth[secondary.rows*secondary.segments+i%secondary.segments],above=secondary.cloth[(secondary.rows-1)*secondary.segments+i%secondary.segments];hemVertices.set([bottom.x*1.004,bottom.y,bottom.z*1.004,T.MathUtils.lerp(bottom.x,above.x,.55)*1.005,T.MathUtils.lerp(bottom.y,above.y,.55),T.MathUtils.lerp(bottom.z,above.z,.55)*1.005],i*6);}hemGeometry.attributes.position.needsUpdate=true;hemGeometry.computeVertexNormals();
secondary.hair.forEach((p,i)=>{const twist=i*2.5,width=.036*(1-i/20);braid[i].left.position.set(p.x+Math.sin(twist)*width,p.y,p.z+Math.cos(twist)*width);braid[i].right.position.set(p.x-Math.sin(twist)*width,p.y,p.z-Math.cos(twist)*width);});for(const strand of braid){strand.left.position.copy(tuckPosition(strand.left.position));strand.right.position.copy(tuckPosition(strand.right.position));}hairTie.position.copy(braid.at(-1).left.position);
torso.position.copy(tuckPosition(v(0,2.05,.15)));head.position.copy(tuckPosition(v(0,2.78,.02)));torso.rotation.x=head.rotation.x=tuck;}
updateSecondaryMeshes();
const arms=[];for(const side of [-1,1]){const arm=rod(v(0,0,0),v(0,1,0),.095,jacket,bike);arms.push({side,arm});sphere(.08,mat('#dda98a'),side*.39,1.7,-.66,bike)}
const legs=[];for(const side of [-1,1]){const leg=new T.Group();bike.add(leg);const upper=rod(v(0,0,0),v(0,1,0),.085,dark,leg);const lower=rod(v(0,0,0),v(0,1,0),.065,dark,leg);const shoe=box(.18,.13,.32,jacket,0,0,0,leg);legs.push({upper,lower,shoe,side});}
function poseRod(o,a,b){o.position.copy(a).add(b).multiplyScalar(.5);const d=b.clone().sub(a);o.scale.y=d.length();o.quaternion.setFromUnitVectors(v(0,1,0),d.normalize());}
const tailLight=sphere(.065,mat('#ef5540',{emissive:'#ff3322',emissiveIntensity:3}),0,1.1,.98,bike);halo(bike,0,1.1,1,0.4);
const headlight=new T.SpotLight('#ffe7b5',18,18,.5,.8,1);headlight.position.set(0,1.35,-.8);headlight.target.position.set(0,0,-10);bike.add(headlight,headlight.target);
const music=createMusic();elMusicSetup();function elMusicSetup(){const b=document.getElementById('music');b.onclick=async()=>{await music.toggle();b.textContent=music.enabled?'♫ Music on':'♫ Music off';b.setAttribute('aria-pressed',String(music.enabled));};document.getElementById('volume').oninput=e=>music.volume(Number(e.target.value));}

let running=false,paused=false,progress=0,lateral=0,speed=0,collected=0,score=0,phase=0;let ride=createRide(),accumulator=0;const reducedMotion=matchMedia('(prefers-reduced-motion: reduce)').matches;const keys=new Set();const el=id=>document.getElementById(id);function pause(){if(!running)return;paused=!paused;keys.clear();accumulator=0;el('pause').textContent=paused?'Resume':'Pause';el('notice').textContent=paused?'Taking a moment.':''}function reset(){score=0;tuck=0;rows.reset();bonusEffects.reset();collectionEffects.reset();pickupPulse=0;secondary.reset();updateSecondaryMeshes();ride=createRide();accumulator=0;keys.clear();phase=0;camera.position.set(center(7),elevation(7)+3.7,7);progress=0;lateral=0;speed=0;collected=0;paused=false;el('pause').textContent='Pause';el('notice').textContent='';chunks.forEach(disposeChunk);chunks.length=0;for(let n=-19;n<4;n++)chunks.push(makeChunk(n));}
el('start').onclick=async()=>{running=true;el('panel').style.display='none';await music.start();el('music').textContent='♫ Music on';el('music').setAttribute('aria-pressed','true')};el('pause').onclick=pause;el('reset').onclick=reset;window.addEventListener('keydown',e=>{if(e.target instanceof HTMLInputElement||(e.target instanceof HTMLButtonElement&&['Space','Enter'].includes(e.code)))return;if(['ArrowLeft','ArrowRight','ArrowUp','ArrowDown','Space'].includes(e.code))e.preventDefault();keys.add(e.code);if(!e.repeat&&e.code==='Space')pause();if(!e.repeat&&e.code==='KeyR')reset()});window.addEventListener('keyup',e=>keys.delete(e.code));window.addEventListener('blur',()=>{keys.clear();if(running&&!paused)pause()});for(const [id,key] of [['left','ArrowLeft'],['right','ArrowRight'],['pedal','KeyW'],['brake','KeyS']]){el(id).onpointerdown=e=>{el(id).setPointerCapture(e.pointerId);keys.add(key)};el(id).onpointerup=el(id).onpointercancel=()=>keys.delete(key)}
const labelCanvas=document.createElement('canvas');labelCanvas.width=256;labelCanvas.height=128;const labelContext=labelCanvas.getContext('2d');labelContext.font='600 70px system-ui';labelContext.textAlign='center';labelContext.textBaseline='middle';labelContext.shadowColor='#c47b28';labelContext.shadowBlur=14;labelContext.fillStyle='#fff2bb';labelContext.fillText('+1',128,64);
const collectionEffects=new CollectionEffects(scene,{glowTexture,labelTexture:new T.CanvasTexture(labelCanvas),reducedMotion});let pickupPulse=0;
const rows=new RowTracker();const bonusCanvas=document.createElement('canvas');bonusCanvas.width=512;bonusCanvas.height=192;const bc=bonusCanvas.getContext('2d');bc.textAlign='center';bc.fillStyle='#fff3bc';bc.font='bold 64px system-ui';bc.fillText('+5',256,78);bc.font='28px system-ui';bc.fillText('ROW COMPLETE',256,128);const bonusEffects=new CollectionEffects(scene,{glowTexture,labelTexture:new T.CanvasTexture(bonusCanvas),reducedMotion});for(const e of bonusEffects.pool)e.label.scale.set(2,.8,1);
const pickupLight=new T.PointLight('#ffd074',0,5,2);bike.add(pickupLight);pickupLight.position.set(0,1.8,.4);
// Sparse peripheral motes give parallax cues without obscuring the road.
scene.add(camera);
const streakPositions=new Float32Array(40*6),motes=[];
for(let i=0;i<40;i++)motes.push({x:(i%2?1:-1)*(2.6+Math.random()*6),y:(Math.random()-.5)*8,z:-2-Math.random()*24});
const streakGeometry=new T.BufferGeometry();streakGeometry.setAttribute('position',new T.BufferAttribute(streakPositions,3));
const streakMaterial=new T.LineBasicMaterial({color:'#d0dcf0',transparent:true,opacity:0,depthWrite:false});const streaks=new T.LineSegments(streakGeometry,streakMaterial);streaks.frustumCulled=false;camera.add(streaks);
let last=performance.now();function tick(now){requestAnimationFrame(tick);const dt=Math.min((now-last)/1000,.1);last=now;
const previousProgress=ride.progress,previousLateral=ride.lateral;
const steer=(keys.has('KeyD')||keys.has('ArrowRight')?1:0)-(keys.has('KeyA')||keys.has('ArrowLeft')?1:0);
const braking=keys.has('KeyS')||keys.has('ArrowDown'),pedaling=keys.has('KeyW')||keys.has('ArrowUp'),coasting=keys.has('ShiftLeft')||keys.has('ShiftRight');
if(running&&!paused){accumulator+=dt;while(accumulator>=FIXED_STEP){const rz=-ride.progress;const dx=(center(rz+.1)-center(rz-.1))/.2;const ddx=(center(rz+.1)-2*center(rz)+center(rz-.1))/.01;stepRide(ride,{steer,brake:braking,pedal:pedaling,coast:coasting},{dx,dy:slope(rz),curvature:ddx/Math.pow(1+dx*dx,1.5)});secondary.step(FIXED_STEP,{speed:ride.speed,acceleration:ride.acceleration,lean:ride.lean,distance:ride.distance});accumulator-=FIXED_STEP;}}
else accumulator=0;
progress=ride.progress;lateral=ride.lateral;speed=ride.speed;phase=ride.cadence;
const z=-progress,moving=running&&!paused?speed:0,intensity=T.MathUtils.smoothstep(moving,5,19);
const roadHeading=Math.atan((center(z+.1)-center(z-.1))/.2);
const surfaceBob=reducedMotion?0:Math.sin(ride.distance*8)*.007*Math.min(speed/10,1);
bike.position.set(center(z)+lateral,elevation(z)+.07+surfaceBob,z);
bike.rotation.set(-Math.atan(slope(z)*Math.cos(roadHeading)),roadHeading-ride.heading,ride.lean,'YXZ');
wheels.forEach(w=>w.rotation.x=-ride.wheelAngle);wheels[0].rotation.y=-ride.steering;
if(running&&!paused)tuck+=(-.18*T.MathUtils.smoothstep(speed,5,24)-tuck)*(1-Math.exp(-dt*3));
updateSecondaryMeshes();for(const {side,arm}of arms)poseRod(arm,tuckPosition(v(side*.3,2.25,.04)),v(side*.39,1.73,-.64));
streakMaterial.opacity=reducedMotion?0:intensity*.17;
for(let i=0;i<motes.length;i++){const m=motes[i];m.z+=moving*dt*1.7;if(m.z>-.5)m.z=-26;const k=i*6;streakPositions.set([m.x,m.y,m.z,m.x,m.y,m.z-Math.max(.05,intensity*.85)],k);}streakGeometry.attributes.position.needsUpdate=true;
legs.forEach((l,i)=>{const a=phase+i*Math.PI;const foot=v(l.side*.23,.59+Math.sin(a)*.2,.1+Math.cos(a)*.22);const hip=v(l.side*.2,1.44,.25);const knee=v(l.side*.26,1.02+Math.sin(a)*.1,-.2+Math.cos(a)*.12);poseRod(l.upper,hip,knee);poseRod(l.lower,knee,foot);l.shoe.position.copy(foot);});
const current=Math.floor(z/24);for(let i=0;i<chunks.length;i++){let c=chunks[i];if(c.n>current+3){disposeChunk(c);chunks[i]=makeChunk(c.n-23);c=chunks[i]}
for(const pickup of c.pickups){const {fire,fireHalo}=pickup;fire.position.y=elevation(fire.position.z)+1.5+Math.sin(now*.002+fire.position.z*.12)*.15;fireHalo.position.y=fire.position.y;if(pickup.ring){pickup.ring.position.copy(fire.position);pickup.ring.rotation.y=now*.002;}
const pickupProgress=-fire.position.z,travel=progress-previousProgress;
const crossing=pickupProgress>=previousProgress-1.1&&pickupProgress<=progress+1.1;
const fraction=travel>0?T.MathUtils.clamp((pickupProgress-previousProgress)/travel,0,1):1;
const laneAtPickup=T.MathUtils.lerp(previousLateral,lateral,fraction);
if(!pickup.taken&&running&&!paused&&crossing&&Math.abs(pickup.lane-laneAtPickup)<.8){pickup.taken=true;fire.visible=false;fireHalo.visible=false;collected++;score++;collectionEffects.burst(fire.position);pickupPulse=1;if(rows.collect(pickup)){score+=5;bonusEffects.burst(fire.position.clone().add(v(0,.7,0)));music.bonus();}else music.collect(collected,pickup.booster);if(pickup.booster){activateBoost(ride);pickup.ring.visible=false;}}
}}
if(running&&!paused){collectionEffects.update(dt);bonusEffects.update(dt);rows.prune(progress);pickupPulse=Math.max(0,pickupPulse-dt*2.8);}pickupLight.intensity=pickupPulse*(reducedMotion?1.5:6);el('distance').style.color=pickupPulse>0?'#ffe5a0':'';el('distance').style.textShadow=`0 0 ${pickupPulse*16}px #ffc65c`;
const rush=reducedMotion?0:T.MathUtils.smoothstep(speed,3,20);
const followDistance=7-rush*1.7,followHeight=3.7-rush*.8;
const cameraZ=z+followDistance;
camera.position.lerp(v(center(cameraZ)+lateral*.86,elevation(cameraZ)+followHeight+surfaceBob*.4,cameraZ),1-Math.exp(-dt*8));
const lookAhead=10+speed*.35;camera.lookAt(center(z-lookAhead)+lateral*.6+ride.heading*2,elevation(z-lookAhead)+1.7,z-lookAhead);
// Keep the horizon level while steering.
const targetFov=62+rush*13;camera.fov+=(targetFov-camera.fov)*(1-Math.exp(-dt*3));camera.updateProjectionMatrix();
music.setSpeed(moving);
el('ride-state').textContent=paused?'Paused':!running?'Ready to ride':ride.contact?'Road edge':braking?'Braking':ride.boostRemaining>0?`Firefly boost · ${Math.ceil(ride.boostRemaining)}s`:coasting?'Coasting':pedaling?'Pedaling hard':slope(z)<-.012?'Assisted climb':slope(z)>.012?'Downhill cruise':'Easy pedaling';
sun.position.set(center(z)-25,elevation(z)+50,z+20);sun.target.position.set(center(z),elevation(z),z-20);ocean.position.z=z;moon.position.z=z-480;moonHalo.position.z=z-480;sea.uniforms.time.value=now*.001;sky.position.copy(camera.position);mountains.position.z=z;starfield.position.z=z;music.setPaused(paused);el('elevation').textContent=`${Math.round(elevation(z))} m elevation · ${Math.round(-slope(z)*100)}% grade`;el('speed').textContent=Math.round(speed*3.6);el('distance').textContent=`${Math.floor(ride.distance)} m  ·  ${collected} fireflies  ·  ${score} score`;composer.render()}camera.position.set(center(7),elevation(7)+3.7,7);requestAnimationFrame(tick);addEventListener('resize',()=>{camera.aspect=innerWidth/innerHeight;camera.updateProjectionMatrix();renderer.setSize(innerWidth,innerHeight);composer.setSize(innerWidth,innerHeight)});
