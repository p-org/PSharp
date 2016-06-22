//-----------------------------------------------------------------------
// <copyright file="HaltEventRewriter.cs">
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

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// The halt event type rewriter.
    /// </summary>
    internal sealed class HaltEventRewriter : PSharpRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal HaltEventRewriter(IPSharpProgram program)
            : base(program)
        {

        }

        /// <summary>
        /// Rewrites the halt event types in the program.
        /// </summary>
        internal void Rewrite()
        {
            var types = base.Program.GetSyntaxTree().GetRoot().
                DescendantNodes().OfType<IdentifierNameSyntax>().
                Where(val => val.Identifier.ValueText.Equals("halt")).
                ToList();

            if (types.Count == 0)
            {
                return;
            }

            var root = base.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                nodes: types,
                computeReplacementNode: (node, rewritten) => this.RewriteType(rewritten));

            base.UpdateSyntaxTree(root.ToString());
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the type with a halt event type.
        /// </summary>
        /// <param name="node">IdentifierNameSyntax</param>
        /// <returns>ExpressionSyntax</returns>
        private ExpressionSyntax RewriteType(IdentifierNameSyntax node)
        {
            var text = typeof(Halt).FullName;

            var rewritten = SyntaxFactory.ParseName(text);
            rewritten = rewritten.WithTriviaFrom(node);

            return rewritten;
        }

        #endregion
    }
}
