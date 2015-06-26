//-----------------------------------------------------------------------
// <copyright file="RaiseRewriter.cs">
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
    /// The raise statement rewriter.
    /// </summary>
    internal sealed class RaiseRewriter : PSharpRewriter
    {
        #region fields

        /// <summary>
        /// The rewritten raise statements.
        /// </summary>
        private List<ExpressionStatementSyntax> RaiseStmts;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        internal RaiseRewriter(PSharpProject project)
            : base(project)
        {
            this.RaiseStmts = new List<ExpressionStatementSyntax>();
        }

        /// <summary>
        /// Rewrites the syntax tree with raise statements.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        internal SyntaxTree Rewrite(SyntaxTree tree)
        {
            var statements = tree.GetRoot().DescendantNodes().OfType<ExpressionStatementSyntax>().
                Where(val => val.Expression is InvocationExpressionSyntax).
                Where(val => (val.Expression as InvocationExpressionSyntax).Expression is IdentifierNameSyntax).
                Where(val => ((val.Expression as InvocationExpressionSyntax).Expression as IdentifierNameSyntax).
                    Identifier.ValueText.Equals("raise")).
                ToList();

            if (statements.Count == 0)
            {
                return tree;
            }

            var root = tree.GetRoot().ReplaceNodes(
                nodes: statements,
                computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten));

            var raiseStmts = root.DescendantNodes().OfType<ExpressionStatementSyntax>().
                Where(val => this.RaiseStmts.Any(v => SyntaxFactory.AreEquivalent(v, val))).ToList();

            foreach (var stmt in raiseStmts)
            {
                root = root.InsertNodesAfter(stmt, new List<SyntaxNode> { this.CreateReturnStatement() });
            }

            return base.UpdateSyntaxTree(tree, root.ToString());
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the statement with a raise statement.
        /// </summary>
        /// <param name="node">ExpressionStatementSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteStatement(ExpressionStatementSyntax node)
        {
            var invocation = node.Expression as InvocationExpressionSyntax;
            var arguments = new List<ArgumentSyntax>(invocation.ArgumentList.Arguments);

            arguments[0] = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(
                "new " + arguments[0].ToString() + "()"));
            invocation = invocation.WithArgumentList(SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(arguments)));
            
            var rewritten = node.
                WithExpression(invocation.WithExpression(SyntaxFactory.IdentifierName("this.Raise")))
                .WithTriviaFrom(node);

            this.RaiseStmts.Add(rewritten);

            rewritten = rewritten.WithoutTrailingTrivia();

            return rewritten;
        }

        /// <summary>
        /// Creates a return statement.
        /// </summary>
        /// <returns>StatementSyntax</returns>
        private StatementSyntax CreateReturnStatement()
        {
            var returnStmt = SyntaxFactory.ParseStatement("return;");
            returnStmt = returnStmt.WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));
            return returnStmt;
        }

        #endregion
    }
}
