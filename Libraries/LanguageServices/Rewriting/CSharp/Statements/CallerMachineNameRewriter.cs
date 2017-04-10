//-----------------------------------------------------------------------
// <copyright file="CallerMachineNameRewriter.cs">
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
    /// The caller machine name argument rewriter.
    /// </summary>
    internal sealed class CallerMachineNameRewriter : CSharpRewriter
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">IPSharpProgram</param>
        internal CallerMachineNameRewriter(IPSharpProgram program)
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
            
            var statements = this.Program.GetSyntaxTree().GetRoot().DescendantNodes().OfType<ExpressionStatementSyntax>();
            var invocationsToRewrite = new Dictionary<ExpressionStatementSyntax, List<string>>();

            foreach (var statement in statements)
            {
                // Checks if the expression is an invocation.
                if (!(statement.Expression is InvocationExpressionSyntax))
                {
                    continue;
                }

                InvocationExpressionSyntax invocation = statement.Expression as InvocationExpressionSyntax;

                // Checks that the invocation is called from the scope of a machine.
                var classDecl = invocation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (classDecl == null || !Querying.IsMachine(compilation, classDecl))
                {
                    continue;
                }

                // Gets the method symbol of this invocation.
                var methodSymbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (methodSymbol == null)
                {
                    continue;
                }

                // If the invocation has the same number of arguments as the number of
                // parameters in the method, then continue to the next invocation.
                if (invocation.ArgumentList.Arguments.Count == methodSymbol.Parameters.Length)
                {
                    continue;
                }

                List<string> parameterNames = null;
                for (int i = 0; i < methodSymbol.Parameters.Length; i++)
                {
                    IParameterSymbol parameter = methodSymbol.Parameters[i];

                    // Checks if the parameter is declared as optional.
                    if (!parameter.IsOptional)
                    {
                        continue;
                    }

                    // Checks if the parameter has type string.
                    if (parameter.Type.SpecialType != SpecialType.System_String)
                    {
                        continue;
                    }
                    
                    // Iterates through the parameter attributes, and checks if the parameter
                    // is annotated with the [CallerMachineName] attribute.
                    if (!parameter.GetAttributes().Any(attr
                        => attr.AttributeClass.ToString().Equals("Microsoft.PSharp.LanguageServices.CallerMachineName")))
                    {
                        continue;
                    }

                    bool found = false;
                    for (int j = 0; j < invocation.ArgumentList.Arguments.Count; j++)
                    {
                        ArgumentSyntax argument = invocation.ArgumentList.Arguments[j];

                        // Checks if the parameter is passed as a named or a regular argument.
                        if ((argument.NameColon != null && argument.NameColon.Name.Identifier.ValueText == parameter.Name) ||
                            (argument.NameColon == null && j == i))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        continue;
                    }

                    if (parameterNames == null)
                    {
                        parameterNames = new List<string>();
                    }

                    parameterNames.Add(methodSymbol.Parameters[i].Name);
                }

                if (parameterNames != null)
                {
                    invocationsToRewrite.Add(statement, parameterNames);
                }
            }

            if (invocationsToRewrite.Count == 0)
            {
                return;
            }

            var root = this.Program.GetSyntaxTree().GetRoot().ReplaceNodes(
                nodes: invocationsToRewrite.Keys.ToList(),
                computeReplacementNode: (node, rewritten) => this.RewriteStatement(rewritten, invocationsToRewrite[rewritten]));

            base.UpdateSyntaxTree(root.ToString());
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the caller machine name arguments in the specified invocation.
        /// </summary>
        /// <param name="node">ExpressionStatementSyntax</param>
        /// <param name="parameterNames">Parameter names to be inserted</param>
        /// <returns>SyntaxNode</returns>
        private SyntaxNode RewriteStatement(ExpressionStatementSyntax node, List<string> parameterNames)
        {
            var invocation = node.Expression as InvocationExpressionSyntax;

            var arguments = new List<ArgumentSyntax>();
            for (int i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
            {
                arguments.Add(invocation.ArgumentList.Arguments[i]);
            }

            foreach (var param in parameterNames)
            {
                arguments.Add(SyntaxFactory.Argument(SyntaxFactory.ParseExpression(param + ": " + "base.Id.Name")));
            }

            invocation = invocation.WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));

            var text = node.WithExpression(invocation).ToString();
            var rewritten = SyntaxFactory.ParseStatement(text);
            rewritten = rewritten.WithTriviaFrom(node);

            return rewritten;
        }

        #endregion
    }
}
