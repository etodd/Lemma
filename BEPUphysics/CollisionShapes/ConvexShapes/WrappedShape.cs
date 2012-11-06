using System;
using System.Collections.Generic;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;
using BEPUphysics.DataStructures;

namespace BEPUphysics.CollisionShapes.ConvexShapes
{
    ///<summary>
    /// Convex shape entry to a WrappedShape.
    ///</summary>
    public struct ConvexShapeEntry
    {
        /// <summary>
        /// Convex shape of the entry.
        /// </summary>
        public ConvexShape CollisionShape;
        /// <summary>
        /// Local transform of the entry.
        /// </summary>
        public RigidTransform Transform;

        /// <summary>
        /// Constructs a convex shape entry.
        /// </summary>
        /// <param name="position">Local position of the entry.</param>
        /// <param name="shape">Shape of the entry.</param>
        public ConvexShapeEntry(Vector3 position, ConvexShape shape)
        {
            Transform = new RigidTransform(position);
            CollisionShape = shape;
        }

        /// <summary>
        /// Constructs a convex shape entry.
        /// </summary>
        /// <param name="orientation">Local orientation of the entry.</param>
        /// <param name="shape">Shape of the entry.</param>
        public ConvexShapeEntry(Quaternion orientation, ConvexShape shape)
        {
            Transform = new RigidTransform(orientation);
            CollisionShape = shape;
        }

        /// <summary>
        /// Constructs a convex shape entry.
        /// </summary>
        /// <param name="transform">Local transform of the entry.</param>
        /// <param name="shape">Shape of the entry.</param>
        public ConvexShapeEntry(RigidTransform transform, ConvexShape shape)
        {
            Transform = transform;
            CollisionShape = shape;
        }

        ///<summary>
        /// Constructs a convex shape entry with identity transformation.
        ///</summary>
        ///<param name="shape">Shape of the entry.</param>
        public ConvexShapeEntry(ConvexShape shape)
        {
            Transform = RigidTransform.Identity;
            CollisionShape = shape;
        }
    }
    ///<summary>
    /// Shape that wraps other convex shapes in a convex hull.
    /// One way to think of it is to collect a bunch of items and wrap shrinkwrap around them.
    /// That surface is the shape of the WrappedShape.
    ///</summary>
    public class WrappedShape : ConvexShape
    {
        ObservableList<ConvexShapeEntry> shapes = new ObservableList<ConvexShapeEntry>();
        ///<summary>
        /// Gets the shapes in wrapped shape.
        ///</summary>
        public ObservableList<ConvexShapeEntry> Shapes
        {
            get
            {
                return shapes;
            }
        }

        void Recenter(out Vector3 center)
        {
            //When first constructed, a wrapped shape may not actually be centered on its local origin.
            //It is helpful to many systems if this is addressed.
            center = ComputeCenter();
            for (int i = 0; i < shapes.Count; i++)
            {
                shapes.list.Elements[i].Transform.Position -= center;
            }
        }

        ///<summary>
        /// Constructs a wrapped shape.
        /// A constructor is also available which takes a list of objects rather than just a pair.
        /// The shape will be recentered.  If the center is needed, use the other constructor.
        ///</summary>
        ///<param name="firstShape">First shape in the wrapped shape.</param>
        ///<param name="secondShape">Second shape in the wrapped shape.</param>
        public WrappedShape(ConvexShapeEntry firstShape, ConvexShapeEntry secondShape)
        {
            shapes.Add(firstShape);
            shapes.Add(secondShape);

            Vector3 v;
            Recenter(out v);

            shapes.Changed += ShapesChanged;
        }

        ///<summary>
        /// Constructs a wrapped shape.
        /// A constructor is also available which takes a list of objects rather than just a pair.
        /// The shape will be recentered.
        ///</summary>
        ///<param name="firstShape">First shape in the wrapped shape.</param>
        ///<param name="secondShape">Second shape in the wrapped shape.</param>
        ///<param name="center">Center of the shape before recentering..</param>
        public WrappedShape(ConvexShapeEntry firstShape, ConvexShapeEntry secondShape, out Vector3 center)
        {
            shapes.Add(firstShape);
            shapes.Add(secondShape);

            Recenter(out center);

            shapes.Changed += ShapesChanged;
            OnShapeChanged();
        }

        ///<summary>
        /// Constructs a wrapped shape.
        /// The shape will be recentered; if the center is needed, use the other constructor.
        ///</summary>
        ///<param name="shapeEntries">Shape entries used to construct the shape.</param>
        ///<exception cref="Exception">Thrown when the shape list is empty.</exception>
        public WrappedShape(IList<ConvexShapeEntry> shapeEntries)
        {
            if (shapeEntries.Count == 0)
                throw new Exception("Cannot create a wrapped shape with no contained shapes.");
            for (int i = 0; i < shapeEntries.Count; i++)
            {
                shapes.Add(shapeEntries[i]);
            }
            Vector3 v;
            Recenter(out v);

            shapes.Changed += ShapesChanged;
            OnShapeChanged();
        }

