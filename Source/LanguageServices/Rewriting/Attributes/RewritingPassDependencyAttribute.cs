using System;

namespace Microsoft.PSharp.LanguageServices.Rewriting.CSharp
{
    /// <summary>
    /// Attribute for custom C# rewriting pass.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RewritingPassDependencyAttribute : Attribute
    {
        /// <summary>
        /// Pass dependencies.
        /// </summary>
        internal Type[] Dependencies;

        /// <summary>
        /// Initializes a new instance of the <see cref="RewritingPassDependencyAttribute"/> class.
        /// </summary>
        public RewritingPassDependencyAttribute(params Type[] dependencies)
        {
            this.Dependencies = dependencies;
        }
    }
}
