using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;

namespace BEPUphysics.DataStructures
{
    ///<summary>
    /// Collection of mesh data which transforms its vertices before returning them.
    ///</summary>
    public class TransformableMeshData : MeshBoundingBoxTreeData
    {
        ///<summary>
        /// Constructs the mesh data.
        ///</summary>
        ///<param name="vertices">Vertices to use in the mesh data.</param>
        ///<param name="indices">Indices to use in the mesh data.</param>
        public TransformableMeshData(Vector3[] vertices, int[] indices)
        {
            Vertices = vertices;
            Indices = indices;
        }

        ///<summary>
        /// Constructs the mesh data.
        ///</summary>
        ///<param name="vertices">Vertice sto use in the mesh data.</param>
        ///<param name="indices">Indices to use in the mesh data.</param>
        ///<param name="worldTransform">Transform to apply to vertices before returning their positions.</param>
        public TransformableMeshData(Vector3[] vertices, int[] indices, AffineTransform worldTransform)
        {
            this.worldTransform = worldTransform;
            Vertices = vertices;
            Indices = indices;
        }


        internal AffineTransform worldTransform = AffineTransform.Identity;

        ///<summary>
        /// Gets or sets the transform to apply to the vertices before returning their position.
        ///</summary>
        public AffineTransform WorldTransform
        {
            get
            {
                return worldTransform;
            }
            set
            {
                worldTransform = value;
            }
        }

        ///<summary>
        /// Gets the triangle vertex positions at a given index.
        ///</summary>
        ///<param name="triangleIndex">First index of a triangle's vertices in the index buffer.</param>
        ///<param name="v1">First vertex of the triangle.</param>
        ///<param name="v2">Second vertex of the triangle.</param>
        ///<param name="v3">Third vertex of the triangle.</param>
        public override void GetTriangle(int triangleIndex, out Vector3 v1, out Vector3 v2, out Vector3 v3)
        {
            AffineTransform.Transform(ref vertices[indices[triangleIndex]], ref worldTransform, out v1);
            AffineTransform.Transform(ref vertices[indices[triangleIndex + 1]], ref worldTransform, out v2);
            AffineTransform.Transform(ref vertices[indices[triangleIndex + 2]], ref worldTransform, out v3);
        }

        ///<summary>
        /// Gets the position of a vertex in the data.
        ///</summary>
        ///<param name="i">Index of the vertex.</param>
        ///<param name="vertex">Position of the vertex.</param>
        public override void GetVertexPosition(int i, out Vector3 vertex)
        {
            AffineTransform.Transform(ref vertices[i], ref worldTransform, out vertex);
        }


    }
}
