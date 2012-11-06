using BEPUphysics.MathExtensions;
using BEPUphysics.Settings;
using Microsoft.Xna.Framework;
using BEPUphysics.DataStructures;
using System.Diagnostics;
using System;

namespace BEPUphysics.CollisionTests
{
    ///<summary>
    /// Helper class that refreshes manifolds to keep them recent.
    ///</summary>
    public class ContactRefresher
    {

        /// <summary>
        /// Refreshes the contact manifold, removing any out of date contacts
        /// and updating others.
        /// </summary>
        public static void ContactRefresh(RawList<Contact> contacts, RawValueList<ContactSupplementData> supplementData, ref RigidTransform transformA, ref RigidTransform transformB, RawList<int> toRemove)
        {
            //TODO: Could also refresh normals with some trickery.
            //Would also need to refresh depth using new normals, and would require some extra information.

            for (int k = 0; k < contacts.count; k++)
            {
                ContactSupplementData data = supplementData.Elements[k];
                Vector3 newPosA, newPosB;
                RigidTransform.Transform(ref data.LocalOffsetA, ref transformA, out newPosA);
                RigidTransform.Transform(ref data.LocalOffsetB, ref transformB, out newPosB);

                //ab - (ab*n)*n
                //Compute the horizontal offset.
                Vector3 ab;
                Vector3.Subtract(ref newPosB, ref newPosA, out ab);
                float dot;
                Vector3.Dot(ref ab, ref contacts.Elements[k].Normal, out dot);
                Vector3 temp;
                Vector3.Multiply(ref contacts.Elements[k].Normal, dot, out temp);
                Vector3.Subtract(ref ab, ref temp, out temp);
                dot = temp.LengthSquared();
                if (dot > CollisionDetectionSettings.ContactInvalidationLengthSquared)
                {
                    toRemove.Add(k);
                }
                else
                {
                    //Depth refresh:
                    //Find deviation (IE, (Ra-Rb)*N) and add to base depth.
                    Vector3.Dot(ref ab, ref contacts.Elements[k].Normal, out dot);
                    contacts.Elements[k].PenetrationDepth = data.BasePenetrationDepth - dot;
                    if (contacts.Elements[k].PenetrationDepth < -CollisionDetectionSettings.maximumContactDistance)
                        toRemove.Add(k);
                    else
                    {
                        //Refresh position and ra/rb.
                        Vector3 newPos;
                        Vector3.Add(ref newPosB, ref newPosA, out newPos);
                        Vector3.Multiply(ref newPos, .5f, out newPos);
                        contacts.Elements[k].Position = newPos;
                        //This is an interesting idea, but has very little effect one way or the other.
                        //data.BasePenetrationDepth = contacts.Elements[k].PenetrationDepth;
                        //RigidTransform.TransformByInverse(ref newPos, ref transformA, out data.LocalOffsetA);
                        //RigidTransform.TransformByInverse(ref newPos, ref transformB, out data.LocalOffsetB);
                    }
                }
               
            }
        }
    }
}
