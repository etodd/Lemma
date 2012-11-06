using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BEPUphysics.DataStructures
{
    ///<summary>
    /// Result of an overlap test between two trees of specified type.
    ///</summary>
    ///<typeparam name="T1">Type of elements in the first tree.</typeparam>
    ///<typeparam name="T2">Type of elements in the second tree.</typeparam>
    public struct TreeOverlapPair<T1, T2>
    {
        /// <summary>
        /// Overlap owned by the first tree.
        /// </summary>
        public T1 OverlapA;
        /// <summary>
        /// Overlap owned by the second tree.
        /// </summary>
        public T2 OverlapB;

        /// <summary>
        /// Constructs a new overlap pair.
        /// </summary>
        /// <param name="overlapA">Overlap owned by the first tree.</param>
        /// <param name="overlapB">Overlap owned by the second tree.</param>
        public TreeOverlapPair(T1 overlapA, T2 overlapB)
        {
            OverlapA = overlapA;
            OverlapB = overlapB;
        }
    }
}
