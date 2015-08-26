//-----------------------------------------------------------------------
// <copyright file="NondeterministicChoiceRewriter.cs">
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

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// The nondetermnistic choice expression rewriter.
    /// </summary>
    internal sealed class NondeterministicChoiceRewriter : PSharpRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        internal NondeterministicChoiceRewriter(PSharpProject project)
            : base(project)
        {

        }

        /// <summary>
        /// Rewrites the syntax tree with nondetermnistic choice expressions.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        internal SyntaxTree Rewrite(SyntaxTree tree)
        {
            var expressions = tree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().
                Where(val => val.Kind() == SyntaxKind.PointerIndirectionExpression).
                Where(val => val.Parent is IfStatementSyntax).
                ToList();

            if (expressions.Count == 0)
            {
                return tree;
            }

            var root = tree.GetRoot().ReplaceNodes(
                nodes: expressions,
                computeReplacementNode: (node, rewritten) => this.RewriteExpression(rewritten));

            return base.UpdateSyntaxTree(tree, root.ToString());
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the expression with a nondetermnistic choice expression.
        /// </summary>
        /// <param name="node">PrefixUnaryExpressionSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteExpression(PrefixUnaryExpressionSyntax node)
        {
            var text = "this.Nondet()";
            var rewritten = SyntaxFactory.ParseExpression(text);
            return rewritten;
        }

        #endregion
    }
}
