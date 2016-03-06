//-----------------------------------------------------------------------
// <copyright file="CreateRemoteMachineRewriter.cs">
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
    /// The create remote machine expression rewriter.
    /// </summary>
    internal sealed class CreateRemoteMachineRewriter : PSharpRewriter
    {
        #region fields

        /// <summary>
        /// Nodes to be replaced.
        /// </summary>
        private List<SyntaxNode> ToReplace;

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
        internal CreateRemoteMachineRewriter(PSharpProject project)
            : base(project)
        {
            this.ToReplace = new List<SyntaxNode>();
            this.ToRemove = new List<SyntaxNode>();
        }

        /// <summary>
        /// Rewrites the syntax tree with create remote machine expressions.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        internal SyntaxTree Rewrite(SyntaxTree tree)
        {
            var statements = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().
                Where(val => val.Expression is IdentifierNameSyntax).
                Where(val => (val.Expression as IdentifierNameSyntax).Identifier.ValueText.Equals("remote")).
                ToList();

            if (statements.Count == 0)
            {
                return tree;
            }

            var root = tree.GetRoot().ReplaceNodes(
                nodes: statements,
                computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten));

            var models = root.DescendantNodes().
                Where(val => val is LocalDeclarationStatementSyntax).
                Where(val => this.ToReplace.Any(n => n.IsEquivalentTo(val)));

            root = root.ReplaceNodes(
                nodes: models,
                computeReplacementNode: (node, rewritten) => SyntaxFactory.ParseStatement(";"));

            root = root.RemoveNodes(this.ToRemove, SyntaxRemoveOptions.KeepNoTrivia);

            return base.UpdateSyntaxTree(tree, root.ToString());
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the expression with a create remote machine expression.
        /// </summary>
        /// <param name="node">InvocationExpressionSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteStatement(InvocationExpressionSyntax node)
        {
            var arguments = new List<ArgumentSyntax>(node.ArgumentList.Arguments);
            var machineIdentifier = arguments[0].ToString();

            SyntaxNode models = null;

            var parent = node.FirstAncestorOrSelf<ExpressionStatementSyntax>();
            if (parent != null)
            {
                models = base.GetNextStatement(parent);
                if (models != null &&
                    (models is LocalDeclarationStatementSyntax) &&
                    (models as LocalDeclarationStatementSyntax).Declaration.
                    Type.ToString().Equals("models"))
                {
                    if (this.Project.CompilationContext.ActiveCompilationTarget != CompilationTarget.Testing)
                    {
                        machineIdentifier = (models as LocalDeclarationStatementSyntax).
                            Declaration.Variables[0].Identifier.ValueText;
                    }
                }
                else
                {
                    models = null;
                }
            }

            arguments[0] = SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(
                SyntaxFactory.IdentifierName(machineIdentifier)));

            var text = "";
            if (base.IsMonitor(machineIdentifier))
            {
                if (this.Project.CompilationContext.ActiveCompilationTarget != CompilationTarget.Testing)
                {
                    this.ToRemove.Add(node);
                    if (models != null)
                    {
                        this.ToRemove.Add(models);
                    }

                    return node;
                }

                text += "this.CreateMonitor";
            }
            else
            {
                text += "this.CreateRemoteMachine";
            }

            var rewritten = node.
                WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))).
                WithExpression(SyntaxFactory.IdentifierName(text)).
                WithTriviaFrom(node);

            if (models != null)
            {
                node = node.WithoutTrailingTrivia();
                this.ToReplace.Add(models);
            }

            return rewritten;
        }

        #endregion
    }
}
