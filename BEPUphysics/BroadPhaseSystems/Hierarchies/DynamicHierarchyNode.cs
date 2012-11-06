using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BEPUphysics.BroadPhaseEntries;
using Microsoft.Xna.Framework;
using BEPUphysics.DataStructures;
using BEPUphysics.ResourceManagement;
using System.Diagnostics;

namespace BEPUphysics.BroadPhaseSystems.Hierarchies
{
    internal abstract class Node
    {
        internal BoundingBox BoundingBox;
        internal abstract void GetOverlaps(ref BoundingBox boundingBox, IList<BroadPhaseEntry> outputOverlappedElements);
        internal abstract void GetOverlaps(ref BoundingSphere boundingSphere, IList<BroadPhaseEntry> outputOverlappedElements);
        internal abstract void GetOverlaps(ref BoundingFrustum boundingFrustum, IList<BroadPhaseEntry> outputOverlappedElements);
        internal abstract void GetOverlaps(ref Ray ray, float maximumLength, IList<BroadPhaseEntry> outputOverlappedElements);
        internal abstract void GetOverlaps(Node node, DynamicHierarchy owner);

        internal abstract bool IsLeaf { get; }

        internal abstract Node ChildA { get; }
        internal abstract Node ChildB { get; }
        internal abstract BroadPhaseEntry Element { get; }

        internal abstract bool TryToInsert(LeafNode node, out Node treeNode);

        internal abstract void Analyze(List<int> depths, int depth, ref int nodeCount);

        internal abstract void Refit();

        internal abstract void RetrieveNodes(RawList<LeafNode> leafNodes);



        internal abstract void CollectMultithreadingNodes(int splitDepth, int currentDepth, RawList<Node> multithreadingSourceNodes);

        internal abstract void PostRefit(int splitDepth, int currentDepth);

        internal abstract void GetMultithreadedOverlaps(Node opposingNode, int splitDepth, int currentDepth, DynamicHierarchy owner, RawList<DynamicHierarchy.NodePair> multithreadingSourceOverlaps);

        internal abstract bool Remove(BroadPhaseEntry entry, out LeafNode leafNode, out Node replacementNode);
        internal abstract bool RemoveFast(BroadPhaseEntry entry, out LeafNode leafNode, out Node replacementNode);
    }

    internal sealed class InternalNode : Node
    {
        internal Node childA;
        internal Node childB;

        internal float currentVolume;
        internal float maximumVolume;

        internal static float MaximumVolumeScale = 1.4f;

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
        internal override BroadPhaseEntry Element
        {
            get
            {
                return default(BroadPhaseEntry);
            }
        }

        internal override bool IsLeaf
        {
            get { return false; }
        }


        internal override void GetOverlaps(ref BoundingBox boundingBox, IList<BroadPhaseEntry> outputOverlappedElements)
        {
            //Users of the GetOverlaps method will have to check the bounding box before calling
            //root.getoverlaps.  This is actually desired in some cases, since the outer bounding box is used
            //to determine a pair, and further overlap tests shouldn'BroadPhaseEntry bother retesting the root.
            bool intersects;
            childA.BoundingBox.Intersects(ref boundingBox, out intersects);
            if (intersects)
                childA.GetOverlaps(ref boundingBox, outputOverlappedElements);
            childB.BoundingBox.Intersects(ref boundingBox, out intersects);
            if (intersects)
                childB.GetOverlaps(ref boundingBox, outputOverlappedElements);
        }

        internal override void GetOverlaps(ref BoundingSphere boundingSphere, IList<BroadPhaseEntry> outputOverlappedElements)
        {
            bool intersects;
            childA.BoundingBox.Intersects(ref boundingSphere, out intersects);
            if (intersects)
                childA.GetOverlaps(ref boundingSphere, outputOverlappedElements);
            childB.BoundingBox.Intersects(ref boundingSphere, out intersects);
            if (intersects)
                childB.GetOverlaps(ref boundingSphere, outputOverlappedElements);
        }

        internal override void GetOverlaps(ref BoundingFrustum boundingFrustum, IList<BroadPhaseEntry> outputOverlappedElements)
        {
            bool intersects;
            boundingFrustum.Intersects(ref childA.BoundingBox, out intersects);
            if (intersects)
                childA.GetOverlaps(ref boundingFrustum, outputOverlappedElements);
            boundingFrustum.Intersects(ref childB.BoundingBox, out intersects);
            if (intersects)
                childB.GetOverlaps(ref boundingFrustum, outputOverlappedElements);
        }

