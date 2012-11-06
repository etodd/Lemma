using System;
using System.Collections.Generic;
using BEPUphysics.Entities;
using BEPUphysics.MathExtensions;
using Microsoft.Xna.Framework;

namespace BEPUphysics.Constraints
{
    /// <summary>
    /// Solves the penetration constraints in a collision pair at once.
    /// </summary>
    internal class DirectEnumerationSolver
    {
        private const float ConditionNumberLimit = 1000;
        private readonly Vector3[] angularA = new Vector3[4];
        private readonly Vector3[] angularB = new Vector3[4];
        private readonly List<int> contactIndicesUsed = new List<int>(4);
        private readonly CollisionPair pair;
        private int contactCount;
        private Vector3 linear;
        private Entity parentA, parentB;

        //Jacobian is of the form
        //LinA1 AngA1 LinB1 AngB1
        //LinA2 AngA2 LinB2 AngB2
        //LinA3 AngA3 LinB3 AngB3
        //LinA4 AngA4 LinB4 AngB4
        //4x12 matrix.

        //Now, it just so happens that all linear entries are equal (B's are negative).
        //Angular entries are not equal.

        //4x12 Jacobian for 4 contacts:
        //[  n  ra1 x n  -n  n x rb1  ]
        //[  n  ra2 x n  -n  n x rb2  ]
        //[  n  ra3 x n  -n  n x rb3  ]
        //[  n  ra4 x n  -n  n x rb4  ]


        private Matrix velocityToImpulse;
        private Matrix3X3 velocityToImpulse3X3;

        internal DirectEnumerationSolver(CollisionPair pair)
        {
            this.pair = pair;
        }

