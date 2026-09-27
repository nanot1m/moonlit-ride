"""Original Moonlit Ride assets. Run with Blender --background --python art/build_rider.py.
Editable .blend source and an explicit-axis mesh exchange file are exported together.
Unity's editor converts the exchange data to native Mesh assets (no Blender dependency at runtime).
"""
import bpy, math, json, pathlib
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / 'Unity/Assets/MoonlitRide/Resources/Rider'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def coord(p): return (p[0], -p[2], p[1])
def material(name, color, roughness=.45):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=roughness
    return m
blue=material('Midnight indigo silk',(.035,.09,.26),.3)
skin=material('Warm porcelain',(.69,.42,.29),.55)
fabric=material('Ivory botanical jacquard',(.84,.81,.71),.8)
jacket=material('Indigo fitted riding bodice',(.04,.085,.15),.7)

def mesh(name, vertices, faces, uv=None, mat=None):
    m=bpy.data.meshes.new(name); m.from_pydata([coord(v) for v in vertices],[],faces); m.update()
    o=bpy.data.objects.new(name,m); bpy.context.collection.objects.link(o)
    if uv:
        layer=m.uv_layers.new(name='UVMap')
        for poly in m.polygons:
            for loop in poly.loop_indices: layer.data[loop].uv=uv[m.loops[loop].vertex_index]
    for poly in m.polygons: poly.use_smooth=True
    if mat: m.materials.append(mat)
    return o

def tube(name, path, radii, sides=8, mat=blue):
    vs=[]; faces=[]; uvs=[]
    for j,p in enumerate(path):
        tangent=Vector(path[min(j+1,len(path)-1)])-Vector(path[max(0,j-1)])
        tangent.normalize(); ref=Vector((1,0,0)) if abs(tangent.x)<.9 else Vector((0,0,1))
        n=tangent.cross(ref).normalized(); b=tangent.cross(n).normalized()
        for i in range(sides+1):
            a=i/sides*math.tau; v=Vector(p)+radii[j]*(math.cos(a)*n+math.sin(a)*b)
            vs.append(v); uvs.append((i/sides,j/(len(path)-1)))
            if j<len(path)-1 and i<sides:
                k=j*(sides+1)+i; faces.append((k,k+1,k+sides+2,k+sides+1))
    return mesh(name,vs,faces,uvs,mat)

def join(name, objects):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]; bpy.ops.object.join(); objects[0].name=name
    return objects[0]

# Cloth-ready continuous grid. Two waist rings are fixed; the blue border is part of the same surface.
vs=[]; fs=[]; uv=[]; N=64; ROWS=28
for j in range(ROWS+1):
    t=j/ROWS
    for i in range(N+1):
        a=i/N*math.tau
        radius=.190+.59*t**.9
        radius+=math.cos(a*10)*(.003+.018*t**1.2)+math.sin(a*8+.4)*.008*t
        y=1.855-.82*t+.16*t**3*max(0,-math.cos(a))
        vs.append((math.sin(a)*radius,y,.105+.095*t+math.cos(a)*(.145+.54*t**.9))); uv.append((i/N,1-t))
        if j<ROWS and i<N:
            k=j*(N+1)+i; fs.append((k,k+N+1,k+N+2,k+1))
dress=mesh('Dress',vs,fs,uv,fabric)

# Shaped bodice, narrower at the waist and shoulders instead of a single sphere.
vs=[]; fs=[]; uv=[]; rings=[(1.83,.25,.18),(1.93,.265,.19),(2.12,.32,.235),(2.31,.31,.20),(2.42,.19,.16)]
for j,(y,rx,rz) in enumerate(rings):
    for i in range(49):
        a=i/48*math.tau; vs.append((math.sin(a)*rx,y,.12+math.cos(a)*rz));uv.append((i/48,j/4))
        if j<4 and i<48:
            k=j*49+i;fs.append((k,k+1,k+50,k+49))
bodice=mesh('Bodice',vs,fs,uv,jacket)

# Face and ears in head-local space; the scalp keeps an asymmetric swept hairline.
def ellipsoid(name,position,scale,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=20,location=coord(position))
    o=bpy.context.object;o.name=name;o.scale=(scale[0],scale[2],scale[1]);o.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for p in o.data.polygons:p.use_smooth=True
    return o
