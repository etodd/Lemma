using System;
using BEPUphysics.DataStructures;
using Microsoft.Xna.Framework;

namespace BEPUphysics.CollisionTests
{
    ///<summary>
    /// Helper class that reduces contact manifolds to reasonable numbers of contacts.
    ///</summary>
    public static class ContactReducer
    {
        //This works in the general case where there can be any  number of contacts and candidates.  Could specialize it as an optimization to single-contact added incremental manifolds.
        ///<summary>
        /// Reduces the contact manifold to a good subset.
        ///</summary>
        ///<param name="contacts">Contacts to reduce.</param>
        ///<param name="contactCandidates">Contact candidates to include in the reduction process.</param>
        ///<param name="contactsToRemove">Contacts that need to removed to reach the reduced state.</param>
        ///<param name="toAdd">Contact candidates that should be added to reach the reduced state.</param>
        ///<exception cref="InvalidOperationException">Thrown when the set being reduced is empty.</exception>
        public static void ReduceContacts(RawList<Contact> contacts, RawValueList<ContactData> contactCandidates, RawList<int> contactsToRemove, RawValueList<ContactData> toAdd)
        {
            //Find the deepest point of all contacts/candidates, as well as a compounded 'normal' vector.
            float maximumDepth = -float.MaxValue;
            int deepestIndex = -1;
            Vector3 normal = Toolbox.ZeroVector;
            for (int i = 0; i < contacts.count; i++)
            {
                Vector3.Add(ref normal, ref contacts.Elements[i].Normal, out normal);
                if (contacts.Elements[i].PenetrationDepth > maximumDepth)
                {
                    deepestIndex = i;
                    maximumDepth = contacts.Elements[i].PenetrationDepth;
                }
            }
            for (int i = 0; i < contactCandidates.count; i++)
            {
                Vector3.Add(ref normal, ref contactCandidates.Elements[i].Normal, out normal);
                if (contactCandidates.Elements[i].PenetrationDepth > maximumDepth)
                {
                    deepestIndex = contacts.count + i;
                    maximumDepth = contactCandidates.Elements[i].PenetrationDepth;
                }
            }
            //If the normals oppose each other, this can happen.  It doesn't need to be normalized, but having SOME normal is necessary.
            if (normal.LengthSquared() < Toolbox.Epsilon)
                if (contacts.count > 0)
                    normal = contacts.Elements[0].Normal;
                else if (contactCandidates.count > 0)
                    normal = contactCandidates.Elements[0].Normal; //This method is only called when there's too many contacts, so if contacts is empty, the candidates must NOT be empty.
                else //This method should not have been called at all if it gets here.
                    throw new ArgumentException("Cannot reduce an empty contact set.");


            //Find the contact (candidate) that is furthest away from the deepest contact (candidate).
            Vector3 deepestPosition;
            if (deepestIndex < contacts.count)
                deepestPosition = contacts.Elements[deepestIndex].Position;
            else
                deepestPosition = contactCandidates.Elements[deepestIndex - contacts.count].Position;
            float distanceSquared;
            float furthestDistance = 0;
            int furthestIndex = -1;
            for (int i = 0; i < contacts.count; i++)
            {
                Vector3.DistanceSquared(ref contacts.Elements[i].Position, ref deepestPosition, out distanceSquared);
                if (distanceSquared > furthestDistance)
                {
                    furthestDistance = distanceSquared;
                    furthestIndex = i;
                }
            }
            for (int i = 0; i < contactCandidates.count; i++)
            {
                Vector3.DistanceSquared(ref contactCandidates.Elements[i].Position, ref deepestPosition, out distanceSquared);
                if (distanceSquared > furthestDistance)
                {
                    furthestDistance = distanceSquared;
                    furthestIndex = contacts.count + i;
                }
            }
            if (furthestIndex == -1)
            {
                //Either this method was called when it shouldn't have been, or all contacts and contact candidates are at the same location.
                if (contacts.count > 0)
                {
                    for (int i = 1; i < contacts.count; i++)
                    {
                        contactsToRemove.Add(i);
                    }
                    return;
                }
                if (contactCandidates.count > 0)
                {
                    toAdd.Add(ref contactCandidates.Elements[0]);
                    return;
                }
                throw new ArgumentException("Cannot reduce an empty contact set.");

            }
            Vector3 furthestPosition;
            if (furthestIndex < contacts.count)
                furthestPosition = contacts.Elements[furthestIndex].Position;
            else
                furthestPosition = contactCandidates.Elements[furthestIndex - contacts.count].Position;
            Vector3 xAxis;
            Vector3.Subtract(ref deepestPosition, ref furthestPosition, out xAxis);

            //Create the second axis of the 2d 'coordinate system' of the manifold.
            Vector3 yAxis;
            Vector3.Cross(ref xAxis, ref normal, out yAxis);

            //Determine the furthest points along the axis.
            float minYAxisDot = float.MaxValue, maxYAxisDot = -float.MaxValue;
            int minYAxisIndex = -1, maxYAxisIndex = -1;

            for (int i = 0; i < contacts.count; i++)
            {
                float dot;
                Vector3.Dot(ref contacts.Elements[i].Position, ref yAxis, out dot);
                if (dot < minYAxisDot)
                {
                    minYAxisIndex = i;
                    minYAxisDot = dot;
                }
                if (dot > maxYAxisDot)
                {
                    maxYAxisIndex = i;
                    maxYAxisDot = dot;
                }

            }
            for (int i = 0; i < contactCandidates.count; i++)
            {
                float dot;
                Vector3.Dot(ref contactCandidates.Elements[i].Position, ref yAxis, out dot);
                if (dot < minYAxisDot)
                {
                    minYAxisIndex = i + contacts.count;
                    minYAxisDot = dot;
                }
                if (dot > maxYAxisDot)
                {
                    maxYAxisIndex = i + contacts.count;
                    maxYAxisDot = dot;
                }

            }

            //the deepestIndex, furthestIndex, minYAxisIndex, and maxYAxisIndex are the extremal points.
            //Cycle through the existing contacts.  If any DO NOT MATCH the existing candidates, add them to the toRemove list.
            //Cycle through the candidates.  If any match, add them to the toAdd list.

            //Repeated entries in the reduced manifold aren't a problem.
            //-Contacts list does not include repeats with itself.
            //-A contact is only removed if it doesn't match anything.

            //-Contact candidates do not repeat with themselves.
            //-Contact candidates do not repeat with contacts.
            //-Contact candidates are added if they match any of the indices.

            for (int i = 0; i < contactCandidates.count; i++)
            {
                int totalIndex = i + contacts.count;
                if (totalIndex == deepestIndex || totalIndex == furthestIndex || totalIndex == minYAxisIndex || totalIndex == maxYAxisIndex)
                {
                    //This contact is present in the new manifold.  Add it.
                    toAdd.Add(ref contactCandidates.Elements[i]);
                }
            }
            for (int i = 0; i < contacts.count; i++)
            {
                if (!(i == deepestIndex || i == furthestIndex || i == minYAxisIndex || i == maxYAxisIndex))
                {
                    //This contact is not present in the new manifold.  Remove it.
                    contactsToRemove.Add(i);
                }
            }



        }


