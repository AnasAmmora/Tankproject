"""Offline Blender modeling source. Unity imports only the exported FBX."""
import bpy, math, os
from mathutils import Vector
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, 'Assets/ProjectsFiles/Models/Bases')
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
mats = []
for name, color in [('Armor', (.44,.49,.50,1)), ('Graphite', (.075,.12,.15,1)), ('Team', (.025,.38,.85,1))]:
    m = bpy.data.materials.new(name); m.diffuse_color=color; mats.append(m)
parts=[]
def mesh(name, verts, faces, mat):
    data=bpy.data.meshes.new(name); data.from_pydata(verts, [], faces); data.update()
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj)
    data.materials.append(mats[mat]); parts.append(obj)
    return obj
def ring(w,d,c,z):
    return [(-w/2+c,-d/2,z),(w/2-c,-d/2,z),(w/2,-d/2+c,z),(w/2,d/2-c,z),
            (w/2-c,d/2,z),(-w/2+c,d/2,z),(-w/2,d/2-c,z),(-w/2,-d/2+c,z)]
def shell(name,w,d,wt,dt,z0,z1,mat,c=.06):
    v=ring(w,d,c,z0)+ring(wt,dt,min(c,wt*.2,dt*.2),z1)
    return mesh(name,v,[tuple(reversed(range(8))),tuple(range(8,16))]+[(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)],mat)
def box(name, pos, size, mat, bevel=.0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos); o=bpy.context.object; o.name=name
    o.dimensions=size; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(mats[mat]); parts.append(o)
    if bevel:
        mod=o.modifiers.new('Single segment chamfer','BEVEL'); mod.width=bevel; mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return o
shell('Foundation',1,1,.98,.98,-.50,-.37,1,.09)
shell('Foundation upper lip',.98,.98,.91,.91,-.37,-.30,0,.08)
shell('Sloped armored command hull',.90,.90,.70,.72,-.30,.10,0,.08)
shell('Team shoulder band',.80,.82,.77,.79,-.015,.105,2,.065)
shell('Overhanging roof armor',.82,.84,.72,.74,.105,.21,0,.07)
shell('Command roof',.59,.61,.52,.54,.21,.285,2,.06)
shell('Roof hatch',.25,.26,.23,.24,.285,.315,1,.03)
# The recessed entry and wide vision slit read from the low gameplay angle.
box('Entry surround',(0,-.435,-.175),(.28,.045,.27),1,.025)
box('Armored door',(0,-.463,-.184),(.205,.018,.21),0,.012)
box('Door seam',(0,-.475,-.184),(.012,.008,.18),1)
box('Door lintel',(0,-.467,-.035),(.29,.028,.052),2,.009)
for x in [-.255,.255]:
    box('Vision port',(x,-.386,.055),(.16,.035,.052),1,.009)
    # Broad flank accents rather than fine panel lines.
    box('Flank insignia',(math.copysign(.382,x),.03,-.10),(.026,.31,.12),2,.009)
for y in [-.17,-.08,.01,.10]:
    box('Roof vent',(-.19,y,.293),(.08,.025,.018),1,.003)
box('Antenna foot',(.205,.17,.25),(.11,.11,.08),1,.012)
box('Communications mast',(.205,.17,.375),(.025,.025,.19),1,.004)
box('Signal pennant',(.25,.17,.405),(.115,.024,.085),2,.01)
box('Antenna tip',(.205,.17,.48),(.04,.04,.04),2,.008)
# One clean flat-shaded mesh; retain the three authored material slots.
bpy.ops.object.select_all(action='DESELECT')
for o in parts: o.select_set(True)
bpy.context.view_layer.objects.active=parts[0]; bpy.ops.object.join()
o=bpy.context.object; o.name='CommandBunker'
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.normals_make_consistent(inside=False); bpy.ops.object.mode_set(mode='OBJECT')
# Joining preserves duplicate material slots: collapse them to exactly three.
face_mats=[mats.index(o.data.materials[p.material_index]) for p in o.data.polygons]
o.data.materials.clear()
for m in mats:o.data.materials.append(m)
for p,i in zip(o.data.polygons,face_mats):p.material_index=i; p.use_smooth=False
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(os.path.dirname(__file__),'CommandBunker.blend'))
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,'CommandBunker.fbx'),use_selection=True,object_types={'MESH'},
    axis_forward='-Z',axis_up='Y',apply_unit_scale=True,add_leaf_bones=False,bake_anim=False,mesh_smooth_type='FACE')
print('BUNKER vertices=%s triangles=%s materials=%s bounds=%s' % (len(o.data.vertices),sum(len(p.vertices)-2 for p in o.data.polygons),len(o.data.materials),o.dimensions[:]))
