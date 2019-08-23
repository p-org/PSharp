// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// Interface to a P# program.
    /// </summary>
    public interface IPSharpProgram
    {
        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        void Rewrite();

        /// <summary>
        /// Returns the project of the P# program.
        /// </summary>
        /// <returns>PSharpProject</returns>
        PSharpProject GetProject();

        /// <summary>
        /// Returns the syntax tree of the P# program.
        /// </summary>
        /// <returns>SyntaxTree</returns>
        SyntaxTree GetSyntaxTree();

        /// <summary>
        /// Updates the syntax tree of the P# program.
        /// </summary>
        /// <param name="text">Text</param>
        void UpdateSyntaxTree(string text);

        /// <summary>
        /// Add a record of a rewritten term (type, statement, expression) from P# to C#.
        /// </summary>
        void AddRewrittenTerm(SyntaxNode node, string rewrittenText);
    }
}
