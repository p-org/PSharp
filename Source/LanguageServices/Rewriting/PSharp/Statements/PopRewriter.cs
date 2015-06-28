//-----------------------------------------------------------------------
// <copyright file="PopRewriter.cs">
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

namespace Microsoft.PSharp.LanguageServices.Rewriting
{
    /// <summary>
    /// The pop statement rewriter.
    /// </summary>
    internal sealed class PopRewriter : PSharpRewriter
    {
        #region fields

        /// <summary>
        /// The rewritten pop statements.
        /// </summary>
        private List<StatementSyntax> PopStmts;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        internal PopRewriter(PSharpProject project)
            : base(project)
        {
            this.PopStmts = new List<StatementSyntax>();
        }

        /// <summary>
        /// Rewrites the syntax tree with pop statements.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        internal SyntaxTree Rewrite(SyntaxTree tree)
        {
            var statements = tree.GetRoot().DescendantNodes().OfType<ExpressionStatementSyntax>().
                Where(val => val.Expression is IdentifierNameSyntax).
                Where(val => (val.Expression as IdentifierNameSyntax).Identifier.ValueText.Equals("pop")).
                ToList();

            if (statements.Count == 0)
            {
                return tree;
            }

            var root = tree.GetRoot().ReplaceNodes(
                nodes: statements,
                computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten));

            var popStmts = root.DescendantNodes().OfType<StatementSyntax>().
                Where(val => this.PopStmts.Any(v => SyntaxFactory.AreEquivalent(v, val))).ToList();

            foreach (var stmt in popStmts)
            {
                root = root.InsertNodesAfter(stmt, new List<SyntaxNode> { this.CreateReturnStatement() });
            }

            return base.UpdateSyntaxTree(tree, root.ToString());
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the statement with a pop statement.
        /// </summary>
        /// <param name="node">ExpressionStatementSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteStatement(ExpressionStatementSyntax node)
        {
            var text = "this.Pop();";

            var rewritten = SyntaxFactory.ParseStatement(text);
            rewritten = rewritten.WithTriviaFrom(node);
            this.PopStmts.Add(rewritten);

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