        internal void ApplyImpulse()
        {
            switch (contactIndicesUsed.Count)
            {
                case 4:
                    //Compute relative velocities.
#if !WINDOWS
                Vector4 lambda = new Vector4();
#else
                    Vector4 lambda;
#endif
                    lambda.X = -GetRelativeVelocity(0);
                    lambda.Y = -GetRelativeVelocity(1);
                    lambda.Z = -GetRelativeVelocity(2);
                    lambda.W = -GetRelativeVelocity(3);

                    //Transform to impulse
                    Vector4.Transform(ref lambda, ref velocityToImpulse, out lambda);


                    //if the solution is acceptable, apply and return
                    if (lambda.X > 0 && lambda.Y > 0 && lambda.Z > 0 && lambda.W > 0)
                    {
#if !WINDOWS
                    Vector3 linearImpulse = new Vector3(), angularImpulse = new Vector3();
#else
                        Vector3 linearImpulse, angularImpulse;
#endif
                        float lambdaSum = lambda.X + lambda.Y + lambda.Z + lambda.W;
                        linearImpulse.X = linear.X * lambdaSum;
                        linearImpulse.Y = linear.Y * lambdaSum;
                        linearImpulse.Z = linear.Z * lambdaSum;
                        if (parentA.isDynamic)
                        {
                            angularImpulse.X = angularA[0].X * lambda.X + angularA[1].X * lambda.Y + angularA[2].X * lambda.Z + angularA[3].X * lambda.W;
                            angularImpulse.Y = angularA[0].Y * lambda.X + angularA[1].Y * lambda.Y + angularA[2].Y * lambda.Z + angularA[3].Y * lambda.W;
                            angularImpulse.Z = angularA[0].Z * lambda.X + angularA[1].Z * lambda.Y + angularA[2].Z * lambda.Z + angularA[3].Z * lambda.W;
                            parentA.ApplyLinearImpulse(ref linearImpulse);
                            parentA.ApplyAngularImpulse(ref angularImpulse);
                        }
                        if (parentB.isDynamic)
                        {
                            linearImpulse.X = -linearImpulse.X;
                            linearImpulse.Y = -linearImpulse.Y;
                            linearImpulse.Z = -linearImpulse.Z;
                            angularImpulse.X = angularB[0].X * lambda.X + angularB[1].X * lambda.Y + angularB[2].X * lambda.Z + angularB[3].X * lambda.W;
                            angularImpulse.Y = angularB[0].Y * lambda.X + angularB[1].Y * lambda.Y + angularB[2].Y * lambda.Z + angularB[3].Y * lambda.W;
                            angularImpulse.Z = angularB[0].Z * lambda.X + angularB[1].Z * lambda.Y + angularB[2].Z * lambda.Z + angularB[3].Z * lambda.W;
                            parentB.ApplyLinearImpulse(ref linearImpulse);
                            parentB.ApplyAngularImpulse(ref angularImpulse);
                        }
                        return;
                    }
                    break;
                case 3:
#if !WINDOWS
                    Vector3 lambda3 = new Vector3();
#else
                    Vector3 lambda3;
#endif
                    lambda3.X = -GetRelativeVelocity(contactIndicesUsed[0]);
                    lambda3.Y = -GetRelativeVelocity(contactIndicesUsed[1]);
                    lambda3.Z = -GetRelativeVelocity(contactIndicesUsed[2]);

                    //Transform to impulse
                    Matrix3X3.Transform(ref lambda3, ref velocityToImpulse3X3, out lambda3);


                    //if the solution is acceptable, apply and return
                    if (lambda3.X >= 0 && lambda3.Y >= 0 && lambda3.Z >= 0)
                    {
#if !WINDOWS
                    Vector3 linearImpulse = new Vector3(), angularImpulse = new Vector3();
#else
                        Vector3 linearImpulse, angularImpulse;
#endif
                        float lambdaSum = lambda3.X + lambda3.Y + lambda3.Z;
                        linearImpulse.X = linear.X * lambdaSum;
                        linearImpulse.Y = linear.Y * lambdaSum;
                        linearImpulse.Z = linear.Z * lambdaSum;
                        if (parentA.isDynamic)
                        {
                            angularImpulse.X = angularA[contactIndicesUsed[0]].X * lambda3.X + angularA[contactIndicesUsed[1]].X * lambda3.Y + angularA[contactIndicesUsed[2]].X * lambda3.Z;
                            angularImpulse.Y = angularA[contactIndicesUsed[0]].Y * lambda3.X + angularA[contactIndicesUsed[1]].Y * lambda3.Y + angularA[contactIndicesUsed[2]].Y * lambda3.Z;
                            angularImpulse.Z = angularA[contactIndicesUsed[0]].Z * lambda3.X + angularA[contactIndicesUsed[1]].Z * lambda3.Y + angularA[contactIndicesUsed[2]].Z * lambda3.Z;
                            parentA.ApplyLinearImpulse(ref linearImpulse);
                            parentA.ApplyAngularImpulse(ref angularImpulse);
                        }
                        if (parentB.isDynamic)
                        {
                            linearImpulse.X = -linearImpulse.X;
                            linearImpulse.Y = -linearImpulse.Y;
                            linearImpulse.Z = -linearImpulse.Z;
                            angularImpulse.X = angularB[contactIndicesUsed[0]].X * lambda3.X + angularB[contactIndicesUsed[1]].X * lambda3.Y + angularB[contactIndicesUsed[2]].X * lambda3.Z;
                            angularImpulse.Y = angularB[contactIndicesUsed[0]].Y * lambda3.X + angularB[contactIndicesUsed[1]].Y * lambda3.Y + angularB[contactIndicesUsed[2]].Y * lambda3.Z;
                            angularImpulse.Z = angularB[contactIndicesUsed[0]].Z * lambda3.X + angularB[contactIndicesUsed[1]].Z * lambda3.Y + angularB[contactIndicesUsed[2]].Z * lambda3.Z;
                            parentB.ApplyLinearImpulse(ref linearImpulse);
                            parentB.ApplyAngularImpulse(ref angularImpulse);
                        }
                        return;
                    }
                    break;
            }
            //Note: No accumulated impulses yet.

            //Note: There's no 'sleeping' here.  It's going to be real slow.
            //foreach (Contact c in pair.contacts)
            //{
            //    c.penetrationConstraint.applyImpulse();
            //}
        }