        internal override void GetOverlaps(ref Ray ray, float maximumLength, IList<BroadPhaseEntry> outputOverlappedElements)
        {
            float? result;
            ray.Intersects(ref childA.BoundingBox, out result);
            if (result != null && result < maximumLength)
                childA.GetOverlaps(ref ray, maximumLength, outputOverlappedElements);
            ray.Intersects(ref childB.BoundingBox, out result);
            if (result != null && result < maximumLength)
                childB.GetOverlaps(ref ray, maximumLength, outputOverlappedElements);
        }

        internal override void GetOverlaps(Node opposingNode, DynamicHierarchy owner)
        {
            bool intersects;

            if (this == opposingNode)
            {
                //We are being compared against ourselves!
                //Obviously we're an internal node, so spawn three children:
                //A versus A:
                if (!childA.IsLeaf) //This is performed in the child method usually by convention, but this saves some time.
                    childA.GetOverlaps(childA, owner);
                //B versus B:
                if (!childB.IsLeaf) //This is performed in the child method usually by convention, but this saves some time.
                    childB.GetOverlaps(childB, owner);
                //A versus B (if they intersect):
                childA.BoundingBox.Intersects(ref childB.BoundingBox, out intersects);
                if (intersects)
                    childA.GetOverlaps(childB, owner);

            }
            else
            {
                //Two different nodes.  The other one may be a leaf.
                if (opposingNode.IsLeaf)
                {
                    //If it's a leaf, go deeper in our hierarchy, but not the opposition.
                    childA.BoundingBox.Intersects(ref opposingNode.BoundingBox, out intersects);
                    if (intersects)
                        childA.GetOverlaps(opposingNode, owner);
                    childB.BoundingBox.Intersects(ref opposingNode.BoundingBox, out intersects);
                    if (intersects)
                        childB.GetOverlaps(opposingNode, owner);
                }
                else
                {
                    var opposingChildA = opposingNode.ChildA;
                    var opposingChildB = opposingNode.ChildB;
                    //If it's not a leaf, try to go deeper in both hierarchies.
                    childA.BoundingBox.Intersects(ref opposingChildA.BoundingBox, out intersects);
                    if (intersects)
                        childA.GetOverlaps(opposingChildA, owner);
                    childA.BoundingBox.Intersects(ref opposingChildB.BoundingBox, out intersects);
                    if (intersects)
                        childA.GetOverlaps(opposingChildB, owner);
                    childB.BoundingBox.Intersects(ref opposingChildA.BoundingBox, out intersects);
                    if (intersects)
                        childB.GetOverlaps(opposingChildA, owner);
                    childB.BoundingBox.Intersects(ref opposingChildB.BoundingBox, out intersects);
                    if (intersects)
                        childB.GetOverlaps(opposingChildB, owner);


                }
            }





        }

        internal static LockingResourcePool<InternalNode> nodePool = new LockingResourcePool<InternalNode>();
        internal override bool TryToInsert(LeafNode node, out Node treeNode)
        {
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
                    var newChildA = nodePool.Take();
                    newChildA.BoundingBox = mergedA;
                    newChildA.childA = this.childA;
                    newChildA.childB = node;
                    newChildA.currentVolume = mergedAVolume;
                    //newChildA.maximumVolume = newChildA.currentVolume * MaximumVolumeScale;
                    childA = newChildA;
                    treeNode = null;
                    return true;
                }
                else
                {
                    childA.BoundingBox = mergedA;
                    var internalNode = (InternalNode)childA;
                    internalNode.currentVolume = mergedAVolume;
                    //internalNode.maximumVolume = internalNode.currentVolume * MaximumVolumeScale;
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
                    var newChildB = nodePool.Take();
                    newChildB.BoundingBox = mergedB;
                    newChildB.childA = node;
                    newChildB.childB = this.childB;
                    newChildB.currentVolume = mergedBVolume;
                    //newChildB.maximumVolume = newChildB.currentVolume * MaximumVolumeScale;
                    childB = newChildB;
                    treeNode = null;
                    return true;
                }
                else
                {
                    childB.BoundingBox = mergedB;
                    treeNode = childB;
                    var internalNode = (InternalNode)childB;
                    internalNode.currentVolume = mergedBVolume;
                    //internalNode.maximumVolume = internalNode.currentVolume * MaximumVolumeScale;
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
            if (currentVolume > maximumVolume)
            {
                Revalidate();
                return;
            }
            childA.Refit();
            childB.Refit();
            BoundingBox.CreateMerged(ref childA.BoundingBox, ref childB.BoundingBox, out BoundingBox);
            //float DEBUGlastVolume = currentVolume;
            currentVolume = (BoundingBox.Max.X - BoundingBox.Min.X) * (BoundingBox.Max.Y - BoundingBox.Min.Y) * (BoundingBox.Max.Z - BoundingBox.Min.Z);
            //if (Math.Abs(currentVolume - DEBUGlastVolume) > .000001 * (DEBUGlastVolume + currentVolume))
            //    Debug.WriteLine(":Break>:)");
        }

