using System.Collections.Generic;

namespace BEPUphysics.UpdateableSystems
{
    ///<summary>
    /// Defines an object which is updated by the space.
    /// These refer to the special Updateable types which
    /// allow for easier integration into the update flow of the space.
    ///</summary>
    public interface ISpaceUpdateable : ISpaceObject
    {
        /// <summary>
        /// Gets and sets whether or not the updateable should be updated sequentially even in a multithreaded space.
        /// If this is true, the updateable can make use of the space's ThreadManager for internal multithreading.
        /// </summary>
        bool IsUpdatedSequentially { get; set; }

        /// <summary>
        /// Gets and sets whether or not the updateable should be updated by the space.
        /// </summary>
        bool IsUpdating { get; set; }

        ///<summary>
        /// List of managers owning the updateable.
        ///</summary>
        List<UpdateableManager> Managers { get; }



    }
}
