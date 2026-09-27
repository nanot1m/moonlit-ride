from pathlib import Path
import bpy,math
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Unity/Assets/MoonlitRide/Resources/Bicycle'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'art/third-party/poly-bicycle/SourceParts.blend'))
def coord(p):return Vector((p[0],-p[2],p[1]))
def mat(name,c,metal=0,rough=.4):
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True;b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*c,1);b.inputs['Metallic'].default_value=metal;b.inputs['Roughness'].default_value=rough;return m
paint=mat('Copper enamel',(.50,.17,.055),.65,.28);chrome=mat('Brushed chrome',(.65,.69,.72),.8,.25);rubber=mat('Tire rubber',(.014,.019,.022),0,.8);leather=mat('Brown leather',(.105,.044,.022),0,.65);black=mat('Chain steel',(.05,.06,.065),.75,.35);cream=mat('Cream sidewall',(.67,.58,.39),0,.7)
groups={k:[] for k in ['Frame','Steering','Wheel','Crank','Pedal']}
steerPivot=Vector((0,1.3,-.64));crankPivot=Vector((0,.54,.1))
def longitudinal(y):
 points=[(-500,-1.455),(-295.33,-.83),(-181.54,-.57),(43.05,.1),(112.85,.25),(250.18,.83),(500,1.89)]
 for (a,va),(b,vb) in zip(points,points[1:]):
  if a<=y<=b:return va+(vb-va)*(y-a)/(b-a)
 return y*.00304
remove=set(range(2,23))|set(range(25,31))|set(range(34,43))|{55}
steering={1,23,24,43,45,46,49,56,58,59,60,61}
for o in list(bpy.data.objects):
 if o.type!='MESH':bpy.data.objects.remove(o,do_unlink=True);continue
 num=int(o.name.split('.')[-1]);
 if num in remove:bpy.data.objects.remove(o,do_unlink=True);continue
 group='Steering' if num in steering else 'Frame';oldmats=list(o.data.materials);mapping=[]
 for m in oldmats:mapping.append(paint if m.name=='Mat.2' else leather if m.name=='Mat.3' else rubber if m.name=='Mat.1' else chrome)
 for i,m in enumerate(mapping):o.data.materials[i]=m
 for v in o.data.vertices:
  x,y,z=v.co;up=.56+(z+80)*.0033
  up+=.11*math.exp(-((y-43)/120)**2)*math.exp(-((z+119)/110)**2)
  if y>50:up-=.14*max(0,min(1,(z-125)/105))
  if y<-130:up+=.10*max(0,min(1,(z-120)/130))
  p=Vector((x*.00252,up,longitudinal(y)))
  if group=='Steering':p-=steerPivot
  v.co=coord(p)
 for poly in o.data.polygons:poly.use_smooth=True
 groups[group].append(o)

def rod(name,a,b,r,material,group,sides=12):
 a,b=coord(a),coord(b);d=b-a;bpy.ops.mesh.primitive_cylinder_add(vertices=sides,radius=r,depth=d.length,location=(a+b)/2);o=bpy.context.object;o.name=name;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();o.data.materials.append(material)
 for p in o.data.polygons:p.use_smooth=True
 groups[group].append(o);return o

def torus(name,center,r,t,material,group):
 bpy.ops.mesh.primitive_torus_add(major_radius=r,minor_radius=t,major_segments=96,minor_segments=10,location=coord(center),rotation=(0,math.pi/2,0));o=bpy.context.object;o.name=name;o.data.materials.append(material)
 for p in o.data.polygons:p.use_smooth=True
 groups[group].append(o)

def box(name,pos,scale,material,group):
 bpy.ops.mesh.primitive_cube_add(size=1,location=coord(pos));o=bpy.context.object;o.name=name;o.scale=(scale[0],scale[2],scale[1]);o.data.materials.append(material);groups[group].append(o)
 bevel=o.modifiers.new('Rounded edges','BEVEL');bevel.width=.008;bevel.segments=2
 bpy.context.view_layer.objects.active=o;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);bpy.ops.object.modifier_apply(modifier=bevel.name)
 return o
# Smooth rims, tires, hubs and 32 crossed spokes replace the coarse source wheels.
torus('Tire',(0,0,0),.493,.027,rubber,'Wheel');torus('Rim',(0,0,0),.459,.013,chrome,'Wheel')
for side in [-1,1]:
 torus('Tan sidewall',(side*.018,0,0),.485,.007,cream,'Wheel')
 for k in range(16):
  a=(k/16+(.025 if side>0 else 0))*math.tau;b=a+side*.18
  rod('Crossed spoke',(side*.055,math.sin(a)*.055,math.cos(a)*.055),(side*.006,math.sin(b)*.455,math.cos(b)*.455),.0028,chrome,'Wheel',6)
rod('Hub',(-.075,0,0),(.075,0,0),.033,chrome,'Wheel')
# Crank is authored about its own centre; the pedals remain level in the runtime rig.
torus('Chainring',(.135,0,0),.135,.014,chrome,'Crank')
for k in range(5):
 a=k*math.tau/5;rod('Chainring spider',(.135,0,0),(.135,math.sin(a)*.13,math.cos(a)*.13),.009,chrome,'Crank')
for side in [-1,1]:rod('Crank arm',(side*.13,0,0),(side*.23,0,-side*.21),.015,chrome,'Crank')
rod('Bottom bracket',(-.15,0,0),(.15,0,0),.028,black,'Crank')
box('Pedal body',(0,0,0),(.15,.045,.19),black,'Pedal')
for zz in [-.065,.065]:box('Pedal reflector',(0,.005,zz),(.12,.025,.015),cream,'Pedal')
# Chain and rear carrier are authored in the fixed-frame mesh.
rod('Chain upper',(.14,.68,.1),(.14,.61,.83),.008,black,'Frame');rod('Chain lower',(.14,.40,.1),(.14,.51,.83),.008,black,'Frame')
torus('Rear sprocket',(.14,.56,.83),.061,.009,black,'Frame')
for side in [-1,1]:
 rod('Carrier stay',(side*.085,.56,.83),(side*.16,1.18,.83),.009,chrome,'Frame')
 rod('Carrier rail',(side*.16,1.18,.47),(side*.16,1.18,1.14),.011,chrome,'Frame')
 for zz in [.49,.64,.79,.94,1.09]:rod('Carrier slat',(-.16,1.18,zz),(.16,1.18,zz),.008,chrome,'Frame')
for side in [-1,1]:
 a=Vector((side*.32,1.80,-.57))-steerPivot;b=Vector((side*.46,1.80,-.57))-steerPivot
 rod('Leather grip',a,b,.022,leather,'Steering');rod('Brake lever',a+Vector((0,-.035,-.065)),b+Vector((0,-.035,-.065)),.008,chrome,'Steering')
# Export five multi-material meshes; all coordinates are local to the runtime pivot.
for name,objects in groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=objects[0];o.name=name
 bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
 bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'art/CityBicycle.blend'))