# CC0 MakeHuman adult female head: preserve anatomical eyelids, lips, ears and nose.
source=ROOT/'art/third-party/makehuman'; human=[]; faces=[]; group=''
for line in (source/'base.obj').read_text().splitlines():
    q=line.split()
    if not q:continue
    if q[0]=='v':human.append(list(map(float,q[1:4])))
    elif q[0]=='g':group=q[1]
    elif q[0]=='f' and group=='body':faces.append([int(v.split('/')[0])-1 for v in q[1:]])
for target in source.glob('*.target'):
    for line in target.read_text().splitlines():
        q=line.split()
        if len(q)==4 and not line.startswith('#'):
            idx=int(q[0])
            for k in range(3):human[idx][k]+=float(q[k+1])
selected=[f for f in faces if all(human[i][1]>5.05 for i in f)]
ids=sorted(set(i for f in selected for i in f)); lookup={v:i for i,v in enumerate(ids)}
# MakeHuman faces +Z; the rider looks -Z.
def fitted_head(p):
    face=np.array([p[0]*.22,(p[1]-7.16)*.22+.08,-(p[2]-.30)*.22])
    neck=np.array([p[0]*.1548,p[1]*.16+1.5-2.675,(-p[2]*.16+.1)*.90+.1-.134])
    t=max(0,min(1,(p[1]-5.35)/.95));t=t*t*(3-2*t)
    return neck*(1-t)+face*t
head=mesh('Head',[fitted_head(human[i]) for i in ids],[[lookup[i] for i in reversed(f)] for f in selected],mat=skin)
# Axis reflection requires reversed winding above.
# Fit hair to the actual anatomical skull so the forehead cannot clip through it.
skull=BVHTree.FromPolygons([v.co.copy() for v in head.data.vertices],[list(p.vertices) for p in head.data.polygons])
hair_center=Vector(coord((0,.035,-.03)))
def fit_hair(p,clearance=.012):
    point=Vector(coord(p));delta=point-hair_center;distance=delta.length
    if distance<.001:return p
    direction=delta.normalized();hit,normal,index,length=skull.ray_cast(hair_center,direction,.6)
    if hit is not None and distance<length+clearance:point=hair_center+direction*(length+clearance)
    return (point.x,point.z,-point.y)
# Fitted scalp with a raised temple hairline and a tapered, gathered nape.
# No hemispherical straight-cut rim: every rear section converges into the braid.
def smooth(a,b,x):
    t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def scalp(theta,azimuth):
    back=max(0,math.cos(azimuth));g=smooth(1.35,2.55,theta)*back**2
    x=.195*math.sin(theta)*math.sin(azimuth)
    y=.245*math.cos(theta)+.012
    z=.225*math.sin(theta)*math.cos(azimuth)-.018
    return fit_hair((x*(1-.82*g),y*(1-g)-.115*g,z*(1-g)+.19*g))
vs=[];fs=[];uv=[]
for j in range(29):
    for i in range(81):
        a=i/80*math.tau;back=max(0,math.cos(a));side=abs(math.sin(a))
        limit=1.04+.40*side+1.55*back**2+.055*math.sin(a*3)
        theta=.012+j/28*limit;v=scalp(theta,a)
        vs.append(v);uv.append((v[0]*2.7+.5+.025*math.sin(theta*2),theta/math.pi))
        if j<28 and i<80:
            k=j*81+i;fs.append((k,k+81,k+82,k+1))
cap=mesh('HairCap',vs,fs,uv,blue)
locks=[]
# Fine curved locks combed from the forehead over the crown into the low braid.
for k in range(30):
    lateral=(k/29*2-1)*.172;path=[];r=[]
    for j in range(49):
        t=j/48;phi=-1.05+t*3.6
        x=lateral*(1-.42*math.sin(t*math.pi))+.012*math.sin(t*math.pi)
        section=math.sqrt(max(.06,1-(x/.201)**2))
        y=.250*section*math.cos(phi)+.016;z=.232*section*math.sin(phi)-.018
        gather=smooth(.74,1,t)
        x=x*(1-gather);y=y*(1-gather)-.115*gather;z=z*(1-gather)+.194*gather
        path.append(fit_hair((x,y,z),.016));r.append(.0008+.0025*math.sin(math.pi*t)**.65)
    locks.append(tube('Combed strand',path,r,6))
# A few tapered wisps follow the temples rather than forming a solid fringe.
for side in [-1,1]:
    for k in range(3):
        path=[];r=[]
        for j in range(18):
            t=j/17
            path.append((side*(.16+.02*math.sin(t*math.pi)),.10-t*(.16+k*.007),-.09+.09*t))
            r.append(.0032*(1-t)+.0003)
        locks.append(tube('Temple wisp',path,r,6))
