//-----------------------------------------------------------------------
// <copyright file="CSharpRewriter.cs">
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices.Rewriting.CSharp
{
    /// <summary>
    /// An abstract C# program rewriter.
    /// </summary>
    public abstract class CSharpRewriter
    {
        #region fields

        /// <summary>
        /// The P# program.
        /// </summary>
        protected IPSharpProgram Program;

        #endregion

        #region internal methods

        /// <summary>
        /// Rewrites the program.
        /// </summary>
        public abstract void Rewrite();

        #endregion

        #region protected methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        protected CSharpRewriter(IPSharpProgram program)
        {
            this.Program = program;
        }

        /// <summary>
        /// Updates the syntax tree.
        /// </summary>
        /// <param name="text">Text</param>
        protected void UpdateSyntaxTree(string text)
        {
            this.Program.UpdateSyntaxTree(text);
        }

        /// <summary>
        /// Returns true if the given expression is the expected one.
        /// </summary>
        /// <param name="expression">ExpressionSyntax</param>
        /// <param name="expectedName">Text</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Boolean</returns>
        protected bool IsExpectedExpression(ExpressionSyntax expression,
            string expectedName, SemanticModel model)
        {
            var symbol = model.GetSymbolInfo(expression).Symbol;
            if (this.IsExpectedSymbol(symbol, expectedName))
            {
                return true;
            }

            var candidateSymbols = model.GetSymbolInfo(expression).CandidateSymbols;
            foreach (var candidate in candidateSymbols)
            {
                if (this.IsExpectedSymbol(candidate, expectedName))
                {
                    return true;
                }
            }
            
            return false;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns true if the given symbol is the expected one.
        /// </summary>
        /// <param name="symbol">ISymbol</param>
        /// <param name="expectedName">Text</param>
        /// <returns>Boolean</returns>
        private bool IsExpectedSymbol(ISymbol symbol, string expectedName)
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

        #endregion
    }
}
