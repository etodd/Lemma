using System.Collections.Generic;
using BEPUphysics.Constraints;

namespace BEPUphysics.UpdateableSystems
{
    ///<summary>
    /// A class which is both a space updateable and a Solver Updateable.
    ///</summary>
    public abstract class CombinedUpdateable : EntitySolverUpdateable, ISpaceUpdateable
    {
        private bool isSequentiallyUpdated = true;

        protected CombinedUpdateable()
        {
            IsUpdating = true;
        }

        #region ISpaceUpdateable Members

        List<UpdateableManager> managers = new List<UpdateableManager>();
        List<UpdateableManager> ISpaceUpdateable.Managers
        {
            get
            {
                return managers;
            }
        }

        /// <summary>
        /// Gets and sets whether or not the updateable should be updated sequentially even in a multithreaded space.
        /// If this is true, the updateable can make use of the space's ThreadManager for internal multithreading.
        /// </summary>
        public bool IsUpdatedSequentially
        {
            get { return isSequentiallyUpdated; }
            set
            {
                bool oldValue = isSequentiallyUpdated;
                isSequentiallyUpdated = value;
                if (value != oldValue)
                    for (int i = 0; i < managers.Count; i++)
                    {
                        managers[i].SequentialUpdatingStateChanged(this);
                    }
            }
        }


        /// <summary>
        /// Gets and sets whether or not the updateable should be updated by the space.
        /// </summary>
        public bool IsUpdating
        {
            get;
            set;
        }


        ISpace ISpaceObject.Space
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the user data associated with this object.
        /// </summary>
        public new object Tag { get; set; }

        #endregion

    }
}
