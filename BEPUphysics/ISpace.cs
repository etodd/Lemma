using System;
using System.Collections.Generic;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseSystems;
using BEPUphysics.Entities;
using Microsoft.Xna.Framework;
using System.Collections.ObjectModel;
using BEPUphysics.DataStructures;

namespace BEPUphysics
{
    ///<summary>
    /// Defines the minimum interface required for a Space object which acts as the main simulation class.
    ///</summary>
    public interface ISpace
    {
        ///<summary>
        /// Adds a space object to the simulation.
        ///</summary>
        ///<param name="spaceObject">Space object to add.</param>
        void Add(ISpaceObject spaceObject);
        ///<summary>
        /// Removes a space object from the simulation.
        ///</summary>
        ///<param name="spaceObject">Space object to remove.</param>
        void Remove(ISpaceObject spaceObject);

        ///<summary>
        /// Performs a single timestep.
        ///</summary>
        void Update();

        /// <summary>
        /// Performs as many timesteps as necessary to get as close to the elapsed time as possible.
        /// </summary>
        /// <param name="dt">Elapsed time from the previous frame.</param>
        void Update(float dt); //Does as many timesteps as necessary, obeying the timing requirements.

        ///<summary>
        /// Gets the list of entities in the space.
        ///</summary>
        ReadOnlyList<Entity> Entities { get; }

        /// <summary>
        /// Tests a ray against the space.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="result">Hit data of the ray, if any.</param>
        /// <returns>Whether or not the ray hit anything.</returns>
        bool RayCast(Ray ray, out RayCastResult result);

        /// <summary>
        /// Tests a ray against the space.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="filter">Delegate to prune out hit candidates before performing a ray cast against them.</param>
        /// <param name="result">Hit data of the ray, if any.</param>
        /// <returns>Whether or not the ray hit anything.</returns>
        bool RayCast(Ray ray, Func<BroadPhaseEntry, bool> filter, out RayCastResult result);

        /// <summary>
        /// Tests a ray against the space.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        /// <param name="result">Hit data of the ray, if any.</param>
        /// <returns>Whether or not the ray hit anything.</returns>
        bool RayCast(Ray ray, float maximumLength, out RayCastResult result);

        /// <summary>
        /// Tests a ray against the space.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        /// <param name="filter">Delegate to prune out hit candidates before performing a ray cast against them.</param>
        /// <param name="result">Hit data of the ray, if any.</param>
        /// <returns>Whether or not the ray hit anything.</returns>
        bool RayCast(Ray ray, float maximumLength, Func<BroadPhaseEntry, bool> filter, out RayCastResult result);

        /// <summary>
        /// Tests a ray against the space, possibly returning multiple hits.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        /// <param name="outputRayCastResults">Hit data of the ray, if any.</param>
        /// <returns>Whether or not the ray hit anything.</returns>
        bool RayCast(Ray ray, float maximumLength, IList<RayCastResult> outputRayCastResults);

        /// <summary>
        /// Tests a ray against the space, possibly returning multiple hits.
        /// </summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="maximumLength">Maximum length of the ray in units of the ray direction's length.</param>
        /// <param name="filter">Delegate to prune out hit candidates before performing a ray cast against them.</param>
        /// <param name="outputRayCastResults">Hit data of the ray, if any.</param>
        /// <returns>Whether or not the ray hit anything.</returns>
        bool RayCast(Ray ray, float maximumLength, Func<BroadPhaseEntry, bool> filter, IList<RayCastResult> outputRayCastResults);
    }
}
