using System.Collections.Generic;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.Collidables.Events;
using BEPUphysics.CollisionShapes;
using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;
using BEPUphysics.ResourceManagement;
using BEPUphysics.DataStructures;
using BEPUphysics.Materials;
using System.Collections.ObjectModel;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.CollisionTests.CollisionAlgorithms;
using System;

namespace BEPUphysics.Collidables.MobileCollidables
{
    ///<summary>
    /// Collidable used by compound shapes.
    ///</summary>
    public class MobileMeshCollidable : EntityCollidable
    {
        ///<summary>
        /// Gets the shape of the collidable.
        ///</summary>
        public new MobileMeshShape Shape
        {
            get
            {
                return (MobileMeshShape)shape;
            }
        }

        /// <summary>
        /// Constructs a new mobile mesh collidable.
        /// </summary>
        /// <param name="shape">Shape to use in the collidable.</param>
        public MobileMeshCollidable(MobileMeshShape shape)
            : base(shape)
        {
            Events = new ContactEventManager<EntityCollidable>();
        }



        internal bool improveBoundaryBehavior = true;
        /// <summary>
        /// Gets or sets whether or not the collision system should attempt to improve contact behavior at the boundaries between triangles.
        /// This has a slight performance cost, but prevents objects sliding across a triangle boundary from 'bumping,' and otherwise improves
        /// the robustness of contacts at edges and vertices.
        /// </summary>
        public bool ImproveBoundaryBehavior
        {
            get
            {
                return improveBoundaryBehavior;
            }
            set
            {
                improveBoundaryBehavior = value;
            }
        }

        protected internal override void UpdateBoundingBoxInternal(float dt)
        {
            Shape.GetBoundingBox(ref worldTransform, out boundingBox);

            //This DOES NOT EXPAND the local hierarchy.
            //The bounding boxes of queries against the local hierarchy
            //should be expanded using the relative velocity.
            ExpandBoundingBox(ref boundingBox, dt);
        }




        /// <summary>
        /// Tests a ray against the entry.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="maximumLength">Maximum length, in units of the ray's direction's length, to test.</param>
        /// <param name="rayHit">Hit location of the ray on the entry, if any.</param>
        /// <returns>Whether or not the ray hit the entry.</returns>
        public override bool RayCast(Ray ray, float maximumLength, out RayHit rayHit)
        {
            //Put the ray into local space.
            Ray localRay;
            Matrix3X3 orientation;
            Matrix3X3.CreateFromQuaternion(ref worldTransform.Orientation, out orientation);
            Matrix3X3.TransformTranspose(ref ray.Direction, ref orientation, out localRay.Direction);
            Vector3.Subtract(ref ray.Position, ref worldTransform.Position, out localRay.Position);
            Matrix3X3.TransformTranspose(ref localRay.Position, ref orientation, out localRay.Position);


            if (Shape.solidity == MobileMeshSolidity.Solid)
            {
                //Find all hits.  Use the count to determine the ray started inside or outside.
                //If it starts inside and we're in 'solid' mode, then return the ray start.
                //The raycast must be of infinite length at first.  This allows it to determine
                //if it is inside or outside.
                if (Shape.IsLocalRayOriginInMesh(ref localRay, out rayHit))
                {
                    //It was inside!
                    rayHit = new RayHit() { Location = ray.Position, Normal = Vector3.Zero, T = 0 };
                    return true;

                }
                else
                {
                    if (rayHit.T < maximumLength)
                    {
                        //Transform the hit into world space.
                        Vector3.Multiply(ref ray.Direction, rayHit.T, out rayHit.Location);
                        Vector3.Add(ref rayHit.Location, ref ray.Position, out rayHit.Location);
                        Matrix3X3.Transform(ref rayHit.Normal, ref orientation, out rayHit.Normal);
                    }
                    else
                    {
                        //The hit was too far away, or there was no hit (in which case T would be float.MaxValue).
                        return false;
                    }
                    return true;
                }
            }
            else
            {
                //Just do a normal raycast since the object isn't solid.
                TriangleSidedness sidedness;
                switch (Shape.solidity)
                {
                    case MobileMeshSolidity.Clockwise:
                        sidedness = TriangleSidedness.Clockwise;
                        break;
                    case MobileMeshSolidity.Counterclockwise:
                        sidedness = TriangleSidedness.Counterclockwise;
                        break;
                    default:
                        sidedness = TriangleSidedness.DoubleSided;
                        break;
                }
                if (Shape.TriangleMesh.RayCast(localRay, maximumLength, sidedness, out rayHit))
                {
                    //Transform the hit into world space.
                    Vector3.Multiply(ref ray.Direction, rayHit.T, out rayHit.Location);
                    Vector3.Add(ref rayHit.Location, ref ray.Position, out rayHit.Location);
                    Matrix3X3.Transform(ref rayHit.Normal, ref orientation, out rayHit.Normal);
                    return true;
                }
            }
            rayHit = new RayHit();
            return false;
        }

