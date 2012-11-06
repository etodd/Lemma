using System;
using BEPUphysics.Collidables;
using BEPUphysics.Collidables.MobileCollidables;
using Microsoft.Xna.Framework;
using BEPUphysics.DataStructures;
using BEPUphysics.MathExtensions;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.CollisionShapes;
using BEPUphysics.ResourceManagement;
using BEPUphysics.CollisionTests.CollisionAlgorithms;
using System.Diagnostics;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using Microsoft.Xna.Framework.Input;

namespace BEPUphysics.CollisionTests.Manifolds
{
    ///<summary>
    /// Manages persistent contacts between a convex and an instanced mesh.
    ///</summary>
    public abstract class MobileMeshContactManifold : TriangleMeshConvexContactManifold
    {
        protected MobileMeshCollidable mesh;
        internal int parentContactCount;

        internal RawList<int> overlappedTriangles = new RawList<int>(4);

        ///<summary>
        /// Gets the mesh of the pair.
        ///</summary>
        public MobileMeshCollidable Mesh
        {
            get
            {
                return mesh;
            }
        }

        protected override RigidTransform MeshTransform
        {
            get { return mesh.worldTransform; }
        }

        //Expand the convex's bounding box to include the mobile mesh's movement.

        protected internal override int FindOverlappingTriangles(float dt)
        {
            BoundingBox boundingBox;
            AffineTransform transform = new AffineTransform(mesh.worldTransform.Orientation, mesh.worldTransform.Position);
            convex.Shape.GetLocalBoundingBox(ref convex.worldTransform, ref transform, out boundingBox);
            Vector3 transformedVelocity;
            //Compute the relative velocity with respect to the mesh.  The mesh's bounding tree is NOT expanded with velocity,
            //so whatever motion there is between the two objects needs to be included in the convex's bounding box.

            if (convex.entity != null)
                transformedVelocity = convex.entity.linearVelocity;
            else
                transformedVelocity = new Vector3();
            if (mesh.entity != null)
                Vector3.Subtract(ref transformedVelocity, ref mesh.entity.linearVelocity, out transformedVelocity);

            //The linear transform is known to be orientation only, so using the transpose is allowed.
            Matrix3X3.TransformTranspose(ref transformedVelocity, ref transform.LinearTransform, out transformedVelocity);
            Vector3.Multiply(ref transformedVelocity, dt, out transformedVelocity);

            if (transformedVelocity.X > 0)
                boundingBox.Max.X += transformedVelocity.X;
            else
                boundingBox.Min.X += transformedVelocity.X;

            if (transformedVelocity.Y > 0)
                boundingBox.Max.Y += transformedVelocity.Y;
            else
                boundingBox.Min.Y += transformedVelocity.Y;

            if (transformedVelocity.Z > 0)
                boundingBox.Max.Z += transformedVelocity.Z;
            else
                boundingBox.Min.Z += transformedVelocity.Z;

            mesh.Shape.TriangleMesh.Tree.GetOverlaps(boundingBox, overlappedTriangles);
            return overlappedTriangles.count;
        }

        protected override bool ConfigureTriangle(int i, out TriangleIndices indices)
        {
            MeshBoundingBoxTreeData data = mesh.Shape.TriangleMesh.Data;
            int triangleIndex = overlappedTriangles.Elements[i];
            data.GetTriangle(triangleIndex, out localTriangleShape.vA, out localTriangleShape.vB, out localTriangleShape.vC);
            AffineTransform transform;
            AffineTransform.CreateFromRigidTransform(ref mesh.worldTransform, out transform);
            AffineTransform.Transform(ref localTriangleShape.vA, ref transform, out localTriangleShape.vA);
            AffineTransform.Transform(ref localTriangleShape.vB, ref transform, out localTriangleShape.vB);
            AffineTransform.Transform(ref localTriangleShape.vC, ref transform, out localTriangleShape.vC);
            //In instanced meshes, the bounding box we found in local space could collect more triangles than strictly necessary.
            //By doing a second pass, we should be able to prune out quite a few of them.
            BoundingBox triangleAABB;
            Toolbox.GetTriangleBoundingBox(ref localTriangleShape.vA, ref localTriangleShape.vB, ref localTriangleShape.vC, out triangleAABB);
            bool toReturn;
            triangleAABB.Intersects(ref convex.boundingBox, out toReturn);
            if (!toReturn)
            {
                indices = new TriangleIndices();
                return false;
            }

            TriangleSidedness sidedness;
            switch (mesh.Shape.solidity)
            {
                case MobileMeshSolidity.Clockwise:
                    sidedness = TriangleSidedness.Clockwise;
                    break;
                case MobileMeshSolidity.Counterclockwise:
                    sidedness = TriangleSidedness.Counterclockwise;
                    break;
                case MobileMeshSolidity.DoubleSided:
                    sidedness = TriangleSidedness.DoubleSided;
                    break;
                default:
                    sidedness = mesh.Shape.solidSidedness;
                    break;
            }
            localTriangleShape.sidedness = sidedness;
            localTriangleShape.collisionMargin = 0;
            indices = new TriangleIndices()
            {
                A = data.indices[triangleIndex],
                B = data.indices[triangleIndex + 1],
                C = data.indices[triangleIndex + 2]
            };
            return true;

        }

        protected internal override void CleanUpOverlappingTriangles()
        {
            overlappedTriangles.Clear();
        }

