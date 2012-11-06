using System.Collections.Generic;

namespace BEPUphysics.CollisionRuleManagement
{
    /// <summary>
    /// A group which can have interaction rules created between it and other collision groups.
    /// Every entity has a collision group and considers the group's interaction rules in collisions with other entities.
    /// </summary>
    public class CollisionGroup
    {
        private readonly int hashCode;

        /// <summary>
        /// Constructs a new collision group.
        /// </summary>
        public CollisionGroup()
        {
            const ulong prime = 0xd8163841;
            var hash = (ulong)(base.GetHashCode());
            hash = hash * hash * hash * hash * hash * prime;
            hashCode = (int)(hash);
        }

        //Equals is not overriden because the hashcode because the hashcode is the default hashcode, just modified a bit.

        /// <summary>
        /// Defines the CollisionRule between the two groups for a given space.
        /// </summary>
        /// <param name="groupA">First CollisionGroup of the pair.</param>
        /// <param name="groupB">Second CollisionGroup of the pair.</param>
        /// <param name="rule">CollisionRule to use between the pair.</param>
        public static void DefineCollisionRule(CollisionGroup groupA, CollisionGroup groupB, CollisionRule rule)
        {
            var pair = new CollisionGroupPair(groupA, groupB);
            if (CollisionRules.CollisionGroupRules.ContainsKey(pair))
                CollisionRules.CollisionGroupRules[pair] = rule;
            else
                CollisionRules.CollisionGroupRules.Add(pair, rule);
        }

        /// <summary>
        /// Defines a CollisionRule between every group in the first set and every group in the second set for a given space.
        /// </summary>
        /// <param name="aGroups">First set of groups.</param>
        /// <param name="bGroups">Second set of groups.</param>
        /// <param name="rule">Collision rule to define between the sets.</param>
        public static void DefineCollisionRulesBetweenSets(List<CollisionGroup> aGroups, List<CollisionGroup> bGroups, CollisionRule rule)
        {
            foreach (CollisionGroup group in aGroups)
            {
                DefineCollisionRulesWithSet(group, bGroups, rule);
            }
        }

        /// <summary>
        /// Defines a CollisionRule between every group in a set with itself and the others in the set for a given space.
        /// </summary>
        /// <param name="groups">Set of CollisionGroups.</param>
        /// <param name="self">CollisionRule between each group and itself.</param>
        /// <param name="other">CollisionRule between each group and every other group in the set.</param>
        public static void DefineCollisionRulesInSet(List<CollisionGroup> groups, CollisionRule self, CollisionRule other)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                DefineCollisionRule(groups[i], groups[i], self);
            }
            for (int i = 0; i < groups.Count - 1; i++)
            {
                for (int j = i + 1; j < groups.Count; j++)
                {
                    DefineCollisionRule(groups[i], groups[j], other);
                }
            }
        }

        /// <summary>
        /// Defines a CollisionRule between a group and every group in a set of groups for a given space.
        /// </summary>
        /// <param name="group">First CollisionGroup of the pair.</param>
        /// <param name="groups">Set of CollisionGroups; each group will have its CollisionRule with the first group defined.</param>
        /// <param name="rule">CollisionRule to use between the pairs.</param>
        public static void DefineCollisionRulesWithSet(CollisionGroup group, List<CollisionGroup> groups, CollisionRule rule)
        {
            foreach (CollisionGroup g in groups)
            {
                DefineCollisionRule(group, g, rule);
            }
        }

        /// <summary>
        /// Removes any rule between the two groups in the space.
        /// </summary>
        /// <param name="groupA">First CollisionGroup of the pair.</param>
        /// <param name="groupB">SecondCollisionGroup of the pair.</param>
        public static void RemoveCollisionRule(CollisionGroup groupA, CollisionGroup groupB)
        {
            Dictionary<CollisionGroupPair, CollisionRule> dictionary = CollisionRules.CollisionGroupRules;
            var pair = new CollisionGroupPair(groupA, groupB);
            if (dictionary.ContainsKey(pair))
                dictionary.Remove(pair);
        }

        /// <summary>
        /// Removes any rule between every group in the first set and every group in the second set for a given space.
        /// </summary>
        /// <param name="aGroups">First set of groups.</param>
        /// <param name="bGroups">Second set of groups.</param>
        public static void RemoveCollisionRulesBetweenSets(List<CollisionGroup> aGroups, List<CollisionGroup> bGroups)
        {
            foreach (CollisionGroup group in aGroups)
            {
                RemoveCollisionRulesWithSet(group, bGroups);
            }
        }

        /// <summary>
        /// Removes any rule between every group in a set with itself and the others in the set for a given space.
        /// </summary>
        /// <param name="groups">Set of CollisionGroups.</param>
        public static void RemoveCollisionRulesInSet(List<CollisionGroup> groups)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                RemoveCollisionRule(groups[i], groups[i]);
            }
            for (int i = 0; i < groups.Count - 1; i++)
            {
                for (int j = i + 1; j < groups.Count; j++)
                {
                    RemoveCollisionRule(groups[i], groups[j]);
                }
            }
        }

        /// <summary>
        /// Removes any rule between a group and every group in a set of groups for a given space.
        /// </summary>
        /// <param name="group">First CollisionGroup of the pair.</param>
        /// <param name="groups">Set of CollisionGroups; each group will have its CollisionRule with the first group removed.</param>
        public static void RemoveCollisionRulesWithSet(CollisionGroup group, List<CollisionGroup> groups)
        {
            foreach (CollisionGroup g in groups)
            {
                RemoveCollisionRule(group, g);
            }
        }

        /// <summary>
        /// Gets a hash code for the object.
        /// </summary>
        /// <returns>Hash code for the object.</returns>
        public override int GetHashCode()
        {
            return hashCode;
        }
    }
}
