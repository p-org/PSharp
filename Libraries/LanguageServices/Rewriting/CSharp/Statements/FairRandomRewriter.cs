//-----------------------------------------------------------------------
// <copyright file="FairRandomRewriter.cs">
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

namespace Microsoft.PSharp.LanguageServices.Rewriting.CSharp
{
    /// <summary>
    /// The fair random statement rewriter.
    /// </summary>
    internal sealed class FairRandomRewriter : CSharpRewriter
    {
        #region public API

        /// <summary>
        /// Counter of unique random statement ids.
        /// </summary>
        private static int IdCounter;

        #endregion

        #region public API

        /// <summary>
        /// Static constructor.
        /// </summary>
        static FairRandomRewriter()
        {
            FairRandomRewriter.IdCounter = 0;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal FairRandomRewriter(IPSharpProgram program)
            : base(program)
        {
            
        }

        /// <summary>
        /// Rewrites the program.
        /// </summary>
        public override void Rewrite()
        {
            var compilation = base.Program.GetProject().GetCompilation();
            var model = compilation.GetSemanticModel(base.Program.GetSyntaxTree());

            var statements = this.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().
                Where(val => base.IsExpectedExpression(val.Expression, "Microsoft.PSharp.FairRandom", model)).
                ToList();

            if (statements.Count == 0)
            {
                return;
            }

            var root = this.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                nodes: statements,
                computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten));

            base.UpdateSyntaxTree(root.ToString());
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the fair random statement.
        /// </summary>
        /// <param name="node">InvocationExpressionSyntax</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteStatement(InvocationExpressionSyntax node)
        {
            var uniqueId = FairRandomRewriter.IdCounter;
            FairRandomRewriter.IdCounter++;

            var text = "this.FairRandom(" + uniqueId + ")";
            var rewritten = SyntaxFactory.ParseExpression(text);
            rewritten = rewritten.WithTriviaFrom(node);

            return rewritten;
        }

        #endregion
    }
}
