using System;
using System.Collections.Generic;

namespace BEPUphysics.Materials
{
    /// <summary>
    /// A function which takes two materials and computes the interaction properties between them.
    /// </summary>
    /// <param name="a">First material to blend.</param>
    /// <param name="b">Second material to blend.</param>
    /// <param name="properties">Interaction properties between the two materials.</param>
    public delegate void MaterialBlender(Material a, Material b, out InteractionProperties properties);

    ///<summary>
    /// Manages the relationship between materials.
    ///</summary>
    public static class MaterialManager
    {
        /// <summary>
        /// Delegate used to blend two materials together when there is no special handler defined in the MaterialInteractions dictionary.
        /// </summary>
        public static MaterialBlender MaterialBlender = DefaultMaterialBlender;


        ///<summary>
        /// Default coefficient of kinetic friction. 
        /// Defaults to 0.8.
        ///</summary>
        public static float DefaultKineticFriction = .8f;
        ///<summary>
        /// Default coefficient of static friction.
        /// Defaults to 1.
        ///</summary>
        public static float DefaultStaticFriction = 1f;
        ///<summary>
        /// Default coefficient of restitution.
        /// Defaults to 0.
        ///</summary>
        public static float DefaultBounciness;

        static MaterialManager()
        {
            MaterialInteractions = new Dictionary<MaterialPair, MaterialBlender>();
        }

        ///<summary>
        /// Computes the interaction properties between two materials.
        ///</summary>
        ///<param name="materialA">First material of the pair.</param>
        ///<param name="materialB">Second material of the pair.</param>
        ///<param name="properties">Interaction properties between two materials.</param>
        public static void GetInteractionProperties(Material materialA, Material materialB, out InteractionProperties properties)
        {
            MaterialBlender specialBlender;
            if (MaterialInteractions.TryGetValue(new MaterialPair(materialA, materialB), out specialBlender))
                specialBlender(materialA, materialB, out properties);
            else
                MaterialBlender(materialA, materialB, out properties);

        }



        ///<summary>
        /// Gets or sets the material interactions dictionary.
        /// This dictionary contains all the special relationships between specific materials.
        /// These interaction properties will override properties obtained by normal blending.
        ///</summary>
        public static Dictionary<MaterialPair, MaterialBlender> MaterialInteractions { get; set; }

        /// <summary>
        /// Blender used to combine materials into a pair's interaction properties.
        /// </summary>
        /// <param name="a">Material associated with the first object to blend.</param>
        /// <param name="b">Material associated with the second object to blend.</param>
        /// <param name="properties">Blended material values.</param>
        public static void DefaultMaterialBlender(Material a, Material b, out InteractionProperties properties)
        {
            properties = new InteractionProperties
            {
                Bounciness = a.bounciness * b.bounciness,
                KineticFriction = a.kineticFriction * b.kineticFriction,
                StaticFriction = a.staticFriction * b.staticFriction
            };
        }


    }
}
