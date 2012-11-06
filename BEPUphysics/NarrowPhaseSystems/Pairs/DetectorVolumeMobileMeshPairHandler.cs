using System;
using System.Collections.Generic;
using System.Diagnostics;
using BEPUphysics.CollisionShapes;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.CollisionTests.CollisionAlgorithms;
using BEPUphysics.DataStructures;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.MathExtensions;
using BEPUphysics.ResourceManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace BEPUphysics.NarrowPhaseSystems.Pairs
{
    ///<summary>
    /// Superclass of pairs between collidables that generate contact points.
    ///</summary>
    public class DetectorVolumeMobileMeshPairHandler : DetectorVolumePairHandler
    {


        private MobileMeshCollidable mesh;

        /// <summary>
        /// Gets the entity collidable associated with the pair.
        /// </summary>
        public override EntityCollidable Collidable
        {
            get { return mesh; }
        }


        ///<summary>
        /// Called when the pair handler is added to the narrow phase.
        ///</summary>
        protected internal override void OnAddedToNarrowPhase()
        {
            DetectorVolume.pairs.Add(Collidable.entity, this);
        }


        public override void Initialize(BroadPhaseEntries.BroadPhaseEntry entryA, BroadPhaseEntries.BroadPhaseEntry entryB)
        {
            base.Initialize(entryA, entryB);
            mesh = entryA as MobileMeshCollidable;
            if (mesh == null)
            {
                mesh = entryB as MobileMeshCollidable;
                if (mesh == null)
                    throw new Exception("Invalid types used to initialize pair handler.");
            }

        }
        ///<summary>
        /// Cleans up the pair handler.
        ///</summary>
        public override void CleanUp()
        {
            base.CleanUp();

            mesh = null;

        }


        private TriangleShape mobileTriangle = new TriangleShape();
        private TriangleShape detectorTriangle = new TriangleShape { collisionMargin = 0 };

        RawList<int> overlaps = new RawList<int>(8);

        public override void UpdateCollision(float dt)
        {
            WasContaining = Containing;
            WasTouching = Touching;

            mobileTriangle.collisionMargin = mesh.Shape.MeshCollisionMargin;

            //Scan the pairs in sequence, updating the state as we go.
            //Touching can be set to true by a single touching subpair.
            Touching = false;
            //Containing can be set to false by a single noncontaining or nontouching subpair.
            Containing = true;


            var meshData = mesh.Shape.TriangleMesh.Data;
            RigidTransform mobileTriangleTransform, detectorTriangleTransform;
            mobileTriangleTransform.Orientation = Quaternion.Identity;
            detectorTriangleTransform.Orientation = Quaternion.Identity;
            for (int i = 0; i < meshData.Indices.Length; i += 3)
            {
                //Grab a triangle associated with the mobile mesh.
                meshData.GetTriangle(i, out mobileTriangle.vA, out mobileTriangle.vB, out mobileTriangle.vC);
                RigidTransform.Transform(ref mobileTriangle.vA, ref mesh.worldTransform, out mobileTriangle.vA);
                RigidTransform.Transform(ref mobileTriangle.vB, ref mesh.worldTransform, out mobileTriangle.vB);
                RigidTransform.Transform(ref mobileTriangle.vC, ref mesh.worldTransform, out mobileTriangle.vC);
                Vector3.Add(ref mobileTriangle.vA, ref mobileTriangle.vB, out mobileTriangleTransform.Position);
                Vector3.Add(ref mobileTriangle.vC, ref mobileTriangleTransform.Position, out mobileTriangleTransform.Position);
                Vector3.Multiply(ref mobileTriangleTransform.Position, 1 / 3f, out mobileTriangleTransform.Position);
                Vector3.Subtract(ref mobileTriangle.vA, ref mobileTriangleTransform.Position, out mobileTriangle.vA);
                Vector3.Subtract(ref mobileTriangle.vB, ref mobileTriangleTransform.Position, out mobileTriangle.vB);
                Vector3.Subtract(ref mobileTriangle.vC, ref mobileTriangleTransform.Position, out mobileTriangle.vC);

                //Go through all the detector volume triangles which are near the mobile mesh triangle.
                bool triangleTouching, triangleContaining;
                BoundingBox mobileBoundingBox;
                mobileTriangle.GetBoundingBox(ref mobileTriangleTransform, out mobileBoundingBox);
                DetectorVolume.TriangleMesh.Tree.GetOverlaps(mobileBoundingBox, overlaps);
                for (int j = 0; j < overlaps.count; j++)
                {
                    DetectorVolume.TriangleMesh.Data.GetTriangle(overlaps.Elements[j], out detectorTriangle.vA, out detectorTriangle.vB, out detectorTriangle.vC);
                    Vector3.Add(ref detectorTriangle.vA, ref detectorTriangle.vB, out detectorTriangleTransform.Position);
                    Vector3.Add(ref detectorTriangle.vC, ref detectorTriangleTransform.Position, out detectorTriangleTransform.Position);
                    Vector3.Multiply(ref detectorTriangleTransform.Position, 1 / 3f, out detectorTriangleTransform.Position);
                    Vector3.Subtract(ref detectorTriangle.vA, ref detectorTriangleTransform.Position, out detectorTriangle.vA);
                    Vector3.Subtract(ref detectorTriangle.vB, ref detectorTriangleTransform.Position, out detectorTriangle.vB);
                    Vector3.Subtract(ref detectorTriangle.vC, ref detectorTriangleTransform.Position, out detectorTriangle.vC);

                    //If this triangle collides with the convex, we can stop immediately since we know we're touching and not containing.)))
                    //[MPR is used here in lieu of GJK because the MPR implementation tends to finish quicker than GJK when objects are overlapping.  The GJK implementation does better on separated objects.]
                    if (MPRToolbox.AreShapesOverlapping(detectorTriangle, mobileTriangle, ref detectorTriangleTransform, ref mobileTriangleTransform))
                    {
                        triangleTouching = true;
                        //The convex can't be fully contained if it's still touching the surface.
                        triangleContaining = false;
                        overlaps.Clear();
                        goto finishTriangleTest;
                    }
                }

                overlaps.Clear();
                //If we get here, then there was no shell intersection.
                //If the convex's center point is contained by the mesh, then the convex is fully contained.
                //This test is only needed if containment hasn't yet been outlawed or a touching state hasn't been established.
                if ((!Touching || Containing) && DetectorVolume.IsPointContained(ref mobileTriangleTransform.Position, overlaps))
                {
                    triangleTouching = true;
                    triangleContaining = true;
                    goto finishTriangleTest;
                }

                //If we get here, then there was no surface intersection and the convex's center is not contained- the volume and convex are separate!
                triangleTouching = false;
                triangleContaining = false;

            finishTriangleTest:
                //Analyze the results of the triangle test.

                if (triangleTouching)
                    Touching = true; //If one child is touching, then we are touching too.
                else
                    Containing = false; //If one child isn't touching, then we aren't containing.

                if (!triangleContaining) //If one child isn't containing, then we aren't containing.
                    Containing = false;

                if (!Containing && Touching)
                {
                    //If it's touching but not containing, no further pairs will change the state.
                    //Containment has been invalidated by something that either didn't touch or wasn't contained.
                    //Touching has been ensured by at least one object touching.
                    break;
                }

            }

            //There is a possibility that the MobileMesh is solid and fully contains the DetectorVolume.
            //In this case, we should be Touching, but currently we are not.
            if (mesh.Shape.solidity == MobileMeshSolidity.Solid && !Containing && !Touching)
            {
                //To determine if the detector volume is fully contained, check if one of the detector mesh's vertices
                //are in the mobile mesh.

                //This *could* fail if the mobile mesh is actually multiple pieces, but that's not a common or really supported case for solids.
                Vector3 vertex;
                DetectorVolume.TriangleMesh.Data.GetVertexPosition(0, out vertex);
                Ray ray;
                ray.Direction = Vector3.Up;
                RayHit hit;
                RigidTransform.TransformByInverse(ref vertex, ref mesh.worldTransform, out ray.Position);
                if (mesh.Shape.IsLocalRayOriginInMesh(ref ray, out hit))
                {
                    Touching = true;
                }
            }

            NotifyDetectorVolumeOfChanges();
        }

    }
}
