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
    internal abstract class CSharpRewriter
    {
        #region fields

        /// <summary>
        /// The P# program.
        /// </summary>
        protected IPSharpProgram Program;

        #endregion

        #region protected API

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
            if (symbol == null)
            {
                return false;
            }
            
            var name = symbol.ContainingNamespace.ToString() + "." + symbol.Name;
            if (expectedName.Equals(name))
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
