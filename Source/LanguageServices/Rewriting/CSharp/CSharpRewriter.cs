// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices.Rewriting.CSharp
{
    /// <summary>
    /// An abstract C# program rewriter.
    /// </summary>
    public abstract class CSharpRewriter
    {
        /// <summary>
        /// The P# program.
        /// </summary>
        protected IPSharpProgram Program;

        /// <summary>
        /// Rewrites the program.
        /// </summary>
        public abstract void Rewrite();

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpRewriter"/> class.
        /// </summary>
        protected CSharpRewriter(IPSharpProgram program)
        {
            this.Program = program;
        }

        /// <summary>
        /// Updates the syntax tree.
        /// </summary>
        protected void UpdateSyntaxTree(string text)
        {
            this.Program.UpdateSyntaxTree(text);
        }

        /// <summary>
        /// Returns true if the given expression is the expected one.
        /// </summary>
        protected static bool IsExpectedExpression(ExpressionSyntax expression, string expectedName, SemanticModel model)
        {
            var symbol = model.GetSymbolInfo(expression).Symbol;
            if (IsExpectedSymbol(symbol, expectedName))
            {
                return true;
            }

            var candidateSymbols = model.GetSymbolInfo(expression).CandidateSymbols;
            foreach (var candidate in candidateSymbols)
            {
                if (IsExpectedSymbol(candidate, expectedName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given symbol is the expected one.
        /// </summary>
        private static bool IsExpectedSymbol(ISymbol symbol, string expectedName)
        {
            if (symbol != null)
            {
                string name = symbol.ContainingNamespace.ToString() + "." + symbol.Name;
                if (name.Equals(expectedName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
