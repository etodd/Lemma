using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BEPUutilities
{
    /// <summary>
    /// Maps indices to permuted versions of the indices.
    /// </summary>
    public class PermutationMapper
    {
        /// <summary>
        /// Constructs a new permutation mapper.
        /// </summary>
        public PermutationMapper()
        {
            PermutationIndex = 0;
        }

        /// <summary>
        /// Gets or sets the permutation index used by the solver.  If the simulation is restarting from a given frame,
        /// setting this index to be consistent is required for deterministic results.
        /// </summary>
        public int PermutationIndex
        {
            get
            {
                return primeIndex;
            }
            set
            {
                primeIndex = value % primes.Length;
                currentPrime = primes[primeIndex = (primeIndex + 1) % primes.Length];
            }
        }

        long currentPrime;
        int primeIndex;
        static long[] primes = {
                                    472882049, 492876847,
                                    492876863, 512927357,
                                    512927377, 533000389,
                                    533000401, 553105243,
                                    553105253, 573259391,
                                    573259433, 593441843,
                                    593441861, 613651349,
                                    613651369, 633910099,
                                    633910111, 654188383,
                                    654188429, 674506081,
                                    674506111, 694847533,
                                    694847539, 715225739,
                                    715225741, 735632791,
                                    735632797, 756065159,
                                    756065179, 776531401,
                                    776531419, 797003413,
                                    797003437, 817504243,
                                    817504253, 838041641,
                                    838041647, 858599503,
                                    858599509, 879190747,
                                    879190841, 899809343,
                                    899809363, 920419813,
                                    920419823, 941083981,
                                    941083987, 961748927,
                                    961748941, 982451653
                               };


        /// <summary>
        /// Gets a remapped index.
        /// </summary>
        /// <param name="index">Original index of an element in the set to be redirected to a shuffled position.</param>
        /// <param name="setSize">Size of the set being permuted. Must be smaller than 472882049.</param>
        /// <returns>The remapped index.</returns>
        public long GetMappedIndex(long index, int setSize)
        {
            return (index * currentPrime) % setSize;
        }

        /// <summary>
        /// Gets a remapped index.
        /// </summary>
        /// <param name="index">Original index of an element in the set to be redirected to a shuffled position.</param>
        /// <param name="setSize">Size of the set being permuted. Must be smaller than 472882049.</param>
        /// <returns>The remapped index.</returns>
        public int GetMappedIndex(int index, int setSize)
        {
            return (int)((index * currentPrime) % setSize);
        }
    }
}
