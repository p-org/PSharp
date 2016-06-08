//-----------------------------------------------------------------------
// <copyright file="NondeterministicChoiceRewriter.cs">
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
    /// The nondetermnistic choice expression rewriter.
    /// </summary>
    internal sealed class NondeterministicChoiceRewriter : PSharpRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal NondeterministicChoiceRewriter(IPSharpProgram program)
            : base(program)
        {

        }

        /// <summary>
        /// Rewrites the nondetermnistic choice expressions in the program.
        /// </summary>
        internal void Rewrite()
        {
            var expressions = base.Program.GetSyntaxTree().GetRoot().DescendantNodes().
                OfType<PrefixUnaryExpressionSyntax>().
                Where(val => val.Kind() == SyntaxKind.PointerIndirectionExpression).
                Where(val => val.Parent is IfStatementSyntax).
                ToList();

            if (expressions.Count == 0)
            {
                return;
            }

            var root = base.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                nodes: expressions,
                computeReplacementNode: (node, rewritten) => this.RewriteExpression(rewritten));

            base.UpdateSyntaxTree(root.ToString());
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
            var text = "this.Random()";
            var rewritten = SyntaxFactory.ParseExpression(text);
            return rewritten;
        }

        #endregion
    }
}
