using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using BEPUphysics.ResourceManagement;

namespace BEPUphysics.DataStructures
{    
    ///<summary>
    /// Acceleration structure of triangles surrounded by axis aligned bounding boxes, supporting various speedy queries.
    ///</summary>
    public class MeshBoundingBoxTree
    {
        MeshBoundingBoxTreeData data;


        /// <summary>
        /// Gets the bounding box surrounding the tree.
        /// </summary>
        public BoundingBox BoundingBox
        {
            get
            {
                if (root != null)
                    return root.BoundingBox;
                else 
                    return new BoundingBox();
            }
        }

        Node root;

        /// <summary>
        /// Gets or sets the data used to construct the tree.
        /// When set, the tree will be reconstructed.
        /// </summary>
        public MeshBoundingBoxTreeData Data
        {
            get
            {
                return data;
            }
            set
            {
                this.data = value;
                Reconstruct();
            }
        }

        /// <summary>
        /// Constructs a new tree.
        /// </summary>
        /// <param name="data">Data to use to construct the tree.</param>
        public MeshBoundingBoxTree(MeshBoundingBoxTreeData data)
        {
            Data = data;
        }


        /// <summary>
        /// Reconstructs the tree based on the current data.
        /// </summary>
        public void Reconstruct()
        {
            root = null;
            for (int i = 0; i < data.indices.Length; i += 3)
            {
                //Use a permuted version of the triangles instead of the actual triangle list.
                //Permuting makes the input basically random, improving the quality of the tree.
                Insert((int)(((982451653L * (i / 3)) % (data.indices.Length / 3)) * 3));
            }
        }

        /// <summary>
        /// Refits the tree based on the current data.
        /// This process is cheaper to perform than a reconstruction when the topology of the mesh
        /// does not change.
        /// </summary>
        public void Refit()
        {
            if (root != null)
                root.Refit(data);
        }

        void Analyze(out List<int> depths, out int minDepth, out int maxDepth, out int nodeCount)
        {
            depths = new List<int>();
            nodeCount = 0;
            root.Analyze(depths, 0, ref nodeCount);

            maxDepth = 0;
            minDepth = int.MaxValue;
            for (int i = 0; i < depths.Count; i++)
            {
                if (depths[i] > maxDepth)
                    maxDepth = depths[i];
                if (depths[i] < minDepth)
                    minDepth = depths[i];
            }
        }


        void Insert(int triangleIndex)
        {
            //Insertions can easily be performed stacklessly.
            //Only one path is chosen at each step and nothing is returned, so the history of the 'recursion' is completely forgotten.

            var node = new LeafNode(triangleIndex, data);
            if (root == null)
            {
                //Empty tree.  This is the first and only node.
                root = node;
            }
            else
            {
                if (root.IsLeaf) //Root is alone.
                    root.TryToInsert(node, out root);
                else
                {
                    //The caller is responsible for the merge.
                    BoundingBox.CreateMerged(ref node.BoundingBox, ref root.BoundingBox, out root.BoundingBox);
                    Node treeNode = root;
                    while (!treeNode.TryToInsert(node, out treeNode)) ;//TryToInsert returns the next node, if any, and updates node bounding box.
                }
            }
        }

        /// <summary>
        /// Gets the triangles whose bounding boxes are overlapped by the query.
        /// </summary>
        /// <param name="boundingBox">Shape to query against the tree.</param>
        /// <param name="outputOverlappedElements">Indices of triangles in the index buffer with bounding boxes which are overlapped by the query.</param>
        /// <returns>Whether or not any elements were overlapped.</returns>
        public bool GetOverlaps(BoundingBox boundingBox, IList<int> outputOverlappedElements)
        {
            if (root != null)
            {
                bool intersects;
                root.BoundingBox.Intersects(ref boundingBox, out intersects);
                if (intersects)
                    root.GetOverlaps(ref boundingBox, outputOverlappedElements);
            }
            return outputOverlappedElements.Count > 0;
        }