        ///<summary>
        /// Constructs a wrapped shape.
        /// The shape will be recentered.
        ///</summary>
        ///<param name="shapeEntries">Shape entries used to construct the shape.</param>
        /// <param name="center">Center of the shape before recentering.</param>
        ///<exception cref="Exception">Thrown when the shape list is empty.</exception>
        public WrappedShape(IList<ConvexShapeEntry> shapeEntries, out Vector3 center)
        {
            if (shapeEntries.Count == 0)
                throw new Exception("Cannot create a wrapped shape with no contained shapes.");
            for (int i = 0; i < shapeEntries.Count; i++)
            {
                shapes.Add(shapeEntries[i]);
            }
            Recenter(out center);

            shapes.Changed += ShapesChanged;
            OnShapeChanged();
        }

        void ShapesChanged(ObservableList<ConvexShapeEntry> list)
        {
            OnShapeChanged();
        }


        /// <summary>
        /// Gets the bounding box of the shape given a transform.
        /// </summary>
        /// <param name="shapeTransform">Transform to use.</param>
        /// <param name="boundingBox">Bounding box of the transformed shape.</param>
        public override void GetBoundingBox(ref RigidTransform shapeTransform, out BoundingBox boundingBox)
        {
            RigidTransform subTransform;
            RigidTransform.Transform(ref shapes.list.Elements[0].Transform, ref shapeTransform, out subTransform);
            shapes.list.Elements[0].CollisionShape.GetBoundingBox(ref subTransform, out boundingBox);
            for (int i = 1; i < shapes.list.count; i++)
            {
                RigidTransform.Transform(ref shapes.list.Elements[i].Transform, ref shapeTransform, out subTransform);
                BoundingBox toMerge;
                shapes.list.Elements[i].CollisionShape.GetBoundingBox(ref subTransform, out toMerge);
                BoundingBox.CreateMerged(ref boundingBox, ref toMerge, out boundingBox);
            }

            boundingBox.Min.X -= collisionMargin;
            boundingBox.Min.Y -= collisionMargin;
            boundingBox.Min.Z -= collisionMargin;

            boundingBox.Max.X += collisionMargin;
            boundingBox.Max.Y += collisionMargin;
            boundingBox.Max.Z += collisionMargin;
        }


        ///<summary>
        /// Gets the extreme point of the shape in local space in a given direction.
        ///</summary>
        ///<param name="direction">Direction to find the extreme point in.</param>
        ///<param name="extremePoint">Extreme point on the shape.</param>
        public override void GetLocalExtremePointWithoutMargin(ref Vector3 direction, out Vector3 extremePoint)
        {
            shapes.list.Elements[0].CollisionShape.GetExtremePoint(direction, ref shapes.list.Elements[0].Transform, out extremePoint);
            float maxDot;
            Vector3.Dot(ref extremePoint, ref direction, out maxDot);
            for (int i = 1; i < shapes.list.count; i++)
            {
                float dot;
                Vector3 temp;

                shapes.list.Elements[i].CollisionShape.GetExtremePoint(direction, ref shapes.list.Elements[i].Transform, out temp);
                Vector3.Dot(ref direction, ref temp, out dot);
                if (dot > maxDot)
                {
                    extremePoint = temp;
                    maxDot = dot;
                }
            }
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
                float radius = shapes.list.Elements[i].CollisionShape.ComputeMaximumRadius() +
                               shapes.list.Elements[i].Transform.Position.Length();
                if (radius > maxRadius)
                    maxRadius = radius;
            }
            return maxRadius + collisionMargin;
        }
        public override float ComputeMinimumRadius()
        {
            //Could also use the tetrahedron approximation approach.
            float minRadius = 0;
            for (int i = 0; i < shapes.Count; i++)
            {
                float radius = shapes.list.Elements[i].CollisionShape.ComputeMinimumRadius();
                if (radius < minRadius)
                    minRadius = radius;
            }
            return minRadius + collisionMargin;
        }

        /// <summary>
        /// Retrieves an instance of an EntityCollidable that uses this EntityShape.  Mainly used by compound bodies.
        /// </summary>
        /// <returns>EntityCollidable that uses this shape.</returns>
        public override EntityCollidable GetCollidableInstance()
        {
            return new ConvexCollidable<WrappedShape>(this);
        }
    }
}
