# ##### BEGIN GPL LICENSE BLOCK #####
#
#  This program is free software; you can redistribute it and/or
#  modify it under the terms of the GNU General Public License
#  as published by the Free Software Foundation; either version 2
#  of the License, or (at your option) any later version.
#
#  This program is distributed in the hope that it will be useful,
#  but WITHOUT ANY WARRANTY; without even the implied warranty of
#  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#  GNU General Public License for more details.
#
#  You should have received a copy of the GNU General Public License
#  along with this program; if not, write to the Free Software Foundation,
#  Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
#
# ##### END GPL LICENSE BLOCK #####

# <pep8 compliant>

# Script copyright (C) Campbell Barton


import os
import time
import math  # math.pi

import bpy
from mathutils import Vector, Matrix

# I guess FBX uses degrees instead of radians (Arystan).
# Call this function just before writing to FBX.
# 180 / math.pi == 57.295779513
def tuple_rad_to_deg(eul):
    return eul[0] * 57.295779513, eul[1] * 57.295779513, eul[2] * 57.295779513

# Used to add the scene name into the filepath without using odd chars
sane_name_mapping_ob = {}
sane_name_mapping_ob_unique = set()
sane_name_mapping_mat = {}
sane_name_mapping_tex = {}
sane_name_mapping_take = {}
sane_name_mapping_group = {}

# Make sure reserved names are not used
sane_name_mapping_ob['Scene'] = 'Scene_'
sane_name_mapping_ob_unique.add('Scene_')


def increment_string(t):
    name = t
    num = ''
    while name and name[-1].isdigit():
        num = name[-1] + num
        name = name[:-1]
    if num:
        return '%s%d' % (name, int(num) + 1)
    else:
        return name + '_0'


# todo - Disallow the name 'Scene' - it will bugger things up.
def sane_name(data, dct, unique_set=None):
    #if not data: return None

    if type(data) == tuple:  # materials are paired up with images
        data, other = data
        use_other = True
    else:
        other = None
        use_other = False

    name = data.name if data else None
    orig_name = name

    if other:
        orig_name_other = other.name
        name = '%s #%s' % (name, orig_name_other)
    else:
        orig_name_other = None

    # dont cache, only ever call once for each data type now,
    # so as to avoid namespace collision between types - like with objects <-> bones
    #try:		return dct[name]
    #except:		pass

    if not name:
        name = 'unnamed'  # blank string, ASKING FOR TROUBLE!
    else:

        name = bpy.path.clean_name(name)  # use our own

    name_unique = dct.values() if unique_set is None else unique_set

    while name in name_unique:
        name = increment_string(name)

    if use_other:  # even if other is None - orig_name_other will be a string or None
        dct[orig_name, orig_name_other] = name
    else:
        dct[orig_name] = name

    if unique_set is not None:
        unique_set.add(name)

    return name


def sane_obname(data):
    return sane_name(data, sane_name_mapping_ob, sane_name_mapping_ob_unique)


def sane_matname(data):
    return sane_name(data, sane_name_mapping_mat)


def sane_texname(data):
    return sane_name(data, sane_name_mapping_tex)


def sane_takename(data):
    return sane_name(data, sane_name_mapping_take)


def sane_groupname(data):
    return sane_name(data, sane_name_mapping_group)


def mat4x4str(mat):
    # blender matrix is row major, fbx is col major so transpose on write
    return ("%.15f,%.15f,%.15f,%.15f,"
            "%.15f,%.15f,%.15f,%.15f,"
            "%.15f,%.15f,%.15f,%.15f,"
            "%.15f,%.15f,%.15f,%.15f" %
            tuple([f for v in mat.transposed() for f in v]))


def action_bone_names(obj, action):
    from bpy.types import PoseBone

    names = set()
    path_resolve = obj.path_resolve

    for fcu in action.fcurves:
        try:
            prop = path_resolve(fcu.data_path, False)
        except:
            prop = None

        if prop is not None:
            data = prop.data
            if isinstance(data, PoseBone):
                names.add(data.name)

    return names


# ob must be OB_MESH
def BPyMesh_meshWeight2List(ob, me):
    """ Takes a mesh and return its group names and a list of lists, one list per vertex.
    aligning the each vert list with the group names, each list contains float value for the weight.
    These 2 lists can be modified and then used with list2MeshWeight to apply the changes.
    """

    # Clear the vert group.
    groupNames = [g.name for g in ob.vertex_groups]
    len_groupNames = len(groupNames)

    if not len_groupNames:
        # no verts? return a vert aligned empty list
        return [[] for i in range(len(me.vertices))], []
    else:
        vWeightList = [[0.0] * len_groupNames for i in range(len(me.vertices))]

    for i, v in enumerate(me.vertices):
        for g in v.groups:
            # possible weights are out of range
            index = g.group
            if index < len_groupNames:
                vWeightList[i][index] = g.weight

    return groupNames, vWeightList


def meshNormalizedWeights(ob, me):
    groupNames, vWeightList = BPyMesh_meshWeight2List(ob, me)

    if not groupNames:
        return [], []

    for i, vWeights in enumerate(vWeightList):
        tot = 0.0
        for w in vWeights:
            tot += w

        if tot:
            for j, w in enumerate(vWeights):
                vWeights[j] = w / tot

    return groupNames, vWeightList

header_comment = \
'''; FBX 6.1.0 project file
; Created by Blender FBX Exporter
; for support mail: ideasman42@gmail.com
; ----------------------------------------------------

'''


