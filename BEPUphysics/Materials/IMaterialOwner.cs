namespace BEPUphysics.Materials
{
    ///<summary>
    /// Defines an object that has a material.
    ///</summary>
    public interface IMaterialOwner
    {
        ///<summary>
        /// Gets or sets the material of the object.
        ///</summary>
        Material Material { get; set; }
    }
}