        /// <summary>
        /// Gets the triangles whose bounding boxes are overlapped by the query.
        /// </summary>
        /// <param name="boundingSphere">Shape to query against the tree.</param>
        /// <param name="outputOverlappedElements">Indices of triangles in the index buffer with bounding boxes which are overlapped by the query.</param>
        /// <returns>Whether or not any elements were overlapped.</returns>
        public bool GetOverlaps(BoundingSphere boundingSphere, IList<int> outputOverlappedElements)
        {
            if (root != null)
            {
                bool intersects;
                root.BoundingBox.Intersects(ref boundingSphere, out intersects);
                if (intersects)
                    root.GetOverlaps(ref boundingSphere, outputOverlappedElements);
            }
            return outputOverlappedElements.Count > 0;
        }
        /// <summary>
        /// Gets the triangles whose bounding boxes are overlapped by the query.
        /// </summary>
        /// <param name="boundingFrustum">Shape to query against the tree.</param>
        /// <param name="outputOverlappedElements">Indices of triangles in the index buffer with bounding boxes which are overlapped by the query.</param>
        /// <returns>Whether or not any elements were overlapped.</returns>
        public bool GetOverlaps(BoundingFrustum boundingFrustum, IList<int> outputOverlappedElements)
        {
            if (root != null)
            {
                bool intersects;
                boundingFrustum.Intersects(ref root.BoundingBox, out intersects);
                if (intersects)
                    root.GetOverlaps(ref boundingFrustum, outputOverlappedElements);
            }
            return outputOverlappedElements.Count > 0;
        }
        /// <summary>
        /// Gets the triangles whose bounding boxes are overlapped by the query.
        /// </summary>
        /// <param name="ray">Shape to query against the tree.</param>
        /// <param name="outputOverlappedElements">Indices of triangles in the index buffer with bounding boxes which are overlapped by the query.</param>
        /// <returns>Whether or not any elements were overlapped.</returns>
        public bool GetOverlaps(Ray ray, IList<int> outputOverlappedElements)
        {
            if (root != null)
            {
                float? result;
                ray.Intersects(ref root.BoundingBox, out result);
                if (result != null)
                    root.GetOverlaps(ref ray, float.MaxValue, outputOverlappedElements);
            }
            return outputOverlappedElements.Count > 0;
        }
        /// <summary>
        /// Gets the triangles whose bounding boxes are overlapped by the query.
        /// </summary>
        /// <param name="ray">Shape to query against the tree.</param>
        /// <param name="maximumLength">Maximum length of the ray in units of the ray's length.</param>
        /// <param name="outputOverlappedElements">Indices of triangles in the index buffer with bounding boxes which are overlapped by the query.</param>
        /// <returns>Whether or not any elements were overlapped.</returns>
        public bool GetOverlaps(Ray ray, float maximumLength, IList<int> outputOverlappedElements)
        {
            if (root != null)
            {
                float? result;
                ray.Intersects(ref root.BoundingBox, out result);
                if (result != null)
                    root.GetOverlaps(ref ray, maximumLength, outputOverlappedElements);
            }
            return outputOverlappedElements.Count > 0;
        }

        abstract class Node
        {
            internal BoundingBox BoundingBox;
            internal abstract void GetOverlaps(ref BoundingBox boundingBox, IList<int> outputOverlappedElements);
            internal abstract void GetOverlaps(ref BoundingSphere boundingSphere, IList<int> outputOverlappedElements);
            internal abstract void GetOverlaps(ref BoundingFrustum boundingFrustum, IList<int> outputOverlappedElements);
            internal abstract void GetOverlaps(ref Ray ray, float maximumLength, IList<int> outputOverlappedElements);

            internal abstract bool IsLeaf { get; }


            internal abstract bool TryToInsert(LeafNode node, out Node treeNode);



            internal abstract void Analyze(List<int> depths, int depth, ref int nodeCount);

            internal abstract void Refit(MeshBoundingBoxTreeData data);
        }

        sealed class InternalNode : Node
        {
            internal Node ChildA;
            internal Node ChildB;

            internal override bool IsLeaf
            {
                get { return false; }
            }


            internal override void GetOverlaps(ref BoundingBox boundingBox, IList<int> outputOverlappedElements)
            {
                //Users of the GetOverlaps method will have to check the bounding box before calling
                //root.getoverlaps.  This is actually desired in some cases, since the outer bounding box is used
                //to determine a pair, and further overlap tests shouldn't bother retesting the root.
                bool intersects;
                ChildA.BoundingBox.Intersects(ref boundingBox, out intersects);
                if (intersects)
                    ChildA.GetOverlaps(ref boundingBox, outputOverlappedElements);
                ChildB.BoundingBox.Intersects(ref boundingBox, out intersects);
                if (intersects)
                    ChildB.GetOverlaps(ref boundingBox, outputOverlappedElements);
            }

