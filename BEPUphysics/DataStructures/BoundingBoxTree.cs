using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using BEPUphysics.ResourceManagement;
using BEPUphysics.BroadPhaseSystems;

namespace BEPUphysics.DataStructures
{
    ///<summary>
    /// Acceleration structure of objects surrounded by axis aligned bounding boxes, supporting various speedy queries.
    ///</summary>
    public class BoundingBoxTree<T> where T : IBoundingBoxOwner
    {
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
        /// Constructs a new tree.
        /// </summary>
        /// <param name="elements">Data to use to construct the tree.</param>
        public BoundingBoxTree(IList<T> elements)
        {
            Reconstruct(elements);
        }


        /// <summary>
        /// Reconstructs the tree based on the current data.
        /// </summary>
        public void Reconstruct(IList<T> elements)
        {
            root = null;
            int count = elements.Count;
            for (int i = 0; i < count; i++)
            {
                //Use a permuted version of the elements instead of the actual elements list.
                //Permuting makes the input basically random, improving the quality of the tree.
                Add(elements[(int)((982451653L * i) % count)]);
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
                root.Refit();
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

        /// <summary>
        /// Adds an element to the tree.
        /// If a list of objects is available, using the Reconstruct method is recommended.
        /// </summary>
        /// <param name="element">Element to add.</param>
        public void Add(T element)
        {
            //Insertions can easily be performed stacklessly.
            //Only one path is chosen at each step and nothing is returned, so the history of the 'recursion' is completely forgotten.

            var node = new LeafNode(element);
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
        public bool GetOverlaps(BoundingBox boundingBox, IList<T> outputOverlappedElements)
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
        public bool GetOverlaps(BoundingSphere boundingSphere, IList<T> outputOverlappedElements)
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
        public bool GetOverlaps(BoundingFrustum boundingFrustum, IList<T> outputOverlappedElements)
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
        public bool GetOverlaps(Ray ray, IList<T> outputOverlappedElements)
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
        public bool GetOverlaps(Ray ray, float maximumLength, IList<T> outputOverlappedElements)
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

        /// <summary>
        /// Gets the pairs of elements in each tree with overlapping bounding boxes.
        /// </summary>
        /// <typeparam name="TElement">Type of the elements in the opposing tree.</typeparam>
        /// <param name="tree">Other tree to test.</param>
        /// <param name="outputOverlappedElements">List of overlaps found by the query.</param>
        /// <returns>Whether or not any overlaps were found.</returns>
        public bool GetOverlaps<TElement>(BoundingBoxTree<TElement> tree, IList<TreeOverlapPair<T, TElement>> outputOverlappedElements)
            where TElement : IBoundingBoxOwner
        {
            bool intersects;
            root.BoundingBox.Intersects(ref tree.root.BoundingBox, out intersects);
            if (intersects)
            {
                root.GetOverlaps<TElement>(tree.root, outputOverlappedElements);
            }
            return outputOverlappedElements.Count > 0;
        }

        internal abstract class Node
        {
            internal BoundingBox BoundingBox;
            internal abstract void GetOverlaps(ref BoundingBox boundingBox, IList<T> outputOverlappedElements);
            internal abstract void GetOverlaps(ref BoundingSphere boundingSphere, IList<T> outputOverlappedElements);
            internal abstract void GetOverlaps(ref BoundingFrustum boundingFrustum, IList<T> outputOverlappedElements);
            internal abstract void GetOverlaps(ref Ray ray, float maximumLength, IList<T> outputOverlappedElements);
            internal abstract void GetOverlaps<TElement>(BoundingBoxTree<TElement>.Node opposingNode, IList<TreeOverlapPair<T, TElement>> outputOverlappedElements) where TElement : IBoundingBoxOwner;

            internal abstract bool IsLeaf { get; }

            internal abstract Node ChildA { get; }
            internal abstract Node ChildB { get; }
            internal abstract T Element { get; }

            internal abstract bool TryToInsert(LeafNode node, out Node treeNode);



            internal abstract void Analyze(List<int> depths, int depth, ref int nodeCount);

            internal abstract void Refit();
        }

        internal sealed class InternalNode : Node
        {
            internal Node childA;
            internal Node childB;

            internal override Node ChildA
            {
                get
                {
                    return childA;
                }
            }
            internal override Node ChildB
            {
                get
                {
                    return childB;
                }
            }
            internal override T Element
            {
                get
                {
                    return default(T);
                }
            }

            internal override bool IsLeaf
            {
                get { return false; }
            }


            internal override void GetOverlaps(ref BoundingBox boundingBox, IList<T> outputOverlappedElements)
            {
                //Users of the GetOverlaps method will have to check the bounding box before calling
                //root.getoverlaps.  This is actually desired in some cases, since the outer bounding box is used
                //to determine a pair, and further overlap tests shouldn't bother retesting the root.
                bool intersects;
                childA.BoundingBox.Intersects(ref boundingBox, out intersects);
                if (intersects)
                    childA.GetOverlaps(ref boundingBox, outputOverlappedElements);
                childB.BoundingBox.Intersects(ref boundingBox, out intersects);
                if (intersects)
                    childB.GetOverlaps(ref boundingBox, outputOverlappedElements);
            }

            internal override void GetOverlaps(ref BoundingSphere boundingSphere, IList<T> outputOverlappedElements)
            {
                bool intersects;
                childA.BoundingBox.Intersects(ref boundingSphere, out intersects);
                if (intersects)
                    childA.GetOverlaps(ref boundingSphere, outputOverlappedElements);
                childB.BoundingBox.Intersects(ref boundingSphere, out intersects);
                if (intersects)
                    childB.GetOverlaps(ref boundingSphere, outputOverlappedElements);
            }

            internal override void GetOverlaps(ref BoundingFrustum boundingFrustum, IList<T> outputOverlappedElements)
            {
                bool intersects;
                boundingFrustum.Intersects(ref childA.BoundingBox, out intersects);
                if (intersects)
                    childA.GetOverlaps(ref boundingFrustum, outputOverlappedElements);
                boundingFrustum.Intersects(ref childB.BoundingBox, out intersects);
                if (intersects)
                    childB.GetOverlaps(ref boundingFrustum, outputOverlappedElements);
            }

            internal override void GetOverlaps(ref Ray ray, float maximumLength, IList<T> outputOverlappedElements)
            {
                float? result;
                ray.Intersects(ref childA.BoundingBox, out result);
                if (result != null && result < maximumLength)
                    childA.GetOverlaps(ref ray, maximumLength, outputOverlappedElements);
                ray.Intersects(ref childB.BoundingBox, out result);
                if (result != null && result < maximumLength)
                    childB.GetOverlaps(ref ray, maximumLength, outputOverlappedElements);
            }

            internal override void GetOverlaps<TElement>(BoundingBoxTree<TElement>.Node opposingNode, IList<TreeOverlapPair<T, TElement>> outputOverlappedElements)
            {
                bool intersects;

                if (opposingNode.IsLeaf)
                {
                    //If it's a leaf, go deeper in our hierarchy, but not the opposition.
                    childA.BoundingBox.Intersects(ref opposingNode.BoundingBox, out intersects);
                    if (intersects)
                        childA.GetOverlaps<TElement>(opposingNode, outputOverlappedElements);
                    childB.BoundingBox.Intersects(ref opposingNode.BoundingBox, out intersects);
                    if (intersects)
                        childB.GetOverlaps<TElement>(opposingNode, outputOverlappedElements);
                }
                else
                {
                    var opposingChildA = opposingNode.ChildA;
                    var opposingChildB = opposingNode.ChildB;
                    //If it's not a leaf, try to go deeper in both hierarchies.
                    childA.BoundingBox.Intersects(ref opposingChildA.BoundingBox, out intersects);
                    if (intersects)
                        childA.GetOverlaps<TElement>(opposingChildA, outputOverlappedElements);
                    childA.BoundingBox.Intersects(ref opposingChildB.BoundingBox, out intersects);
                    if (intersects)
                        childA.GetOverlaps<TElement>(opposingChildB, outputOverlappedElements);
                    childB.BoundingBox.Intersects(ref opposingChildA.BoundingBox, out intersects);
                    if (intersects)
                        childB.GetOverlaps<TElement>(opposingChildA, outputOverlappedElements);
                    childB.BoundingBox.Intersects(ref opposingChildB.BoundingBox, out intersects);
                    if (intersects)
                        childB.GetOverlaps<TElement>(opposingChildB, outputOverlappedElements);


                }





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
                BoundingBox.CreateMerged(ref childA.BoundingBox, ref node.BoundingBox, out mergedA);
                BoundingBox.CreateMerged(ref childB.BoundingBox, ref node.BoundingBox, out mergedB);

                Vector3 offset;
                float originalAVolume, originalBVolume;
                Vector3.Subtract(ref childA.BoundingBox.Max, ref childA.BoundingBox.Min, out offset);
                originalAVolume = offset.X * offset.Y * offset.Z;
                Vector3.Subtract(ref childB.BoundingBox.Max, ref childB.BoundingBox.Min, out offset);
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
                    if (childA.IsLeaf)
                    {
                        childA = new InternalNode() { BoundingBox = mergedA, childA = this.childA, childB = node };
                        treeNode = null;
                        return true;
                    }
                    else
                    {
                        childA.BoundingBox = mergedA;
                        treeNode = childA;
                        return false;
                    }
                }
                else
                {
                    //merging B produces a better result.
                    if (childB.IsLeaf)
                    {
                        //Target is a leaf! Return.
                        childB = new InternalNode() { BoundingBox = mergedB, childA = node, childB = this.childB };
                        treeNode = null;
                        return true;
                    }
                    else
                    {
                        childB.BoundingBox = mergedB;
                        treeNode = childB;
                        return false;
                    }
                }



            }

            public override string ToString()
            {
                return "{" + childA + ", " + childB + "}";

            }

            internal override void Analyze(List<int> depths, int depth, ref int nodeCount)
            {
                nodeCount++;
                childA.Analyze(depths, depth + 1, ref nodeCount);
                childB.Analyze(depths, depth + 1, ref nodeCount);
            }

            internal override void Refit()
            {
                childA.Refit();
                childB.Refit();
                BoundingBox.CreateMerged(ref childA.BoundingBox, ref childB.BoundingBox, out BoundingBox);
            }
        }

        /// <summary>
        /// The tiny extra margin added to leaf bounding boxes that allow the volume cost metric to function properly even in degenerate cases.
        /// </summary>
        public static float LeafMargin = .001f;
        internal sealed class LeafNode : Node
        {
            T element;
            internal override Node ChildA
            {
                get
                {
                    return null;
                }
            }
            internal override Node ChildB
            {
                get
                {
                    return null;
                }
            }

            internal override T Element
            {
                get
                {
                    return element;
                }
            }

            internal override bool IsLeaf
            {
                get { return true; }
            }

            internal LeafNode(T element)
            {
                this.element = element;
                BoundingBox = element.BoundingBox;
                //Having an ever-so-slight margin allows the hierarchy use a volume metric even for degenerate shapes (consider a flat tessellated plane).
                BoundingBox.Max.X += LeafMargin;
                BoundingBox.Max.Y += LeafMargin;
                BoundingBox.Max.Z += LeafMargin;
                BoundingBox.Min.X -= LeafMargin;
                BoundingBox.Min.Y -= LeafMargin;
                BoundingBox.Min.Z -= LeafMargin;
            }

            internal override void GetOverlaps(ref BoundingBox boundingBox, IList<T> outputOverlappedElements)
            {
                //Our parent already tested the bounding box.  All that's left is to add myself to the list.
                outputOverlappedElements.Add(element);
            }

            internal override void GetOverlaps(ref BoundingSphere boundingSphere, IList<T> outputOverlappedElements)
            {
                outputOverlappedElements.Add(element);
            }

            internal override void GetOverlaps(ref BoundingFrustum boundingFrustum, IList<T> outputOverlappedElements)
            {
                outputOverlappedElements.Add(element);
            }

            internal override void GetOverlaps(ref Ray ray, float maximumLength, IList<T> outputOverlappedElements)
            {
                outputOverlappedElements.Add(element);
            }

            internal override void GetOverlaps<TElement>(BoundingBoxTree<TElement>.Node opposingNode, IList<TreeOverlapPair<T, TElement>> outputOverlappedElements)
            {
                bool intersects;

                if (opposingNode.IsLeaf)
                {
                    //We're both leaves!  Our parents have already done the testing for us, so we know we're overlapping.
                    outputOverlappedElements.Add(new TreeOverlapPair<T, TElement>(element, opposingNode.Element));
                }
                else
                {
                    var opposingChildA = opposingNode.ChildA;
                    var opposingChildB = opposingNode.ChildB;
                    //If it's not a leaf, try to go deeper in the opposing hierarchy.
                    BoundingBox.Intersects(ref opposingChildA.BoundingBox, out intersects);
                    if (intersects)
                        GetOverlaps<TElement>(opposingChildA, outputOverlappedElements);
                    BoundingBox.Intersects(ref opposingChildB.BoundingBox, out intersects);
                    if (intersects)
                        GetOverlaps<TElement>(opposingChildB, outputOverlappedElements);

                }
            }

            internal override bool TryToInsert(LeafNode node, out Node treeNode)
            {
                var newTreeNode = new InternalNode();
                BoundingBox.CreateMerged(ref BoundingBox, ref node.BoundingBox, out newTreeNode.BoundingBox);
                newTreeNode.childA = this;
                newTreeNode.childB = node;
                treeNode = newTreeNode;
                return true;
            }

            public override string ToString()
            {
                return element.ToString();
            }

            internal override void Analyze(List<int> depths, int depth, ref int nodeCount)
            {
                nodeCount++;
                depths.Add(depth);
            }

            internal override void Refit()
            {
                BoundingBox = element.BoundingBox;
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