        internal static LockingResourcePool<RawList<LeafNode>> nodeListPool = new LockingResourcePool<RawList<LeafNode>>();
        internal void Revalidate()
        {
            //The revalidation procedure 'reconstructs' a portion of the tree that has expanded beyond its old limits.
            //To reconstruct the tree, the nodes (internal and leaf) currently in use need to be retrieved.
            //The internal nodes can be put back into the nodePool.  LeafNodes are reinserted one by one into the new tree.
            //To retrieve the nodes, a depth-first search is used.

            //Given that an internal node is being revalidated, it is known that there are at least two children.
            var oldChildA = childA;
            var oldChildB = childB;
            childA = null;
            childB = null;
            var leafNodes = nodeListPool.Take();
            oldChildA.RetrieveNodes(leafNodes);
            oldChildB.RetrieveNodes(leafNodes);
            for (int i = 0; i < leafNodes.count; i++)
                leafNodes.Elements[i].Refit();
            Reconstruct(leafNodes, 0, leafNodes.count);
            leafNodes.Clear();
            nodeListPool.GiveBack(leafNodes);


        }

        void Reconstruct(RawList<LeafNode> leafNodes, int begin, int end)
        {
            //It is known that we have 2 children; this is safe.
            //This is because this is only an internal node if the parent figured out it involved more than 2 leaf nodes, OR
            //this node was the initiator of the revalidation (in which case, it was an internal node with 2+ children).
            BoundingBox.CreateMerged(ref leafNodes.Elements[begin].BoundingBox, ref leafNodes.Elements[begin + 1].BoundingBox, out BoundingBox);
            for (int i = begin + 2; i < end; i++)
            {
                BoundingBox.CreateMerged(ref BoundingBox, ref leafNodes.Elements[i].BoundingBox, out BoundingBox);
            }
            Vector3 offset;
            Vector3.Subtract(ref BoundingBox.Max, ref BoundingBox.Min, out offset);
            currentVolume = offset.X * offset.Y * offset.Z;
            maximumVolume = currentVolume * MaximumVolumeScale;

            //Pick an axis and sort along it.
            if (offset.X > offset.Y && offset.X > offset.Z)
            {
                //Maximum variance axis is X.
                Array.Sort<LeafNode>(leafNodes.Elements, begin, end - begin, xComparer);
            }
            else if (offset.Y > offset.Z)
            {
                //Maximum variance axis is Y.  
                Array.Sort<LeafNode>(leafNodes.Elements, begin, end - begin, yComparer);
            }
            else
            {
                //Maximum variance axis is Z.
                Array.Sort<LeafNode>(leafNodes.Elements, begin, end - begin, zComparer);
            }

            //Find the median index.
            int median = (begin + end) / 2;

            if (median - begin >= 2)
            {
                //There are 2 or more leaf nodes remaining in the first half.  The next childA will be an internal node.
                var newChildA = nodePool.Take();
                newChildA.Reconstruct(leafNodes, begin, median);
                childA = newChildA;
            }
            else
            {
                //There is only 1 leaf node remaining in this half.  It's a leaf node.
                childA = leafNodes.Elements[begin];
            }

            if (end - median >= 2)
            {
                //There are 2 or more leaf nodes remaining in the second half.  The next childB will be an internal node.
                var newChildB = nodePool.Take();
                newChildB.Reconstruct(leafNodes, median, end);
                childB = newChildB;
            }
            else
            {
                //There is only 1 leaf node remaining in this half.  It's a leaf node.
                childB = leafNodes.Elements[median];
            }

        }

