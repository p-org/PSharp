using
namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// The set of modifiers for a P# declaration.
    /// </summary>
    internal class ModifierSet
    {
        /// <summary>
        /// The access modifier.
        /// </summary>
        public AccessModifier AccessModifier;

        /// <summary>
        /// The inheritance modifier.
        /// </summary>
        public InheritanceModifier InheritanceModifier;

        /// <summary>
        /// The async modifier.
        /// </summary>
        public bool IsAsync;

        /// <summary>
        /// The partial modifier.
        /// </summary>
        public bool IsPartial;

        /// <summary>
        /// The start modifier.
        /// </summary>
        public bool IsStart;

        /// <summary>
        /// The hot modifier.
        /// </summary>
        public bool IsHot;

        /// <summary>
        /// The cold modifier.
        /// </summary>
        public bool IsCold;

        /// <summary>
        /// Creates a default modifier set.
        /// </summary>
        /// <returns>ModifierSet</returns>
        public static ModifierSet CreateDefault()
        {
            ModifierSet modSet = new ModifierSet();
            modSet.AccessModifier = AccessModifier.None;
            modSet.InheritanceModifier = InheritanceModifier.None;
            modSet.IsAsync = false;
            modSet.IsPartial = false;
            modSet.IsStart = false;
            modSet.IsHot = false;
            modSet.IsCold = false;

            return modSet;
        }
    }
}
