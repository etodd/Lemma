using Microsoft.Xna.Framework;

namespace BEPUphysics.DataStructures
{
    ///<summary>
    /// Collection of triangle mesh data that directly returns vertices from its vertex buffer instead of transforming them first.
    ///</summary>
    public class StaticMeshData : MeshBoundingBoxTreeData
    {
        ///<summary>
        /// Constructs the triangle mesh data.
        ///</summary>
        ///<param name="vertices">Vertices to use in the data.</param>
        ///<param name="indices">Indices to use in the data.</param>
        public StaticMeshData(Vector3[] vertices, int[] indices)
        {
            Vertices = vertices;
            Indices = indices;
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
            v1 = vertices[indices[triangleIndex]];
            v2 = vertices[indices[triangleIndex + 1]];
            v3 = vertices[indices[triangleIndex + 2]];
        }


        ///<summary>
        /// Gets the position of a vertex in the data.
        ///</summary>
        ///<param name="i">Index of the vertex.</param>
        ///<param name="vertex">Position of the vertex.</param>
        public override void GetVertexPosition(int i, out Vector3 vertex)
        {
            vertex = vertices[i];
        }


    }
}
