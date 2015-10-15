//-----------------------------------------------------------------------
// <copyright file="MonitorRewriter.cs">
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

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// The monitor statement rewriter.
    /// </summary>
    internal sealed class MonitorRewriter : PSharpRewriter
    {
        #region fields

        /// <summary>
        /// Nodes to be removed.
        /// </summary>
        private List<SyntaxNode> ToRemove;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        internal MonitorRewriter(PSharpProject project)
            : base(project)
        {
            this.ToRemove = new List<SyntaxNode>();
        }

        /// <summary>
        /// Rewrites the syntax tree with monitor statements.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        internal SyntaxTree Rewrite(SyntaxTree tree)
        {
            var statements = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().
                Where(val => val.Expression is GenericNameSyntax).
                Where(val => (val.Expression as GenericNameSyntax).Identifier.ValueText.Equals("monitor")).
                ToList();

            if (statements.Count == 0)
            {
                return tree;
            }

            var root = tree.GetRoot().ReplaceNodes(
                nodes: statements,
                computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten));

            root = root.RemoveNodes(this.ToRemove, SyntaxRemoveOptions.KeepNoTrivia);

            return base.UpdateSyntaxTree(tree, root.ToString());
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the statement with a monitor statement.
        /// </summary>
        /// <param name="node">LocalDeclarationStatementSyntax</param>
        /// <returns>StatementSyntax</returns>
        private SyntaxNode RewriteStatement(InvocationExpressionSyntax node)
        {
            if (this.Project.CompilationContext.ActiveCompilationTarget != CompilationTarget.Testing)
            {
                this.ToRemove.Add(node.Parent);
                return node;
            }

            var arguments = new List<ArgumentSyntax>(node.ArgumentList.Arguments);

            arguments[0] = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(
                "new " + arguments[0].ToString() + "()"));

            var rewritten = node.
                WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))).
                WithExpression((node.Expression as GenericNameSyntax).
                WithIdentifier(SyntaxFactory.Identifier("this.Monitor"))).
                WithTriviaFrom(node);

            return rewritten;
        }

        #endregion
    }
}