# This func can be called with just the filepath
def save_single(operator, scene, filepath="",
        global_matrix=None,
        context_objects=None,
        object_types={'EMPTY', 'CAMERA', 'LAMP', 'ARMATURE', 'MESH'},
        use_mesh_modifiers=True,
        mesh_smooth_type='FACE',
        use_armature_deform_only=False,
        use_anim=True,
        use_anim_optimize=True,
        anim_optimize_precision=6,
        use_anim_action_all=False,
        use_metadata=True,
        path_mode='AUTO',
        use_mesh_edges=True,
        use_rotate_workaround=False,
        use_default_take=True,
    ):

    import bpy_extras.io_utils

    # Only used for camera and lamp rotations
    mtx_x90 = Matrix.Rotation(math.pi / 2.0, 3, 'X')
    # Used for mesh and armature rotations
    mtx4_z90 = Matrix.Rotation(math.pi / 2.0, 4, 'Z')
    # Rotation does not work for XNA animations.  I do not know why but they end up a mess! (JCB)
    if use_rotate_workaround:
        # Set rotation to Matrix Identity for XNA (JCB)
        mtx4_z90.identity()

    if global_matrix is None:
        global_matrix = Matrix()
        global_scale = 1.0
    else:
        global_scale = global_matrix.median_scale

    # Use this for working out paths relative to the export location
    base_src = os.path.dirname(bpy.data.filepath)
    base_dst = os.path.dirname(filepath)

    # collect images to copy
    copy_set = set()

    # ----------------------------------------------
    # storage classes
    class my_bone_class(object):
        __slots__ = ("blenName",
                     "blenBone",
                     "blenMeshes",
                     "restMatrix",
                     "parent",
                     "blenName",
                     "fbxName",
                     "fbxArm",
                     "__pose_bone",
                     "__anim_poselist")

        def __init__(self, blenBone, fbxArm):

            # This is so 2 armatures dont have naming conflicts since FBX bones use object namespace
            self.fbxName = sane_obname(blenBone)

            self.blenName = blenBone.name
            self.blenBone = blenBone
            self.blenMeshes = {}  # fbxMeshObName : mesh
            self.fbxArm = fbxArm
            self.restMatrix = blenBone.matrix_local

            # not used yet
            #~ self.restMatrixInv = self.restMatrix.inverted()
            #~ self.restMatrixLocal = None # set later, need parent matrix

            self.parent = None

            # not public
            pose = fbxArm.blenObject.pose
            self.__pose_bone = pose.bones[self.blenName]

            # store a list if matrices here, (poseMatrix, head, tail)
            # {frame:posematrix, frame:posematrix, ...}
            self.__anim_poselist = {}

        '''
        def calcRestMatrixLocal(self):
            if self.parent:
                self.restMatrixLocal = self.restMatrix * self.parent.restMatrix.inverted()
            else:
                self.restMatrixLocal = self.restMatrix.copy()
        '''
        def setPoseFrame(self, f):
            # cache pose info here, frame must be set beforehand

            # Didnt end up needing head or tail, if we do - here it is.
            '''
            self.__anim_poselist[f] = (\
                self.__pose_bone.poseMatrix.copy(),\
                self.__pose_bone.head.copy(),\
                self.__pose_bone.tail.copy() )
            '''

            self.__anim_poselist[f] = self.__pose_bone.matrix.copy()

        def getPoseBone(self):
            return self.__pose_bone

        # get pose from frame.
        def getPoseMatrix(self, f):  # ----------------------------------------------
            return self.__anim_poselist[f]
        '''
        def getPoseHead(self, f):
            #return self.__pose_bone.head.copy()
            return self.__anim_poselist[f][1].copy()
        def getPoseTail(self, f):
            #return self.__pose_bone.tail.copy()
            return self.__anim_poselist[f][2].copy()
        '''
        # end

        def getAnimParRelMatrix(self, frame):
            #arm_mat = self.fbxArm.matrixWorld
            #arm_mat = self.fbxArm.parRelMatrix()
            if not self.parent:
                #return mtx4_z90 * (self.getPoseMatrix(frame) * arm_mat) # dont apply arm matrix anymore
                return self.getPoseMatrix(frame) * mtx4_z90
            else:
                #return (mtx4_z90 * ((self.getPoseMatrix(frame) * arm_mat)))  *  (mtx4_z90 * (self.parent.getPoseMatrix(frame) * arm_mat)).inverted()
                return (self.parent.getPoseMatrix(frame) * mtx4_z90).inverted() * ((self.getPoseMatrix(frame)) * mtx4_z90)

        # we need thes because cameras and lights modified rotations
        def getAnimParRelMatrixRot(self, frame):
            return self.getAnimParRelMatrix(frame)

        def flushAnimData(self):
            self.__anim_poselist.clear()

    class my_object_generic(object):
        __slots__ = ("fbxName",
                     "blenObject",
                     "blenData",
                     "origData",
                     "blenTextures",
                     "blenMaterials",
                     "blenMaterialList",
                     "blenAction",
                     "blenActionList",
                     "fbxGroupNames",
                     "fbxParent",
                     "fbxBoneParent",
                     "fbxBones",
                     "fbxArm",
                     "matrixWorld",
                     "__anim_poselist",
                     )

        # Other settings can be applied for each type - mesh, armature etc.
        def __init__(self, ob, matrixWorld=None):
            self.fbxName = sane_obname(ob)
            self.blenObject = ob
            self.fbxGroupNames = []
            self.fbxParent = None  # set later on IF the parent is in the selection.
            self.fbxArm = None
            if matrixWorld:
                self.matrixWorld = global_matrix * matrixWorld
            else:
                self.matrixWorld = global_matrix * ob.matrix_world

            self.__anim_poselist = {}  # we should only access this

        def parRelMatrix(self):
            if self.fbxParent:
                return self.fbxParent.matrixWorld.inverted() * self.matrixWorld
            else:
                return self.matrixWorld

        def setPoseFrame(self, f, fake=False):
            if fake:
                self.__anim_poselist[f] = self.matrixWorld * global_matrix.inverted()
            else:
                self.__anim_poselist[f] = self.blenObject.matrix_world.copy()

        def getAnimParRelMatrix(self, frame):
            if self.fbxParent:
                #return (self.__anim_poselist[frame] * self.fbxParent.__anim_poselist[frame].inverted() ) * global_matrix
                return (global_matrix * self.fbxParent.__anim_poselist[frame]).inverted() * (global_matrix * self.__anim_poselist[frame])
            else:
                return global_matrix * self.__anim_poselist[frame]

        def getAnimParRelMatrixRot(self, frame):
            obj_type = self.blenObject.type
            if self.fbxParent:
                matrix_rot = ((global_matrix * self.fbxParent.__anim_poselist[frame]).inverted() * (global_matrix * self.__anim_poselist[frame])).to_3x3()
            else:
                matrix_rot = (global_matrix * self.__anim_poselist[frame]).to_3x3()

            # Lamps need to be rotated
            if obj_type == 'LAMP':
                matrix_rot = matrix_rot * mtx_x90
            elif obj_type == 'CAMERA':
                y = matrix_rot * Vector((0.0, 1.0, 0.0))
                matrix_rot = Matrix.Rotation(math.pi / 2.0, 3, y) * matrix_rot

            return matrix_rot

    # ----------------------------------------------

    print('\nFBX export starting... %r' % filepath)
    start_time = time.clock()
    try:
        file = open(filepath, "w", encoding="utf8", newline="\n")
    except:
        import traceback
        traceback.print_exc()
        operator.report({'ERROR'}, "Couldn't open file %r" % filepath)
        return {'CANCELLED'}

    # convenience
    fw = file.write

    # scene = context.scene  # now passed as an arg instead of context
    world = scene.world

    # ---------------------------- Write the header first
    fw(header_comment)
    if use_metadata:
        curtime = time.localtime()[0:6]
    else:
        curtime = (0, 0, 0, 0, 0, 0)
    #
    fw(
'''FBXHeaderExtension:  {
	FBXHeaderVersion: 1003
	FBXVersion: 6100
	CreationTimeStamp:  {
		Version: 1000
		Year: %.4i
		Month: %.2i
		Day: %.2i
		Hour: %.2i
		Minute: %.2i
		Second: %.2i
		Millisecond: 0
	}
	Creator: "FBX SDK/FBX Plugins build 20070228"
	OtherFlags:  {
		FlagPLE: 0
	}
}''' % (curtime))

    fw('\nCreationTime: "%.4i-%.2i-%.2i %.2i:%.2i:%.2i:000"' % curtime)
    fw('\nCreator: "Blender version %s"' % bpy.app.version_string)

    pose_items = []  # list of (fbxName, matrix) to write pose data for, easier to collect along the way

    # --------------- funcs for exporting
    def object_tx(ob, loc, matrix, matrix_mod=None):
        """
        Matrix mod is so armature objects can modify their bone matrices
        """
        if isinstance(ob, bpy.types.Bone):

            # we know we have a matrix
            # matrix = mtx4_z90 * (ob.matrix['ARMATURESPACE'] * matrix_mod)
            matrix = ob.matrix_local * mtx4_z90  # dont apply armature matrix anymore

            parent = ob.parent
            if parent:
                #par_matrix = mtx4_z90 * (parent.matrix['ARMATURESPACE'] * matrix_mod)
                par_matrix = parent.matrix_local * mtx4_z90  # dont apply armature matrix anymore
                matrix = par_matrix.inverted() * matrix

            loc, rot, scale = matrix.decompose()
            matrix_rot = rot.to_matrix()

            loc = tuple(loc)
            rot = tuple(rot.to_euler())  # quat -> euler
            scale = tuple(scale)

            # Essential for XNA to use the original matrix not rotated nor scaled (JCB)
            if use_rotate_workaround:
                matrix = ob.matrix_local

        else:
            # This is bad because we need the parent relative matrix from the fbx parent (if we have one), dont use anymore
            #if ob and not matrix: matrix = ob.matrix_world * global_matrix
            if ob and not matrix:
                raise Exception("error: this should never happen!")

            matrix_rot = matrix
            #if matrix:
            #    matrix = matrix_scale * matrix

            if matrix:
                loc, rot, scale = matrix.decompose()
                matrix_rot = rot.to_matrix()

                # Lamps need to be rotated
                if ob and ob.type == 'LAMP':
                    matrix_rot = matrix_rot * mtx_x90
                elif ob and ob.type == 'CAMERA':
                    y = matrix_rot * Vector((0.0, 1.0, 0.0))
                    matrix_rot = Matrix.Rotation(math.pi / 2.0, 3, y) * matrix_rot
                # else do nothing.

                loc = tuple(loc)
                rot = tuple(matrix_rot.to_euler())
                scale = tuple(scale)
            else:
                if not loc:
                    loc = 0.0, 0.0, 0.0
                scale = 1.0, 1.0, 1.0
                rot = 0.0, 0.0, 0.0

        return loc, rot, scale, matrix, matrix_rot

    def write_object_tx(ob, loc, matrix, matrix_mod=None):
        """
        We have loc to set the location if non blender objects that have a location

        matrix_mod is only used for bones at the moment
        """
        loc, rot, scale, matrix, matrix_rot = object_tx(ob, loc, matrix, matrix_mod)

        fw('\n\t\t\tProperty: "Lcl Translation", "Lcl Translation", "A+",%.15f,%.15f,%.15f' % loc)
        fw('\n\t\t\tProperty: "Lcl Rotation", "Lcl Rotation", "A+",%.15f,%.15f,%.15f' % tuple_rad_to_deg(rot))
        fw('\n\t\t\tProperty: "Lcl Scaling", "Lcl Scaling", "A+",%.15f,%.15f,%.15f' % scale)
        return loc, rot, scale, matrix, matrix_rot

    def get_constraints(ob=None):
        # Set variables to their defaults.
        constraint_values = {"loc_min": (0.0, 0.0, 0.0),
                             "loc_max": (0.0, 0.0, 0.0),
                             "loc_limit": (0.0, 0.0, 0.0, 0.0, 0.0, 0.0),
                             "rot_min": (0.0, 0.0, 0.0),
                             "rot_max": (0.0, 0.0, 0.0),
                             "rot_limit": (0.0, 0.0, 0.0),
                             "sca_min": (1.0, 1.0, 1.0),
                             "sca_max": (1.0, 1.0, 1.0),
                             "sca_limit": (0.0, 0.0, 0.0, 0.0, 0.0, 0.0),
                            }

        # Iterate through the list of constraints for this object to get the information in a format which is compatible with the FBX format.
        if ob is not None:
            for constraint in ob.constraints:
                if constraint.type == 'LIMIT_LOCATION':
                    constraint_values["loc_min"] = constraint.min_x, constraint.min_y, constraint.min_z
                    constraint_values["loc_max"] = constraint.max_x, constraint.max_y, constraint.max_z
                    constraint_values["loc_limit"] = constraint.use_min_x, constraint.use_min_y, constraint.use_min_z, constraint.use_max_x, constraint.use_max_y, constraint.use_max_z
                elif constraint.type == 'LIMIT_ROTATION':
                    constraint_values["rot_min"] = math.degrees(constraint.min_x), math.degrees(constraint.min_y), math.degrees(constraint.min_z)
                    constraint_values["rot_max"] = math.degrees(constraint.max_x), math.degrees(constraint.max_y), math.degrees(constraint.max_z)
                    constraint_values["rot_limit"] = constraint.use_limit_x, constraint.use_limit_y, constraint.use_limit_z
                elif constraint.type == 'LIMIT_SCALE':
                    constraint_values["sca_min"] = constraint.min_x, constraint.min_y, constraint.min_z
                    constraint_values["sca_max"] = constraint.max_x, constraint.max_y, constraint.max_z
                    constraint_values["sca_limit"] = constraint.use_min_x, constraint.use_min_y, constraint.use_min_z, constraint.use_max_x, constraint.use_max_y, constraint.use_max_z

        # in case bad values are assigned.
        assert(len(constraint_values) == 9)

        return constraint_values

    def write_object_props(ob=None, loc=None, matrix=None, matrix_mod=None, pose_bone=None):
        # Check if a pose exists for this object and set the constraint soruce accordingly. (Poses only exsit if the object is a bone.)
        if pose_bone:
            constraints = get_constraints(pose_bone)
        else:
            constraints = get_constraints(ob)

        # if the type is 0 its an empty otherwise its a mesh
        # only difference at the moment is one has a color
        fw('''
		Properties60:  {
			Property: "QuaternionInterpolate", "bool", "",0
			Property: "Visibility", "Visibility", "A+",1''')

        loc, rot, scale, matrix, matrix_rot = write_object_tx(ob, loc, matrix, matrix_mod)

        # Rotation order, note, for FBX files Iv loaded normal order is 1
        # setting to zero.
        # eEULER_XYZ = 0
        # eEULER_XZY
        # eEULER_YZX
        # eEULER_YXZ
        # eEULER_ZXY
        # eEULER_ZYX

        fw('\n\t\t\tProperty: "RotationOffset", "Vector3D", "",0,0,0'
           '\n\t\t\tProperty: "RotationPivot", "Vector3D", "",0,0,0'
           '\n\t\t\tProperty: "ScalingOffset", "Vector3D", "",0,0,0'
           '\n\t\t\tProperty: "ScalingPivot", "Vector3D", "",0,0,0'
           '\n\t\t\tProperty: "TranslationActive", "bool", "",0'
           )

        fw('\n\t\t\tProperty: "TranslationMin", "Vector3D", "",%.15g,%.15g,%.15g' % constraints["loc_min"])
        fw('\n\t\t\tProperty: "TranslationMax", "Vector3D", "",%.15g,%.15g,%.15g' % constraints["loc_max"])
        fw('\n\t\t\tProperty: "TranslationMinX", "bool", "",%d' % constraints["loc_limit"][0])
        fw('\n\t\t\tProperty: "TranslationMinY", "bool", "",%d' % constraints["loc_limit"][1])
        fw('\n\t\t\tProperty: "TranslationMinZ", "bool", "",%d' % constraints["loc_limit"][2])
        fw('\n\t\t\tProperty: "TranslationMaxX", "bool", "",%d' % constraints["loc_limit"][3])
        fw('\n\t\t\tProperty: "TranslationMaxY", "bool", "",%d' % constraints["loc_limit"][4])
        fw('\n\t\t\tProperty: "TranslationMaxZ", "bool", "",%d' % constraints["loc_limit"][5])

        fw('\n\t\t\tProperty: "RotationOrder", "enum", "",0'
           '\n\t\t\tProperty: "RotationSpaceForLimitOnly", "bool", "",0'
           '\n\t\t\tProperty: "AxisLen", "double", "",10'
           '\n\t\t\tProperty: "PreRotation", "Vector3D", "",0,0,0'
           '\n\t\t\tProperty: "PostRotation", "Vector3D", "",0,0,0'
           '\n\t\t\tProperty: "RotationActive", "bool", "",0'
           )

        fw('\n\t\t\tProperty: "RotationMin", "Vector3D", "",%.15g,%.15g,%.15g' % constraints["rot_min"])
        fw('\n\t\t\tProperty: "RotationMax", "Vector3D", "",%.15g,%.15g,%.15g' % constraints["rot_max"])
        fw('\n\t\t\tProperty: "RotationMinX", "bool", "",%d' % constraints["rot_limit"][0])
        fw('\n\t\t\tProperty: "RotationMinY", "bool", "",%d' % constraints["rot_limit"][1])
        fw('\n\t\t\tProperty: "RotationMinZ", "bool", "",%d' % constraints["rot_limit"][2])
        fw('\n\t\t\tProperty: "RotationMaxX", "bool", "",%d' % constraints["rot_limit"][0])
        fw('\n\t\t\tProperty: "RotationMaxY", "bool", "",%d' % constraints["rot_limit"][1])
        fw('\n\t\t\tProperty: "RotationMaxZ", "bool", "",%d' % constraints["rot_limit"][2])

        fw('\n\t\t\tProperty: "RotationStiffnessX", "double", "",0'
           '\n\t\t\tProperty: "RotationStiffnessY", "double", "",0'
           '\n\t\t\tProperty: "RotationStiffnessZ", "double", "",0'
           '\n\t\t\tProperty: "MinDampRangeX", "double", "",0'
           '\n\t\t\tProperty: "MinDampRangeY", "double", "",0'
           '\n\t\t\tProperty: "MinDampRangeZ", "double", "",0'
           '\n\t\t\tProperty: "MaxDampRangeX", "double", "",0'
           '\n\t\t\tProperty: "MaxDampRangeY", "double", "",0'
           '\n\t\t\tProperty: "MaxDampRangeZ", "double", "",0'
           '\n\t\t\tProperty: "MinDampStrengthX", "double", "",0'
           '\n\t\t\tProperty: "MinDampStrengthY", "double", "",0'
           '\n\t\t\tProperty: "MinDampStrengthZ", "double", "",0'
           '\n\t\t\tProperty: "MaxDampStrengthX", "double", "",0'
           '\n\t\t\tProperty: "MaxDampStrengthY", "double", "",0'
           '\n\t\t\tProperty: "MaxDampStrengthZ", "double", "",0'
           '\n\t\t\tProperty: "PreferedAngleX", "double", "",0'
           '\n\t\t\tProperty: "PreferedAngleY", "double", "",0'
           '\n\t\t\tProperty: "PreferedAngleZ", "double", "",0'
           '\n\t\t\tProperty: "InheritType", "enum", "",0'
           '\n\t\t\tProperty: "ScalingActive", "bool", "",0'
           )

        fw('\n\t\t\tProperty: "ScalingMin", "Vector3D", "",%.15g,%.15g,%.15g' % constraints["sca_min"])
        fw('\n\t\t\tProperty: "ScalingMax", "Vector3D", "",%.15g,%.15g,%.15g' % constraints["sca_max"])
        fw('\n\t\t\tProperty: "ScalingMinX", "bool", "",%d' % constraints["sca_limit"][0])
        fw('\n\t\t\tProperty: "ScalingMinY", "bool", "",%d' % constraints["sca_limit"][1])
        fw('\n\t\t\tProperty: "ScalingMinZ", "bool", "",%d' % constraints["sca_limit"][2])
        fw('\n\t\t\tProperty: "ScalingMaxX", "bool", "",%d' % constraints["sca_limit"][3])
        fw('\n\t\t\tProperty: "ScalingMaxY", "bool", "",%d' % constraints["sca_limit"][4])
        fw('\n\t\t\tProperty: "ScalingMaxZ", "bool", "",%d' % constraints["sca_limit"][5])

        fw('\n\t\t\tProperty: "GeometricTranslation", "Vector3D", "",0,0,0'
           '\n\t\t\tProperty: "GeometricRotation", "Vector3D", "",0,0,0'
           '\n\t\t\tProperty: "GeometricScaling", "Vector3D", "",1,1,1'
           '\n\t\t\tProperty: "LookAtProperty", "object", ""'
           '\n\t\t\tProperty: "UpVectorProperty", "object", ""'
           '\n\t\t\tProperty: "Show", "bool", "",1'
           '\n\t\t\tProperty: "NegativePercentShapeSupport", "bool", "",1'
           '\n\t\t\tProperty: "DefaultAttributeIndex", "int", "",0'
           )

        if ob and not isinstance(ob, bpy.types.Bone):
            # Only mesh objects have color
            fw('\n\t\t\tProperty: "Color", "Color", "A",0.8,0.8,0.8'
               '\n\t\t\tProperty: "Size", "double", "",100'
               '\n\t\t\tProperty: "Look", "enum", "",1'
               )

        return loc, rot, scale, matrix, matrix_rot

    # -------------------------------------------- Armatures
    #def write_bone(bone, name, matrix_mod):
    def write_bone(my_bone):
        fw('\n\tModel: "Model::%s", "Limb" {' % my_bone.fbxName)
        fw('\n\t\tVersion: 232')

        #~ poseMatrix = write_object_props(my_bone.blenBone, None, None, my_bone.fbxArm.parRelMatrix())[3]
        poseMatrix = write_object_props(my_bone.blenBone, pose_bone=my_bone.getPoseBone())[3]  # dont apply bone matrices anymore

        # Use the same calculation as in write_sub_deformer_skin to compute the global
        # transform of the bone for the bind pose.
        global_matrix_bone = (my_bone.fbxArm.matrixWorld * my_bone.restMatrix) * mtx4_z90
        pose_items.append((my_bone.fbxName, global_matrix_bone))

        # fw('\n\t\t\tProperty: "Size", "double", "",%.6f' % ((my_bone.blenData.head['ARMATURESPACE'] - my_bone.blenData.tail['ARMATURESPACE']) * my_bone.fbxArm.parRelMatrix()).length)
        fw('\n\t\t\tProperty: "Size", "double", "",1')

        #((my_bone.blenData.head['ARMATURESPACE'] * my_bone.fbxArm.matrixWorld) - (my_bone.blenData.tail['ARMATURESPACE'] * my_bone.fbxArm.parRelMatrix())).length)

        """
        fw('\n\t\t\tProperty: "LimbLength", "double", "",%.6f' %\
            ((my_bone.blenBone.head['ARMATURESPACE'] - my_bone.blenBone.tail['ARMATURESPACE']) * my_bone.fbxArm.parRelMatrix()).length)
        """

        fw('\n\t\t\tProperty: "LimbLength", "double", "",%.6f' %
           (my_bone.blenBone.head_local - my_bone.blenBone.tail_local).length)

        #fw('\n\t\t\tProperty: "LimbLength", "double", "",1')
        fw('\n\t\t\tProperty: "Color", "ColorRGB", "",0.8,0.8,0.8'
           '\n\t\t\tProperty: "Color", "Color", "A",0.8,0.8,0.8'
           '\n\t\t}'
           '\n\t\tMultiLayer: 0'
           '\n\t\tMultiTake: 1'
           '\n\t\tShading: Y'
           '\n\t\tCulling: "CullingOff"'
           '\n\t\tTypeFlags: "Skeleton"'
           '\n\t}'
           )

    def write_camera_switch():
        fw('''
	Model: "Model::Camera Switcher", "CameraSwitcher" {
		Version: 232''')

        write_object_props()
        fw('''
			Property: "Color", "Color", "A",0.8,0.8,0.8
			Property: "Camera Index", "Integer", "A+",100
		}
		MultiLayer: 0
		MultiTake: 1
		Hidden: "True"
		Shading: W
		Culling: "CullingOff"
		Version: 101
		Name: "Model::Camera Switcher"
		CameraId: 0
		CameraName: 100
		CameraIndexName:
	}''')

    def write_camera_dummy(name, loc, near, far, proj_type, up):
        fw('\n\tModel: "Model::%s", "Camera" {' % name)
        fw('\n\t\tVersion: 232')
        write_object_props(None, loc)

        fw('\n\t\t\tProperty: "Color", "Color", "A",0.8,0.8,0.8'
           '\n\t\t\tProperty: "Roll", "Roll", "A+",0'
           '\n\t\t\tProperty: "FieldOfView", "FieldOfView", "A+",40'
           '\n\t\t\tProperty: "FieldOfViewX", "FieldOfView", "A+",1'
           '\n\t\t\tProperty: "FieldOfViewY", "FieldOfView", "A+",1'
           '\n\t\t\tProperty: "OpticalCenterX", "Real", "A+",0'
           '\n\t\t\tProperty: "OpticalCenterY", "Real", "A+",0'
           '\n\t\t\tProperty: "BackgroundColor", "Color", "A+",0.63,0.63,0.63'
           '\n\t\t\tProperty: "TurnTable", "Real", "A+",0'
           '\n\t\t\tProperty: "DisplayTurnTableIcon", "bool", "",1'
           '\n\t\t\tProperty: "Motion Blur Intensity", "Real", "A+",1'
           '\n\t\t\tProperty: "UseMotionBlur", "bool", "",0'
           '\n\t\t\tProperty: "UseRealTimeMotionBlur", "bool", "",1'
           '\n\t\t\tProperty: "ResolutionMode", "enum", "",0'
           '\n\t\t\tProperty: "ApertureMode", "enum", "",2'
           '\n\t\t\tProperty: "GateFit", "enum", "",0'
           '\n\t\t\tProperty: "FocalLength", "Real", "A+",21.3544940948486'
           '\n\t\t\tProperty: "CameraFormat", "enum", "",0'
           '\n\t\t\tProperty: "AspectW", "double", "",320'
           '\n\t\t\tProperty: "AspectH", "double", "",200'
           '\n\t\t\tProperty: "PixelAspectRatio", "double", "",1'
           '\n\t\t\tProperty: "UseFrameColor", "bool", "",0'
           '\n\t\t\tProperty: "FrameColor", "ColorRGB", "",0.3,0.3,0.3'
           '\n\t\t\tProperty: "ShowName", "bool", "",1'
           '\n\t\t\tProperty: "ShowGrid", "bool", "",1'
           '\n\t\t\tProperty: "ShowOpticalCenter", "bool", "",0'
           '\n\t\t\tProperty: "ShowAzimut", "bool", "",1'
           '\n\t\t\tProperty: "ShowTimeCode", "bool", "",0'
           )

        fw('\n\t\t\tProperty: "NearPlane", "double", "",%.6f' % near)
        fw('\n\t\t\tProperty: "FarPlane", "double", "",%.6f' % far)

        fw('\n\t\t\tProperty: "FilmWidth", "double", "",0.816'
           '\n\t\t\tProperty: "FilmHeight", "double", "",0.612'
           '\n\t\t\tProperty: "FilmAspectRatio", "double", "",1.33333333333333'
           '\n\t\t\tProperty: "FilmSqueezeRatio", "double", "",1'
           '\n\t\t\tProperty: "FilmFormatIndex", "enum", "",4'
           '\n\t\t\tProperty: "ViewFrustum", "bool", "",1'
           '\n\t\t\tProperty: "ViewFrustumNearFarPlane", "bool", "",0'
           '\n\t\t\tProperty: "ViewFrustumBackPlaneMode", "enum", "",2'
           '\n\t\t\tProperty: "BackPlaneDistance", "double", "",100'
           '\n\t\t\tProperty: "BackPlaneDistanceMode", "enum", "",0'
           '\n\t\t\tProperty: "ViewCameraToLookAt", "bool", "",1'
           '\n\t\t\tProperty: "LockMode", "bool", "",0'
           '\n\t\t\tProperty: "LockInterestNavigation", "bool", "",0'
           '\n\t\t\tProperty: "FitImage", "bool", "",0'
           '\n\t\t\tProperty: "Crop", "bool", "",0'
           '\n\t\t\tProperty: "Center", "bool", "",1'
           '\n\t\t\tProperty: "KeepRatio", "bool", "",1'
           '\n\t\t\tProperty: "BackgroundMode", "enum", "",0'
           '\n\t\t\tProperty: "BackgroundAlphaTreshold", "double", "",0.5'
           '\n\t\t\tProperty: "ForegroundTransparent", "bool", "",1'
           '\n\t\t\tProperty: "DisplaySafeArea", "bool", "",0'
           '\n\t\t\tProperty: "SafeAreaDisplayStyle", "enum", "",1'
           '\n\t\t\tProperty: "SafeAreaAspectRatio", "double", "",1.33333333333333'
           '\n\t\t\tProperty: "Use2DMagnifierZoom", "bool", "",0'
           '\n\t\t\tProperty: "2D Magnifier Zoom", "Real", "A+",100'
           '\n\t\t\tProperty: "2D Magnifier X", "Real", "A+",50'
           '\n\t\t\tProperty: "2D Magnifier Y", "Real", "A+",50'
           )

        fw('\n\t\t\tProperty: "CameraProjectionType", "enum", "",%i' % proj_type)

        fw('\n\t\t\tProperty: "UseRealTimeDOFAndAA", "bool", "",0'
           '\n\t\t\tProperty: "UseDepthOfField", "bool", "",0'
           '\n\t\t\tProperty: "FocusSource", "enum", "",0'
           '\n\t\t\tProperty: "FocusAngle", "double", "",3.5'
           '\n\t\t\tProperty: "FocusDistance", "double", "",200'
           '\n\t\t\tProperty: "UseAntialiasing", "bool", "",0'
           '\n\t\t\tProperty: "AntialiasingIntensity", "double", "",0.77777'
           '\n\t\t\tProperty: "UseAccumulationBuffer", "bool", "",0'
           '\n\t\t\tProperty: "FrameSamplingCount", "int", "",7'
           '\n\t\t}'
           '\n\t\tMultiLayer: 0'
           '\n\t\tMultiTake: 0'
           '\n\t\tHidden: "True"'
           '\n\t\tShading: Y'
           '\n\t\tCulling: "CullingOff"'
           '\n\t\tTypeFlags: "Camera"'
           '\n\t\tGeometryVersion: 124'
           )

        fw('\n\t\tPosition: %.6f,%.6f,%.6f' % loc)
        fw('\n\t\tUp: %i,%i,%i' % up)

        fw('\n\t\tLookAt: 0,0,0'
           '\n\t\tShowInfoOnMoving: 1'
           '\n\t\tShowAudio: 0'
           '\n\t\tAudioColor: 0,1,0'
           '\n\t\tCameraOrthoZoom: 1'
           '\n\t}'
           )

    def write_camera_default():
        # This sucks but to match FBX converter its easier to
        # write the cameras though they are not needed.
        write_camera_dummy('Producer Perspective', (0, 71.3, 287.5), 10, 4000, 0, (0, 1, 0))
        write_camera_dummy('Producer Top', (0, 4000, 0), 1, 30000, 1, (0, 0, -1))
        write_camera_dummy('Producer Bottom', (0, -4000, 0), 1, 30000, 1, (0, 0, -1))
        write_camera_dummy('Producer Front', (0, 0, 4000), 1, 30000, 1, (0, 1, 0))
        write_camera_dummy('Producer Back', (0, 0, -4000), 1, 30000, 1, (0, 1, 0))
        write_camera_dummy('Producer Right', (4000, 0, 0), 1, 30000, 1, (0, 1, 0))
        write_camera_dummy('Producer Left', (-4000, 0, 0), 1, 30000, 1, (0, 1, 0))

    def write_camera(my_cam):
        """
        Write a blender camera
        """
        render = scene.render
        width = render.resolution_x
        height = render.resolution_y
        aspect = width / height

        data = my_cam.blenObject.data
        # film width & height from mm to inches
        filmwidth = data.sensor_width * 0.0393700787
        filmheight = data.sensor_height * 0.0393700787
        filmaspect = filmwidth / filmheight
        # film offset
        offsetx = filmwidth * data.shift_x
        offsety = filmaspect * filmheight * data.shift_y

        fw('\n\tModel: "Model::%s", "Camera" {' % my_cam.fbxName)
        fw('\n\t\tVersion: 232')
        loc, rot, scale, matrix, matrix_rot = write_object_props(my_cam.blenObject, None, my_cam.parRelMatrix())

        fw('\n\t\t\tProperty: "Roll", "Roll", "A+",0')
        fw('\n\t\t\tProperty: "FieldOfView", "FieldOfView", "A+",%.6f' % math.degrees(data.angle_y))

        fw('\n\t\t\tProperty: "FieldOfViewX", "FieldOfView", "A+",1'
           '\n\t\t\tProperty: "FieldOfViewY", "FieldOfView", "A+",1'
           )

        fw('\n\t\t\tProperty: "FocalLength", "Number", "A+",%.6f' % data.lens)
        fw('\n\t\t\tProperty: "FilmOffsetX", "Number", "A+",%.6f' % offsetx)
        fw('\n\t\t\tProperty: "FilmOffsetY", "Number", "A+",%.6f' % offsety)

        fw('\n\t\t\tProperty: "BackgroundColor", "Color", "A+",0,0,0'
           '\n\t\t\tProperty: "TurnTable", "Real", "A+",0'
           '\n\t\t\tProperty: "DisplayTurnTableIcon", "bool", "",1'
           '\n\t\t\tProperty: "Motion Blur Intensity", "Real", "A+",1'
           '\n\t\t\tProperty: "UseMotionBlur", "bool", "",0'
           '\n\t\t\tProperty: "UseRealTimeMotionBlur", "bool", "",1'
           '\n\t\t\tProperty: "ResolutionMode", "enum", "",0'
           '\n\t\t\tProperty: "ApertureMode", "enum", "",3'  # horizontal - Houdini compatible
           '\n\t\t\tProperty: "GateFit", "enum", "",2'
           '\n\t\t\tProperty: "CameraFormat", "enum", "",0'
           )

        fw('\n\t\t\tProperty: "AspectW", "double", "",%i' % width)
        fw('\n\t\t\tProperty: "AspectH", "double", "",%i' % height)

        """Camera aspect ratio modes.
            0 If the ratio mode is eWINDOW_SIZE, both width and height values aren't relevant.
            1 If the ratio mode is eFIXED_RATIO, the height value is set to 1.0 and the width value is relative to the height value.
            2 If the ratio mode is eFIXED_RESOLUTION, both width and height values are in pixels.
            3 If the ratio mode is eFIXED_WIDTH, the width value is in pixels and the height value is relative to the width value.
            4 If the ratio mode is eFIXED_HEIGHT, the height value is in pixels and the width value is relative to the height value.

        Definition at line 234 of file kfbxcamera.h. """

        fw('\n\t\t\tProperty: "PixelAspectRatio", "double", "",1'
           '\n\t\t\tProperty: "UseFrameColor", "bool", "",0'
           '\n\t\t\tProperty: "FrameColor", "ColorRGB", "",0.3,0.3,0.3'
           '\n\t\t\tProperty: "ShowName", "bool", "",1'
           '\n\t\t\tProperty: "ShowGrid", "bool", "",1'
           '\n\t\t\tProperty: "ShowOpticalCenter", "bool", "",0'
           '\n\t\t\tProperty: "ShowAzimut", "bool", "",1'
           '\n\t\t\tProperty: "ShowTimeCode", "bool", "",0'
           )

        fw('\n\t\t\tProperty: "NearPlane", "double", "",%.6f' % (data.clip_start * global_scale))
        fw('\n\t\t\tProperty: "FarPlane", "double", "",%.6f' % (data.clip_end * global_scale))

        fw('\n\t\t\tProperty: "FilmWidth", "double", "",%.6f' % filmwidth)
        fw('\n\t\t\tProperty: "FilmHeight", "double", "",%.6f' % filmheight)
        fw('\n\t\t\tProperty: "FilmAspectRatio", "double", "",%.6f' % filmaspect)

        fw('\n\t\t\tProperty: "FilmSqueezeRatio", "double", "",1'
           '\n\t\t\tProperty: "FilmFormatIndex", "enum", "",0'
           '\n\t\t\tProperty: "ViewFrustum", "bool", "",1'
           '\n\t\t\tProperty: "ViewFrustumNearFarPlane", "bool", "",0'
           '\n\t\t\tProperty: "ViewFrustumBackPlaneMode", "enum", "",2'
           '\n\t\t\tProperty: "BackPlaneDistance", "double", "",100'
           '\n\t\t\tProperty: "BackPlaneDistanceMode", "enum", "",0'
           '\n\t\t\tProperty: "ViewCameraToLookAt", "bool", "",1'
           '\n\t\t\tProperty: "LockMode", "bool", "",0'
           '\n\t\t\tProperty: "LockInterestNavigation", "bool", "",0'
           '\n\t\t\tProperty: "FitImage", "bool", "",0'
           '\n\t\t\tProperty: "Crop", "bool", "",0'
           '\n\t\t\tProperty: "Center", "bool", "",1'
           '\n\t\t\tProperty: "KeepRatio", "bool", "",1'
           '\n\t\t\tProperty: "BackgroundMode", "enum", "",0'
           '\n\t\t\tProperty: "BackgroundAlphaTreshold", "double", "",0.5'
           '\n\t\t\tProperty: "ForegroundTransparent", "bool", "",1'
           '\n\t\t\tProperty: "DisplaySafeArea", "bool", "",0'
           '\n\t\t\tProperty: "SafeAreaDisplayStyle", "enum", "",1'
           )

        fw('\n\t\t\tProperty: "SafeAreaAspectRatio", "double", "",%.6f' % aspect)

        fw('\n\t\t\tProperty: "Use2DMagnifierZoom", "bool", "",0'
           '\n\t\t\tProperty: "2D Magnifier Zoom", "Real", "A+",100'
           '\n\t\t\tProperty: "2D Magnifier X", "Real", "A+",50'
           '\n\t\t\tProperty: "2D Magnifier Y", "Real", "A+",50'
           '\n\t\t\tProperty: "CameraProjectionType", "enum", "",0'
           '\n\t\t\tProperty: "UseRealTimeDOFAndAA", "bool", "",0'
           '\n\t\t\tProperty: "UseDepthOfField", "bool", "",0'
           '\n\t\t\tProperty: "FocusSource", "enum", "",0'
           '\n\t\t\tProperty: "FocusAngle", "double", "",3.5'
           '\n\t\t\tProperty: "FocusDistance", "double", "",200'
           '\n\t\t\tProperty: "UseAntialiasing", "bool", "",0'
           '\n\t\t\tProperty: "AntialiasingIntensity", "double", "",0.77777'
           '\n\t\t\tProperty: "UseAccumulationBuffer", "bool", "",0'
           '\n\t\t\tProperty: "FrameSamplingCount", "int", "",7'
           )

        fw('\n\t\t}')

        fw('\n\t\tMultiLayer: 0'
           '\n\t\tMultiTake: 0'
           '\n\t\tShading: Y'
           '\n\t\tCulling: "CullingOff"'
           '\n\t\tTypeFlags: "Camera"'
           '\n\t\tGeometryVersion: 124'
           )

        fw('\n\t\tPosition: %.6f,%.6f,%.6f' % loc)
        fw('\n\t\tUp: %.6f,%.6f,%.6f' % (matrix_rot * Vector((0.0, 1.0, 0.0)))[:])
        fw('\n\t\tLookAt: %.6f,%.6f,%.6f' % (matrix_rot * Vector((0.0, 0.0, -1.0)))[:])

        #fw('\n\t\tUp: 0,0,0' )
        #fw('\n\t\tLookAt: 0,0,0' )

        fw('\n\t\tShowInfoOnMoving: 1')
        fw('\n\t\tShowAudio: 0')
        fw('\n\t\tAudioColor: 0,1,0')
        fw('\n\t\tCameraOrthoZoom: 1')
        fw('\n\t}')

    def write_light(my_light):
        light = my_light.blenObject.data
        fw('\n\tModel: "Model::%s", "Light" {' % my_light.fbxName)
        fw('\n\t\tVersion: 232')

        write_object_props(my_light.blenObject, None, my_light.parRelMatrix())

        # Why are these values here twice?????? - oh well, follow the holy sdk's output

        # Blender light types match FBX's, funny coincidence, we just need to
        # be sure that all unsupported types are made into a point light
        #ePOINT,
        #eDIRECTIONAL
        #eSPOT
        light_type_items = {'POINT': 0, 'SUN': 1, 'SPOT': 2, 'HEMI': 3, 'AREA': 4}
        light_type = light_type_items[light.type]

        if light_type > 2:
            light_type = 1  # hemi and area lights become directional

        if light.type == 'HEMI':
            do_light = not (light.use_diffuse or light.use_specular)
            do_shadow = False
        else:
            do_light = not (light.use_only_shadow or (not light.use_diffuse and not light.use_specular))
            do_shadow = (light.shadow_method in {'RAY_SHADOW', 'BUFFER_SHADOW'})

        # scale = abs(global_matrix.to_scale()[0])  # scale is always uniform in this case  #  UNUSED

        fw('\n\t\t\tProperty: "LightType", "enum", "",%i' % light_type)
        fw('\n\t\t\tProperty: "CastLightOnObject", "bool", "",1')
        fw('\n\t\t\tProperty: "DrawVolumetricLight", "bool", "",1')
        fw('\n\t\t\tProperty: "DrawGroundProjection", "bool", "",1')
        fw('\n\t\t\tProperty: "DrawFrontFacingVolumetricLight", "bool", "",0')
        fw('\n\t\t\tProperty: "GoboProperty", "object", ""')
        fw('\n\t\t\tProperty: "Color", "Color", "A+",1,1,1')
        fw('\n\t\t\tProperty: "Intensity", "Intensity", "A+",%.2f' % (min(light.energy * 100.0, 200.0)))  # clamp below 200
        if light.type == 'SPOT':
            fw('\n\t\t\tProperty: "Cone angle", "Cone angle", "A+",%.2f' % math.degrees(light.spot_size))
        fw('\n\t\t\tProperty: "Fog", "Fog", "A+",50')
        fw('\n\t\t\tProperty: "Color", "Color", "A",%.2f,%.2f,%.2f' % tuple(light.color))

        fw('\n\t\t\tProperty: "Intensity", "Intensity", "A+",%.2f' % (min(light.energy * 100.0, 200.0)))  # clamp below 200

        fw('\n\t\t\tProperty: "Fog", "Fog", "A+",50')
        fw('\n\t\t\tProperty: "LightType", "enum", "",%i' % light_type)
        fw('\n\t\t\tProperty: "CastLightOnObject", "bool", "",%i' % do_light)
        fw('\n\t\t\tProperty: "DrawGroundProjection", "bool", "",1')
        fw('\n\t\t\tProperty: "DrawFrontFacingVolumetricLight", "bool", "",0')
        fw('\n\t\t\tProperty: "DrawVolumetricLight", "bool", "",1')
        fw('\n\t\t\tProperty: "GoboProperty", "object", ""')
        fw('\n\t\t\tProperty: "DecayType", "enum", "",0')
        fw('\n\t\t\tProperty: "DecayStart", "double", "",%.2f' % light.distance)

        fw('\n\t\t\tProperty: "EnableNearAttenuation", "bool", "",0'
           '\n\t\t\tProperty: "NearAttenuationStart", "double", "",0'
           '\n\t\t\tProperty: "NearAttenuationEnd", "double", "",0'
           '\n\t\t\tProperty: "EnableFarAttenuation", "bool", "",0'
           '\n\t\t\tProperty: "FarAttenuationStart", "double", "",0'
           '\n\t\t\tProperty: "FarAttenuationEnd", "double", "",0'
           )

        fw('\n\t\t\tProperty: "CastShadows", "bool", "",%i' % do_shadow)
        fw('\n\t\t\tProperty: "ShadowColor", "ColorRGBA", "",0,0,0,1')
        fw('\n\t\t}')

        fw('\n\t\tMultiLayer: 0'
           '\n\t\tMultiTake: 0'
           '\n\t\tShading: Y'
           '\n\t\tCulling: "CullingOff"'
           '\n\t\tTypeFlags: "Light"'
           '\n\t\tGeometryVersion: 124'
           '\n\t}'
           )

    # matrixOnly is not used at the moment
    def write_null(my_null=None, fbxName=None, fbxType="Null", fbxTypeFlags="Null"):
        # ob can be null
        if not fbxName:
            fbxName = my_null.fbxName

        fw('\n\tModel: "Model::%s", "%s" {' % (fbxName, fbxType))
        fw('\n\t\tVersion: 232')

        if my_null:
            poseMatrix = write_object_props(my_null.blenObject, None, my_null.parRelMatrix())[3]
        else:
            poseMatrix = write_object_props()[3]

        pose_items.append((fbxName, poseMatrix))

        fw('\n\t\t}'
           '\n\t\tMultiLayer: 0'
           '\n\t\tMultiTake: 1'
           '\n\t\tShading: Y'
           '\n\t\tCulling: "CullingOff"'
           )

        fw('\n\t\tTypeFlags: "%s"' % fbxTypeFlags)
        fw('\n\t}')

    # Material Settings
    if world:
        world_amb = world.ambient_color[:]
    else:
        world_amb = 0.0, 0.0, 0.0  # default value

    def write_material(matname, mat):
        fw('\n\tMaterial: "Material::%s", "" {' % matname)

        # Todo, add more material Properties.
        if mat:
            mat_cold = tuple(mat.diffuse_color)
            mat_cols = tuple(mat.specular_color)
            #mat_colm = tuple(mat.mirCol) # we wont use the mirror color
            mat_colamb = world_amb

            mat_dif = mat.diffuse_intensity
            mat_amb = mat.ambient
            mat_hard = (float(mat.specular_hardness) - 1.0) / 5.10
            mat_spec = mat.specular_intensity / 2.0
            mat_alpha = mat.alpha
            mat_emit = mat.emit
            mat_shadeless = mat.use_shadeless
            if mat_shadeless:
                mat_shader = 'Lambert'
            else:
                if mat.diffuse_shader == 'LAMBERT':
                    mat_shader = 'Lambert'
                else:
                    mat_shader = 'Phong'
        else:
            mat_cols = mat_cold = 0.8, 0.8, 0.8
            mat_colamb = 0.0, 0.0, 0.0
            # mat_colm
            mat_dif = 1.0
            mat_amb = 0.5
            mat_hard = 20.0
            mat_spec = 0.2
            mat_alpha = 1.0
            mat_emit = 0.0
            mat_shadeless = False
            mat_shader = 'Phong'

        fw('\n\t\tVersion: 102')
        fw('\n\t\tShadingModel: "%s"' % mat_shader.lower())
        fw('\n\t\tMultiLayer: 0')

        fw('\n\t\tProperties60:  {')
        fw('\n\t\t\tProperty: "ShadingModel", "KString", "", "%s"' % mat_shader)
        fw('\n\t\t\tProperty: "MultiLayer", "bool", "",0')
        fw('\n\t\t\tProperty: "EmissiveColor", "ColorRGB", "",%.4f,%.4f,%.4f' % mat_cold)  # emit and diffuse color are he same in blender
        fw('\n\t\t\tProperty: "EmissiveFactor", "double", "",%.4f' % mat_emit)

        fw('\n\t\t\tProperty: "AmbientColor", "ColorRGB", "",%.4f,%.4f,%.4f' % mat_colamb)
        fw('\n\t\t\tProperty: "AmbientFactor", "double", "",%.4f' % mat_amb)
        fw('\n\t\t\tProperty: "DiffuseColor", "ColorRGB", "",%.4f,%.4f,%.4f' % mat_cold)
        fw('\n\t\t\tProperty: "DiffuseFactor", "double", "",%.4f' % mat_dif)
        fw('\n\t\t\tProperty: "Bump", "Vector3D", "",0,0,0')
        fw('\n\t\t\tProperty: "TransparentColor", "ColorRGB", "",1,1,1')
        fw('\n\t\t\tProperty: "TransparencyFactor", "double", "",%.4f' % (1.0 - mat_alpha))
        if not mat_shadeless:
            fw('\n\t\t\tProperty: "SpecularColor", "ColorRGB", "",%.4f,%.4f,%.4f' % mat_cols)
            fw('\n\t\t\tProperty: "SpecularFactor", "double", "",%.4f' % mat_spec)
            fw('\n\t\t\tProperty: "ShininessExponent", "double", "",80.0')
            fw('\n\t\t\tProperty: "ReflectionColor", "ColorRGB", "",0,0,0')
            fw('\n\t\t\tProperty: "ReflectionFactor", "double", "",1')
        fw('\n\t\t\tProperty: "Emissive", "ColorRGB", "",0,0,0')
        fw('\n\t\t\tProperty: "Ambient", "ColorRGB", "",%.1f,%.1f,%.1f' % mat_colamb)
        fw('\n\t\t\tProperty: "Diffuse", "ColorRGB", "",%.1f,%.1f,%.1f' % mat_cold)
        if not mat_shadeless:
            fw('\n\t\t\tProperty: "Specular", "ColorRGB", "",%.1f,%.1f,%.1f' % mat_cols)
            fw('\n\t\t\tProperty: "Shininess", "double", "",%.1f' % mat_hard)
        fw('\n\t\t\tProperty: "Opacity", "double", "",%.1f' % mat_alpha)
        if not mat_shadeless:
            fw('\n\t\t\tProperty: "Reflectivity", "double", "",0')

        fw('\n\t\t}')
        fw('\n\t}')

    # tex is an Image (Arystan)
    def write_video(texname, tex):
        # Same as texture really!
        fw('\n\tVideo: "Video::%s", "Clip" {' % texname)

        fw('''
		Type: "Clip"
		Properties60:  {
			Property: "FrameRate", "double", "",0
			Property: "LastFrame", "int", "",0
			Property: "Width", "int", "",0
			Property: "Height", "int", "",0''')
        if tex:
            fname_rel = bpy_extras.io_utils.path_reference(tex.filepath, base_src, base_dst, path_mode, "", copy_set, tex.library)
            fname_strip = bpy.path.basename(fname_rel)
        else:
            fname_strip = fname_rel = ""

        fw('\n\t\t\tProperty: "Path", "charptr", "", "%s"' % fname_strip)

        fw('''
			Property: "StartFrame", "int", "",0
			Property: "StopFrame", "int", "",0
			Property: "PlaySpeed", "double", "",1
			Property: "Offset", "KTime", "",0
			Property: "InterlaceMode", "enum", "",0
			Property: "FreeRunning", "bool", "",0
			Property: "Loop", "bool", "",0
			Property: "AccessMode", "enum", "",0
		}
		UseMipMap: 0''')

        fw('\n\t\tFilename: "%s"' % fname_strip)
        fw('\n\t\tRelativeFilename: "%s"' % fname_rel)  # make relative
        fw('\n\t}')

    def write_texture(texname, tex, num):
        # if tex is None then this is a dummy tex
        fw('\n\tTexture: "Texture::%s", "TextureVideoClip" {' % texname)
        fw('\n\t\tType: "TextureVideoClip"')
        fw('\n\t\tVersion: 202')
        # TODO, rare case _empty_ exists as a name.
        fw('\n\t\tTextureName: "Texture::%s"' % texname)

        fw('''
		Properties60:  {
			Property: "Translation", "Vector", "A+",0,0,0
			Property: "Rotation", "Vector", "A+",0,0,0
			Property: "Scaling", "Vector", "A+",1,1,1''')
        fw('\n\t\t\tProperty: "Texture alpha", "Number", "A+",%i' % num)

        # WrapModeU/V 0==rep, 1==clamp, TODO add support
        fw('''
			Property: "TextureTypeUse", "enum", "",0
			Property: "CurrentTextureBlendMode", "enum", "",1
			Property: "UseMaterial", "bool", "",0
			Property: "UseMipMap", "bool", "",0
			Property: "CurrentMappingType", "enum", "",0
			Property: "UVSwap", "bool", "",0''')

        fw('\n\t\t\tProperty: "WrapModeU", "enum", "",%i' % tex.use_clamp_x)
        fw('\n\t\t\tProperty: "WrapModeV", "enum", "",%i' % tex.use_clamp_y)

        fw('''
			Property: "TextureRotationPivot", "Vector3D", "",0,0,0
			Property: "TextureScalingPivot", "Vector3D", "",0,0,0
			Property: "VideoProperty", "object", ""
		}''')

        fw('\n\t\tMedia: "Video::%s"' % texname)

        if tex:
            fname_rel = bpy_extras.io_utils.path_reference(tex.filepath, base_src, base_dst, path_mode, "", copy_set, tex.library)
            fname_strip = bpy.path.basename(fname_rel)
        else:
            fname_strip = fname_rel = ""

        fw('\n\t\tFileName: "%s"' % fname_strip)
        fw('\n\t\tRelativeFilename: "%s"' % fname_rel)  # need some make relative command

        fw('''
		ModelUVTranslation: 0,0
		ModelUVScaling: 1,1
		Texture_Alpha_Source: "None"
		Cropping: 0,0,0,0
	}''')

    def write_deformer_skin(obname):
        """
        Each mesh has its own deformer
        """
        fw('\n\tDeformer: "Deformer::Skin %s", "Skin" {' % obname)
        fw('''
		Version: 100
		MultiLayer: 0
		Type: "Skin"
		Properties60:  {
		}
		Link_DeformAcuracy: 50
	}''')

    # in the example was 'Bip01 L Thigh_2'
    def write_sub_deformer_skin(my_mesh, my_bone, weights):

        """
        Each subdeformer is specific to a mesh, but the bone it links to can be used by many sub-deformers
        So the SubDeformer needs the mesh-object name as a prefix to make it unique

        Its possible that there is no matching vgroup in this mesh, in that case no verts are in the subdeformer,
        a but silly but dosnt really matter
        """
        fw('\n\tDeformer: "SubDeformer::Cluster %s %s", "Cluster" {' % (my_mesh.fbxName, my_bone.fbxName))

        fw('''
		Version: 100
		MultiLayer: 0
		Type: "Cluster"
		Properties60:  {
			Property: "SrcModel", "object", ""
			Property: "SrcModelReference", "object", ""
		}
		UserData: "", ""''')

        # Support for bone parents
        if my_mesh.fbxBoneParent:
            if my_mesh.fbxBoneParent == my_bone:
                # TODO - this is a bit lazy, we could have a simple write loop
                # for this case because all weights are 1.0 but for now this is ok
                # Parent Bones arent used all that much anyway.
                vgroup_data = [(j, 1.0) for j in range(len(my_mesh.blenData.vertices))]
            else:
                # This bone is not a parent of this mesh object, no weights
                vgroup_data = []

        else:
            # Normal weight painted mesh
            if my_bone.blenName in weights[0]:
                # Before we used normalized weight list
                group_index = weights[0].index(my_bone.blenName)
                vgroup_data = [(j, weight[group_index]) for j, weight in enumerate(weights[1]) if weight[group_index]]
            else:
                vgroup_data = []

        fw('\n\t\tIndexes: ')

        i = -1
        for vg in vgroup_data:
            if i == -1:
                fw('%i' % vg[0])
                i = 0
            else:
                if i == 23:
                    fw('\n\t\t')
                    i = 0
                fw(',%i' % vg[0])
            i += 1

        fw('\n\t\tWeights: ')
        i = -1
        for vg in vgroup_data:
            if i == -1:
                fw('%.8f' % vg[1])
                i = 0
            else:
                if i == 38:
                    fw('\n\t\t')
                    i = 0
                fw(',%.8f' % vg[1])
            i += 1

        # Set TransformLink to the global transform of the bone and Transform
        # equal to the mesh's transform in bone space.
        # http://area.autodesk.com/forum/autodesk-fbx/fbx-sdk/why-the-values-return-by-fbxcluster-gettransformmatrix-x-not-same-with-the-value-in-ascii-fbx-file/

        global_bone_matrix = (my_bone.fbxArm.matrixWorld * my_bone.restMatrix) * mtx4_z90
        global_mesh_matrix = my_mesh.matrixWorld
        transform_matrix = (global_bone_matrix.inverted() * global_mesh_matrix)

        global_bone_matrix_string = mat4x4str(global_bone_matrix )
        transform_matrix_string = mat4x4str(transform_matrix )

        fw('\n\t\tTransform: %s' % transform_matrix_string)
        fw('\n\t\tTransformLink: %s' % global_bone_matrix_string)
        fw('\n\t}')

    def write_mesh(my_mesh):

        me = my_mesh.blenData

        # if there are non NULL materials on this mesh
        do_materials = bool(my_mesh.blenMaterials)
        do_textures = bool(my_mesh.blenTextures)
        do_uvs = bool(me.tessface_uv_textures)
        do_shapekeys = (my_mesh.blenObject.type == 'MESH' and
                        my_mesh.blenObject.data.shape_keys and
                        len(my_mesh.blenObject.data.vertices) == len(me.vertices))

        fw('\n\tModel: "Model::%s", "Mesh" {' % my_mesh.fbxName)
        fw('\n\t\tVersion: 232')  # newline is added in write_object_props

        # convert into lists once.
        me_vertices = me.vertices[:]
        me_edges = me.edges[:] if use_mesh_edges else ()
        me_faces = me.tessfaces[:]

        poseMatrix = write_object_props(my_mesh.blenObject, None, my_mesh.parRelMatrix())[3]

        # Calculate the global transform for the mesh in the bind pose the same way we do
        # in write_sub_deformer_skin
        globalMeshBindPose = my_mesh.matrixWorld * mtx4_z90
        pose_items.append((my_mesh.fbxName, globalMeshBindPose))

        if do_shapekeys:
            for kb in my_mesh.blenObject.data.shape_keys.key_blocks[1:]:
                fw('\n\t\t\tProperty: "%s", "Number", "AN",0' % kb.name)

        fw('\n\t\t}')

        fw('\n\t\tMultiLayer: 0'
           '\n\t\tMultiTake: 1'
           '\n\t\tShading: Y'
           '\n\t\tCulling: "CullingOff"'
           )

        # Write the Real Mesh data here
        fw('\n\t\tVertices: ')
        i = -1

        for v in me_vertices:
            if i == -1:
                fw('%.6f,%.6f,%.6f' % v.co[:])
                i = 0
            else:
                if i == 7:
                    fw('\n\t\t')
                    i = 0
                fw(',%.6f,%.6f,%.6f' % v.co[:])
            i += 1

        fw('\n\t\tPolygonVertexIndex: ')
        i = -1
        for f in me_faces:
            fi = f.vertices[:]

            # last index XORd w. -1 indicates end of face
            if i == -1:
                if len(fi) == 3:
                    fw('%i,%i,%i' % (fi[0], fi[1], fi[2] ^ -1))
                else:
                    fw('%i,%i,%i,%i' % (fi[0], fi[1], fi[2], fi[3] ^ -1))
                i = 0
            else:
                if i == 13:
                    fw('\n\t\t')
                    i = 0
                if len(fi) == 3:
                    fw(',%i,%i,%i' % (fi[0], fi[1], fi[2] ^ -1))
                else:
                    fw(',%i,%i,%i,%i' % (fi[0], fi[1], fi[2], fi[3] ^ -1))
            i += 1

        # write loose edges as faces.
        for ed in me_edges:
            if ed.is_loose:
                ed_val = ed.vertices[:]
                ed_val = ed_val[0], ed_val[-1] ^ -1

                if i == -1:
                    fw('%i,%i' % ed_val)
                    i = 0
                else:
                    if i == 13:
                        fw('\n\t\t')
                        i = 0
                    fw(',%i,%i' % ed_val)
            i += 1

        fw('\n\t\tEdges: ')
        i = -1
        for ed in me_edges:
            if i == -1:
                fw('%i,%i' % (ed.vertices[0], ed.vertices[1]))
                i = 0
            else:
                if i == 13:
                    fw('\n\t\t')
                    i = 0
                fw(',%i,%i' % (ed.vertices[0], ed.vertices[1]))
            i += 1

        fw('\n\t\tGeometryVersion: 124')

        fw('''
		LayerElementNormal: 0 {
			Version: 101
			Name: ""
			MappingInformationType: "ByVertice"
			ReferenceInformationType: "Direct"
			Normals: ''')

        i = -1
        for v in me_vertices:
            if i == -1:
                fw('%.15f,%.15f,%.15f' % v.normal[:])
                i = 0
            else:
                if i == 2:
                    fw('\n\t\t\t ')
                    i = 0
                fw(',%.15f,%.15f,%.15f' % v.normal[:])
            i += 1
        fw('\n\t\t}')

        # Write Face Smoothing
        if mesh_smooth_type == 'FACE':
            fw('''
		LayerElementSmoothing: 0 {
			Version: 102
			Name: ""
			MappingInformationType: "ByPolygon"
			ReferenceInformationType: "Direct"
			Smoothing: ''')

            i = -1
            for f in me_faces:
                if i == -1:
                    fw('%i' % f.use_smooth)
                    i = 0
                else:
                    if i == 54:
                        fw('\n\t\t\t ')
                        i = 0
                    fw(',%i' % f.use_smooth)
                i += 1

            fw('\n\t\t}')

        elif mesh_smooth_type == 'EDGE':
            # Write Edge Smoothing
            fw('''
		LayerElementSmoothing: 0 {
			Version: 101
			Name: ""
			MappingInformationType: "ByEdge"
			ReferenceInformationType: "Direct"
			Smoothing: ''')

            i = -1
            for ed in me_edges:
                if i == -1:
                    fw('%i' % (ed.use_edge_sharp))
                    i = 0
                else:
                    if i == 54:
                        fw('\n\t\t\t ')
                        i = 0
                    fw(',%i' % ed.use_edge_sharp)
                i += 1

            fw('\n\t\t}')
        elif mesh_smooth_type == 'OFF':
            pass
        else:
            raise Exception("invalid mesh_smooth_type: %r" % mesh_smooth_type)

        # Write VertexColor Layers
        # note, no programs seem to use this info :/
        collayers = []
        if len(me.tessface_vertex_colors):
            collayers = me.tessface_vertex_colors
            for colindex, collayer in enumerate(collayers):
                fw('\n\t\tLayerElementColor: %i {' % colindex)
                fw('\n\t\t\tVersion: 101')
                fw('\n\t\t\tName: "%s"' % collayer.name)

                fw('''
			MappingInformationType: "ByPolygonVertex"
			ReferenceInformationType: "IndexToDirect"
			Colors: ''')

                i = -1
                ii = 0  # Count how many Colors we write
                print(len(me_faces), len(collayer.data))
                for fi, cf in enumerate(collayer.data):
                    if len(me_faces[fi].vertices) == 4:
                        colors = cf.color1[:], cf.color2[:], cf.color3[:], cf.color4[:]
                    else:
                        colors = cf.color1[:], cf.color2[:], cf.color3[:]

                    for col in colors:
                        if i == -1:
                            fw('%.4f,%.4f,%.4f,1' % col)
                            i = 0
                        else:
                            if i == 7:
                                fw('\n\t\t\t\t')
                                i = 0
                            fw(',%.4f,%.4f,%.4f,1' % col)
                        i += 1
                        ii += 1  # One more Color

                fw('\n\t\t\tColorIndex: ')
                i = -1
                for j in range(ii):
                    if i == -1:
                        fw('%i' % j)
                        i = 0
                    else:
                        if i == 55:
                            fw('\n\t\t\t\t')
                            i = 0
                        fw(',%i' % j)
                    i += 1

                fw('\n\t\t}')

        # Write UV and texture layers.
        uvlayers = []
        if do_uvs:
            uvlayers = me.tessface_uv_textures
            for uvindex, uvlayer in enumerate(me.tessface_uv_textures):
                fw('\n\t\tLayerElementUV: %i {' % uvindex)
                fw('\n\t\t\tVersion: 101')
                fw('\n\t\t\tName: "%s"' % uvlayer.name)

                fw('''
			MappingInformationType: "ByPolygonVertex"
			ReferenceInformationType: "IndexToDirect"
			UV: ''')

                i = -1
                ii = 0  # Count how many UVs we write

                for uf in uvlayer.data:
                    # workaround, since uf.uv iteration is wrong atm
                    for uv in uf.uv:
                        if i == -1:
                            fw('%.6f,%.6f' % uv[:])
                            i = 0
                        else:
                            if i == 7:
                                fw('\n\t\t\t ')
                                i = 0
                            fw(',%.6f,%.6f' % uv[:])
                        i += 1
                        ii += 1  # One more UV

                fw('\n\t\t\tUVIndex: ')
                i = -1
                for j in range(ii):
                    if i == -1:
                        fw('%i' % j)
                        i = 0
                    else:
                        if i == 55:
                            fw('\n\t\t\t\t')
                            i = 0
                        fw(',%i' % j)
                    i += 1

                fw('\n\t\t}')

                if do_textures:
                    fw('\n\t\tLayerElementTexture: %i {' % uvindex)
                    fw('\n\t\t\tVersion: 101')
                    fw('\n\t\t\tName: "%s"' % uvlayer.name)

                    if len(my_mesh.blenTextures) == 1:
                        fw('\n\t\t\tMappingInformationType: "AllSame"')
                    else:
                        fw('\n\t\t\tMappingInformationType: "ByPolygon"')

                    fw('\n\t\t\tReferenceInformationType: "IndexToDirect"')
                    fw('\n\t\t\tBlendMode: "Translucent"')
                    fw('\n\t\t\tTextureAlpha: 1')
                    fw('\n\t\t\tTextureId: ')

                    if len(my_mesh.blenTextures) == 1:
                        fw('0')
                    else:
                        texture_mapping_local = {None: -1}

                        i = 0  # 1 for dummy
                        for tex in my_mesh.blenTextures:
                            if tex:  # None is set above
                                texture_mapping_local[tex] = i
                                i += 1

                        i = -1
                        for f in uvlayer.data:
                            img_key = f.image

                            if i == -1:
                                i = 0
                                fw('%s' % texture_mapping_local[img_key])
                            else:
                                if i == 55:
                                    fw('\n			 ')
                                    i = 0

                                fw(',%s' % texture_mapping_local[img_key])
                            i += 1

                else:
                    fw('''
		LayerElementTexture: 0 {
			Version: 101
			Name: ""
			MappingInformationType: "NoMappingInformation"
			ReferenceInformationType: "IndexToDirect"
			BlendMode: "Translucent"
			TextureAlpha: 1
			TextureId: ''')
                fw('\n\t\t}')

        # Done with UV/textures.
        if do_materials:
            fw('\n\t\tLayerElementMaterial: 0 {')
            fw('\n\t\t\tVersion: 101')
            fw('\n\t\t\tName: ""')

            if len(my_mesh.blenMaterials) == 1:
                fw('\n\t\t\tMappingInformationType: "AllSame"')
            else:
                fw('\n\t\t\tMappingInformationType: "ByPolygon"')

            fw('\n\t\t\tReferenceInformationType: "IndexToDirect"')
            fw('\n\t\t\tMaterials: ')

            if len(my_mesh.blenMaterials) == 1:
                fw('0')
            else:
                # Build a material mapping for this
                material_mapping_local = {}  # local-mat & tex : global index.

                for j, mat_tex_pair in enumerate(my_mesh.blenMaterials):
                    material_mapping_local[mat_tex_pair] = j

                mats = my_mesh.blenMaterialList

                if me.tessface_uv_textures.active:
                    uv_faces = me.tessface_uv_textures.active.data
                else:
                    uv_faces = [None] * len(me_faces)

                i = -1
                for f, uf in zip(me_faces, uv_faces):
                    try:
                        mat = mats[f.material_index]
                    except:
                        mat = None

                    if do_uvs:
                        tex = uf.image  # WARNING - MULTI UV LAYER IMAGES NOT SUPPORTED :/
                    else:
                        tex = None

                    if i == -1:
                        i = 0
                        fw('%s' % material_mapping_local[mat, tex])  # None for mat or tex is ok
                    else:
                        if i == 55:
                            fw('\n\t\t\t\t')
                            i = 0

                        fw(',%s' % material_mapping_local[mat, tex])
                    i += 1

            fw('\n\t\t}')

        fw('''
		Layer: 0 {
			Version: 100
			LayerElement:  {
				Type: "LayerElementNormal"
				TypedIndex: 0
			}''')

        if do_materials:
            fw('''
			LayerElement:  {
				Type: "LayerElementMaterial"
				TypedIndex: 0
			}''')

        # Smoothing info
        if mesh_smooth_type != 'OFF':
            fw('''
			LayerElement:  {
				Type: "LayerElementSmoothing"
				TypedIndex: 0
			}''')

        # Always write this
        if do_textures:
            fw('''
			LayerElement:  {
				Type: "LayerElementTexture"
				TypedIndex: 0
			}''')

        if me.tessface_vertex_colors:
            fw('''
			LayerElement:  {
				Type: "LayerElementColor"
				TypedIndex: 0
			}''')

        if do_uvs:  # same as me.faceUV
            fw('''
			LayerElement:  {
				Type: "LayerElementUV"
				TypedIndex: 0
			}''')

        fw('\n\t\t}')

        if len(uvlayers) > 1:
            for i in range(1, len(uvlayers)):

                fw('\n\t\tLayer: %i {' % i)
                fw('\n\t\t\tVersion: 100')

                fw('''
			LayerElement:  {
				Type: "LayerElementUV"''')

                fw('\n\t\t\t\tTypedIndex: %i' % i)
                fw('\n\t\t\t}')

                if do_textures:

                    fw('''
			LayerElement:  {
				Type: "LayerElementTexture"''')

                    fw('\n\t\t\t\tTypedIndex: %i' % i)
                    fw('\n\t\t\t}')

                fw('\n\t\t}')

        if len(collayers) > 1:
            # Take into account any UV layers
            layer_offset = 0
            if uvlayers:
                layer_offset = len(uvlayers) - 1

            for i in range(layer_offset, len(collayers) + layer_offset):
                fw('\n\t\tLayer: %i {' % i)
                fw('\n\t\t\tVersion: 100')

                fw('''
			LayerElement:  {
				Type: "LayerElementColor"''')

                fw('\n\t\t\t\tTypedIndex: %i' % i)
                fw('\n\t\t\t}')
                fw('\n\t\t}')

        if do_shapekeys:
            key_blocks = my_mesh.blenObject.data.shape_keys.key_blocks[:]
            for kb in key_blocks[1:]:

                fw('\n\t\tShape: "%s" {' % kb.name)
                fw('\n\t\t\tIndexes: ')

                basis_verts = key_blocks[0].data
                range_verts = []
                delta_verts = []
                i = -1
                for j, kv in enumerate(kb.data):
                    delta = kv.co - basis_verts[j].co
                    if delta.length > 0.000001:
                        if i == -1:
                            fw('%d' % j)
                        else:
                            if i == 7:
                                fw('\n\t\t\t')
                                i = 0
                            fw(',%d' % j)
                        delta_verts.append(delta[:])
                        i += 1

                fw('\n\t\t\tVertices: ')
                i = -1
                for dv in delta_verts:
                    if i == -1:
                        fw("%.6f,%.6f,%.6f" % dv)
                    else:
                        if i == 4:
                            fw('\n\t\t\t')
                            i = 0
                        fw(",%.6f,%.6f,%.6f" % dv)
                    i += 1

                # all zero, why? - campbell
                fw('\n\t\t\tNormals: ')
                for j in range(len(delta_verts)):
                    if i == -1:
                        fw("0,0,0")
                    else:
                        if i == 4:
                            fw('\n\t\t\t')
                            i = 0
                        fw(",0,0,0")
                    i += 1
                fw('\n\t\t}')

        fw('\n\t}')

    def write_group(name):
        fw('\n\tGroupSelection: "GroupSelection::%s", "Default" {' % name)

        fw('''
		Properties60:  {
			Property: "MultiLayer", "bool", "",0
			Property: "Pickable", "bool", "",1
			Property: "Transformable", "bool", "",1
			Property: "Show", "bool", "",1
		}
		MultiLayer: 0
	}''')

    # add meshes here to clear because they are not used anywhere.
    meshes_to_clear = []

    ob_meshes = []
    ob_lights = []
    ob_cameras = []
    # in fbx we export bones as children of the mesh
    # armatures not a part of a mesh, will be added to ob_arms
    ob_bones = []
    ob_arms = []
    ob_null = []  # emptys

    # List of types that have blender objects (not bones)
    ob_all_typegroups = [ob_meshes, ob_lights, ob_cameras, ob_arms, ob_null]

    groups = []  # blender groups, only add ones that have objects in the selections
    materials = {}  # (mat, image) keys, should be a set()
    textures = {}  # should be a set()

    tmp_ob_type = None  # in case no objects are exported, so as not to raise an error

