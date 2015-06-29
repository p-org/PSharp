//-----------------------------------------------------------------------
// <copyright file="FieldAccessRewriter.cs">
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
    /// The field access expression rewriter.
    /// </summary>
    internal sealed class FieldAccessRewriter : PSharpRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        internal FieldAccessRewriter(PSharpProject project)
            : base(project)
        {

        }

        /// <summary>
        /// Rewrites the syntax tree with field access expressions.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        internal SyntaxTree Rewrite(SyntaxTree tree)
        {
            var expressions = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().
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

        #region private API

        /// <summary>
        /// Rewrites the expression with a field access expression.
        /// </summary>
        /// <param name="node">IdentifierNameSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteExpression(IdentifierNameSyntax node)
        {
            SyntaxNode rewritten = node;

            if (!base.IsInStateScope(rewritten) ||
                !(base.IsMachineField(rewritten) || base.IsMachineMethod(rewritten)))
            {
                return rewritten;
            }

            if (rewritten.Parent is ArgumentSyntax &&
                rewritten.Parent.Parent is ArgumentListSyntax &&
                rewritten.Parent.Parent.Parent is InvocationExpressionSyntax)
            {
                var invocation = rewritten.Parent.Parent.Parent as InvocationExpressionSyntax;
                if (invocation.Expression is IdentifierNameSyntax &&
                    (invocation.Expression as IdentifierNameSyntax).Identifier.ValueText.Equals("nameof") &&
                    invocation.ArgumentList.Arguments.Count == 1)
                {
                    return rewritten;
                }
            }

            if (!(rewritten.Parent is MemberAccessExpressionSyntax) &&
                !(rewritten.Parent is ObjectCreationExpressionSyntax) &&
                !(rewritten.Parent is TypeOfExpressionSyntax))
            {
                var text = "this." + node.ToString();

                rewritten = SyntaxFactory.ParseExpression(text);
                rewritten = rewritten.WithTriviaFrom(node);
            }

            if ((rewritten.Parent is MemberAccessExpressionSyntax) &&
                (rewritten.Parent as MemberAccessExpressionSyntax).Expression is IdentifierNameSyntax &&
                ((rewritten.Parent as MemberAccessExpressionSyntax).Expression as IdentifierNameSyntax).
                IsEquivalentTo(node))
            {
                var text = "this." + node.ToString();

                rewritten = SyntaxFactory.ParseExpression(text);
                rewritten = rewritten.WithTriviaFrom(node);
            }

            return rewritten;
        }

        #endregion
    }
}
