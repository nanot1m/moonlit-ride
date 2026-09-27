"""Re-export the selected Quaternius CC0 meshes using Blender --background --python art/export_nature.py."""
import bpy
from pathlib import Path
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'art/third-party/quaternius/CoastalNature.blend'))
for obj in bpy.data.objects:
    if obj.type!='MESH':continue
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.ops.export_scene.fbx(filepath=str(root/'Unity/Assets/MoonlitRide/Resources/Nature'/(obj.name+'.fbx')),use_selection=True,object_types={'MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