        protected override bool UseImprovedBoundaryHandling
        {
            get { return mesh.improveBoundaryBehavior; }
        }

        float previousDepth;
        Vector3 lastValidConvexPosition;
        protected override void ProcessCandidates(RawValueList<ContactData> candidates)
        {
            if (candidates.count == 0 && parentContactCount == 0 && Mesh.Shape.solidity == MobileMeshSolidity.Solid)
            {

                //If there's no new contacts on the mesh and it's supposed to be a solid,
                //then we must check the convex for containment within the shell.
                //We already know that it's not on the shell, meaning that the shape is either
                //far enough away outside the shell that there's no contact (and we're done), 
                //or it's far enough inside the shell that the triangles cannot create contacts.

                //To find out which it is, raycast against the shell.

                Matrix3X3 orientation;
                Matrix3X3.CreateFromQuaternion(ref mesh.worldTransform.Orientation, out orientation);

                Ray ray;
                Vector3.Subtract(ref convex.worldTransform.Position, ref mesh.worldTransform.Position, out ray.Position);
                Matrix3X3.TransformTranspose(ref ray.Position, ref orientation, out ray.Position);

                //Cast from the current position back to the previous position.
                Vector3.Subtract(ref lastValidConvexPosition, ref ray.Position, out ray.Direction);
                float rayDirectionLength = ray.Direction.LengthSquared();
                if (rayDirectionLength < Toolbox.Epsilon)
                {
                    //The object may not have moved enough to normalize properly.  If so, choose something arbitrary.
                    //Try the direction from the center of the object to the convex's position.
                    ray.Direction = ray.Position;
                    rayDirectionLength = ray.Direction.LengthSquared();
                    if (rayDirectionLength < Toolbox.Epsilon)
                    {
                        //This is unlikely; just pick something completely arbitrary then.
                        ray.Direction = Vector3.Up;
                        rayDirectionLength = 1;
                    }
                }
                Vector3.Divide(ref ray.Direction, (float)Math.Sqrt(rayDirectionLength), out ray.Direction);


                RayHit hit;
                if (mesh.Shape.IsLocalRayOriginInMesh(ref ray, out hit))
                {
                    ContactData newContact = new ContactData {Id = 2};
                    //Give it a special id so that we know that it came from the inside.
                    Matrix3X3.Transform(ref ray.Position, ref orientation, out newContact.Position);
                    Vector3.Add(ref newContact.Position, ref mesh.worldTransform.Position, out newContact.Position);

                    newContact.Normal = hit.Normal;
                    newContact.Normal.Normalize();

                    float factor;
                    Vector3.Dot(ref ray.Direction, ref newContact.Normal, out factor);
                    newContact.PenetrationDepth = -factor * hit.T + convex.Shape.minimumRadius;

                    Matrix3X3.Transform(ref newContact.Normal, ref orientation, out newContact.Normal);

                    //Do not yet create a new contact.  Check to see if an 'inner contact' with id == 2 already exists.
                    bool addContact = true;
                    for (int i = 0; i < contacts.count; i++)
                    {
                        if (contacts.Elements[i].Id == 2)
                        {
                            contacts.Elements[i].Position = newContact.Position;
                            contacts.Elements[i].Normal = newContact.Normal;
                            contacts.Elements[i].PenetrationDepth = newContact.PenetrationDepth;
                            supplementData.Elements[i].BasePenetrationDepth = newContact.PenetrationDepth;
                            supplementData.Elements[i].LocalOffsetA = new Vector3();
                            supplementData.Elements[i].LocalOffsetB = ray.Position; //convex local position in mesh.
                            addContact = false;
                            break;
                        }
                    }
                    if (addContact && contacts.count == 0)
                        Add(ref newContact);
                    previousDepth = newContact.PenetrationDepth;
                }
                else
                {
                    //It's possible that we had a false negative.  The previous frame may have been in deep intersection, and this frame just failed to come to the same conclusion.
                    //If we set the target location to the current location, the object will never escape the mesh.  Instead, only do that if two frames agree that we are no longer colliding.
                    if (previousDepth > 0)
                    {
                        //We're not touching the mesh.
                        lastValidConvexPosition = ray.Position;
                    }
                    previousDepth = 0;

                }
            }
        }

        ///<summary>
        /// Cleans up the manifold.
        ///</summary>
        public override void CleanUp()
        {
            mesh = null;
            convex = null;
            parentContactCount = 0;
            base.CleanUp();
        }

        ///<summary>
        /// Initializes the manifold.
        ///</summary>
        ///<param name="newCollidableA">First collidable.</param>
        ///<param name="newCollidableB">Second collidable.</param>
        public override void Initialize(Collidable newCollidableA, Collidable newCollidableB)
        {
            convex = newCollidableA as ConvexCollidable;
            mesh = newCollidableB as MobileMeshCollidable;


            if (convex == null || mesh == null)
            {
                convex = newCollidableB as ConvexCollidable;
                mesh = newCollidableA as MobileMeshCollidable;
                if (convex == null || mesh == null)
                    throw new Exception("Inappropriate types used to initialize contact manifold.");
            }

        }

        UnsafeResourcePool<TriangleConvexPairTester> testerPool = new UnsafeResourcePool<TriangleConvexPairTester>();
        protected override void GiveBackTester(TrianglePairTester tester)
        {
            testerPool.GiveBack((TriangleConvexPairTester)tester);
        }

        protected override TrianglePairTester GetTester()
        {
            return testerPool.Take();
        }



    }
}