detail=join('HairDetail',locks)

# Three interwoven continuous strands. Unity skins this mesh to physical joint bones.
strands=[]
for strand in range(3):
    path=[];rs=[]
    for j in range(113):
        t=j/112; a=t*math.tau*7+strand*math.tau/3; taper=(1-.78*t)
        rad=.054*taper*min(1,.28+t*16)
        path.append((math.sin(a)*rad,-t*.96,math.cos(a)*rad))
        rs.append((.045+.003*math.sin(a*2))*taper)
    strands.append(tube('Woven strand',path,rs,10))
braid=join('Braid',strands)

# Original tileable texture maps, generated in Blender; no external asset licenses.
def image(name,rgb):
    h,w,_=rgb.shape; im=bpy.data.images.new(name,width=w,height=h,alpha=True)
    rgba=np.concatenate([np.clip(rgb,0,1),np.ones((h,w,1))],axis=2).astype(np.float32)
    im.pixels.foreach_set(rgba.ravel());im.filepath_raw=str(OUT/(name+'.png'));im.file_format='PNG';im.save();return im
size=1024;y,x=np.mgrid[0:size,0:size]/size
# Broad cobalt folk-ornament bands, matching the reference's white/blue skirt.
# Horizontal porcelain-inspired borders alternate with large leafy sprays.
# y=0 is the hem in Blender's bottom-up pixel buffer.
ink=np.array([.018,.065,.62]);ivory=np.array([.985,.985,.975])
pattern=np.zeros_like(x,dtype=bool)
def flowers(repeats,center,height):
    u=(x*repeats)%1-.5;v=(y-center)/height
    angle=np.arctan2(v,u);rad=np.sqrt(u*u+v*v)
    edge=.26+.105*np.cos(angle*6)
    return ((rad<edge)&(rad>.095)) | (rad<.055)
for center,height,repeats in [(.16,.14,18),(.49,.18,14),(.81,.13,20)]:
    band=np.abs(y-center)<height*.48
    floral=flowers(repeats,center,height)
    scallop=np.abs(y-center)<height*(.43+.035*np.cos(x*math.tau*repeats))
    # White rosettes punched through dense cobalt borders.
    pattern |= band & scallop & ~floral
    pattern |= (np.abs(np.abs(y-center)-height*.49)<.004)
for center,height in [(.325,.22),(.655,.20)]:
    u=(x*12)%1-.5;v=(y-center)/height
    stem=np.abs(u-.15*np.sin(v*4))<.018
    spray=stem & (np.abs(v)<.45)
    for offset in [-.28,0,.28]:
        for side in [-1,1]:
            du=u-side*.14-.15*np.sin(offset*4);dv=v-offset
            along=du*side*.75+dv*.66;across=-du*side*.66+dv*.75
            spray |= (along/.20)**2+(across/.066)**2<1
    pattern |= spray & (np.abs(v)<.49)
