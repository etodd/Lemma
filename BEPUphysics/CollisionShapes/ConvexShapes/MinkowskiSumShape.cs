using System;
using System.Collections.Generic;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;
using BEPUphysics.DataStructures;

namespace BEPUphysics.CollisionShapes.ConvexShapes
{
    ///<summary>
    /// A shape associated with an orientation.
    ///</summary>
    public struct OrientedConvexShapeEntry
    {
        ///<summary>
        /// The entry's shape.
        ///</summary>
        public ConvexShape CollisionShape;
        ///<summary>
        /// The entry's orientation.
        ///</summary>
        public Quaternion Orientation;

        ///<summary>
        /// Constructs a new entry.
        ///</summary>
        ///<param name="orientation">Orientation of the entry.</param>
        ///<param name="shape">Shape of the entry.</param>
        public OrientedConvexShapeEntry(Quaternion orientation, ConvexShape shape)
        {
            Orientation = orientation;
            CollisionShape = shape;
        }

        ///<summary>
        /// Constructs a new entry with identity orientation.
        ///</summary>
        ///<param name="shape">Shape of the entry.</param>
        public OrientedConvexShapeEntry(ConvexShape shape)
        {
            Orientation = Quaternion.Identity;
            CollisionShape = shape;
        }
    }
    ///<summary>
    /// A shape composed of the pointwise summation of all points in child shapes.
    /// For example, the minkowski sum of two spheres would be a sphere with the radius of both spheres combined.
    /// The minkowski sum of a box and a sphere would be a rounded box.
    ///</summary>
    public class MinkowskiSumShape : ConvexShape
    {
        ObservableList<OrientedConvexShapeEntry> shapes = new ObservableList<OrientedConvexShapeEntry>();
        ///<summary>
        /// Gets the list of shapes in the minkowski sum.
        ///</summary>
        public ObservableList<OrientedConvexShapeEntry> Shapes
        {
            get
            {
                return shapes;
            }
        }

        //Local offset is needed to ensure that the minkowski sum is centered on the local origin.
        Vector3 localOffset;
        ///<summary>
        /// Gets the local offset of the elements in the minkowski sum.
        /// This is required because convex shapes need to be centered on their local origin.
        ///</summary>
        public Vector3 LocalOffset
        {
            get
            {
                return localOffset;
            }
        }

        /// <summary>
        /// Constructs a minkowski sum shape.
        /// A minkowski sum can be created from more than two objects; use the other constructors.
        /// The sum will be recentered on its local origin.
        /// </summary>
        /// <param name="firstShape">First entry in the sum.</param>
        /// <param name="secondShape">Second entry in the sum.</param>
        /// <param name="center">Center of the minkowski sum computed pre-recentering.</param>
        public MinkowskiSumShape(OrientedConvexShapeEntry firstShape, OrientedConvexShapeEntry secondShape, out Vector3 center)
            : this(firstShape, secondShape)
        {
            center = -localOffset;
        }

        /// <summary>
        /// Constructs a minkowski sum shape.
        /// The sum will be recentered on its local origin.
        /// </summary>
        /// <param name="shapeEntries">Entries composing the minkowski sum.</param>
        /// <param name="center">Center of the minkowski sum computed pre-recentering.</param>
        public MinkowskiSumShape(IList<OrientedConvexShapeEntry> shapeEntries, out Vector3 center)
            : this(shapeEntries)
        {
            center = -localOffset;
        }

        /// <summary>
        /// Constructs a minkowski sum shape.
        /// A minkowski sum can be created from more than two objects; use the other constructors.
        /// The sum will be recentered on its local origin.  The computed center is outputted by the other constructor.
        /// </summary>
        /// <param name="firstShape">First entry in the sum.</param>
        /// <param name="secondShape">Second entry in the sum.</param>
        public MinkowskiSumShape(OrientedConvexShapeEntry firstShape, OrientedConvexShapeEntry secondShape)
        {
            shapes.Add(firstShape);
            shapes.Add(secondShape);
            shapes.Changed += ShapesChanged;
            localOffset = -ComputeCenter();
            OnShapeChanged();
        }

