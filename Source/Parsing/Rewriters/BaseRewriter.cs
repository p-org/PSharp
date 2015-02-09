//-----------------------------------------------------------------------
// <copyright file="BaseRewriter.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// Abstract rewriter.
    /// </summary>
    internal abstract class BaseRewriter
    {
        #region fields

        /// <summary>
        /// Root of the given compilation unit.
        /// </summary>
        protected CompilationUnitSyntax Root;

        /// <summary>
        /// Root of the rewritten compilation unit.
        /// </summary>
        protected CompilationUnitSyntax Result;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="root">CompilationUnitSyntax</param>
        internal BaseRewriter(CompilationUnitSyntax root)
        {
            this.Root = root;
            this.Result = null;
        }

        #endregion

        #region helped methods

        /// <summary>
        /// Creates a white space trivia list.
        /// </summary>
        /// <param name="spaceLength">Optional lenght for the white space</param>
        /// <returns>SyntaxTriviaList</returns>
        protected SyntaxTriviaList CreateWhitespaceTriviaList(int spaceLength = 0)
        {
            string space = "";
            for (int idx = 0; idx < spaceLength; idx++)
            {
                space += space + " ";
            }

            var trivia = SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, space);
            return SyntaxFactory.TriviaList(trivia);
        }

        /// <summary>
        /// Creates an end of line trivia list.
        /// </summary>
        /// <returns>SyntaxTriviaList</returns>
        protected SyntaxTriviaList CreateEndOfLineTriviaList()
        {
            var trivia = SyntaxFactory.SyntaxTrivia(SyntaxKind.EndOfLineTrivia, "\n");
            return SyntaxFactory.TriviaList(trivia);
        }

        #endregion
    }
}