        internal override void RetrieveNodes(RawList<LeafNode> leafNodes)
        {
            var oldChildA = childA;
            var oldChildB = childB;
            childA = null;
            childB = null;
            nodePool.GiveBack(this); //Give internal nodes back to the pool before going deeper to minimize the creation of additional internal instances.
            oldChildA.RetrieveNodes(leafNodes);
            oldChildB.RetrieveNodes(leafNodes);


        }

        internal override void CollectMultithreadingNodes(int splitDepth, int currentDepth, RawList<Node> multithreadingSourceNodes)
        {
            if (currentVolume > maximumVolume)
            {
                //Very rarely, one of these extremely high level nodes will need to be revalidated.  This isn't great.
                //We may lose a frame.  This could be independently multithreaded, but the benefit is unknown.
                Revalidate();
                return;
            }
            if (currentDepth == splitDepth)
            {
                //We are deep enough in the tree where our children will act as the starting point for multithreaded refits.
                //The split depth ensures that we have enough tasks to thread across our core count.
                multithreadingSourceNodes.Add(childA);
                multithreadingSourceNodes.Add(childB);
            }
            else
            {
                childA.CollectMultithreadingNodes(splitDepth, currentDepth + 1, multithreadingSourceNodes);
                childB.CollectMultithreadingNodes(splitDepth, currentDepth + 1, multithreadingSourceNodes);
            }
        }

        internal override void PostRefit(int splitDepth, int currentDepth)
        {
            if (splitDepth > currentDepth)
            {
                //We are not yet back to the nodes that triggered the multithreaded split.
                //Need to go deeper into the tree.
                childA.PostRefit(splitDepth, currentDepth + 1);
                childB.PostRefit(splitDepth, currentDepth + 1);
            }
            BoundingBox.CreateMerged(ref childA.BoundingBox, ref childB.BoundingBox, out BoundingBox);
            currentVolume = (BoundingBox.Max.X - BoundingBox.Min.X) * (BoundingBox.Max.Y - BoundingBox.Min.Y) * (BoundingBox.Max.Z - BoundingBox.Min.Z);
        }