## XXX

    if 'ARMATURE' in object_types:
        # This is needed so applying modifiers dosnt apply the armature deformation, its also needed
        # ...so mesh objects return their rest worldspace matrix when bone-parents are exported as weighted meshes.
        # set every armature to its rest, backup the original values so we done mess up the scene
        ob_arms_orig_rest = [arm.pose_position for arm in bpy.data.armatures]

        for arm in bpy.data.armatures:
            arm.pose_position = 'REST'

        if ob_arms_orig_rest:
            for ob_base in bpy.data.objects:
                if ob_base.type == 'ARMATURE':
                    ob_base.update_tag()

            # This causes the makeDisplayList command to effect the mesh
            scene.frame_set(scene.frame_current)

    for ob_base in context_objects:

        # ignore dupli children
        if ob_base.parent and ob_base.parent.dupli_type in {'VERTS', 'FACES'}:
            continue

        obs = [(ob_base, ob_base.matrix_world.copy())]
        if ob_base.dupli_type != 'NONE':
            ob_base.dupli_list_create(scene)
            obs = [(dob.object, dob.matrix.copy()) for dob in ob_base.dupli_list]

        for ob, mtx in obs:
            tmp_ob_type = ob.type
            if tmp_ob_type == 'CAMERA':
                if 'CAMERA' in object_types:
                    ob_cameras.append(my_object_generic(ob, mtx))
            elif tmp_ob_type == 'LAMP':
                if 'LAMP' in object_types:
                    ob_lights.append(my_object_generic(ob, mtx))
            elif tmp_ob_type == 'ARMATURE':
                if 'ARMATURE' in object_types:
                    # TODO - armatures dont work in dupligroups!
                    if ob not in ob_arms:
                        ob_arms.append(ob)
                    # ob_arms.append(ob) # replace later. was "ob_arms.append(sane_obname(ob), ob)"
            elif tmp_ob_type == 'EMPTY':
                if 'EMPTY' in object_types:
                    ob_null.append(my_object_generic(ob, mtx))
            elif 'MESH' in object_types:
                origData = True
                if tmp_ob_type != 'MESH':
                    try:
                        me = ob.to_mesh(scene, True, 'PREVIEW')
                    except:
                        me = None

                    if me:
                        meshes_to_clear.append(me)
                        mats = me.materials
                        origData = False
                else:
                    # Mesh Type!
                    if use_mesh_modifiers:
                        me = ob.to_mesh(scene, True, 'PREVIEW')

                        # print ob, me, me.getVertGroupNames()
                        meshes_to_clear.append(me)
                        origData = False
                        mats = me.materials
                    else:
                        me = ob.data
                        me.update(calc_tessface=True)
                        mats = me.materials

