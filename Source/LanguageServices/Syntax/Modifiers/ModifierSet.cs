//-----------------------------------------------------------------------
// <copyright file="ModifierSet.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

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
