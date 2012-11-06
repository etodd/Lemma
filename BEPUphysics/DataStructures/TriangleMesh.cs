using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using BEPUphysics.ResourceManagement;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Microsoft.Xna.Framework.Graphics;

namespace BEPUphysics.DataStructures
{
    ///<summary>
    /// Data structure containing triangle mesh data and its associated bounding box tree.
    ///</summary>
    public class TriangleMesh
    {
        private MeshBoundingBoxTreeData data;
        ///<summary>
        /// Gets or sets the bounding box data used in the mesh.
        ///</summary>
        public MeshBoundingBoxTreeData Data
        {
            get
            {
                return data;
            }
            set
            {
                data = value;
                tree.Data = data;
            }
        }

        private MeshBoundingBoxTree tree;
        ///<summary>
        /// Gets the bounding box tree that accelerates queries to this triangle mesh.
        ///</summary>
        public MeshBoundingBoxTree Tree
        {
            get
            {
                return tree;
            }
        }

        ///<summary>
        /// Constructs a new triangle mesh.
        ///</summary>
        ///<param name="data">Data to use to construct the mesh.</param>
        public TriangleMesh(MeshBoundingBoxTreeData data)
        {
            this.data = data;
            tree = new MeshBoundingBoxTree(data);
        }

        ///<summary>
        /// Tests a ray against the triangle mesh.
        ///</summary>
        ///<param name="ray">Ray to test against the mesh.</param>
        ///<param name="hitCount">Number of hits between the ray and the mesh.</param>
        ///<returns>Whether or not the ray hit the mesh.</returns>
        public bool RayCast(Ray ray, out int hitCount)
        {
            var rayHits = Resources.GetRayHitList();
            bool toReturn = RayCast(ray, rayHits);
            hitCount = rayHits.Count;
            Resources.GiveBack(rayHits);
            return toReturn;
        }

        ///<summary>
        /// Tests a ray against the triangle mesh.
        ///</summary>
        ///<param name="ray">Ray to test against the mesh.</param>
        ///<param name="rayHit">Hit data for the ray, if any.</param>
        ///<returns>Whether or not the ray hit the mesh.</returns>
        public bool RayCast(Ray ray, out RayHit rayHit)
        {
            return RayCast(ray, float.MaxValue, TriangleSidedness.DoubleSided, out rayHit);
        }

        ///<summary>
        /// Tests a ray against the triangle mesh.
        ///</summary>
        ///<param name="ray">Ray to test against the mesh.</param>
        /// <param name="sidedness">Sidedness to apply to the mesh for the ray cast.</param>
        ///<param name="rayHit">Hit data for the ray, if any.</param>
        ///<returns>Whether or not the ray hit the mesh.</returns>
        public bool RayCast(Ray ray, TriangleSidedness sidedness, out RayHit rayHit)
        {
            return RayCast(ray, float.MaxValue, sidedness, out rayHit);
        }

        ///<summary>
        /// Tests a ray against the triangle mesh.
        ///</summary>
        ///<param name="ray">Ray to test against the mesh.</param>
        ///<param name="hits">Hit data for the ray, if any.</param>
        ///<returns>Whether or not the ray hit the mesh.</returns>
        public bool RayCast(Ray ray, IList<RayHit> hits)
        {
            return RayCast(ray, float.MaxValue, TriangleSidedness.DoubleSided, hits);
        }

        ///<summary>
        /// Tests a ray against the triangle mesh.
        ///</summary>
        ///<param name="ray">Ray to test against the mesh.</param>
        /// <param name="sidedness">Sidedness to apply to the mesh for the ray cast.</param>
        ///<param name="hits">Hit data for the ray, if any.</param>
        ///<returns>Whether or not the ray hit the mesh.</returns>
        public bool RayCast(Ray ray, TriangleSidedness sidedness, IList<RayHit> hits)
        {
            return RayCast(ray, float.MaxValue, sidedness, hits);
        }

        ///<summary>
        /// Tests a ray against the triangle mesh.
        ///</summary>
        ///<param name="ray">Ray to test against the mesh.</param>
        /// <param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        ///<param name="rayHit">Hit data for the ray, if any.</param>
        ///<returns>Whether or not the ray hit the mesh.</returns>
        public bool RayCast(Ray ray, float maximumLength, out RayHit rayHit)
        {
            return RayCast(ray, maximumLength, TriangleSidedness.DoubleSided, out rayHit);
        }

        ///<summary>
        /// Tests a ray against the triangle mesh.
        ///</summary>
        ///<param name="ray">Ray to test against the mesh.</param>
        /// <param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        /// <param name="sidedness">Sidedness to apply to the mesh for the ray cast.</param>
        ///<param name="rayHit">Hit data for the ray, if any.</param>
        ///<returns>Whether or not the ray hit the mesh.</returns>
        public bool RayCast(Ray ray, float maximumLength, TriangleSidedness sidedness, out RayHit rayHit)
        {
            var rayHits = Resources.GetRayHitList();
            bool toReturn = RayCast(ray, maximumLength, sidedness, rayHits);
            if (toReturn)
            {
                rayHit = rayHits[0];
                for (int i = 1; i < rayHits.Count; i++)
                {
                    RayHit hit = rayHits[i];
                    if (hit.T < rayHit.T)
                        rayHit = hit;
                }
            }
            else
                rayHit = new RayHit();
            Resources.GiveBack(rayHits);
            return toReturn;
        }