        internal override void GetMultithreadedOverlaps(Node opposingNode, int splitDepth, int currentDepth, DynamicHierarchy owner, RawList<DynamicHierarchy.NodePair> multithreadingSourceOverlaps)
        {
            bool intersects;
            if (currentDepth == splitDepth)
            {
                //We've reached the depth where our child comparisons will be multithreaded.
                if (this == opposingNode)
                {
                    //We are being compared against ourselves!
                    //Obviously we're an internal node, so spawn three children:
                    //A versus A:
                    if (!childA.IsLeaf) //This is performed in the child method usually by convention, but this saves some time.
                        multithreadingSourceOverlaps.Add(new DynamicHierarchy.NodePair() { a = childA, b = childA });
                    //B versus B:
                    if (!childB.IsLeaf) //This is performed in the child method usually by convention, but this saves some time.
                        multithreadingSourceOverlaps.Add(new DynamicHierarchy.NodePair() { a = childB, b = childB });
                    //A versus B (if they intersect):
                    childA.BoundingBox.Intersects(ref childB.BoundingBox, out intersects);
                    if (intersects)
                        multithreadingSourceOverlaps.Add(new DynamicHierarchy.NodePair() { a = childA, b = childB });

                }
                else
                {
                    //Two different nodes.  The other one may be a leaf.
                    if (opposingNode.IsLeaf)
                    {
                        //If it's a leaf, go deeper in our hierarchy, but not the opposition.
                        childA.BoundingBox.Intersects(ref opposingNode.BoundingBox, out intersects);
                        if (intersects)
                            multithreadingSourceOverlaps.Add(new DynamicHierarchy.NodePair() { a = childA, b = opposingNode });
                        childB.BoundingBox.Intersects(ref opposingNode.BoundingBox, out intersects);
                        if (intersects)
                            multithreadingSourceOverlaps.Add(new DynamicHierarchy.NodePair() { a = childB, b = opposingNode });
                    }
                    else
                    {
                        var opposingChildA = opposingNode.ChildA;
                        var opposingChildB = opposingNode.ChildB;
                        //If it's not a leaf, try to go deeper in both hierarchies.
                        childA.BoundingBox.Intersects(ref opposingChildA.BoundingBox, out intersects);
                        if (intersects)
                            multithreadingSourceOverlaps.Add(new DynamicHierarchy.NodePair() { a = childA, b = opposingChildA });
                        childA.BoundingBox.Intersects(ref opposingChildB.BoundingBox, out intersects);
                        if (intersects)
                            multithreadingSourceOverlaps.Add(new DynamicHierarchy.NodePair() { a = childA, b = opposingChildB });
                        childB.BoundingBox.Intersects(ref opposingChildA.BoundingBox, out intersects);
                        if (intersects)
                            multithreadingSourceOverlaps.Add(new DynamicHierarchy.NodePair() { a = childB, b = opposingChildA });
                        childB.BoundingBox.Intersects(ref opposingChildB.BoundingBox, out intersects);
                        if (intersects)
                            multithreadingSourceOverlaps.Add(new DynamicHierarchy.NodePair() { a = childB, b = opposingChildB });
                    }
                }
                return;
            }
            if (this == opposingNode)
            {
                //We are being compared against ourselves!
                //Obviously we're an internal node, so spawn three children:
                //A versus A:
                if (!childA.IsLeaf) //This is performed in the child method usually by convention, but this saves some time.
                    childA.GetMultithreadedOverlaps(childA, splitDepth, currentDepth + 1, owner, multithreadingSourceOverlaps);
                //B versus B:
                if (!childB.IsLeaf) //This is performed in the child method usually by convention, but this saves some time.
                    childB.GetMultithreadedOverlaps(childB, splitDepth, currentDepth + 1, owner, multithreadingSourceOverlaps);
                //A versus B (if they intersect):
                childA.BoundingBox.Intersects(ref childB.BoundingBox, out intersects);
                if (intersects)
                    childA.GetMultithreadedOverlaps(childB, splitDepth, currentDepth + 1, owner, multithreadingSourceOverlaps);

            }
            else
            {
                //Two different nodes.  The other one may be a leaf.
                if (opposingNode.IsLeaf)
                {
                    //If it's a leaf, go deeper in our hierarchy, but not the opposition.
                    childA.BoundingBox.Intersects(ref opposingNode.BoundingBox, out intersects);
                    if (intersects)
                        childA.GetMultithreadedOverlaps(opposingNode, splitDepth, currentDepth + 1, owner, multithreadingSourceOverlaps);
                    childB.BoundingBox.Intersects(ref opposingNode.BoundingBox, out intersects);
                    if (intersects)
                        childB.GetMultithreadedOverlaps(opposingNode, splitDepth, currentDepth + 1, owner, multithreadingSourceOverlaps);
                }
                else
                {
                    var opposingChildA = opposingNode.ChildA;
                    var opposingChildB = opposingNode.ChildB;
                    //If it's not a leaf, try to go deeper in both hierarchies.
                    childA.BoundingBox.Intersects(ref opposingChildA.BoundingBox, out intersects);
                    if (intersects)
                        childA.GetMultithreadedOverlaps(opposingChildA, splitDepth, currentDepth + 1, owner, multithreadingSourceOverlaps);
                    childA.BoundingBox.Intersects(ref opposingChildB.BoundingBox, out intersects);
                    if (intersects)
                        childA.GetMultithreadedOverlaps(opposingChildB, splitDepth, currentDepth + 1, owner, multithreadingSourceOverlaps);
                    childB.BoundingBox.Intersects(ref opposingChildA.BoundingBox, out intersects);
                    if (intersects)
                        childB.GetMultithreadedOverlaps(opposingChildA, splitDepth, currentDepth + 1, owner, multithreadingSourceOverlaps);
                    childB.BoundingBox.Intersects(ref opposingChildB.BoundingBox, out intersects);
                    if (intersects)
                        childB.GetMultithreadedOverlaps(opposingChildB, splitDepth, currentDepth + 1, owner, multithreadingSourceOverlaps);
                }
            }
        }


