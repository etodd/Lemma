using System.Collections.Generic;
using BEPUphysics.Constraints;
using BEPUphysics.DeactivationManagement;
using BEPUphysics.DataStructures;

namespace BEPUphysics.Entities
{
    ///<summary>
    /// Convenience collection for easily scanning the two entity constraints connected to an entity.
    ///</summary>
    public class EntitySolverUpdateableCollection : IEnumerable<EntitySolverUpdateable>
    {
        private RawList<SimulationIslandConnection> connections;


        /// <summary>
        /// Constructs a new constraint collection.
        /// </summary>
        /// <param name="connections">Solver updateables to enumerate over.</param>
        public EntitySolverUpdateableCollection(RawList<SimulationIslandConnection> connections)
        {
            this.connections = connections;
        }

        /// <summary>
        /// Gets an enumerator for the collection.
        /// </summary>
        /// <returns>Enumerator for the collection.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(connections);
        }

        IEnumerator<EntitySolverUpdateable> IEnumerable<EntitySolverUpdateable>.GetEnumerator()
        {
            return new Enumerator(connections);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(connections);
        }

        ///<summary>
        /// Enumerator for the EntityConstraintCollection.
        ///</summary>
        public struct Enumerator : IEnumerator<EntitySolverUpdateable>
        {
            private RawList<SimulationIslandConnection> connections;
            private int index;
            private EntitySolverUpdateable current;

            /// <summary>
            /// Constructs an enumerator for the solver updateables list.
            /// </summary>
            /// <param name="connections">List of solver updateables to enumerate.</param>
            public Enumerator(RawList<SimulationIslandConnection> connections)
            {
                this.connections = connections;
                index = -1;
                current = null;
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            /// </returns>
            public EntitySolverUpdateable Current
            {
                get { return current; }
            }

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            /// <filterpriority>2</filterpriority>
            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
            public bool MoveNext()
            {
                while (++index < connections.Count)
                {
                    if (!connections.Elements[index].SlatedForRemoval)
                    {
                        current = connections.Elements[index].Owner as EntitySolverUpdateable;
                        if (current != null)
                            return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception><filterpriority>2</filterpriority>
            public void Reset()
            {
                index = -1;
                current = null;
            }
        }
    }
}
