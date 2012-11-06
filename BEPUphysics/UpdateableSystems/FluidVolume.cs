using System;
using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.Collidables;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.ResourceManagement;
using Microsoft.Xna.Framework;
using BEPUphysics.Threading;

namespace BEPUphysics.UpdateableSystems
{

    /// <summary>
    /// Volume in which physically simulated objects have a buoyancy force applied to them based on their density and volume.
    /// </summary>
    public class FluidVolume : Updateable, IDuringForcesUpdateable
    {
        //TODO: The current FluidVolume implementation is awfully awful.
        //It only currently supports horizontal surface planes, since it uses
        //entity bounding boxes directly rather than using an affinely transformed bounding box.
        //The 'surfacetriangles' approach to fluid volumes is pretty goofy to begin with. 
        //A mesh-based volume would be a lot better for content development.

        float surfacePlaneHeight;
        Vector3 upVector;
        ///<summary>
        /// Gets the up vector of the fluid volume.
        ///</summary>
        public Vector3 UpVector
        {
            get
            {
                return upVector;
            }
            set
            {
                upVector = value;
                RecalculateBoundingBox();
            }
        }

        BoundingBox boundingBox;
        /// <summary>
        /// Bounding box surrounding the surface tris and entire depth of the object.
        /// </summary>
        public BoundingBox BoundingBox
        {
            get
            {
                return boundingBox;
            }
        }

        float maxDepth;
        /// <summary>
        /// Maximum depth of the fluid from the surface.
        /// </summary>
        public float MaxDepth
        {
            get
            {
                return maxDepth;
            }
            set
            {
                maxDepth = value;
                RecalculateBoundingBox();
            }
        }

        /// <summary>
        /// Density of the fluid represented in the volume.
        /// </summary>
        public float Density { get; set; }

        int samplePointsPerDimension = 8;
        /// <summary>
        /// Number of locations along each of the horizontal axes from which to sample the shape.
        /// Defaults to 8.
        /// </summary>
        public int SamplePointsPerDimension
        {
            get
            {
                return samplePointsPerDimension;
            }
            set
            {
                samplePointsPerDimension = value;
            }
        }

        /// <summary>
        /// Fraction by which to reduce the linear momentum of floating objects each update.
        /// </summary>
        public float LinearDamping { get; set; }

        /// <summary>
        /// Fraction by which to reduce the angular momentum of floating objects each update.
        /// </summary>
        public float AngularDamping { get; set; }



        private Vector3 flowDirection;
        /// <summary>
        /// Direction in which to exert force on objects within the fluid.
        /// flowForce and maxFlowSpeed must have valid values as well for the flow to work.
        /// </summary>
        public Vector3 FlowDirection
        {
            get
            {
                return flowDirection;
            }
            set
            {
                float length = value.Length();
                if (length > 0)
                {
                    flowDirection = value / length;
                }
                else
                    flowDirection = Vector3.Zero;
                //TODO: Activate bodies in water
            }
        }

        private float flowForce;

        /// <summary>
        /// Magnitude of the flow's force, in units of flow direction.
        /// flowDirection and maxFlowSpeed must have valid values as well for the flow to work.
        /// </summary>
        public float FlowForce
        {
            get
            {
                return flowForce;
            }
            set
            {
                flowForce = value;
                //TODO: Activate bodies in water
            }
        }

        float maxFlowSpeed;
        /// <summary>
        /// Maximum speed of the flow; objects will not be accelerated by the flow force beyond this speed.
        /// flowForce and flowDirection must have valid values as well for the flow to work.
        /// </summary>
        public float MaxFlowSpeed
        {
            get
            {
                return maxFlowSpeed;
            }
            set
            {
                maxFlowSpeed = value;
            }

        }

        IQueryAccelerator QueryAccelerator { get; set; }

        ///<summary>
        /// Gets or sets the thread manager used by the fluid volume.
        ///</summary>
        public IThreadManager ThreadManager { get; set; }

        private List<Vector3[]> surfaceTriangles;
        /// <summary>
        /// List of coplanar triangles composing the surface of the fluid.
        /// </summary>
        public List<Vector3[]> SurfaceTriangles
        {
            get
            {
                return surfaceTriangles;
            }
            set
            {
                surfaceTriangles = value;
                RecalculateBoundingBox();
            }
        }

