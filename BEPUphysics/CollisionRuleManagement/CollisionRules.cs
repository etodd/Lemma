﻿using System;
using System.Collections.Generic;
using BEPUphysics.DataStructures;

namespace BEPUphysics.CollisionRuleManagement
{
    /// <summary>
    /// Stores how an object can interact with other objects through collisions.
    /// </summary>
    public class CollisionRules
    {

        ///<summary>
        /// Fires when the contained collision rules are altered.
        ///</summary>
        public event Action CollisionRulesChanged;


        ///<summary>
        /// Constructs a new CollisionRules instance.
        ///</summary>
        public CollisionRules()
        {
            hashCode = (int)(base.GetHashCode() * 0x8da6b343);
            OnChangedDelegate = OnChanged;
        }

        int hashCode;

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return hashCode;
        }

        private Action OnChangedDelegate;
        protected void OnChanged()
        {
            if (CollisionRulesChanged != null)
                CollisionRulesChanged();
        }


        internal CollisionGroup group;
        /// <summary>
        /// The collision group to which the object owning this instance belongs to.
        /// This is overridden by any relationships defined in the Specific collection with CollisionRules other than CollisionRule.Defer.
        /// This is also overriden by the Personal CollisionRule if it is anything but CollisionRule.Defer.
        /// If the interaction type between the group is defined as CollisionRule.Defer, it is considered to be CollisionRule.normal as the collision group is the final stage.
        /// </summary>
        public CollisionGroup Group
        {
            get { return group; }
            set
            {
                group = value;
                OnChanged();
            }
        }


        internal CollisionRule personal = CollisionRule.Defer;
        /// <summary>
        /// Determines in general how the object owning this instance should react to other objects.
        /// This is overridden by any relationships defined in the Specific collection with CollisionRules other than CollisionRule.Defer.
        /// If this is not set to CollisionRule.Defer, it will override the collision group's collision rules.
        /// </summary>
        public CollisionRule Personal
        {
            get { return personal; }
            set
            {
                personal = value;
                OnChanged();
            }
        }



        internal ObservableDictionary<CollisionRules, CollisionRule> specific = new ObservableDictionary<CollisionRules, CollisionRule>();
        /// <summary>
        /// Specifies how the object owning this instance should react to other individual objects.
        /// Any rules defined in this collection will take priority over the Personal collision rule and the collision group's collision rules.
        /// Objects that are not in this collection are considered to have a relationship of CollisionRule.Defer.
        /// </summary>
        public ObservableDictionary<CollisionRules, CollisionRule> Specific
        {
            get { return specific; }
            set
            {
                if (value != specific)
                {
                    if (specific != null)
                        specific.Changed -= OnChangedDelegate;
                    if (value != null)
                        value.Changed += OnChangedDelegate;

                    specific = value;
                    OnChanged();
                }
            }
        }

        //Pure convenience method.
        ///<summary>
        /// Adds an entry in ownerA's Specific relationships list about ownerB.
        ///</summary>
        ///<param name="ownerA">Owner of the collision rules that will gain an entry in its Specific relationships.</param>
        ///<param name="ownerB">Owner of the collision rules that will be added to ownerA's Specific relationships.</param>
        ///<param name="rule">Rule assigned to the pair.</param>
        public static void AddRule(ICollisionRulesOwner ownerA, ICollisionRulesOwner ownerB, CollisionRule rule)
        {
            ownerA.CollisionRules.specific.Add(ownerB.CollisionRules, rule);
        }
        ///<summary>
        /// Adds an entry in rulesA's Specific relationships list about ownerB.
        ///</summary>
        ///<param name="rulesA">Collision rules that will gain an entry in its Specific relationships.</param>
        ///<param name="ownerB">Owner of the collision rules that will be added to ownerA's Specific relationships.</param>
        ///<param name="rule">Rule assigned to the pair.</param>
        public static void AddRule(CollisionRules rulesA, ICollisionRulesOwner ownerB, CollisionRule rule)
        {
            rulesA.specific.Add(ownerB.CollisionRules, rule);
        }