        //This works in the specific case of 4 contacts and 1 contact candidate.
        ///<summary>
        /// Reduces a 4-contact manifold and contact candidate to 4 total contacts.
        ///</summary>
        ///<param name="contacts">Contacts to reduce.</param>
        ///<param name="contactCandidate">Contact candidate to include in the reduction process.</param>
        ///<param name="toRemove">Contacts that need to be removed to reduce the manifold.</param>
        ///<param name="addCandidate">Whether or not to add the contact candidate to reach the reduced manifold.</param>
        ///<exception cref="ArgumentException">Thrown when the contact manifold being reduced doesn't have 4 contacts.</exception>
        public static void ReduceContacts(RawList<Contact> contacts, ref ContactData contactCandidate, RawList<int> toRemove, out bool addCandidate)
        {
            if (contacts.count != 4)
                throw new ArgumentException("Can only use this method to reduce contact lists with four contacts and a contact candidate.");

            //addCandidate = true;
            //float min = float.MaxValue;
            //int minIndex = 3;
            //for (int i = 0; i < 4; i++)
            //{
            //    if (contacts.Elements[i].PenetrationDepth < min)
            //    {
            //        min = contacts.Elements[i].PenetrationDepth;
            //        minIndex = i;
            //    }
            //}
            //toRemove.Add(minIndex);
            //return;

            //Find the deepest point of all contacts/candidates, as well as a compounded 'normal' vector.
            float maximumDepth = -float.MaxValue;
            int deepestIndex = -1;
            for (int i = 0; i < 4; i++)
            {
                if (contacts.Elements[i].PenetrationDepth > maximumDepth)
                {
                    deepestIndex = i;
                    maximumDepth = contacts.Elements[i].PenetrationDepth;
                }
            }
            if (contactCandidate.PenetrationDepth > maximumDepth)
            {
                deepestIndex = 4;
            }


            //Find the contact (candidate) that is furthest away from the deepest contact (candidate).
            Vector3 deepestPosition;
            if (deepestIndex < 4)
                deepestPosition = contacts.Elements[deepestIndex].Position;
            else
                deepestPosition = contactCandidate.Position;
            float distanceSquared;
            float furthestDistance = 0;
            int furthestIndex = -1;
            for (int i = 0; i < 4; i++)
            {
                Vector3.DistanceSquared(ref contacts.Elements[i].Position, ref deepestPosition, out distanceSquared);
                if (distanceSquared > furthestDistance)
                {
                    furthestDistance = distanceSquared;
                    furthestIndex = i;
                }
            }

            Vector3.DistanceSquared(ref contactCandidate.Position, ref deepestPosition, out distanceSquared);
            if (distanceSquared > furthestDistance)
            {
                furthestIndex = 4;
            }
            Vector3 furthestPosition;
            if (furthestIndex < contacts.count)
                furthestPosition = contacts.Elements[furthestIndex].Position;
            else
                furthestPosition = contactCandidate.Position;
            Vector3 xAxis;
            Vector3.Subtract(ref deepestPosition, ref furthestPosition, out xAxis);

            //Create the second axis of the 2d 'coordinate system' of the manifold.
            Vector3 yAxis;
            Vector3.Cross(ref xAxis, ref contacts.Elements[0].Normal, out yAxis);

            //Determine the furthest points along the axis.
            float minYAxisDot = float.MaxValue, maxYAxisDot = -float.MaxValue;
            int minYAxisIndex = -1, maxYAxisIndex = -1;

            float dot;
            for (int i = 0; i < 4; i++)
            {
                Vector3.Dot(ref contacts.Elements[i].Position, ref yAxis, out dot);
                if (dot < minYAxisDot)
                {
                    minYAxisIndex = i;
                    minYAxisDot = dot;
                }
                if (dot > maxYAxisDot)
                {
                    maxYAxisIndex = i;
                    maxYAxisDot = dot;
                }

            }
            Vector3.Dot(ref contactCandidate.Position, ref yAxis, out dot);
            if (dot < minYAxisDot)
            {
                minYAxisIndex = 4;
            }
            if (dot > maxYAxisDot)
            {
                maxYAxisIndex = 4;
            }

            //the deepestIndex, furthestIndex, minYAxisIndex, and maxYAxisIndex are the extremal points.
            //Cycle through the existing contacts.  If any DO NOT MATCH the existing candidates, add them to the toRemove list.
            //Cycle through the candidates.  If any match, add them to the toAdd list.

            //Repeated entries in the reduced manifold aren't a problem.
            //-Contacts list does not include repeats with itself.
            //-A contact is only removed if it doesn't match anything.

            //-Contact candidates do not repeat with themselves.
            //-Contact candidates do not repeat with contacts.
            //-Contact candidates are added if they match any of the indices.

            if (4 == deepestIndex || 4 == furthestIndex || 4 == minYAxisIndex || 4 == maxYAxisIndex)
            {

                addCandidate = true;
                //Only reduce when we are going to add a new contact, and only get rid of one.
                for (int i = 0; i < 4; i++)
                {
                    if (!(i == deepestIndex || i == furthestIndex || i == minYAxisIndex || i == maxYAxisIndex))
                    {
                        //This contact is not present in the new manifold.  Remove it.
                        toRemove.Add(i);
                        break;
                    }
                }
            }
            else
                addCandidate = false;



        }
    }
}
