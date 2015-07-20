//-----------------------------------------------------------------------
// <copyright file="NondetRewriter.cs">
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

namespace Microsoft.PSharp.LanguageServices.Rewriting.CSharp
{
    /// <summary>
    /// The nondet statement rewriter.
    /// </summary>
    internal sealed class NondetRewriter : CSharpRewriter
    {
        #region public API

        /// <summary>
        /// Counter of unique nondet statement ids.
        /// </summary>
        private static int IdCounter;

        #endregion

        #region public API

        /// <summary>
        /// Static constructor.
        /// </summary>
        static NondetRewriter()
        {
            NondetRewriter.IdCounter = 0;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        internal NondetRewriter(PSharpProject project)
            : base(project)
        {
            
        }

        /// <summary>
        /// Rewrites the syntax tree with nondet statements.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>SyntaxTree</returns>
        internal SyntaxTree Rewrite(SyntaxTree tree)
        {
            var stmts1 = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().
                Where(val => val.Expression is IdentifierNameSyntax).
                Where(val => (val.Expression as IdentifierNameSyntax).
                    Identifier.ValueText.Equals("Nondet")).
                ToList();
            
            var stmts2 = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().
                Where(val => val.Expression is MemberAccessExpressionSyntax).
                Where(val => (val.Expression as MemberAccessExpressionSyntax).
                    Name.Identifier.ValueText.Equals("Nondet")).
                ToList();

            var statements = stmts1;
            statements.AddRange(stmts2);

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
        /// Rewrites the nondet statement.
        /// </summary>
        /// <param name="node">InvocationExpressionSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteStatement(InvocationExpressionSyntax node)
        {
            var uniqueId = NondetRewriter.IdCounter;
            NondetRewriter.IdCounter++;

            var text = "this.Nondet(" + uniqueId + ")";
            var rewritten = SyntaxFactory.ParseExpression(text);
            rewritten = rewritten.WithTriviaFrom(node);

            return rewritten;
        }

        #endregion
    }
}