        internal void PreStep(float dt)
        {
            parentA = pair.ParentA;
            parentB = pair.ParentB;
            contactCount = pair.Contacts.Count;

            if (contactCount > 0)
            {
                //Populate the jacobian
                linear = pair.Contacts[0].Normal;
                for (int i = 0; i < contactCount; i++)
                {
                    angularA[i] = new Vector3(pair.Contacts[i].penetrationConstraint.angularAX, pair.Contacts[i].penetrationConstraint.angularAY, pair.Contacts[i].penetrationConstraint.angularAZ);
                    angularB[i] = new Vector3(pair.Contacts[i].penetrationConstraint.angularBX, pair.Contacts[i].penetrationConstraint.angularBY, pair.Contacts[i].penetrationConstraint.angularBZ);
                    //TODO: Penetration constraint calculates the diagonal entries already.
                    //Could also cache out the angular[i] * I^-1 part, which would then speed up mass matrix calculation.
                    //massMatrixInverse[i][i] = pair.contacts[0].penetrationConstraint.velocityToImpulse;
                }
            }
            contactIndicesUsed.Clear();
            switch (contactCount)
            {
                case 4:

                    //Create the effective mass matrix
                    //Could optimize this since the penetration constraints need to compute the diagonal anyway.
                    //Also, it does a lot of unnecessary 1/mass calculations.

                    Matrix inverse;
                    if (Get4X4MassMatrix(out inverse, out velocityToImpulse))
                    {
                        //The 4x4 version is acceptable.
                        contactIndicesUsed.Add(0);
                        contactIndicesUsed.Add(1);
                        contactIndicesUsed.Add(2);
                        contactIndicesUsed.Add(3);
                    }
                        //TODO: Okay, this is a 'bad matrix' even in the ideal 4 point manifold situation.  Is this to be expected, or is the above mass matrix entry calculation screwed up?
                        //Yea.
                        //J * M^-1 * JT could be singular in the case of redundant contacts.  Pretest? Gramian, condition number..

                    else
                    {
                        Matrix3X3 inverse3X3;
                        if (Get3X3InverseMassMatrix(1, 2, 3, out inverse3X3, out velocityToImpulse3X3))
                        {
                            contactIndicesUsed.Add(1);
                            contactIndicesUsed.Add(2);
                            contactIndicesUsed.Add(3);
                        }
                        else if (Get3X3InverseMassMatrix(0, 2, 3, out inverse3X3, out velocityToImpulse3X3))
                        {
                            contactIndicesUsed.Add(0);
                            contactIndicesUsed.Add(2);
                            contactIndicesUsed.Add(3);
                        }
                        else if (Get3X3InverseMassMatrix(0, 1, 3, out inverse3X3, out velocityToImpulse3X3))
                        {
                            contactIndicesUsed.Add(0);
                            contactIndicesUsed.Add(1);
                            contactIndicesUsed.Add(3);
                        }
                        else if (Get3X3InverseMassMatrix(0, 1, 2, out inverse3X3, out velocityToImpulse3X3))
                        {
                            contactIndicesUsed.Add(0);
                            contactIndicesUsed.Add(1);
                            contactIndicesUsed.Add(2);
                        }
                    }

                    break;
                case 3:

                    break;
            }
        }


        private bool Get2X2InverseMassMatrix(int indexA, int indexB, out Matrix2X2 massMatrix)
        {
            massMatrix.M11 = GetMassMatrixEntry(indexA, indexA);
            massMatrix.M12 = GetMassMatrixEntry(indexA, indexB);

            massMatrix.M21 = massMatrix.M12; // getMassMatrixEntry(indexB, indexA);
            massMatrix.M22 = GetMassMatrixEntry(indexB, indexB);

            return ComputeNorm(ref massMatrix) < ConditionNumberLimit;
        }

        private bool Get3X3InverseMassMatrix(int indexA, int indexB, int indexC, out Matrix3X3 inverseMassMatrix, out Matrix3X3 massMatrix)
        {
            inverseMassMatrix.M11 = GetMassMatrixEntry(indexA, indexA);
            inverseMassMatrix.M12 = GetMassMatrixEntry(indexA, indexB);
            inverseMassMatrix.M13 = GetMassMatrixEntry(indexA, indexC);

            inverseMassMatrix.M21 = inverseMassMatrix.M12; // getMassMatrixEntry(indexB, indexA);
            inverseMassMatrix.M22 = GetMassMatrixEntry(indexB, indexB);
            inverseMassMatrix.M23 = GetMassMatrixEntry(indexB, indexC);

            inverseMassMatrix.M31 = inverseMassMatrix.M13; // getMassMatrixEntry(indexC, indexA);
            inverseMassMatrix.M32 = inverseMassMatrix.M23; // getMassMatrixEntry(indexC, indexB);
            inverseMassMatrix.M33 = GetMassMatrixEntry(indexC, indexC);

            Matrix3X3.Invert(ref inverseMassMatrix, out massMatrix);

            return ComputeNorm(ref inverseMassMatrix) * ComputeNorm(ref massMatrix) < ConditionNumberLimit;
        }