        internal override bool Remove(BroadPhaseEntry entry, out LeafNode leafNode, out Node replacementNode)
        {
            if (childA.Remove(entry, out leafNode, out replacementNode))
            {
                if (childA.IsLeaf)
                    replacementNode = childB;
                else
                {
                    //It was not a leaf node, but a child found the leaf.
                    //Change the child to the replacement node.
                    childA = replacementNode;
                    replacementNode = this; //We don't need to be replaced!
                }
                return true;

            }
            if (childB.Remove(entry, out leafNode, out replacementNode))
            {
                if (childB.IsLeaf)
                    replacementNode = childA;
                else
                {
                    //It was not a leaf node, but a child found the leaf.
                    //Change the child to the replacement node.
                    childB = replacementNode;
                    replacementNode = this; //We don't need to be replaced!
                }
                return true;
            }
            replacementNode = this;
            return false;
        }

        internal override bool RemoveFast(BroadPhaseEntry entry, out LeafNode leafNode, out Node replacementNode)
        {
            //Only bother checking deeper in the path if the entry and child have overlapping bounding boxes.
            bool intersects;
            childA.BoundingBox.Intersects(ref entry.boundingBox, out intersects);
            if (intersects && childA.RemoveFast(entry, out leafNode, out replacementNode))
            {
                if (childA.IsLeaf)
                    replacementNode = childB;
                else
                {
                    //It was not a leaf node, but a child found the leaf.
                    //Change the child to the replacement node.
                    childA = replacementNode;
                    replacementNode = this; //We don't need to be replaced!
                }
                return true;

            }
            childB.BoundingBox.Intersects(ref entry.boundingBox, out intersects);
            if (intersects && childB.RemoveFast(entry, out leafNode, out replacementNode))
            {
                if (childB.IsLeaf)
                    replacementNode = childA;
                else
                {
                    //It was not a leaf node, but a child found the leaf.
                    //Change the child to the replacement node.
                    childB = replacementNode;
                    replacementNode = this; //We don't need to be replaced!
                }
                return true;
            }
            replacementNode = this;
            leafNode = null;
            return false;
        }


        static XComparer xComparer = new XComparer();
        static YComparer yComparer = new YComparer();
        static ZComparer zComparer = new ZComparer();
        //Try using Comparer instead of IComparer- is there some tricky hardcoded optimization?
        class XComparer : IComparer<LeafNode>
        {
            public int Compare(LeafNode x, LeafNode y)
            {
                return x.BoundingBox.Min.X < y.BoundingBox.Min.X ? -1 : 1;
            }
        }
        class YComparer : IComparer<LeafNode>
        {
            public int Compare(LeafNode x, LeafNode y)
            {
                return x.BoundingBox.Min.Y < y.BoundingBox.Min.Y ? -1 : 1;
            }
        }
        class ZComparer : IComparer<LeafNode>
        {
            public int Compare(LeafNode x, LeafNode y)
            {
                return x.BoundingBox.Min.Z < y.BoundingBox.Min.Z ? -1 : 1;
            }
        }
    }




    internal sealed class LeafNode : Node
    {
        BroadPhaseEntry element;
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

        internal override BroadPhaseEntry Element
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


        internal void Initialize(BroadPhaseEntry element)
        {
            this.element = element;
            BoundingBox = element.BoundingBox;
        }
        internal void CleanUp()
        {
            element = null;
        }

        internal override void GetOverlaps(ref BoundingBox boundingBox, IList<BroadPhaseEntry> outputOverlappedElements)
        {
            //Our parent already tested the bounding box.  All that's left is to add myself to the list.
            outputOverlappedElements.Add(element);
        }

        internal override void GetOverlaps(ref BoundingSphere boundingSphere, IList<BroadPhaseEntry> outputOverlappedElements)
        {
            outputOverlappedElements.Add(element);
        }

        internal override void GetOverlaps(ref BoundingFrustum boundingFrustum, IList<BroadPhaseEntry> outputOverlappedElements)
        {
            outputOverlappedElements.Add(element);
        }

        internal override void GetOverlaps(ref Ray ray, float maximumLength, IList<BroadPhaseEntry> outputOverlappedElements)
        {
            outputOverlappedElements.Add(element);
        }

