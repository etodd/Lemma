﻿namespace BEPUphysics.CollisionRuleManagement
 {
     ///<summary>
     /// Defines a class which must own CollisionRules.
     ///</summary>
     public interface ICollisionRulesOwner
     {
         ///<summary>
         /// Collision rules owned by the object.
         ///</summary>
         CollisionRules CollisionRules { get; set; }
     }
 }