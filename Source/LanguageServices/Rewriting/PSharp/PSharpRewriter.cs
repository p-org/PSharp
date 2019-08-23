using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// An abstract P# program rewriter.
    /// </summary>
    internal abstract class PSharpRewriter
    {
        /// <summary>
        /// The P# program.
        /// </summary>
        protected IPSharpProgram Program;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSharpRewriter"/> class.
        /// </summary>
        protected PSharpRewriter(IPSharpProgram program) => this.Program = program;

        /// <summary>
        /// Updates the syntax tree.
        /// </summary>
        protected void UpdateSyntaxTree(string text) => this.Program.UpdateSyntaxTree(text);
    }
}
