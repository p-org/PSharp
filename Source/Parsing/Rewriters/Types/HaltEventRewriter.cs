//-----------------------------------------------------------------------
// <copyright file="HaltEventRewriter.cs">
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

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.Parsing.Syntax
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
        /// <param name="project">PSharpProject</param>
        internal HaltEventRewriter(PSharpProject project)
            : base(project)
        {

        }

        /// <summary>
        /// Rewrites the syntax tree with halt event types.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        internal SyntaxTree Rewrite(SyntaxTree tree)
        {
            var types = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().
                Where(val => val.Identifier.ValueText.Equals("halt")).
                ToList();

            if (types.Count == 0)
            {
                return tree;
            }

            var root = tree.GetRoot().ReplaceNodes(
                nodes: types,
                computeReplacementNode: (node, rewritten) => this.RewriteType(rewritten));

            return base.UpdateSyntaxTree(tree, root.ToString());
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the type with a halt event type.
        /// </summary>
        /// <param name="node">IdentifierNameSyntax</param>
        /// <returns>ExpressionSyntax</returns>
        private ExpressionSyntax RewriteType(IdentifierNameSyntax node)
        {
            var text = "Halt";

            var rewritten = SyntaxFactory.ParseExpression(text);
            rewritten = rewritten.WithTriviaFrom(node);

            return rewritten;
        }

        #endregion
    }
}