        ///<summary>
        /// Tests a ray against the surface of the mesh.  This does not take into account solidity.
        ///</summary>
        ///<param name="ray">Ray to test.</param>
        ///<param name="maximumLength">Maximum length of the ray to test; in units of the ray's direction's length.</param>
        ///<param name="sidedness">Sidedness to use during the ray cast.  This does not have to be the same as the mesh's sidedness.</param>
        ///<param name="rayHit">The hit location of the ray on the mesh, if any.</param>
        ///<returns>Whether or not the ray hit the mesh.</returns>
        public bool RayCast(Ray ray, float maximumLength, TriangleSidedness sidedness, out RayHit rayHit)
        {
            //Put the ray into local space.
            Ray localRay;
            Matrix3X3 orientation;
            Matrix3X3.CreateFromQuaternion(ref worldTransform.Orientation, out orientation);
            Matrix3X3.TransformTranspose(ref ray.Direction, ref orientation, out localRay.Direction);
            Vector3.Subtract(ref ray.Position, ref worldTransform.Position, out localRay.Position);
            Matrix3X3.TransformTranspose(ref localRay.Position, ref orientation, out localRay.Position);

            if (Shape.TriangleMesh.RayCast(localRay, maximumLength, sidedness, out rayHit))
            {
                //Transform the hit into world space.
                Vector3.Multiply(ref ray.Direction, rayHit.T, out rayHit.Location);
                Vector3.Add(ref rayHit.Location, ref ray.Position, out rayHit.Location);
                Matrix3X3.Transform(ref rayHit.Normal, ref orientation, out rayHit.Normal);
                return true;
            }
            rayHit = new RayHit();
            return false;
        }

        /// <summary>
        /// Casts a convex shape against the collidable.
        /// </summary>
        /// <param name="castShape">Shape to cast.</param>
        /// <param name="startingTransform">Initial transform of the shape.</param>
        /// <param name="sweep">Sweep to apply to the shape.</param>
        /// <param name="hit">Hit data, if any.</param>
        /// <returns>Whether or not the cast hit anything.</returns>
        public override bool ConvexCast(ConvexShape castShape, ref RigidTransform startingTransform, ref Vector3 sweep, out RayHit hit)
        {
            if (Shape.solidity == MobileMeshSolidity.Solid)
            {
                //If the convex cast is inside the mesh and the mesh is solid, it should return t = 0.
                var ray = new Ray() { Position = startingTransform.Position, Direction = Toolbox.UpVector };
                if (Shape.IsLocalRayOriginInMesh(ref ray, out hit))
                {

                    hit = new RayHit() { Location = startingTransform.Position, Normal = new Vector3(), T = 0 };
                    return true;
                }
            }
            hit = new RayHit();
            BoundingBox boundingBox;
            var transform = new AffineTransform {Translation = worldTransform.Position};
            Matrix3X3.CreateFromQuaternion(ref worldTransform.Orientation, out transform.LinearTransform);
            castShape.GetSweptLocalBoundingBox(ref startingTransform, ref transform, ref sweep, out boundingBox);
            var tri = Resources.GetTriangle();
            var hitElements = Resources.GetIntList();
            if (this.Shape.TriangleMesh.Tree.GetOverlaps(boundingBox, hitElements))
            {
                hit.T = float.MaxValue;
                for (int i = 0; i < hitElements.Count; i++)
                {
                    Shape.TriangleMesh.Data.GetTriangle(hitElements[i], out tri.vA, out tri.vB, out tri.vC);
                    AffineTransform.Transform(ref tri.vA, ref transform, out tri.vA);
                    AffineTransform.Transform(ref tri.vB, ref transform, out tri.vB);
                    AffineTransform.Transform(ref tri.vC, ref transform, out tri.vC);
                    Vector3 center;
                    Vector3.Add(ref tri.vA, ref tri.vB, out center);
                    Vector3.Add(ref center, ref tri.vC, out center);
                    Vector3.Multiply(ref center, 1f / 3f, out center);
                    Vector3.Subtract(ref tri.vA, ref center, out tri.vA);
                    Vector3.Subtract(ref tri.vB, ref center, out tri.vB);
                    Vector3.Subtract(ref tri.vC, ref center, out tri.vC);
                    tri.maximumRadius = tri.vA.LengthSquared();
                    float radius = tri.vB.LengthSquared();
                    if (tri.maximumRadius < radius)
                        tri.maximumRadius = radius;
                    radius = tri.vC.LengthSquared();
                    if (tri.maximumRadius < radius)
                        tri.maximumRadius = radius;
                    tri.maximumRadius = (float)Math.Sqrt(tri.maximumRadius);
                    tri.collisionMargin = 0;
                    var triangleTransform = new RigidTransform {Orientation = Quaternion.Identity, Position = center};
                    RayHit tempHit;
                    if (MPRToolbox.Sweep(castShape, tri, ref sweep, ref Toolbox.ZeroVector, ref startingTransform, ref triangleTransform, out tempHit) && tempHit.T < hit.T)
                    {
                        hit = tempHit;
                    }
                }
                tri.maximumRadius = 0;
                Resources.GiveBack(tri);
                Resources.GiveBack(hitElements);
                return hit.T != float.MaxValue;
            }
            Resources.GiveBack(tri);
            Resources.GiveBack(hitElements);
            return false;
        }
    }



}
