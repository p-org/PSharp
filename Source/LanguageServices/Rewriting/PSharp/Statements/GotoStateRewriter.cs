//-----------------------------------------------------------------------
// <copyright file="GotoStateRewriter.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
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
    /// The goto state statement rewriter.
    /// </summary>
    internal sealed class GotoStateRewriter : PSharpRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal GotoStateRewriter(IPSharpProgram program)
            : base(program)
        {

        }

        /// <summary>
        /// Rewrites the goto state statements in the program.
        /// </summary>
        internal void Rewrite()
        {
            var statements = base.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<ExpressionStatementSyntax>().
                Where(val => val.Expression is InvocationExpressionSyntax).
                Where(val => (val.Expression as InvocationExpressionSyntax).Expression is IdentifierNameSyntax).
                Where(val => ((val.Expression as InvocationExpressionSyntax).Expression as IdentifierNameSyntax).
                    Identifier.ValueText.Equals("jump")).
                ToList();

            if (statements.Count == 0)
            {
                return;
            }

            var root = base.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                nodes: statements,
                computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten));

            base.UpdateSyntaxTree(root.ToString());
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the jump(StateType) statement with a goto&lt;StateType&gt;() statement.
        /// </summary>
        /// <param name="node">ExpressionStatementSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteStatement(ExpressionStatementSyntax node)
        {
            var invocation = node.Expression as InvocationExpressionSyntax;

            var arg = invocation.ArgumentList.Arguments[0].ToString();
            var text = node.WithExpression(SyntaxFactory.ParseExpression($"this.Goto<{arg}>()")).ToString();

            var rewritten = SyntaxFactory.ParseStatement(text).WithTriviaFrom(node);
            return rewritten;
        }

#endregion
    }
}
