namespace BEPUutilities
{
    ///<summary>
    /// Sidedness of a triangle or mesh.
    /// A triangle can be double sided, or allow one of its sides to let interacting objects through.
    ///</summary>
    public enum TriangleSidedness
    {
        /// <summary>
        /// The triangle will interact with objects coming from both directions.
        /// </summary>
        DoubleSided,
        /// <summary>
        /// The triangle will interact with objects from which the winding of the triangle appears to be clockwise.
        /// </summary>
        Clockwise,
        /// <summary>
        /// The triangle will interact with objects from which the winding of the triangle appears to be counterclockwise..
        /// </summary>
        Counterclockwise
    }
}