        float gravity;
        ///<summary>
        /// Gets or sets the gravity used by the fluid volume.
        ///</summary>
        public float Gravity
        {
            get
            {
                return gravity;
            }
            set
            {
                gravity = value;
            }
        }



        /// <summary>
        /// Creates a fluid volume.
        /// </summary>
        /// <param name="upVector">Up vector of the fluid volume.</param>
        /// <param name="gravity">Strength of gravity for the purposes of the fluid volume.</param>
        /// <param name="surfaceTriangles">List of triangles composing the surface of the fluid.  Set up as a list of length 3 arrays of Vector3's.</param>
        /// <param name="depth">Depth of the fluid back along the surface normal.</param>
        /// <param name="fluidDensity">Density of the fluid represented in the volume.</param>
        /// <param name="linearDamping">Fraction by which to reduce the linear momentum of floating objects each update, in addition to any of the body's own damping.</param>
        /// <param name="angularDamping">Fraction by which to reduce the angular momentum of floating objects each update, in addition to any of the body's own damping.</param>
        /// <param name="queryAccelerator">System to accelerate queries to find nearby entities.</param>
        /// <param name="threadManager">Thread manager used by the fluid volume.</param>
        public FluidVolume(Vector3 upVector, float gravity, List<Vector3[]> surfaceTriangles, float depth, float fluidDensity, float linearDamping, float angularDamping,
            IQueryAccelerator queryAccelerator, IThreadManager threadManager)
        {
            Gravity = gravity;
            SurfaceTriangles = surfaceTriangles;
            MaxDepth = depth;
            Density = fluidDensity;
            LinearDamping = linearDamping;
            AngularDamping = angularDamping;

            UpVector = upVector;
            QueryAccelerator = queryAccelerator;
            ThreadManager = threadManager;

            analyzeCollisionEntryDelegate = AnalyzeCollisionEntry;
        }

        /// <summary>
        /// Recalculates the bounding box of the fluid based on its depth, surface normal, and surface triangles.
        /// </summary>
        public void RecalculateBoundingBox()
        {
            var points = Resources.GetVectorList();
            foreach (var tri in SurfaceTriangles)
            {
                points.Add(tri[0]);
                points.Add(tri[1]);
                points.Add(tri[2]);
                points.Add(tri[0] - upVector * MaxDepth);
                points.Add(tri[1] - upVector * MaxDepth);
                points.Add(tri[2] - upVector * MaxDepth);
            }
            boundingBox = BoundingBox.CreateFromPoints(points);
            surfacePlaneHeight = Vector3.Dot(points[0], upVector);
            Resources.GiveBack(points);
        }

        List<BroadPhaseEntry> collisionEntries = new List<BroadPhaseEntry>();

        /// <summary>
        /// Applies buoyancy forces to appropriate objects.
        /// Called automatically when needed by the owning Space.
        /// </summary>
        /// <param name="dt">Time since last frame in physical logic.</param>
        void IDuringForcesUpdateable.Update(float dt)
        {
            QueryAccelerator.GetEntries(boundingBox, collisionEntries);
            //TODO: Could integrate the entire thing into the collision detection pipeline.  Applying forces
            //in the collision detection pipeline isn't allowed, so there'd still need to be an Updateable involved.
            //However, the broadphase query would be eliminated and the raycasting work would be automatically multithreaded.

            this.dt = dt;
            
            //Don't always multithread.  For small numbers of objects, the overhead of using multithreading isn't worth it.
            //Could tune this value depending on platform for better performance.
            if (collisionEntries.Count > 30 && ThreadManager.ThreadCount > 1)
                ThreadManager.ForLoop(0, collisionEntries.Count, analyzeCollisionEntryDelegate);
            else
                for (int i = 0; i < collisionEntries.Count; i++)
                {
                    AnalyzeCollisionEntry(i);
                }

            collisionEntries.Clear();




        }

        float dt;
        Action<int> analyzeCollisionEntryDelegate;

