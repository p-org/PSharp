//-----------------------------------------------------------------------
// <copyright file="MonitorRewriter.cs">
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

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// The monitor statement rewriter.
    /// </summary>
    internal sealed class MonitorRewriter : PSharpRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal MonitorRewriter(IPSharpProgram program)
            : base(program)
        {

        }

        /// <summary>
        /// Rewrites the monitor statements in the program.
        /// </summary>
        internal void Rewrite()
        {
            var statements = base.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().
                Where(val => val.Expression is GenericNameSyntax).
                Where(val => (val.Expression as GenericNameSyntax).Identifier.ValueText.Equals("monitor")).
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
        /// Rewrites the statement with a monitor statement.
        /// </summary>
        /// <param name="node">LocalDeclarationStatementSyntax</param>
        /// <returns>StatementSyntax</returns>
        private SyntaxNode RewriteStatement(InvocationExpressionSyntax node)
        {
            var arguments = new List<ArgumentSyntax>();
            arguments.Add(node.ArgumentList.Arguments[0]);

            string payload = String.Empty;
            for (int i = 1; i < node.ArgumentList.Arguments.Count; i++)
            {
                if (i == node.ArgumentList.Arguments.Count - 1)
                {
                    payload += node.ArgumentList.Arguments[i].ToString();
                }
                else
                {
                    payload += node.ArgumentList.Arguments[i].ToString() + ", ";
                }
            }

            arguments[0] = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(
                "new " + arguments[0].ToString() + "(" + payload + ")"));

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
