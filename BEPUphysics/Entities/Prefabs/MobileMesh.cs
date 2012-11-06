using System;
using System.Collections.Generic;
using BEPUphysics.Collidables.MobileCollidables;
using Microsoft.Xna.Framework;
using BEPUphysics.DataStructures;
using BEPUphysics.CollisionShapes;
using BEPUphysics.MathExtensions;
using System.Collections.ObjectModel;
using BEPUphysics.CollisionShapes.ConvexShapes;

namespace BEPUphysics.Entities.Prefabs
{
    /// <summary>
    /// Acts as a grouping of multiple other objects.  Can be used to form physically simulated concave shapes.
    /// </summary>
    public class MobileMesh : Entity<MobileMeshCollidable>
    {

        /// <summary>
        /// Creates a new kinematic MobileMesh.
        /// </summary>
        /// <param name="vertices">Vertices in the mesh.</param>
        /// <param name="indices">Indices of the mesh.</param>
        /// <param name="localTransform">Affine transform to apply to the vertices.</param>
        /// <param name="solidity">Solidity/sidedness of the mesh.  "Solid" is only permitted if the mesh is closed.</param>
        public MobileMesh(Vector3[] vertices, int[] indices, AffineTransform localTransform, MobileMeshSolidity solidity)
        {
            ShapeDistributionInformation info;
            var shape = new MobileMeshShape(vertices, indices, localTransform, solidity, out info);
            Initialize(new MobileMeshCollidable(shape));
            Position = info.Center;
        }



        /// <summary>
        /// Creates a new dynamic MobileMesh.
        /// </summary>
        /// <param name="vertices">Vertices in the mesh.</param>
        /// <param name="indices">Indices of the mesh.</param>
        /// <param name="localTransform">Affine transform to apply to the vertices.</param>
        /// <param name="solidity">Solidity/sidedness of the mesh.  "Solid" is only permitted if the mesh is closed.</param>
        /// <param name="mass">Mass of the mesh.</param>
        public MobileMesh(Vector3[] vertices, int[] indices,  AffineTransform localTransform, MobileMeshSolidity solidity, float mass)
        {
            ShapeDistributionInformation info;
            var shape = new MobileMeshShape(vertices, indices, localTransform, solidity, out info);
            Matrix3X3 inertia;
            Matrix3X3.Multiply(ref info.VolumeDistribution, mass * InertiaHelper.InertiaTensorScale, out inertia);
            Initialize(new MobileMeshCollidable(shape), mass, inertia, info.Volume);
            Position = info.Center;
        }




    }


}