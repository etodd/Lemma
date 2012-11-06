using System.Collections.Generic;
using BEPUphysics.Collidables.MobileCollidables;
using BEPUphysics.Entities;

namespace BEPUphysics.Collidables
{
    ///<summary>
    /// Enumerable collection of entities associated with a collidable.
    ///</summary>
    public struct EntityCollidableCollection : IEnumerable<Entity>
    {

        ///<summary>
        /// Enumerator for the EntityCollidableCollection.
        ///</summary>
        public struct Enumerator : IEnumerator<Entity>
        {
            EntityCollidableCollection collection;
            EntityCollidable current;
            int index;
            ///<summary>
            /// Constructs a new enumerator.
            ///</summary>
            ///<param name="collection">Owning collection.</param>
            public Enumerator(EntityCollidableCollection collection)
            {
                this.collection = collection;
                index = -1;
                current = null;
            }

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            /// </returns>
            public Entity Current
            {
                get { return current.entity; }
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
            /// <exception>The collection was modified after the enumerator was created.
            ///   <cref>T:System.InvalidOperationException</cref>
            /// </exception><filterpriority>2</filterpriority>
            public bool MoveNext()
            {
                while (++index < collection.owner.pairs.Count)
                {
                    if ((current = (collection.owner.pairs[index].broadPhaseOverlap.entryA == collection.owner ?
                        collection.owner.pairs[index].broadPhaseOverlap.entryB : 
                        collection.owner.pairs[index].broadPhaseOverlap.entryA) as EntityCollidable) != null)
                        return true;
                }
                return false;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception>The collection was modified after the enumerator was created.
            ///   <cref>T:System.InvalidOperationException</cref>
            /// </exception><filterpriority>2</filterpriority>
            public void Reset()
            {
                index = -1;
            }
        }

        ///<summary>
        /// Constructs a new EntityCollidableCollection.
        ///</summary>
        ///<param name="owner">Owner of the collection.</param>
        public EntityCollidableCollection(EntityCollidable owner)
        {
            this.owner = owner;
        }

        internal EntityCollidable owner;


        ///<summary>
        /// Gets an enumerator over the entities in the collection.
        ///</summary>
        ///<returns>Enumerator over the entities in the collection.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }




    }
}