        ///<summary>
        /// Adds an entry in rulesA's Specific relationships list about ownerB.
        ///</summary>
        ///<param name="ownerA">Owner of the collision rules that will gain an entry in its Specific relationships.</param>
        ///<param name="rulesB">Collision rules that will be added to ownerA's Specific relationships.</param>
        ///<param name="rule">Rule assigned to the pair.</param>
        public static void AddRule(ICollisionRulesOwner ownerA, CollisionRules rulesB, CollisionRule rule)
        {
            ownerA.CollisionRules.specific.Add(rulesB, rule);
        }

        ///<summary>
        /// Tries to remove a relationship about ownerB from ownerA's Specific list.
        ///</summary>
        ///<param name="ownerA">Owner of the collision rules that will lose an entry in its Specific relationships.</param>
        ///<param name="ownerB">Owner of the collision rules that will be removed from ownerA's Specific relationships.</param>
        public static void RemoveRule(ICollisionRulesOwner ownerA, ICollisionRulesOwner ownerB)
        {
            if (!ownerA.CollisionRules.specific.Remove(ownerB.CollisionRules))
                ownerB.CollisionRules.specific.Remove(ownerA.CollisionRules);
        }
        ///<summary>
        /// Tries to remove a relationship about ownerB from rulesA's Specific list.
        ///</summary>
        ///<param name="rulesA">Collision rules that will lose an entry in its Specific relationships.</param>
        ///<param name="ownerB">Owner of the collision rules that will be removed from ownerA's Specific relationships.</param>
        public static void RemoveRule(CollisionRules rulesA, ICollisionRulesOwner ownerB)
        {
            if (!rulesA.specific.Remove(ownerB.CollisionRules))
                ownerB.CollisionRules.specific.Remove(rulesA);
        }

        ///<summary>
        /// Tries to remove a relationship about rulesB from ownerA's Specific list.
        ///</summary>
        ///<param name="ownerA">Owner of the collision rules that will lose an entry in its Specific relationships.</param>
        ///<param name="rulesB">Collision rules that will be removed from ownerA's Specific relationships.</param>
        public static void RemoveRule(ICollisionRulesOwner ownerA, CollisionRules rulesB)
        {
            if (!ownerA.CollisionRules.specific.Remove(rulesB))
                rulesB.specific.Remove(ownerA.CollisionRules);
        }

        static CollisionRules()
        {
            CollisionGroupRules.Add(new CollisionGroupPair(DefaultKinematicCollisionGroup, DefaultKinematicCollisionGroup), CollisionRule.NoBroadPhase);
        }

        internal static Func<ICollisionRulesOwner, ICollisionRulesOwner, CollisionRule> collisionRuleCalculator = GetCollisionRuleDefault;
        ///<summary>
        /// Gets or sets the delegate used to calculate collision rules.
        /// Defaults to CollisionRules.GetCollisionRuleDefault.
        ///</summary>
        public static Func<ICollisionRulesOwner, ICollisionRulesOwner, CollisionRule> CollisionRuleCalculator
        {
            get
            {
                return collisionRuleCalculator;
            }
            set
            {
                collisionRuleCalculator = value;
            }
        }

        /// <summary>
        /// Uses the CollisionRuleCalculator to get the collision rule between two collision rules owners.
        /// </summary>
        /// <param name="ownerA">First owner of the pair.</param>
        /// <param name="ownerB">Second owner of the pair.</param>
        /// <returns>CollisionRule between the pair, according to the CollisionRuleCalculator.</returns>
        public static CollisionRule GetCollisionRule(ICollisionRulesOwner ownerA, ICollisionRulesOwner ownerB)
        {
            return collisionRuleCalculator(ownerA, ownerB);
        }


        /// <summary>
        /// Defines any special collision rules between collision groups.
        /// </summary>
        public static Dictionary<CollisionGroupPair, CollisionRule> CollisionGroupRules = new Dictionary<CollisionGroupPair, CollisionRule>();

        /// <summary>
        /// If a CollisionRule calculation between two colliding objects results in no defined CollisionRule, this value will be used.
        /// </summary>
        public static CollisionRule DefaultCollisionRule = CollisionRule.Normal;