            internal override void GetOverlaps(ref BoundingSphere boundingSphere, IList<int> outputOverlappedElements)
            {
                bool intersects;
                ChildA.BoundingBox.Intersects(ref boundingSphere, out intersects);
                if (intersects)
                    ChildA.GetOverlaps(ref boundingSphere, outputOverlappedElements);
                ChildB.BoundingBox.Intersects(ref boundingSphere, out intersects);
                if (intersects)
                    ChildB.GetOverlaps(ref boundingSphere, outputOverlappedElements);
            }

            internal override void GetOverlaps(ref BoundingFrustum boundingFrustum, IList<int> outputOverlappedElements)
            {
                bool intersects;
                boundingFrustum.Intersects(ref ChildA.BoundingBox, out intersects);
                if (intersects)
                    ChildA.GetOverlaps(ref boundingFrustum, outputOverlappedElements);
                boundingFrustum.Intersects(ref ChildB.BoundingBox, out intersects);
                if (intersects)
                    ChildB.GetOverlaps(ref boundingFrustum, outputOverlappedElements);
            }

            internal override void GetOverlaps(ref Ray ray, float maximumLength, IList<int> outputOverlappedElements)
            {
                float? result;
                ray.Intersects(ref ChildA.BoundingBox, out result);
                if (result != null && result < maximumLength)
                    ChildA.GetOverlaps(ref ray, maximumLength, outputOverlappedElements);
                ray.Intersects(ref ChildB.BoundingBox, out result);
                if (result != null && result < maximumLength)
                    ChildB.GetOverlaps(ref ray, maximumLength, outputOverlappedElements);
            }


            internal override bool TryToInsert(LeafNode node, out Node treeNode)
            {
                ////The following can make the tree shorter, but it actually hurt query times in testing.
                //bool aIsLeaf = childA.IsLeaf;
                //bool bIsLeaf = childB.IsLeaf;
                //if (aIsLeaf && !bIsLeaf)
                //{
                //    //Just put us with the leaf.  Keeps the tree shallower.
                //    BoundingBox merged;
                //    BoundingBox.CreateMerged(ref childA.BoundingBox, ref node.BoundingBox, out merged);
                //    childA = new InternalNode() { BoundingBox = merged, childA = this.childA, childB = node };
                //    treeNode = null;
                //    return true;
                //}
                //else if (!aIsLeaf && bIsLeaf)
                //{
                //    //Just put us with the leaf.  Keeps the tree shallower.
                //    BoundingBox merged;
                //    BoundingBox.CreateMerged(ref childB.BoundingBox, ref node.BoundingBox, out merged);
                //    childB = new InternalNode() { BoundingBox = merged, childA = node, childB = this.childB };
                //    treeNode = null;
                //    return true;
                //}


                //Since we are an internal node, we know we have two children.
                //Regardless of what kind of nodes they are, figure out which would be a better choice to merge the new node with.

                //Use the path which produces the smallest 'volume.'
                BoundingBox mergedA, mergedB;
                BoundingBox.CreateMerged(ref ChildA.BoundingBox, ref node.BoundingBox, out mergedA);
                BoundingBox.CreateMerged(ref ChildB.BoundingBox, ref node.BoundingBox, out mergedB);

                Vector3 offset;
                float originalAVolume, originalBVolume;
                Vector3.Subtract(ref ChildA.BoundingBox.Max, ref ChildA.BoundingBox.Min, out offset);
                originalAVolume = offset.X * offset.Y * offset.Z;
                Vector3.Subtract(ref ChildB.BoundingBox.Max, ref ChildB.BoundingBox.Min, out offset);
                originalBVolume = offset.X * offset.Y * offset.Z;

                float mergedAVolume, mergedBVolume;
                Vector3.Subtract(ref mergedA.Max, ref mergedA.Min, out offset);
                mergedAVolume = offset.X * offset.Y * offset.Z;
                Vector3.Subtract(ref mergedB.Max, ref mergedB.Min, out offset);
                mergedBVolume = offset.X * offset.Y * offset.Z;

                //Could use factor increase or absolute difference
                if (mergedAVolume - originalAVolume < mergedBVolume - originalBVolume)
                {
                    //merging A produces a better result.
                    if (ChildA.IsLeaf)
                    {
                        ChildA = new InternalNode() { BoundingBox = mergedA, ChildA = this.ChildA, ChildB = node };
                        treeNode = null;
                        return true;
                    }
                    else
                    {
                        ChildA.BoundingBox = mergedA;
                        treeNode = ChildA;
                        return false;
                    }
                }
                else
                {
                    //merging B produces a better result.
                    if (ChildB.IsLeaf)
                    {
                        //Target is a leaf! Return.
                        ChildB = new InternalNode() { BoundingBox = mergedB, ChildA = node, ChildB = this.ChildB };
                        treeNode = null;
                        return true;
                    }
                    else
                    {
                        ChildB.BoundingBox = mergedB;
                        treeNode = ChildB;
                        return false;
                    }
                }



            }