        private bool Get4X4MassMatrix(out Matrix inverseMassMatrix, out Matrix massMatrix)
        {
#if !WINDOWS
            inverseMassMatrix = new Matrix();
#endif
            inverseMassMatrix.M11 = GetMassMatrixEntry(0, 0);
            inverseMassMatrix.M12 = GetMassMatrixEntry(0, 1);
            inverseMassMatrix.M13 = GetMassMatrixEntry(0, 2);
            inverseMassMatrix.M14 = GetMassMatrixEntry(0, 3);

            inverseMassMatrix.M21 = inverseMassMatrix.M12; // getMassMatrixEntry(1, 0);
            inverseMassMatrix.M22 = GetMassMatrixEntry(1, 1);
            inverseMassMatrix.M23 = GetMassMatrixEntry(1, 2);
            inverseMassMatrix.M24 = GetMassMatrixEntry(1, 3);

            inverseMassMatrix.M31 = inverseMassMatrix.M13; // getMassMatrixEntry(2, 0);
            inverseMassMatrix.M32 = inverseMassMatrix.M23; // getMassMatrixEntry(2, 1);
            inverseMassMatrix.M33 = GetMassMatrixEntry(2, 2);
            inverseMassMatrix.M34 = GetMassMatrixEntry(2, 3);

            inverseMassMatrix.M41 = inverseMassMatrix.M14; // getMassMatrixEntry(3, 0);
            inverseMassMatrix.M42 = inverseMassMatrix.M24; // getMassMatrixEntry(3, 1);
            inverseMassMatrix.M43 = inverseMassMatrix.M34; // getMassMatrixEntry(3, 2);
            inverseMassMatrix.M44 = GetMassMatrixEntry(3, 3);

            Matrix.Invert(ref inverseMassMatrix, out massMatrix);

            return ComputeNorm(ref inverseMassMatrix) * ComputeNorm(ref massMatrix) < ConditionNumberLimit;
        }

        //TODO: DON'T JUST ADD 1/MASS!!!!!! look at Point on Line Joint formulation redux for more information
        //TODO: ON the other hand, uhh.. normal * normal is always 1 no matter how you look at it.  LOOK AT IT MORE

        private float GetMassMatrixEntry(int i, int j)
        {
            //This is wasteful; there are 4 angular jacobians for each entity in 4x4 case.
            //Even with the reduced 10-element only mass matrix calculation, there is significant
            //retransforming going on.
            //Re-used values:
            //A[0] * Ia^-1
            //A[1] * Ia^-1
            //A[2] * Ia^-1
            //A[3] * Ia^-1
            //B[0] * Ib^-1
            //B[1] * Ib^-1
            //B[2] * Ib^-1
            //B[3] * Ib^-1
            //A, B inverse mass
            //The waste gets more wastey when the 4x4 fails and it has to do even more calculations...
            //In fact, the matrix entries themselves are redundant.
            //Can construct smaller matrices from bigger matrices????? Maybe...
            //Since getMassMatrixEntry is the same for 1x1, 2x2, 3x3, and 4x4 versions (1 2 3 4 contacts)...
            //me^-1 i,j is unique based on i,j alone!!!
            //Compute "max size matrix" and derive smaller ones directly from it :)))
            //Technically only the inverse mass matrix shares entries.  Post inversion to mass matrix, they will be different.
            float entryA, entryB;
            Vector3 transform;
            if (parentA.isDynamic)
            {
                Matrix3X3.Transform(ref angularA[i], ref parentA.inertiaTensorInverse, out transform);
                Vector3.Dot(ref angularA[j], ref transform, out entryA);
                entryA += 1 / parentA.mass;
            }
            else
                entryA = 0;

            if (parentB.isDynamic)
            {
                Matrix3X3.Transform(ref angularB[i], ref parentB.inertiaTensorInverse, out transform);
                Vector3.Dot(ref angularB[j], ref transform, out entryB);
                entryB += 1 / parentB.mass;
            }
            else
                entryB = 0;

            return entryA + entryB;
        }

