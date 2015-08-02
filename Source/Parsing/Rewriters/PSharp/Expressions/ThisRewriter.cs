//-----------------------------------------------------------------------
// <copyright file="ThisRewriter.cs">
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
    /// The this expression rewriter.
    /// </summary>
    internal sealed class ThisRewriter : PSharpRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        internal ThisRewriter(PSharpProject project)
            : base(project)
        {

        }

        /// <summary>
        /// Rewrites the syntax tree with this expressions.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        internal SyntaxTree Rewrite(SyntaxTree tree)
        {
            var expressions = tree.GetRoot().DescendantNodes().OfType<ThisExpressionSyntax>().
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
        /// Rewrites the expression with a this expression.
        /// </summary>
        /// <param name="node">ThisExpressionSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteExpression(ThisExpressionSyntax node)
        {
            SyntaxNode rewritten = node;

            MachineDeclaration machine = null;
            if (!base.TryGetParentMachine(rewritten, out machine))
            {
                return rewritten;
            }

            var isMonitor = base.IsMonitor(machine.Identifier.TextUnit.Text);
            if (!isMonitor && (rewritten.Parent is ArgumentSyntax ||
                (rewritten.Parent is AssignmentExpressionSyntax &&
                (rewritten.Parent as AssignmentExpressionSyntax).Right.IsEquivalentTo(node))))
            {
                var text = "this.Id";

                rewritten = SyntaxFactory.ParseExpression(text);
                rewritten = rewritten.WithTriviaFrom(node);
            }
            else if (rewritten.Parent is MemberAccessExpressionSyntax &&
                base.IsInStateScope(rewritten) &&
                (base.IsMachineField((rewritten.Parent as MemberAccessExpressionSyntax).Name) ||
                base.IsMachineMethod((rewritten.Parent as MemberAccessExpressionSyntax).Name)))
            {
                var text = "(";

                if (isMonitor)
                {
                    text += "this.Monitor";
                }
                else
                {
                    text += "this.Machine";
                }

                text += " as " + machine.Identifier.TextUnit.Text + ")";

                rewritten = SyntaxFactory.ParseExpression(text);
                rewritten = rewritten.WithTriviaFrom(node);
            }

            return rewritten;
        }

        #endregion
    }
}