        void AnalyzeCollisionEntry(int i)
        {
            var entityEntry = collisionEntries[i] as EntityCollidable;
            if (entityEntry != null && entityEntry.IsActive && entityEntry.entity.isDynamic)
            {
                bool keepGoing = false;
                foreach (var tri in surfaceTriangles)
                {
                    //Don't need to do anything if the entity is outside of the water.
                    if (Toolbox.IsPointInsideTriangle(ref tri[0], ref tri[1], ref tri[2], ref entityEntry.worldTransform.Position))
                    {
                        keepGoing = true;
                        break;
                    }
                }
                if (!keepGoing)
                    return;

                //The entity is submerged, apply buoyancy forces.
                float submergedVolume;
                Vector3 submergedCenter;
                GetBuoyancyInformation(entityEntry, out submergedVolume, out submergedCenter);
                if (submergedVolume > 0)
                {
                    Vector3 force;
                    Vector3.Multiply(ref upVector, -gravity * Density * dt * submergedVolume, out force);
                    entityEntry.entity.ApplyImpulse(ref submergedCenter, ref force);

                    float fractionSubmerged = submergedVolume / entityEntry.entity.volume;
                    //Flow
                    if (FlowForce != 0)
                    {
                        float dot = Math.Max(Vector3.Dot(entityEntry.entity.linearVelocity, flowDirection), 0);
                        if (dot < MaxFlowSpeed)
                        {
                            force = Math.Min(FlowForce, (MaxFlowSpeed - dot) * entityEntry.entity.mass) * dt * fractionSubmerged * FlowDirection;
                            entityEntry.entity.ApplyLinearImpulse(ref force);
                        }
                    }
                    //Damping
                    entityEntry.entity.ModifyLinearDamping(fractionSubmerged * LinearDamping);
                    entityEntry.entity.ModifyAngularDamping(fractionSubmerged * AngularDamping);

                }
            }
        }

        void GetBuoyancyInformation(EntityCollidable info, out float submergedVolume, out Vector3 submergedCenter)
        {
            BoundingBox entityBoundingBox;
            //TODO: Figure out how to best reenable this.
            entityBoundingBox = info.boundingBox;// new BoundingBox();
            //info.ComputeBoundingBox(ref surfaceOrientationTranspose, out entityBoundingBox);
            if (entityBoundingBox.Min.Y > surfacePlaneHeight)
            {
                //Fish out of the water.  Don't need to do raycast tests on objects not at the boundary.
                submergedVolume = 0;
                submergedCenter = info.worldTransform.Position;
                return;
            }
            if (entityBoundingBox.Max.Y < surfacePlaneHeight)
            {
                submergedVolume = info.entity.volume;
                submergedCenter = info.worldTransform.Position;
                return;
            }

            Vector3 origin, xSpacing, zSpacing;
            float perColumnArea;
            GetSamplingOrigin(ref entityBoundingBox, out xSpacing, out zSpacing, out perColumnArea, out origin);

            float boundingBoxHeight = entityBoundingBox.Max.Y - entityBoundingBox.Min.Y;
            float maxLength = surfacePlaneHeight - entityBoundingBox.Min.Y;
            submergedCenter = new Vector3();
            submergedVolume = 0;
            for (int i = 0; i < samplePointsPerDimension; i++)
            {
                for (int j = 0; j < samplePointsPerDimension; j++)
                {
                    Vector3 columnVolumeCenter;
                    float submergedHeight;
                    if ((submergedHeight = GetSubmergedHeight(info, maxLength, boundingBoxHeight, ref origin, ref xSpacing, ref zSpacing, i, j, out columnVolumeCenter)) > 0)
                    {
                        float columnVolume = submergedHeight * perColumnArea;
                        Vector3.Multiply(ref columnVolumeCenter, columnVolume, out columnVolumeCenter);
                        Vector3.Add(ref columnVolumeCenter, ref submergedCenter, out submergedCenter);
                        submergedVolume += columnVolume;
                    }
                }
            }
            Vector3.Divide(ref submergedCenter, submergedVolume, out submergedCenter);

        }