        private float GetRelativeVelocity(int i)
        {
            float relativeVelocity, dot;
            Vector3.Dot(ref linear, ref parentA.linearVelocity, out relativeVelocity);
            Vector3.Dot(ref angularA[i], ref parentA.angularVelocity, out dot);
            relativeVelocity += dot;
            Vector3.Dot(ref linear, ref parentB.linearVelocity, out dot);
            relativeVelocity -= dot;
            Vector3.Dot(ref angularB[i], ref parentB.angularVelocity, out dot);
            return relativeVelocity + dot;
        }

        private float GetSimulatedRelativeVelocity(int i, ref Vector3 linearVelocityA, ref Vector3 linearVelocityB, ref Vector3 angularVelocityA, ref Vector3 angularVelocityB)
        {
            float relativeVelocity, dot;
            Vector3.Dot(ref linear, ref linearVelocityA, out relativeVelocity);
            Vector3.Dot(ref angularA[i], ref angularVelocityA, out dot);
            relativeVelocity += dot;
            Vector3.Dot(ref linear, ref linearVelocityB, out dot);
            relativeVelocity -= dot;
            Vector3.Dot(ref angularB[i], ref angularVelocityB, out dot);
            return relativeVelocity + dot;
        }

        #region Norms

        private static float ComputeNorm(ref Matrix m)
        {
            //Would a square-based norm be faster and sufficient ?
            //Huge number of branches in this
            float norm = MathHelper.Max(Math.Abs(m.M11), Math.Abs(m.M12));
            norm = MathHelper.Max(norm, Math.Abs(m.M13));
            norm = MathHelper.Max(norm, Math.Abs(m.M14));

            norm = MathHelper.Max(norm, Math.Abs(m.M21));
            norm = MathHelper.Max(norm, Math.Abs(m.M22));
            norm = MathHelper.Max(norm, Math.Abs(m.M23));
            norm = MathHelper.Max(norm, Math.Abs(m.M24));

            norm = MathHelper.Max(norm, Math.Abs(m.M31));
            norm = MathHelper.Max(norm, Math.Abs(m.M32));
            norm = MathHelper.Max(norm, Math.Abs(m.M33));
            norm = MathHelper.Max(norm, Math.Abs(m.M34));

            norm = MathHelper.Max(norm, Math.Abs(m.M41));
            norm = MathHelper.Max(norm, Math.Abs(m.M42));
            norm = MathHelper.Max(norm, Math.Abs(m.M43));
            norm = MathHelper.Max(norm, Math.Abs(m.M44));

            return norm;
        }

        private static float ComputeNorm(ref Matrix3X3 m)
        {
            //Would a square-based norm be faster and sufficient ?
            //Huge number of branches in this
            float norm = MathHelper.Max(Math.Abs(m.M11), Math.Abs(m.M12));
            norm = MathHelper.Max(norm, Math.Abs(m.M13));

            norm = MathHelper.Max(norm, Math.Abs(m.M21));
            norm = MathHelper.Max(norm, Math.Abs(m.M22));
            norm = MathHelper.Max(norm, Math.Abs(m.M23));

            norm = MathHelper.Max(norm, Math.Abs(m.M31));
            norm = MathHelper.Max(norm, Math.Abs(m.M32));
            norm = MathHelper.Max(norm, Math.Abs(m.M33));

            return norm;
        }

        private static float ComputeNorm(ref Matrix2X2 m)
        {
            //Would a square-based norm be faster and sufficient ?
            //Huge number of branches in this
            float norm = MathHelper.Max(Math.Abs(m.M11), Math.Abs(m.M12));

            norm = MathHelper.Max(norm, Math.Abs(m.M21));
            norm = MathHelper.Max(norm, Math.Abs(m.M22));

            return norm;
        }

        #endregion
    }
}