# 						# Support object colors
# 						tmp_colbits = ob.colbits
# 						if tmp_colbits:
# 							tmp_ob_mats = ob.getMaterials(1) # 1 so we get None's too.
# 							for i in xrange(16):
# 								if tmp_colbits & (1<<i):
# 									mats[i] = tmp_ob_mats[i]
# 							del tmp_ob_mats
# 						del tmp_colbits

                if me:
# 					# This WILL modify meshes in blender if use_mesh_modifiers is disabled.
# 					# so strictly this is bad. but only in rare cases would it have negative results
# 					# say with dupliverts the objects would rotate a bit differently
# 					if EXP_MESH_HQ_NORMALS:
# 						BPyMesh.meshCalcNormals(me) # high quality normals nice for realtime engines.

                    texture_mapping_local = {}
                    material_mapping_local = {}
                    if me.tessface_uv_textures:
                        for uvlayer in me.tessface_uv_textures:
                            for f, uf in zip(me.tessfaces, uvlayer.data):
                                tex = uf.image
                                textures[tex] = texture_mapping_local[tex] = None

                                try:
                                    mat = mats[f.material_index]
                                except:
                                    mat = None

                                materials[mat, tex] = material_mapping_local[mat, tex] = None  # should use sets, wait for blender 2.5

                    else:
                        for mat in mats:
                            # 2.44 use mat.lib too for uniqueness
                            materials[mat, None] = material_mapping_local[mat, None] = None
                        else:
                            materials[None, None] = None

                    if 'ARMATURE' in object_types:
                        armob = ob.find_armature()
                        blenParentBoneName = None

                        # parent bone - special case
                        if (not armob) and ob.parent and ob.parent.type == 'ARMATURE' and \
                                ob.parent_type == 'BONE':
                            armob = ob.parent
                            blenParentBoneName = ob.parent_bone

                        if armob and armob not in ob_arms:
                            ob_arms.append(armob)

                        # Warning for scaled, mesh objects with armatures
                        if abs(ob.scale[0] - 1.0) > 0.05 or abs(ob.scale[1] - 1.0) > 0.05 or abs(ob.scale[1] - 1.0) > 0.05:
                            operator.report({'WARNING'}, "Object '%s' has a scale of (%.3f, %.3f, %.3f), " \
                                                         "Armature deformation will not work as expected " \
                                                         "(apply Scale to fix)" % ((ob.name,) + tuple(ob.scale)))

                    else:
                        blenParentBoneName = armob = None

                    my_mesh = my_object_generic(ob, mtx)
                    my_mesh.blenData = me
                    my_mesh.origData = origData
                    my_mesh.blenMaterials = list(material_mapping_local.keys())
                    my_mesh.blenMaterialList = mats
                    my_mesh.blenTextures = list(texture_mapping_local.keys())

                    # sort the name so we get predictable output, some items may be NULL
                    my_mesh.blenMaterials.sort(key=lambda m: (getattr(m[0], "name", ""), getattr(m[1], "name", "")))
                    my_mesh.blenTextures.sort(key=lambda m: getattr(m, "name", ""))

                    # if only 1 null texture then empty the list
                    if len(my_mesh.blenTextures) == 1 and my_mesh.blenTextures[0] is None:
                        my_mesh.blenTextures = []

                    my_mesh.fbxArm = armob  # replace with my_object_generic armature instance later
                    my_mesh.fbxBoneParent = blenParentBoneName  # replace with my_bone instance later

                    ob_meshes.append(my_mesh)

        # not forgetting to free dupli_list
        if ob_base.dupli_list:
            ob_base.dupli_list_clear()

    if 'ARMATURE' in object_types:
        # now we have the meshes, restore the rest arm position
        for i, arm in enumerate(bpy.data.armatures):
            arm.pose_position = ob_arms_orig_rest[i]

        if ob_arms_orig_rest:
            for ob_base in bpy.data.objects:
                if ob_base.type == 'ARMATURE':
                    ob_base.update_tag()
            # This causes the makeDisplayList command to effect the mesh
            scene.frame_set(scene.frame_current)

    del tmp_ob_type, context_objects

    # now we have collected all armatures, add bones
    for i, ob in enumerate(ob_arms):

        ob_arms[i] = my_arm = my_object_generic(ob)

        my_arm.fbxBones = []
        my_arm.blenData = ob.data
        if ob.animation_data:
            my_arm.blenAction = ob.animation_data.action
        else:
            my_arm.blenAction = None
        my_arm.blenActionList = []

        # fbxName, blenderObject, my_bones, blenderActions
        #ob_arms[i] = fbxArmObName, ob, arm_my_bones, (ob.action, [])

        if use_armature_deform_only:
            # tag non deforming bones that have no deforming children
            deform_map = dict.fromkeys(my_arm.blenData.bones, False)
            for bone in my_arm.blenData.bones:
                if bone.use_deform:
                    deform_map[bone] = True
                    # tag all parents, even ones that are not deform since their child _is_
                    for parent in bone.parent_recursive:
                        deform_map[parent] = True

        for bone in my_arm.blenData.bones:

            if use_armature_deform_only:
                # if this bone doesnt deform, and none of its children deform, skip it!
                if not deform_map[bone]:
                    continue

            my_bone = my_bone_class(bone, my_arm)
            my_arm.fbxBones.append(my_bone)
            ob_bones.append(my_bone)

        if use_armature_deform_only:
            del deform_map

    # add the meshes to the bones and replace the meshes armature with own armature class
    #for obname, ob, mtx, me, mats, arm, armname in ob_meshes:
    for my_mesh in ob_meshes:
        # Replace
        # ...this could be sped up with dictionary mapping but its unlikely for
        # it ever to be a bottleneck - (would need 100+ meshes using armatures)
        if my_mesh.fbxArm:
            for my_arm in ob_arms:
                if my_arm.blenObject == my_mesh.fbxArm:
                    my_mesh.fbxArm = my_arm
                    break

        for my_bone in ob_bones:

            # The mesh uses this bones armature!
            if my_bone.fbxArm == my_mesh.fbxArm:
                if my_bone.blenBone.use_deform:
                    my_bone.blenMeshes[my_mesh.fbxName] = me

                # parent bone: replace bone names with our class instances
                # my_mesh.fbxBoneParent is None or a blender bone name initialy, replacing if the names match.
                if my_mesh.fbxBoneParent == my_bone.blenName:
                    my_mesh.fbxBoneParent = my_bone

    bone_deformer_count = 0  # count how many bones deform a mesh
    my_bone_blenParent = None
    for my_bone in ob_bones:
        my_bone_blenParent = my_bone.blenBone.parent
        if my_bone_blenParent:
            for my_bone_parent in ob_bones:
                # Note 2.45rc2 you can compare bones normally
                if my_bone_blenParent.name == my_bone_parent.blenName and my_bone.fbxArm == my_bone_parent.fbxArm:
                    my_bone.parent = my_bone_parent
                    break

        # Not used at the moment
        # my_bone.calcRestMatrixLocal()
        bone_deformer_count += len(my_bone.blenMeshes)

    del my_bone_blenParent

    # Build blenObject -> fbxObject mapping
    # this is needed for groups as well as fbxParenting
    bpy.data.objects.tag(False)

    # using a list of object names for tagging (Arystan)

    tmp_obmapping = {}
    for ob_generic in ob_all_typegroups:
        for ob_base in ob_generic:
            ob_base.blenObject.tag = True
            tmp_obmapping[ob_base.blenObject] = ob_base

    # Build Groups from objects we export
    for blenGroup in bpy.data.groups:
        fbxGroupName = None
        for ob in blenGroup.objects:
            if ob.tag:
                if fbxGroupName is None:
                    fbxGroupName = sane_groupname(blenGroup)
                    groups.append((fbxGroupName, blenGroup))

                tmp_obmapping[ob].fbxGroupNames.append(fbxGroupName)  # also adds to the objects fbxGroupNames

    groups.sort()  # not really needed

    # Assign parents using this mapping
    for ob_generic in ob_all_typegroups:
        for my_ob in ob_generic:
            parent = my_ob.blenObject.parent
            if parent and parent.tag:  # does it exist and is it in the mapping
                my_ob.fbxParent = tmp_obmapping[parent]

    del tmp_obmapping
    # Finished finding groups we use

    # == WRITE OBJECTS TO THE FILE ==
    # == From now on we are building the FBX file from the information collected above (JCB)

    materials = [(sane_matname(mat_tex_pair), mat_tex_pair) for mat_tex_pair in materials.keys()]
    textures = [(sane_texname(tex), tex) for tex in textures.keys()  if tex]
    materials.sort(key=lambda m: m[0])  # sort by name
    textures.sort(key=lambda m: m[0])

    camera_count = 8 if 'CAMERA' in object_types else 0

    # sanity checks
    try:
        assert(not (ob_meshes and ('MESH' not in object_types)))
        assert(not (materials and ('MESH' not in object_types)))
        assert(not (textures and ('MESH' not in object_types)))
        assert(not (textures and ('MESH' not in object_types)))

        assert(not (ob_lights and ('LAMP' not in object_types)))

        assert(not (ob_cameras and ('CAMERA' not in object_types)))
    except AssertionError:
        import traceback
        traceback.print_exc()

    fw('''

; Object definitions
;------------------------------------------------------------------

Definitions:  {
	Version: 100
	Count: %i''' % (
        1 + camera_count +
        len(ob_meshes) +
        len(ob_lights) +
        len(ob_cameras) +
        len(ob_arms) +
        len(ob_null) +
        len(ob_bones) +
        bone_deformer_count +
        len(materials) +
        (len(textures) * 2)))  # add 1 for global settings

    del bone_deformer_count

    fw('''
	ObjectType: "Model" {
		Count: %i
	}''' % (
        camera_count +
        len(ob_meshes) +
        len(ob_lights) +
        len(ob_cameras) +
        len(ob_arms) +
        len(ob_null) +
        len(ob_bones)))

    fw('''
	ObjectType: "Geometry" {
		Count: %i
	}''' % len(ob_meshes))

    if materials:
        fw('''
	ObjectType: "Material" {
		Count: %i
	}''' % len(materials))

    if textures:
        fw('''
	ObjectType: "Texture" {
		Count: %i
	}''' % len(textures))  # add 1 for an empty tex
        fw('''
	ObjectType: "Video" {
		Count: %i
	}''' % len(textures))  # add 1 for an empty tex

    tmp = 0
    # Add deformer nodes
    for my_mesh in ob_meshes:
        if my_mesh.fbxArm:
            tmp += 1

    # Add subdeformers
    for my_bone in ob_bones:
        tmp += len(my_bone.blenMeshes)

    if tmp:
        fw('''
	ObjectType: "Deformer" {
		Count: %i
	}''' % tmp)
    del tmp

    # Bind pose is essential for XNA if the 'MESH' is included (JCB)
    fw('''
	ObjectType: "Pose" {
		Count: 1
	}''')

    if groups:
        fw('''
	ObjectType: "GroupSelection" {
		Count: %i
	}''' % len(groups))

    fw('''
	ObjectType: "GlobalSettings" {
		Count: 1
	}
}''')

    fw('''

; Object properties
;------------------------------------------------------------------

Objects:  {''')

    if 'CAMERA' in object_types:
        # To comply with other FBX FILES
        write_camera_switch()

    for my_null in ob_null:
        write_null(my_null)

    # XNA requires the armature to be a Limb (JCB)
    # Note, 2.58 and previous wrote these as normal empties and it worked mostly (except for XNA)
    for my_arm in ob_arms:
        write_null(my_arm, fbxType="Limb", fbxTypeFlags="Skeleton")

    for my_cam in ob_cameras:
        write_camera(my_cam)

    for my_light in ob_lights:
        write_light(my_light)

    for my_mesh in ob_meshes:
        write_mesh(my_mesh)

    #for bonename, bone, obname, me, armob in ob_bones:
    for my_bone in ob_bones:
        write_bone(my_bone)

    if 'CAMERA' in object_types:
        write_camera_default()

    for matname, (mat, tex) in materials:
        write_material(matname, mat)  # We only need to have a material per image pair, but no need to write any image info into the material (dumb fbx standard)

    # each texture uses a video, odd
    for texname, tex in textures:
        write_video(texname, tex)
    i = 0
    for texname, tex in textures:
        write_texture(texname, tex, i)
        i += 1

    for groupname, group in groups:
        write_group(groupname)

    # NOTE - c4d and motionbuilder dont need normalized weights, but deep-exploration 5 does and (max?) do.

    # Write armature modifiers
    # TODO - add another MODEL? - because of this skin definition.
    for my_mesh in ob_meshes:
        if my_mesh.fbxArm:
            write_deformer_skin(my_mesh.fbxName)

            # Get normalized weights for temorary use
            if my_mesh.fbxBoneParent:
                weights = None
            else:
                weights = meshNormalizedWeights(my_mesh.blenObject, my_mesh.blenData)

            #for bonename, bone, obname, bone_mesh, armob in ob_bones:
            for my_bone in ob_bones:
                if me in iter(my_bone.blenMeshes.values()):
                    write_sub_deformer_skin(my_mesh, my_bone, weights)

    # Write pose is really weird, only needed when an armature and mesh are used together
    # each by themselves do not need pose data. For now only pose meshes and bones

    # Bind pose is essential for XNA if the 'MESH' is included (JCB)
    fw('''
	Pose: "Pose::BIND_POSES", "BindPose" {
		Type: "BindPose"
		Version: 100
		Properties60:  {
		}
		NbPoseNodes: ''')
    fw(str(len(pose_items)))

    for fbxName, matrix in pose_items:
        fw('\n\t\tPoseNode:  {')
        fw('\n\t\t\tNode: "Model::%s"' % fbxName)
        fw('\n\t\t\tMatrix: %s' % mat4x4str(matrix if matrix else Matrix()))
        fw('\n\t\t}')

    fw('\n\t}')

    # Finish Writing Objects
    # Write global settings
    fw('''
	GlobalSettings:  {
		Version: 1000
		Properties60:  {
			Property: "UpAxis", "int", "",1
			Property: "UpAxisSign", "int", "",1
			Property: "FrontAxis", "int", "",2
			Property: "FrontAxisSign", "int", "",1
			Property: "CoordAxis", "int", "",0
			Property: "CoordAxisSign", "int", "",1
			Property: "UnitScaleFactor", "double", "",1
		}
	}
''')
    fw('}')

    fw('''

; Object relations
;------------------------------------------------------------------

Relations:  {''')

    # Nulls are likely to cause problems for XNA

    for my_null in ob_null:
        fw('\n\tModel: "Model::%s", "Null" {\n\t}' % my_null.fbxName)

    # Armature must be a Limb for XNA
    # Note, 2.58 and previous wrote these as normal empties and it worked mostly (except for XNA)
    for my_arm in ob_arms:
        fw('\n\tModel: "Model::%s", "Limb" {\n\t}' % my_arm.fbxName)

    for my_mesh in ob_meshes:
        fw('\n\tModel: "Model::%s", "Mesh" {\n\t}' % my_mesh.fbxName)

    # TODO - limbs can have the same name for multiple armatures, should prefix.
    #for bonename, bone, obname, me, armob in ob_bones:
    for my_bone in ob_bones:
        fw('\n\tModel: "Model::%s", "Limb" {\n\t}' % my_bone.fbxName)

    for my_cam in ob_cameras:
        fw('\n\tModel: "Model::%s", "Camera" {\n\t}' % my_cam.fbxName)

    for my_light in ob_lights:
        fw('\n\tModel: "Model::%s", "Light" {\n\t}' % my_light.fbxName)

    fw('''
	Model: "Model::Producer Perspective", "Camera" {
	}
	Model: "Model::Producer Top", "Camera" {
	}
	Model: "Model::Producer Bottom", "Camera" {
	}
	Model: "Model::Producer Front", "Camera" {
	}
	Model: "Model::Producer Back", "Camera" {
	}
	Model: "Model::Producer Right", "Camera" {
	}
	Model: "Model::Producer Left", "Camera" {
	}
	Model: "Model::Camera Switcher", "CameraSwitcher" {
	}''')

    for matname, (mat, tex) in materials:
        fw('\n\tMaterial: "Material::%s", "" {\n\t}' % matname)

    if textures:
        for texname, tex in textures:
            fw('\n\tTexture: "Texture::%s", "TextureVideoClip" {\n\t}' % texname)
        for texname, tex in textures:
            fw('\n\tVideo: "Video::%s", "Clip" {\n\t}' % texname)

    # deformers - modifiers
    for my_mesh in ob_meshes:
        if my_mesh.fbxArm:
            fw('\n\tDeformer: "Deformer::Skin %s", "Skin" {\n\t}' % my_mesh.fbxName)

    #for bonename, bone, obname, me, armob in ob_bones:
    for my_bone in ob_bones:
        for fbxMeshObName in my_bone.blenMeshes:  # .keys() - fbxMeshObName
            # is this bone effecting a mesh?
            fw('\n\tDeformer: "SubDeformer::Cluster %s %s", "Cluster" {\n\t}' % (fbxMeshObName, my_bone.fbxName))

    # This should be at the end
    # fw('\n\tPose: "Pose::BIND_POSES", "BindPose" {\n\t}')

    for groupname, group in groups:
        fw('\n\tGroupSelection: "GroupSelection::%s", "Default" {\n\t}' % groupname)

    fw('\n}')
    fw('''

; Object connections
;------------------------------------------------------------------

Connections:  {''')

    # NOTE - The FBX SDK does not care about the order but some importers DO!
    # for instance, defining the material->mesh connection
    # before the mesh->parent crashes cinema4d

    for ob_generic in ob_all_typegroups:  # all blender 'Object's we support
        for my_ob in ob_generic:
            # for deformed meshes, don't have any parents or they can get twice transformed.
            if my_ob.fbxParent and (not my_ob.fbxArm):
                fw('\n\tConnect: "OO", "Model::%s", "Model::%s"' % (my_ob.fbxName, my_ob.fbxParent.fbxName))
            else:
                fw('\n\tConnect: "OO", "Model::%s", "Model::Scene"' % my_ob.fbxName)

    if materials:
        for my_mesh in ob_meshes:
            # Connect all materials to all objects, not good form but ok for now.
            for mat, tex in my_mesh.blenMaterials:
                mat_name = mat.name if mat else None
                tex_name = tex.name if tex else None

                fw('\n\tConnect: "OO", "Material::%s", "Model::%s"' % (sane_name_mapping_mat[mat_name, tex_name], my_mesh.fbxName))

    if textures:
        for my_mesh in ob_meshes:
            if my_mesh.blenTextures:
                # fw('\n\tConnect: "OO", "Texture::_empty_", "Model::%s"' % my_mesh.fbxName)
                for tex in my_mesh.blenTextures:
                    if tex:
                        fw('\n\tConnect: "OO", "Texture::%s", "Model::%s"' % (sane_name_mapping_tex[tex.name], my_mesh.fbxName))

        for texname, tex in textures:
            fw('\n\tConnect: "OO", "Video::%s", "Texture::%s"' % (texname, texname))

    if 'MESH' in object_types:
        for my_mesh in ob_meshes:
            if my_mesh.fbxArm:
                fw('\n\tConnect: "OO", "Deformer::Skin %s", "Model::%s"' % (my_mesh.fbxName, my_mesh.fbxName))

        for my_bone in ob_bones:
            for fbxMeshObName in my_bone.blenMeshes:  # .keys()
                fw('\n\tConnect: "OO", "SubDeformer::Cluster %s %s", "Deformer::Skin %s"' % (fbxMeshObName, my_bone.fbxName, fbxMeshObName))

        # limbs -> deformers
        for my_bone in ob_bones:
            for fbxMeshObName in my_bone.blenMeshes:  # .keys()
                fw('\n\tConnect: "OO", "Model::%s", "SubDeformer::Cluster %s %s"' % (my_bone.fbxName, fbxMeshObName, my_bone.fbxName))

    #for bonename, bone, obname, me, armob in ob_bones:
    for my_bone in ob_bones:
        # Always parent to armature now
        if my_bone.parent:
            fw('\n\tConnect: "OO", "Model::%s", "Model::%s"' % (my_bone.fbxName, my_bone.parent.fbxName))
        else:
            # the armature object is written as an empty and all root level bones connect to it
            fw('\n\tConnect: "OO", "Model::%s", "Model::%s"' % (my_bone.fbxName, my_bone.fbxArm.fbxName))

    # groups
    if groups:
        for ob_generic in ob_all_typegroups:
            for ob_base in ob_generic:
                for fbxGroupName in ob_base.fbxGroupNames:
                    fw('\n\tConnect: "OO", "Model::%s", "GroupSelection::%s"' % (ob_base.fbxName, fbxGroupName))

    # I think the following always duplicates the armature connection because it is also in ob_all_typegroups above! (JCB)
    # for my_arm in ob_arms:
    #     fw('\n\tConnect: "OO", "Model::%s", "Model::Scene"' % my_arm.fbxName)

    fw('\n}')

    # Needed for scene footer as well as animation
    render = scene.render

    # from the FBX sdk
    #define KTIME_ONE_SECOND        KTime (K_LONGLONG(46186158000))
    def fbx_time(t):
        # 0.5 + val is the same as rounding.
        return int(0.5 + ((t / fps) * 46186158000))

    fps = float(render.fps)
    start = scene.frame_start
    end = scene.frame_end
    if end < start:
        start, end = end, start

    # comment the following line, otherwise we dont get the pose
    # if start==end: use_anim = False

    # animations for these object types
    ob_anim_lists = ob_bones, ob_meshes, ob_null, ob_cameras, ob_lights, ob_arms

    if use_anim and [tmp for tmp in ob_anim_lists if tmp]:

        frame_orig = scene.frame_current

        if use_anim_optimize:
            ANIM_OPTIMIZE_PRECISSION_FLOAT = 0.1 ** anim_optimize_precision

        # default action, when no actions are avaioable
        tmp_actions = []
        blenActionDefault = None
        action_lastcompat = None

        # instead of tagging
        tagged_actions = []

        # get the current action first so we can use it if we only export one action (JCB)
        for my_arm in ob_arms:
            if not blenActionDefault:
                blenActionDefault = my_arm.blenAction
                if blenActionDefault:
                    break

        if use_anim_action_all:
            tmp_actions = bpy.data.actions[:]
        elif not use_default_take:
            if blenActionDefault:
                # Export the current action (JCB)
                tmp_actions.append(blenActionDefault)

        if tmp_actions:
            # find which actions are compatible with the armatures
            tmp_act_count = 0
            for my_arm in ob_arms:

                arm_bone_names = set([my_bone.blenName for my_bone in my_arm.fbxBones])

                for action in tmp_actions:

                    if arm_bone_names.intersection(action_bone_names(my_arm.blenObject, action)):  # at least one channel matches.
                        my_arm.blenActionList.append(action)
                        tagged_actions.append(action.name)
                        tmp_act_count += 1

                        # in case there are no actions applied to armatures
                        # for example, when a user deletes the current action.
                        action_lastcompat = action

            if tmp_act_count:
                # unlikely to ever happen but if no actions applied to armatures, just use the last compatible armature.
                if not blenActionDefault:
                    blenActionDefault = action_lastcompat

        del action_lastcompat

        if use_default_take:
            tmp_actions.insert(0, None)  # None is the default action

        fw('''
;Takes and animation section
;----------------------------------------------------

Takes:  {''')

        if blenActionDefault and not use_default_take:
            fw('\n\tCurrent: "%s"' % sane_takename(blenActionDefault))
        else:
            fw('\n\tCurrent: "Default Take"')

        for blenAction in tmp_actions:
            # we have tagged all actious that are used be selected armatures
            if blenAction:
                if blenAction.name in tagged_actions:
                    print('\taction: "%s" exporting...' % blenAction.name)
                else:
                    print('\taction: "%s" has no armature using it, skipping' % blenAction.name)
                    continue

            if blenAction is None:
                # Warning, this only accounts for tmp_actions being [None]
                take_name = "Default Take"
                act_start = start
                act_end = end
            else:
                # use existing name
                take_name = sane_name_mapping_take.get(blenAction.name)
                if take_name is None:
                    take_name = sane_takename(blenAction)

                act_start, act_end = blenAction.frame_range
                act_start = int(act_start)
                act_end = int(act_end)

                # Set the action active
                for my_arm in ob_arms:
                    if my_arm.blenObject.animation_data and blenAction in my_arm.blenActionList:
                        my_arm.blenObject.animation_data.action = blenAction

            # Use the action name as the take name and the take filename (JCB)
            fw('\n\tTake: "%s" {' % take_name)
            fw('\n\t\tFileName: "%s.tak"' % take_name.replace(" ", "_"))
            fw('\n\t\tLocalTime: %i,%i' % (fbx_time(act_start - 1), fbx_time(act_end - 1)))  # ??? - not sure why this is needed
            fw('\n\t\tReferenceTime: %i,%i' % (fbx_time(act_start - 1), fbx_time(act_end - 1)))  # ??? - not sure why this is needed

            fw('''

		;Models animation
		;----------------------------------------------------''')

            # set pose data for all bones
            # do this here in case the action changes
            '''
            for my_bone in ob_bones:
                my_bone.flushAnimData()
            '''
            i = act_start
            while i <= act_end:
                scene.frame_set(i)
                for ob_generic in ob_anim_lists:
                    for my_ob in ob_generic:
                        #Blender.Window.RedrawAll()
                        if ob_generic == ob_meshes and my_ob.fbxArm:
                            # We cant animate armature meshes!
                            my_ob.setPoseFrame(i, fake=True)
                        else:
                            my_ob.setPoseFrame(i)

                i += 1

            #for bonename, bone, obname, me, armob in ob_bones:
            for ob_generic in (ob_bones, ob_meshes, ob_null, ob_cameras, ob_lights, ob_arms):

                for my_ob in ob_generic:

                    if ob_generic == ob_meshes and my_ob.fbxArm:
                        # do nothing,
                        pass
                    else:
                        if isinstance(my_ob, my_bone_class) and my_ob.blenName not in action_bone_names(my_ob.fbxArm.blenObject, blenAction):
                            continue
                        
                        fw('\n\t\tModel: "Model::%s" {' % my_ob.fbxName)  # ??? - not sure why this is needed
                        fw('\n\t\t\tVersion: 1.1')
                        fw('\n\t\t\tChannel: "Transform" {')

                        context_bone_anim_mats = [(my_ob.getAnimParRelMatrix(frame), my_ob.getAnimParRelMatrixRot(frame)) for frame in range(act_start, act_end + 1)]

                        # ----------------
                        # ----------------
                        for TX_LAYER, TX_CHAN in enumerate('TRS'):  # transform, rotate, scale

                            if TX_CHAN == 'T':
                                context_bone_anim_vecs = [mtx[0].to_translation() for mtx in context_bone_anim_mats]
                            elif	TX_CHAN == 'S':
                                context_bone_anim_vecs = [mtx[0].to_scale() for mtx in context_bone_anim_mats]
                            elif	TX_CHAN == 'R':
                                # Was....
                                # elif 	TX_CHAN=='R':	context_bone_anim_vecs = [mtx[1].to_euler()			for mtx in context_bone_anim_mats]
                                #
                                # ...but we need to use the previous euler for compatible conversion.
                                context_bone_anim_vecs = []
                                prev_eul = None
                                for mtx in context_bone_anim_mats:
                                    if prev_eul:
                                        prev_eul = mtx[1].to_euler('XYZ', prev_eul)
                                    else:
                                        prev_eul = mtx[1].to_euler()
                                    context_bone_anim_vecs.append(tuple_rad_to_deg(prev_eul))

                            fw('\n\t\t\t\tChannel: "%s" {' % TX_CHAN)  # translation

                            for i in range(3):
                                # Loop on each axis of the bone
                                fw('\n\t\t\t\t\tChannel: "%s" {' % ('XYZ'[i]))  # translation
                                fw('\n\t\t\t\t\t\tDefault: %.15f' % context_bone_anim_vecs[0][i])
                                fw('\n\t\t\t\t\t\tKeyVer: 4005')

                                if not use_anim_optimize:
                                    # Just write all frames, simple but in-eficient
                                    fw('\n\t\t\t\t\t\tKeyCount: %i' % (1 + act_end - act_start))
                                    fw('\n\t\t\t\t\t\tKey: ')
                                    frame = act_start
                                    while frame <= act_end:
                                        if frame != act_start:
                                            fw(',')

                                        # Curve types are 'C,n' for constant, 'L' for linear
                                        # C,n is for bezier? - linear is best for now so we can do simple keyframe removal
                                        fw('\n\t\t\t\t\t\t\t%i,%.15f,L' % (fbx_time(frame - 1), context_bone_anim_vecs[frame - act_start][i]))
                                        frame += 1
                                else:
                                    # remove unneeded keys, j is the frame, needed when some frames are removed.
                                    context_bone_anim_keys = [(vec[i], j) for j, vec in enumerate(context_bone_anim_vecs)]

                                    # last frame to fisrt frame, missing 1 frame on either side.
                                    # removeing in a backwards loop is faster
                                    #for j in xrange( (act_end-act_start)-1, 0, -1 ):
                                    # j = (act_end-act_start)-1
                                    j = len(context_bone_anim_keys) - 2
                                    while j > 0 and len(context_bone_anim_keys) > 2:
                                        # print j, len(context_bone_anim_keys)
                                        # Is this key the same as the ones next to it?

                                        # co-linear horizontal...
                                        if		abs(context_bone_anim_keys[j][0] - context_bone_anim_keys[j - 1][0]) < ANIM_OPTIMIZE_PRECISSION_FLOAT and \
                                                abs(context_bone_anim_keys[j][0] - context_bone_anim_keys[j + 1][0]) < ANIM_OPTIMIZE_PRECISSION_FLOAT:

                                            del context_bone_anim_keys[j]

                                        else:
                                            frame_range = float(context_bone_anim_keys[j + 1][1] - context_bone_anim_keys[j - 1][1])
                                            frame_range_fac1 = (context_bone_anim_keys[j + 1][1] - context_bone_anim_keys[j][1]) / frame_range
                                            frame_range_fac2 = 1.0 - frame_range_fac1

                                            if abs(((context_bone_anim_keys[j - 1][0] * frame_range_fac1 + context_bone_anim_keys[j + 1][0] * frame_range_fac2)) - context_bone_anim_keys[j][0]) < ANIM_OPTIMIZE_PRECISSION_FLOAT:
                                                del context_bone_anim_keys[j]
                                            else:
                                                j -= 1

                                        # keep the index below the list length
                                        if j > len(context_bone_anim_keys) - 2:
                                            j = len(context_bone_anim_keys) - 2

                                    if len(context_bone_anim_keys) == 2 and context_bone_anim_keys[0][0] == context_bone_anim_keys[1][0]:

                                        # This axis has no moton, its okay to skip KeyCount and Keys in this case
                                        # pass

                                        # better write one, otherwise we loose poses with no animation
                                        fw('\n\t\t\t\t\t\tKeyCount: 1')
                                        fw('\n\t\t\t\t\t\tKey: ')
                                        fw('\n\t\t\t\t\t\t\t%i,%.15f,L' % (fbx_time(start), context_bone_anim_keys[0][0]))
                                    else:
                                        # We only need to write these if there is at least one
                                        fw('\n\t\t\t\t\t\tKeyCount: %i' % len(context_bone_anim_keys))
                                        fw('\n\t\t\t\t\t\tKey: ')
                                        for val, frame in context_bone_anim_keys:
                                            if frame != context_bone_anim_keys[0][1]:  # not the first
                                                fw(',')
                                            # frame is already one less then blenders frame
                                            fw('\n\t\t\t\t\t\t\t%i,%.15f,L' % (fbx_time(frame), val))

                                if i == 0:
                                    fw('\n\t\t\t\t\t\tColor: 1,0,0')
                                elif i == 1:
                                    fw('\n\t\t\t\t\t\tColor: 0,1,0')
                                elif i == 2:
                                    fw('\n\t\t\t\t\t\tColor: 0,0,1')

                                fw('\n\t\t\t\t\t}')
                            fw('\n\t\t\t\t\tLayerType: %i' % (TX_LAYER + 1))
                            fw('\n\t\t\t\t}')

                        # ---------------

                        fw('\n\t\t\t}')
                        fw('\n\t\t}')

            # end the take
            fw('\n\t}')

            # end action loop. set original actions
            # do this after every loop in case actions effect eachother.
            for my_arm in ob_arms:
                if my_arm.blenObject.animation_data:
                    my_arm.blenObject.animation_data.action = my_arm.blenAction

        fw('\n}')

        scene.frame_set(frame_orig)

    else:
        # no animation
        fw('\n;Takes and animation section')
        fw('\n;----------------------------------------------------')
        fw('\n')
        fw('\nTakes:  {')
        fw('\n\tCurrent: ""')
        fw('\n}')

    # write meshes animation
    #for obname, ob, mtx, me, mats, arm, armname in ob_meshes:

    # Clear mesh data Only when writing with modifiers applied
    for me in meshes_to_clear:
        bpy.data.meshes.remove(me)

    # --------------------------- Footer
    if world:
        m = world.mist_settings
        has_mist = m.use_mist
        mist_intense = m.intensity
        mist_start = m.start
        mist_end = m.depth
        # mist_height = m.height  # UNUSED
        world_hor = world.horizon_color
    else:
        has_mist = mist_intense = mist_start = mist_end = 0
        world_hor = 0, 0, 0

    fw('\n;Version 5 settings')
    fw('\n;------------------------------------------------------------------')
    fw('\n')
    fw('\nVersion5:  {')
    fw('\n\tAmbientRenderSettings:  {')
    fw('\n\t\tVersion: 101')
    fw('\n\t\tAmbientLightColor: %.1f,%.1f,%.1f,0' % tuple(world_amb))
    fw('\n\t}')
    fw('\n\tFogOptions:  {')
    fw('\n\t\tFogEnable: %i' % has_mist)
    fw('\n\t\tFogMode: 0')
    fw('\n\t\tFogDensity: %.3f' % mist_intense)
    fw('\n\t\tFogStart: %.3f' % mist_start)
    fw('\n\t\tFogEnd: %.3f' % mist_end)
    fw('\n\t\tFogColor: %.1f,%.1f,%.1f,1' % tuple(world_hor))
    fw('\n\t}')
    fw('\n\tSettings:  {')
    fw('\n\t\tFrameRate: "%i"' % int(fps))
    fw('\n\t\tTimeFormat: 1')
    fw('\n\t\tSnapOnFrames: 0')
    fw('\n\t\tReferenceTimeIndex: -1')
    fw('\n\t\tTimeLineStartTime: %i' % fbx_time(start - 1))
    fw('\n\t\tTimeLineStopTime: %i' % fbx_time(end - 1))
    fw('\n\t}')
    fw('\n\tRendererSetting:  {')
    fw('\n\t\tDefaultCamera: "Producer Perspective"')
    fw('\n\t\tDefaultViewingMode: 0')
    fw('\n\t}')
    fw('\n}')
    fw('\n')

    # XXX, shouldnt be global!
    for mapping in (sane_name_mapping_ob,
                    sane_name_mapping_ob_unique,
                    sane_name_mapping_mat,
                    sane_name_mapping_tex,
                    sane_name_mapping_take,
                    sane_name_mapping_group,
                    ):
        mapping.clear()
    del mapping

    del ob_arms[:]
    del ob_bones[:]
    del ob_cameras[:]
    del ob_lights[:]
    del ob_meshes[:]
    del ob_null[:]

    file.close()

    # copy all collected files.
    bpy_extras.io_utils.path_reference_copy(copy_set)

    print('export finished in %.4f sec.' % (time.clock() - start_time))
    return {'FINISHED'}