        /// <summary>
        /// Constructs a minkowski sum shape.
        /// The sum will be recentered on its local origin.  The computed center is outputted by the other constructor.
        /// </summary>
        /// <param name="shapeEntries">Entries composing the minkowski sum.</param>
        public MinkowskiSumShape(IList<OrientedConvexShapeEntry> shapeEntries)
        {
            if (shapeEntries.Count == 0)
                throw new Exception("Cannot create a wrapped shape with no contained shapes.");
            for (int i = 0; i < shapeEntries.Count; i++)
            {
                shapes.Add(shapeEntries[i]);
            }
            shapes.Changed += ShapesChanged;
            localOffset = -ComputeCenter();
            OnShapeChanged();
        }

        void ShapesChanged(ObservableList<OrientedConvexShapeEntry> list)
        {
            OnShapeChanged();
            //Computing the center uses extreme point calculations.
            //Extreme point calculations make use of the localOffset.
            //So, set the local offset to zero before doing the computation.
            //The new offset is then computed.
            localOffset = new Vector3();
            localOffset = -ComputeCenter();
        }


        ///<summary>
        /// Gets the extreme point of the shape in local space in a given direction.
        ///</summary>
        ///<param name="direction">Direction to find the extreme point in.</param>
        ///<param name="extremePoint">Extreme point on the shape.</param>
        public override void GetLocalExtremePointWithoutMargin(ref Vector3 direction, out Vector3 extremePoint)
        {
            var transform = new RigidTransform {Orientation = shapes.list.Elements[0].Orientation};
            shapes.list.Elements[0].CollisionShape.GetExtremePoint(direction, ref transform, out extremePoint);
            for (int i = 1; i < shapes.list.count; i++)
            {
                Vector3 temp;
                transform.Orientation = shapes.list.Elements[i].Orientation;
                shapes.list.Elements[i].CollisionShape.GetExtremePoint(direction, ref transform, out temp);
                Vector3.Add(ref extremePoint, ref temp, out extremePoint);
            }
            Vector3.Add(ref extremePoint, ref localOffset, out extremePoint);
        }

        ///<summary>
        /// Computes the minimum radius of the shape.
        /// This is often smaller than the actual minimum radius;
        /// it is simply an approximation that avoids overestimating.
        ///</summary>
        ///<returns>Minimum radius of the shape.</returns>
        public override float ComputeMinimumRadius()
        {
            float minRadius = 0;
            for (int i = 0; i < shapes.Count; i++)
            {
                minRadius += shapes.list.Elements[i].CollisionShape.ComputeMinimumRadius();
            }
            return minRadius + collisionMargin;
        }


        /// <summary>
        /// Computes the maximum radius of the shape.
        /// This is often larger than the actual maximum radius;
        /// it is simply an approximation that avoids underestimating.
        /// </summary>
        /// <returns>Maximum radius of the shape.</returns>
        public override float ComputeMaximumRadius()
        {
            //This can overestimate the actual maximum radius, but such is the defined behavior of the ComputeMaximumRadius function.  It's not exact; it's an upper bound on the actual maximum.
            float maxRadius = 0;
            for (int i = 0; i < shapes.Count; i++)
            {
                maxRadius += shapes.list.Elements[i].CollisionShape.ComputeMaximumRadius();
            }
            return maxRadius + collisionMargin;
        }


        /// <summary>
        /// Retrieves an instance of an EntityCollidable that uses this EntityShape.  Mainly used by compound bodies.
        /// </summary>
        /// <returns>EntityCollidable that uses this shape.</returns>
        public override EntityCollidable GetCollidableInstance()
        {
            return new ConvexCollidable<MinkowskiSumShape>(this);
        }


    }
}