        /// <summary>
        /// When a dynamic entity is created and added to a space without having a specific collision group set beforehand, it inherits this collision group.
        /// There are no special rules associated with this group by default; entities within this group have normal, full interaction with all other entities.
        /// Collision group interaction rules can be overriden by entity personal collision rules or entity-to-entity specific collision rules.
        /// </summary>
        public static CollisionGroup DefaultDynamicCollisionGroup = new CollisionGroup();

        /// <summary>
        /// When a kinematic entity is created and added to a space without having a specific collision group set beforehand, it inherits this collision group.
        /// Entities in this collision group will not create collision pairs with other entities of this collision group by default.  All other interactions are normal.
        /// Collision group interaction rules can be overriden by entity personal collision rules or entity-to-entity specific collision rules.
        /// 
        /// Non-entity collidable objects like static triangle meshes also use this collision group by default.
        /// </summary>
        public static CollisionGroup DefaultKinematicCollisionGroup = new CollisionGroup();

        /// <summary>
        /// Determines what collision rule governs the interaction between the two objects.
        /// </summary>
        /// <param name="aOwner">First ruleset owner in the pair.  This entity's space is used to determine the collision detection settings that contain special collision group interaction rules.</param>
        /// <param name="bOwner">Second ruleset owner in the pair.</param>
        /// <returns>Collision rule governing the interaction between the pair.</returns>
        public static CollisionRule GetCollisionRuleDefault(ICollisionRulesOwner aOwner, ICollisionRulesOwner bOwner)
        {
            var a = aOwner.CollisionRules;
            var b = bOwner.CollisionRules;
            CollisionRule pairRule = GetSpecificCollisionRuleDefault(a, b);
            if (pairRule == CollisionRule.Defer)
            {
                pairRule = GetPersonalCollisionRuleDefault(a, b);
                if (pairRule == CollisionRule.Defer)
                    pairRule = GetGroupCollisionRuleDefault(a, b);
            }

            if (pairRule == CollisionRule.Defer)
                pairRule = DefaultCollisionRule;
            return pairRule;
        }


        ///<summary>
        /// Default implementation used to calculate collision rules due to the rulesets' specific relationships.
        ///</summary>
        ///<param name="a">First ruleset in the pair.</param>
        ///<param name="b">Second ruleset in the pair.</param>
        ///<returns>Collision rule governing the interaction between the pair.</returns>
        public static CollisionRule GetSpecificCollisionRuleDefault(CollisionRules a, CollisionRules b)
        {
            CollisionRule aToB;
            a.specific.wrappedDictionary.TryGetValue(b, out aToB);
            CollisionRule bToA;
            b.specific.wrappedDictionary.TryGetValue(a, out bToA);
            return aToB > bToA ? aToB : bToA;

        }

        ///<summary>
        /// Default implementation used to calculate collision rules due to the rulesets' collision groups.
        ///</summary>
        ///<param name="a">First ruleset in the pair.</param>
        ///<param name="b">Second ruleset in the pair.</param>
        ///<returns>Collision rule governing the interaction between the pair.</returns>
        public static CollisionRule GetGroupCollisionRuleDefault(CollisionRules a, CollisionRules b)
        {
            if (a.group == null || b.group == null)
                return CollisionRule.Defer; //This can happen occasionally when objects aren't in a space or are being handled uniquely (like in compound bodies).
            CollisionRule pairRule;
            CollisionGroupRules.TryGetValue(new CollisionGroupPair(a.group, b.group), out pairRule);
            return pairRule;
        }
        ///<summary>
        /// Default implementation used to calculate collision rules due to the rulesets' personal rules.
        ///</summary>
        ///<param name="a">First ruleset in the pair.</param>
        ///<param name="b">Second ruleset in the pair.</param>
        ///<returns>Collision rule governing the interaction between the pair.</returns>
        public static CollisionRule GetPersonalCollisionRuleDefault(CollisionRules a, CollisionRules b)
        {
            return a.personal > b.personal ? a.personal : b.personal;
        }


    }
}