# defaults for applications, currently only unity but could add others.
def defaults_unity3d():
    return dict(global_matrix=Matrix.Rotation(-math.pi / 2.0, 4, 'X'),
                use_selection=False,
                object_types={'ARMATURE', 'EMPTY', 'MESH'},
                use_mesh_modifiers=True,
                use_armature_deform_only=True,
                use_anim=True,
                use_anim_optimize=False,
                use_anim_action_all=True,
                batch_mode='OFF',
                use_default_take=True,
                )


def save(operator, context,
         filepath="",
         use_selection=False,
         batch_mode='OFF',
         use_batch_own_dir=False,
         **kwargs
         ):

    if bpy.ops.object.mode_set.poll():
        bpy.ops.object.mode_set(mode='OBJECT')

    if batch_mode == 'OFF':
        kwargs_mod = kwargs.copy()
        if use_selection:
            kwargs_mod["context_objects"] = context.selected_objects
        else:
            kwargs_mod["context_objects"] = context.scene.objects

        return save_single(operator, context.scene, filepath, **kwargs_mod)
    else:
        fbxpath = filepath

        prefix = os.path.basename(fbxpath)
        if prefix:
            fbxpath = os.path.dirname(fbxpath)

        if not fbxpath.endswith(os.sep):
            fbxpath += os.sep

        if batch_mode == 'GROUP':
            data_seq = bpy.data.groups
        else:
            data_seq = bpy.data.scenes

        # call this function within a loop with BATCH_ENABLE == False
        # no scene switching done at the moment.
        # orig_sce = context.scene

        new_fbxpath = fbxpath  # own dir option modifies, we need to keep an original
        for data in data_seq:  # scene or group
            newname = prefix + bpy.path.clean_name(data.name)

            if use_batch_own_dir:
                new_fbxpath = fbxpath + newname + os.sep
                # path may already exist
                # TODO - might exist but be a file. unlikely but should probably account for it.

                if not os.path.exists(new_fbxpath):
                    os.makedirs(new_fbxpath)

            filepath = new_fbxpath + newname + '.fbx'

            print('\nBatch exporting %s as...\n\t%r' % (data, filepath))

            # XXX don't know what to do with this, probably do the same? (Arystan)
            if batch_mode == 'GROUP':  # group
                # group, so objects update properly, add a dummy scene.
                scene = bpy.data.scenes.new(name="FBX_Temp")
                scene.layers = [True] * 20
                # bpy.data.scenes.active = scene # XXX, cant switch
                for ob_base in data.objects:
                    scene.objects.link(ob_base)

                scene.update()
            else:
                scene = data

                # TODO - BUMMER! Armatures not in the group wont animate the mesh

            # else:  # scene
            #     data_seq.active = data

            # Call self with modified args
            # Dont pass batch options since we already usedt them
            kwargs_batch = kwargs.copy()

            kwargs_batch["context_objects"] = data.objects

            save_single(operator, scene, filepath, **kwargs_batch)

            if batch_mode == 'GROUP':
                # remove temp group scene
                bpy.data.scenes.remove(scene)

        # no active scene changing!
        # bpy.data.scenes.active = orig_sce

        return {'FINISHED'}  # so the script wont run after we have batch exported.

