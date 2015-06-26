//-----------------------------------------------------------------------
// <copyright file="CreateMachineRewriter.cs">
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
    /// The create machine expression rewriter.
    /// </summary>
    internal sealed class CreateMachineRewriter : PSharpRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        internal CreateMachineRewriter(PSharpProject project)
            : base(project)
        {

        }

        /// <summary>
        /// Rewrites the syntax tree with create machine expressions.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        internal SyntaxTree Rewrite(SyntaxTree tree)
        {
            var statements = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().
                Where(val => val.Expression is IdentifierNameSyntax).
                Where(val => (val.Expression as IdentifierNameSyntax).Identifier.ValueText.Equals("create")).
                ToList();

            if (statements.Count == 0)
            {
                return tree;
            }

            var root = tree.GetRoot().ReplaceNodes(
                nodes: statements,
                computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten));

            return base.UpdateSyntaxTree(tree, root.ToString());
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the expression with a create machine expression.
        /// </summary>
        /// <param name="node">InvocationExpressionSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteStatement(InvocationExpressionSyntax node)
        {
            var arguments = new List<ArgumentSyntax>(node.ArgumentList.Arguments);
            arguments[0] = SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(
                SyntaxFactory.IdentifierName(arguments[0].ToString())));

            var rewritten = node.
                WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))).
                WithExpression(SyntaxFactory.IdentifierName("this.CreateMachine")).
                WithTriviaFrom(node);

            return rewritten;
        }

        #endregion
    }
}