        internal override void GetOverlaps(Node opposingNode, DynamicHierarchy owner)
        {
            bool intersects;
            //note: This is never executed when the opposing node is the current node.
            if (opposingNode.IsLeaf)
            {
                //We're both leaves!  Our parents have already done the testing for us, so we know we're overlapping.
                owner.TryToAddOverlap(element, opposingNode.Element);
            }
            else
            {
                var opposingChildA = opposingNode.ChildA;
                var opposingChildB = opposingNode.ChildB;
                //If it's not a leaf, try to go deeper in the opposing hierarchy.
                BoundingBox.Intersects(ref opposingChildA.BoundingBox, out intersects);
                if (intersects)
                    GetOverlaps(opposingChildA, owner);
                BoundingBox.Intersects(ref opposingChildB.BoundingBox, out intersects);
                if (intersects)
                    GetOverlaps(opposingChildB, owner);

            }
        }

        internal override bool TryToInsert(LeafNode node, out Node treeNode)
        {
            var newTreeNode = InternalNode.nodePool.Take();
            BoundingBox.CreateMerged(ref BoundingBox, ref node.BoundingBox, out newTreeNode.BoundingBox);
            Vector3 offset;
            Vector3.Subtract(ref newTreeNode.BoundingBox.Max, ref newTreeNode.BoundingBox.Min, out offset);
            newTreeNode.currentVolume = offset.X * offset.Y * offset.Z;
            //newTreeNode.maximumVolume = newTreeNode.currentVolume * InternalNode.MaximumVolumeScale;
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
            BoundingBox = element.boundingBox;
        }

        internal override void RetrieveNodes(RawList<LeafNode> leafNodes)
        {
            Refit();
            leafNodes.Add(this);
        }

        internal override void CollectMultithreadingNodes(int splitDepth, int currentDepth, RawList<Node> multithreadingSourceNodes)
        {
            //This could happen if there are almost no elements in the tree.  No biggie- do nothing!
        }

        internal override void PostRefit(int splitDepth, int currentDepth)
        {
            //This could happen if there are almost no elements in the tree.  Just do a normal leaf refit.
            BoundingBox = element.boundingBox;
        }

        internal override void GetMultithreadedOverlaps(Node opposingNode, int splitDepth, int currentDepth, DynamicHierarchy owner, RawList<DynamicHierarchy.NodePair> multithreadingSourceOverlaps)
        {
            bool intersects;
            //note: This is never executed when the opposing node is the current node.
            if (opposingNode.IsLeaf)
            {
                //We're both leaves!  Our parents have already done the testing for us, so we know we're overlapping.
                owner.TryToAddOverlap(element, opposingNode.Element);
            }
            else
            {
                var opposingChildA = opposingNode.ChildA;
                var opposingChildB = opposingNode.ChildB;
                if (splitDepth == currentDepth)
                {
                    //Time to add the child overlaps to the multithreading set!
                    BoundingBox.Intersects(ref opposingChildA.BoundingBox, out intersects);
                    if (intersects)
                        multithreadingSourceOverlaps.Add(new DynamicHierarchy.NodePair() { a = this, b = opposingChildA });
                    BoundingBox.Intersects(ref opposingChildB.BoundingBox, out intersects);
                    if (intersects)
                        multithreadingSourceOverlaps.Add(new DynamicHierarchy.NodePair() { a = this, b = opposingChildB });

                    return;
                }
                //If it's not a leaf, try to go deeper in the opposing hierarchy.
                BoundingBox.Intersects(ref opposingChildA.BoundingBox, out intersects);
                if (intersects)
                    GetOverlaps(opposingChildA, owner);
                BoundingBox.Intersects(ref opposingChildB.BoundingBox, out intersects);
                if (intersects)
                    GetOverlaps(opposingChildB, owner);

            }
        }

        internal override bool Remove(BroadPhaseEntry entry, out LeafNode leafNode, out Node replacementNode)
        {
            replacementNode = null;
            if (element == entry)
            {
                leafNode = this;
                return true;
            }
            leafNode = null;
            return false;
        }
        internal override bool RemoveFast(BroadPhaseEntry entry, out LeafNode leafNode, out Node replacementNode)
        {
            //The fastremove leaf node procedure is identical to the brute force approach.
            //We don't need to perform any bounding box test here; if they're equal, they're equal!
            replacementNode = null;
            if (element == entry)
            {
                leafNode = this;
                return true;
            }
            leafNode = null;
            return false;
        }
    }
}