# APPLICATION REQUIREMENTS
# Please update the lists for UDK, Unity, XNA etc. on the following web page:
#   http://wiki.blender.org/index.php/Dev:2.5/Py/Scripts/Import-Export/UnifiedFBX

# XNA FBX Requirements (JCB 29 July 2011)
# - Armature must be parented to the scene
# - Armature must be a 'Limb' never a 'null'.  This is in several places.
# - First bone must be parented to the armature.
# - Rotation must be completely disabled including
#       always returning the original matrix in In object_tx().
#       It is the animation that gets distorted during rotation!
# - Lone edges cause intermittent errors in the XNA content pipeline!
#       I have added a warning message and excluded them.
# - Bind pose must be included with the 'MESH'
# Typical settings for XNA export
#   No Cameras, No Lamps, No Edges, No face smoothing, No Default_Take, Armature as bone, Disable rotation

# NOTE TO Campbell -
#   Can any or all of the following notes be removed because some have been here for a long time? (JCB 27 July 2011)
# NOTES (all line numbers correspond to original export_fbx.py (under release/scripts)
# - get rid of bpy.path.clean_name somehow
# + get rid of BPyObject_getObjectArmature, move it in RNA?
# - implement all BPyMesh_* used here with RNA
# - getDerivedObjects is not fully replicated with .dupli* funcs
# - don't know what those colbits are, do we need them? they're said to be deprecated in DNA_object_types.h: 1886-1893
# - no hq normals: 1900-1901