        ///<summary>
        /// Tests a ray against the triangle mesh.
        ///</summary>
        ///<param name="ray">Ray to test against the mesh.</param>
        /// <param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        ///<param name="hits">Hit data for the ray, if any.</param>
        ///<returns>Whether or not the ray hit the mesh.</returns>
        public bool RayCast(Ray ray, float maximumLength, IList<RayHit> hits)
        {
            return RayCast(ray, maximumLength, TriangleSidedness.DoubleSided, hits);
        }

        ///<summary>
        /// Tests a ray against the triangle mesh.
        ///</summary>
        ///<param name="ray">Ray to test against the mesh.</param>
        /// <param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        /// <param name="sidedness">Sidedness to apply to the mesh for the ray cast.</param>
        ///<param name="hits">Hit data for the ray, if any.</param>
        ///<returns>Whether or not the ray hit the mesh.</returns>
        public bool RayCast(Ray ray, float maximumLength, TriangleSidedness sidedness, IList<RayHit> hits)
        {
            var hitElements = Resources.GetIntList();
            tree.GetOverlaps(ray, maximumLength, hitElements);
            for (int i = 0; i < hitElements.Count; i++)
            {
                Vector3 v1, v2, v3;
                data.GetTriangle(hitElements[i], out v1, out v2, out v3);
                RayHit hit;
                if (Toolbox.FindRayTriangleIntersection(ref ray, maximumLength, sidedness, ref v1, ref v2, ref v3, out hit))
                {
                    hits.Add(hit);
                }
            }
            Resources.GiveBack(hitElements);
            return hits.Count > 0;
        }

        #region Vertex extraction helpers


        /// <summary>
        /// Gets an array of vertices and indices from the provided model.
        /// </summary>
        /// <param name="collisionModel">Model to use for the collision shape.</param>
        /// <param name="vertices">Compiled set of vertices from the model.</param>
        /// <param name="indices">Compiled set of indices from the model.</param>
        public static void GetVerticesAndIndicesFromModel(Model collisionModel, out Vector3[] vertices, out int[] indices)
        {
            var verticesList = new List<Vector3>();
            var indicesList = new List<int>();
            var transforms = new Matrix[collisionModel.Bones.Count];
            collisionModel.CopyAbsoluteBoneTransformsTo(transforms);

            Matrix transform;
            foreach (ModelMesh mesh in collisionModel.Meshes)
            {
                if (mesh.ParentBone != null)
                    transform = transforms[mesh.ParentBone.Index];
                else
                    transform = Matrix.Identity;
                AddMesh(mesh, transform, verticesList, indicesList);
            }

            vertices = verticesList.ToArray();
            indices = indicesList.ToArray();


        }

        /// <summary>
        /// Adds a mesh's vertices and indices to the given lists.
        /// </summary>
        /// <param name="collisionModelMesh">Model to use for the collision shape.</param>
        /// <param name="transform">Transform to apply to the mesh.</param>
        /// <param name="vertices">List to receive vertices from the mesh.</param>
        /// <param name="indices">List to receive indices from the mesh.</param>
        public static void AddMesh(ModelMesh collisionModelMesh, Matrix transform, List<Vector3> vertices, IList<int> indices)
        {
            foreach (ModelMeshPart meshPart in collisionModelMesh.MeshParts)
            {
                int startIndex = vertices.Count;
                var meshPartVertices = new Vector3[meshPart.NumVertices];
                //Grab position data from the mesh part.
                int stride = meshPart.VertexBuffer.VertexDeclaration.VertexStride;
                meshPart.VertexBuffer.GetData(
                        meshPart.VertexOffset * stride,
                        meshPartVertices,
                        0,
                        meshPart.NumVertices,
                        stride);

                //Transform it so its vertices are located in the model's space as opposed to mesh part space.
                Vector3.Transform(meshPartVertices, ref transform, meshPartVertices);
                vertices.AddRange(meshPartVertices);

                if (meshPart.IndexBuffer.IndexElementSize == IndexElementSize.ThirtyTwoBits)
                {
                    var meshIndices = new int[meshPart.PrimitiveCount * 3];
                    meshPart.IndexBuffer.GetData(meshPart.StartIndex * 4, meshIndices, 0, meshPart.PrimitiveCount * 3);
                    for (int k = 0; k < meshIndices.Length; k++)
                    {
                        indices.Add(startIndex + meshIndices[k]);
                    }
                }
                else
                {
                    var meshIndices = new ushort[meshPart.PrimitiveCount * 3];
                    meshPart.IndexBuffer.GetData(meshPart.StartIndex * 2, meshIndices, 0, meshPart.PrimitiveCount * 3);
                    for (int k = 0; k < meshIndices.Length; k++)
                    {
                        indices.Add(startIndex + meshIndices[k]);
                    }


                }
            }




        }

        #endregion



    }
}