pattern |= (np.abs(y-.07)<.006)|(np.abs(y-.91)<.006)
rgb=np.where(pattern[...,None],ink,ivory)
weave=.003*np.sin(x*size*math.pi)*np.cos(y*size*math.pi)
im=image('DressFabric',rgb+weave[...,None]); fabric.node_tree.nodes.new('ShaderNodeTexImage').image=im
node=next(n for n in fabric.node_tree.nodes if n.type=='TEX_IMAGE');fabric.node_tree.links.new(node.outputs['Color'],fabric.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
hair=np.zeros((size,size,3)); bands=.94+.035*np.sin(x*math.tau*90)+.015*np.sin(x*math.tau*207+y*3)
hair[:]=np.array([.008,.10,.98]);hair*=bands[...,None];image('HairStrands',hair)
row=np.floor(y*16);bx=(x*8+(row%2)*.5)%1;by=(y*16)%1
edge=np.minimum(np.minimum(bx,1-bx)*2,np.minimum(by,1-by)*2)
mortar=edge<.06;variation=np.sin(np.floor(x*8+(row%2)*.5)*71+row*33)*.045
stone=np.array([.50,.47,.44])+variation[...,None]+.012*np.sin(x*355+y*499)[...,None]
stone=np.where(mortar[...,None],np.array([.25,.25,.26]),stone)
image('CoastalPaving',stone)
height=np.clip(edge/.13,0,1)*.07;dy,dx=np.gradient(height);n=np.stack([-dx*26,-dy*26,np.ones_like(dx)],axis=-1);n/=np.linalg.norm(n,axis=2)[...,None]
image('CoastalPavingNormal',n*.5+.5)

# Full clothed anatomical body, using CC0 MakeHuman skin weights reduced to cycling bones.
rig=json.loads((source/'default.mhskel').read_text()); rawweights=json.loads((source/'default_weights.mhw').read_text())['weights']
def joint(name):
    indices=rig['joints'][name]
    return np.mean([human[i] for i in indices],axis=0)
def humanpos(p):return np.array([p[0]*.1548,p[1]*.16+1.5,(-p[2]*.16+.1)*.90+.1])
def endpoints(name):
    b=rig['bones'][name];return humanpos(joint(b['head'])),humanpos(joint(b['tail']))
bone_names=['pelvis','torso']; rests=[(np.array([0,1.5,.2]),np.array([0,1.7,.2])),(np.array([0,1.855,.2]),np.array([0,2.25,.2]))]
for side in ['R','L']:
    for name in ['upperarm01','lowerarm01','wrist','upperleg01','lowerleg01','foot']:
        full=name+'.'+side
        if name=='upperarm01':a,_=endpoints(full);b,_=endpoints('lowerarm01.'+side)
        elif name=='lowerarm01':a,_=endpoints(full);b,_=endpoints('wrist.'+side)
        elif name=='upperleg01':a,_=endpoints(full);b,_=endpoints('lowerleg01.'+side)
        elif name=='lowerleg01':a,_=endpoints(full);b,_=endpoints('foot.'+side)
        else:a,b=endpoints(full)
        bone_names.append(full);rests.append((a,b))
weights=[{} for _ in human]
for name,entries in rawweights.items():
    side=name[-1] if name.endswith(('.L','.R')) else ''
    prefix=name.split('.')[0];mapped=1
    if prefix.startswith(('upperarm','lowerarm')):mapped=bone_names.index(('upperarm01' if prefix.startswith('upperarm') else 'lowerarm01')+'.'+side)
    elif prefix.startswith(('finger','thumb','wrist','metacarpal')):mapped=bone_names.index('wrist.'+side)
    elif prefix.startswith(('upperleg','lowerleg')):mapped=bone_names.index(('upperleg01' if prefix.startswith('upperleg') else 'lowerleg01')+'.'+side)
    elif prefix.startswith(('foot','toe')):mapped=bone_names.index('foot.'+side)
    elif prefix.startswith(('pelvis','root')):mapped=0
    for idx,w in entries:weights[idx][mapped]=weights[idx].get(mapped,0)+w
# Bake a relaxed handlebar grip into the fingers; the hand bones remain IK-driven.
grip_positions=[humanpos(p) for p in human]
for side in ['R','L']:
    for finger in range(2,6):
        base,_=endpoints('finger%d-1.%s'%(finger,side));_,tip=endpoints('finger%d-3.%s'%(finger,side))
        axis=tip-base;axis/=np.linalg.norm(axis)
        bend=np.array([0.,0.,-1.]);bend-=axis*np.dot(bend,axis);bend/=np.linalg.norm(bend)
        influence={}
        for segment in range(1,4):
            for idx,w in rawweights.get('finger%d-%d.%s'%(finger,segment,side),[]):influence[idx]=influence.get(idx,0)+w
        for idx,w in influence.items():
            original=humanpos(human[idx]);along=max(0,float(np.dot(original-base,axis)));radius=.047
            angle=min(along/radius,2.1)
            delta=axis*(radius*math.sin(angle)-along)+bend*(radius*(1-math.cos(angle)))
            grip_positions[idx]+=delta*min(1,w)
body_objects=[];body_weights={}
for name,kind,mat in [('AnatomicalBodice','bodice',jacket),('AnatomicalSkin','skin',skin),('AnatomicalLeggings','legs',jacket)]:
    chosen=[]
    for face in faces:
        center=np.mean([human[i] for i in face],axis=0)
        if center[1]>5.45:continue
        arm=sum(sum(w for k,w in weights[i].items() if k in (2,3,4,8,9,10)) for i in face)/len(face)
        forearm=sum(sum(w for k,w in weights[i].items() if k in (3,4,9,10)) for i in face)/len(face)
        wrist=sum(sum(w for k,w in weights[i].items() if k in (4,10)) for i in face)/len(face)
        category='skin' if wrist>.5 else 'bodice' if center[1]>2.10 or arm>.5 else 'legs'
        if category==kind and not (kind=='legs' and center[1]<-7.3):chosen.append(face)
    ids=sorted(set(i for f in chosen for i in f));lookup={v:i for i,v in enumerate(ids)}
    ob=mesh(name,[grip_positions[i] for i in ids],[[lookup[i] for i in reversed(f)] for f in chosen],uv=[(human[i][0]*.3,human[i][1]*.3) for i in ids],mat=mat)
    # A loose blouse has its own volume, rather than skin painted black.
    if kind=='bodice':
        ob.data.update(); original_normals=[v.normal.copy() for v in ob.data.vertices]
        for local,original in enumerate(ids):
            v=ob.data.vertices[local];y=human[original][1]
            sleeve=sum(w for k,w in weights[original].items() if k in (2,8))
            cuff=sum(w for k,w in weights[original].items() if k in (3,9))
            waist=max(0,min(1,(y-2.1)/.65));neck=max(0,min(1,(5.45-y)/.5))
            loosen=(.010+.012*sleeve)*waist*neck*(1-min(1,cuff)*.85)
            p=humanpos(human[original]);fold=math.sin(p[0]*34+p[1]*19)*math.sin(p[1]*27)*.006
            v.co += original_normals[local]*(loosen+fold*waist*neck)
        bpy.context.view_layer.objects.active=ob
        smooth=ob.modifiers.new('Soft fabric shaping','SMOOTH');smooth.factor=.4;smooth.iterations=4
        bpy.ops.object.modifier_apply(modifier=smooth.name)
    body_objects.append(ob);body_weights[name]=[weights[i] for i in ids]
(OUT/'BodyRig.json').write_text(json.dumps({'bones':[{'name':name,'start':dict(zip(('x','y','z'),map(float,a))),'end':dict(zip(('x','y','z'),map(float,b)))} for name,(a,b) in zip(bone_names,rests)]}))

# Export Blender evaluated topology with UV seams preserved and explicit Unity axes.
records=[]
for obj in [dress,bodice,head,cap,detail,braid]+body_objects:
    m=obj.data;m.calc_loop_triangles();vs=[];ns=[];uvs=[];indices=[];lookup={};uv2s=[];bone_indices=[];bone_weights=[]
    for tri in m.loop_triangles:
        for loopid in tri.loops:
            loop=m.loops[loopid];v=obj.matrix_world@m.vertices[loop.vertex_index].co;n=obj.matrix_world.to_3x3()@m.vertices[loop.vertex_index].normal
            tex=m.uv_layers.active.data[loopid].uv if m.uv_layers.active else (0,0)
            pos=tuple(round(q,6) for q in (v.x,v.z,-v.y));normal=(n.x,n.z,-n.y)
            second=(0,0)
            if obj.name=='Dress':
                second=(tex[1],0);tex=(round(math.sin(tex[0]*math.tau),6),round(math.cos(tex[0]*math.tau),6))
                key=pos # A welded simulation mesh; angular texture coordinates avoid any UV seam.
            else:key=tuple(round(q,6) for q in (*pos,*normal,*tex))
            if key not in lookup:
                lookup[key]=len(vs)//3;vs.extend(pos);ns.extend(normal);uvs.extend(tex);uv2s.extend(second)
                if obj.name in body_weights:
                    values=sorted(body_weights[obj.name][loop.vertex_index].items(),key=lambda item:-item[1])[:4]
                    total=sum(w for k,w in values) or 1
                    values += [(0,0)]*(4-len(values));bone_indices.extend(k for k,w in values);bone_weights.extend(w/total for k,w in values)
            indices.append(lookup[key])
    records.append(dict(name=obj.name,positions=vs,normals=ns,uv=uvs,uv2=uv2s,triangles=indices,boneIndices=bone_indices,boneWeights=bone_weights))
(ROOT/'art/RiderMeshes.json').write_text(json.dumps(dict(meshes=records),separators=(',',':')))
# Separate objects in the saved source for editing; runtime coordinates above are unaffected.
for i,obj in enumerate([dress,bodice,head,cap,detail,braid]): obj['unity_asset']=obj.name
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'art/MoonlitRider.blend'))
print('MOONLIT_BLENDER_ASSETS_READY',[(r['name'],len(r['positions'])//3) for r in records])