            public override string ToString()
            {
                return "{" + ChildA.ToString() + ", " + ChildB.ToString() + "}";

            }

            internal override void Analyze(List<int> depths, int depth, ref int nodeCount)
            {
                nodeCount++;
                ChildA.Analyze(depths, depth + 1, ref nodeCount);
                ChildB.Analyze(depths, depth + 1, ref nodeCount);
            }

            internal override void Refit(MeshBoundingBoxTreeData data)
            {
                ChildA.Refit(data);
                ChildB.Refit(data);
                BoundingBox.CreateMerged(ref ChildA.BoundingBox, ref ChildB.BoundingBox, out BoundingBox);
            }
        }

        /// <summary>
        /// The tiny extra margin added to leaf bounding boxes that allow the volume cost metric to function properly even in degenerate cases.
        /// </summary>
        public static float LeafMargin = .001f;
        sealed class LeafNode : Node
        {
            int LeafIndex;

            internal override bool IsLeaf
            {
                get { return true; }
            }

            internal LeafNode(int leafIndex, MeshBoundingBoxTreeData data)
            {
                LeafIndex = leafIndex;
                data.GetBoundingBox(leafIndex, out BoundingBox);
                //Having an ever-so-slight margin allows the hierarchy use a volume metric even for degenerate shapes (consider a flat tessellated plane).
                BoundingBox.Max.X += LeafMargin;
                BoundingBox.Max.Y += LeafMargin;
                BoundingBox.Max.Z += LeafMargin;
                BoundingBox.Min.X -= LeafMargin;
                BoundingBox.Min.Y -= LeafMargin;
                BoundingBox.Min.Z -= LeafMargin;
            }

            internal override void GetOverlaps(ref BoundingBox boundingBox, IList<int> outputOverlappedElements)
            {
                //Our parent already tested the bounding box.  All that's left is to add myself to the list.
                outputOverlappedElements.Add(LeafIndex);
            }

            internal override void GetOverlaps(ref BoundingSphere boundingSphere, IList<int> outputOverlappedElements)
            {
                outputOverlappedElements.Add(LeafIndex);
            }

            internal override void GetOverlaps(ref BoundingFrustum boundingFrustum, IList<int> outputOverlappedElements)
            {
                outputOverlappedElements.Add(LeafIndex);
            }

            internal override void GetOverlaps(ref Ray ray, float maximumLength, IList<int> outputOverlappedElements)
            {
                outputOverlappedElements.Add(LeafIndex);
            }

            internal override bool TryToInsert(LeafNode node, out Node treeNode)
            {
                var newTreeNode = new InternalNode();
                BoundingBox.CreateMerged(ref BoundingBox, ref node.BoundingBox, out newTreeNode.BoundingBox);
                newTreeNode.ChildA = this;
                newTreeNode.ChildB = node;
                treeNode = newTreeNode;
                return true;
            }

            public override string ToString()
            {
                return LeafIndex.ToString(CultureInfo.InvariantCulture);
            }

            internal override void Analyze(List<int> depths, int depth, ref int nodeCount)
            {
                nodeCount++;
                depths.Add(depth);
            }

            internal override void Refit(MeshBoundingBoxTreeData data)
            {
                data.GetBoundingBox(LeafIndex, out BoundingBox);
                //Having an ever-so-slight margin allows the hierarchy use a volume metric even for degenerate shapes (consider a flat tessellated plane).
                BoundingBox.Max.X += LeafMargin;
                BoundingBox.Max.Y += LeafMargin;
                BoundingBox.Max.Z += LeafMargin;
                BoundingBox.Min.X -= LeafMargin;
                BoundingBox.Min.Y -= LeafMargin;
                BoundingBox.Min.Z -= LeafMargin;
            }
        }

    }


}