        void GetSamplingOrigin(ref BoundingBox entityBoundingBox, out Vector3 xSpacing, out Vector3 zSpacing, out float perColumnArea, out Vector3 origin)
        {
            //Compute spacing and increment informaiton.
            float widthIncrement = (entityBoundingBox.Max.X - entityBoundingBox.Min.X) / samplePointsPerDimension;
            float lengthIncrement = (entityBoundingBox.Max.Z - entityBoundingBox.Min.Z) / samplePointsPerDimension;
            Vector3 right = Toolbox.RightVector;// new Vector3(surfaceOrientationTranspose.M11, surfaceOrientationTranspose.M21, surfaceOrientationTranspose.M31);
            Vector3.Multiply(ref right, widthIncrement, out xSpacing);
            Vector3 backward = Toolbox.BackVector;// new Vector3(surfaceOrientationTranspose.M13, surfaceOrientationTranspose.M23, surfaceOrientationTranspose.M33);
            Vector3.Multiply(ref backward, lengthIncrement, out zSpacing);
            perColumnArea = widthIncrement * lengthIncrement;


            //Compute the origin.
            Vector3 minimum = entityBoundingBox.Min;
            //Matrix3X3.TransformTranspose(ref entityBoundingBox.Min, ref surfaceOrientationTranspose, out minimum);
            Vector3 offset;
            Vector3.Multiply(ref xSpacing, .5f, out offset);
            Vector3.Add(ref minimum, ref offset, out origin);
            Vector3.Multiply(ref zSpacing, .5f, out offset);
            Vector3.Add(ref origin, ref offset, out origin);


            //TODO: Could adjust the grid origin such that a ray always hits the deepest point.
            //The below code is a prototype of the idea, but has bugs.
            //var convexInfo = info as ConvexCollisionInformation;
            //if (convexInfo != null)
            //{
            //    Vector3 dir;
            //    Vector3.Negate(ref upVector, out dir);
            //    Vector3 extremePoint;
            //    convexInfo.Shape.GetExtremePoint(dir, ref convexInfo.worldTransform, out extremePoint);
            //    //Use extreme point to snap to grid.
            //    Vector3.Subtract(ref extremePoint, ref origin, out offset);
            //    float offsetX, offsetZ;
            //    Vector3.Dot(ref offset, ref right, out offsetX);
            //    Vector3.Dot(ref offset, ref backward, out offsetZ);
            //    offsetX %= widthIncrement;
            //    offsetZ %= lengthIncrement;

            //    if (offsetX > .5f * widthIncrement)
            //    {
            //        Vector3.Multiply(ref right, 1 - offsetX, out offset);
            //    }
            //    else
            //    {
            //        Vector3.Multiply(ref right, -offsetX, out offset);
            //    }

            //    if (offsetZ > .5f * lengthIncrement)
            //    {
            //        Vector3 temp;
            //        Vector3.Multiply(ref right, 1 - offsetZ, out temp);
            //        Vector3.Add(ref temp, ref offset, out offset);
            //    }
            //    else
            //    {
            //        Vector3 temp;
            //        Vector3.Multiply(ref right, -offsetZ, out temp);
            //        Vector3.Add(ref temp, ref offset, out offset);
            //    }

            //    Vector3.Add(ref origin, ref offset, out origin);


            //}
        }

        float GetSubmergedHeight(Collidable info, float maxLength, float boundingBoxHeight, ref Vector3 rayOrigin, ref Vector3 xSpacing, ref Vector3 zSpacing, int i, int j, out Vector3 volumeCenter)
        {
            Ray ray;
            Vector3.Multiply(ref xSpacing, i, out ray.Position);
            Vector3.Multiply(ref zSpacing, j, out ray.Direction);
            Vector3.Add(ref ray.Position, ref ray.Direction, out ray.Position);
            Vector3.Add(ref ray.Position, ref rayOrigin, out ray.Position);
            ray.Direction = upVector;
            //do a bottom-up raycast.
            RayHit rayHit;
            //Only go up to maxLength.  If it's further away than maxLength, then it's above the water and it doesn't contribute anything.
            if (info.RayCast(ray, maxLength, out rayHit))
            {
                //Position the ray to point from the other side.
                Vector3.Multiply(ref ray.Direction, boundingBoxHeight, out ray.Direction);
                Vector3.Add(ref ray.Position, ref ray.Direction, out ray.Position);
                Vector3.Negate(ref upVector, out ray.Direction);
                float bottomY = rayHit.Location.Y;
                float bottom = rayHit.T;
                Vector3 bottomPosition = rayHit.Location;
                if (info.RayCast(ray, boundingBoxHeight - rayHit.T, out rayHit))
                {
                    Vector3.Add(ref rayHit.Location, ref bottomPosition, out volumeCenter);
                    Vector3.Multiply(ref volumeCenter, .5f, out volumeCenter);
                    return Math.Min(surfacePlaneHeight - bottomY, boundingBoxHeight - rayHit.T - bottom);
                }
                //This inner raycast should always hit, but just in case it doesn't due to some numerical problem, give it a graceful way out.
                volumeCenter = Vector3.Zero;
                return 0;
            }
            volumeCenter = Vector3.Zero;
            return 0;
        }
    }
}