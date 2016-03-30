//-----------------------------------------------------------------------
// <copyright file="GivesUpOwnershipAnalysisPass.cs">
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
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis pass computes the gives-up ownership
    /// summaries for each machine of a P# program.
    /// </summary>
    public sealed class GivesUpOwnershipAnalysisPass : AnalysisPass
    {
        #region public API

        /// <summary>
        /// Creates a new gives-up ownership analysis pass.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <returns>GivesUpOwnershipAnalysisPass</returns>
        public static GivesUpOwnershipAnalysisPass Create(PSharpAnalysisContext context)
        {
            return new GivesUpOwnershipAnalysisPass(context);
        }

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        public override void Run()
        {
            // Starts profiling the summarization.
            if (this.AnalysisContext.Configuration.ShowDFARuntimeResults &&
                !this.AnalysisContext.Configuration.ShowRuntimeResults &&
                !this.AnalysisContext.Configuration.ShowROARuntimeResults)
            {
                Profiler.StartMeasuringExecutionTime();
            }

            foreach (var machine in this.AnalysisContext.Machines)
            {
                this.SummarizeStateMachine(machine);
            }

            // Stops profiling the summarization.
            if (this.AnalysisContext.Configuration.ShowDFARuntimeResults &&
                !this.AnalysisContext.Configuration.ShowRuntimeResults &&
                !this.AnalysisContext.Configuration.ShowROARuntimeResults)
            {
                Profiler.StopMeasuringExecutionTime();
            }

            if (this.AnalysisContext.Configuration.ShowSummarizationInformation)
            {
                this.PrintGivesUpResults();
            }
        }

        /// <summary>
        /// Prints the results of the analysis.
        /// </summary>
        public void PrintGivesUpResults()
        {
            IO.PrintLine("... Gives-up ownership summaries");
            if (this.AnalysisContext.Summaries.Any(val
                => (val.Value as PSharpMethodSummary).GivesUpSet.Count > 0))
            {
                foreach (var kvp in this.AnalysisContext.Summaries)
                {
                    var method = kvp.Key;
                    var summary = kvp.Value as PSharpMethodSummary;

                    if (summary.GivesUpSet.Count == 0)
                    {
                        continue;
                    }

                    string methodName = this.AnalysisContext.GetFullMethodName(method);

                    IO.Print("..... '{0}' gives up parameters:", methodName);
                    foreach (var index in summary.GivesUpSet)
                    {
                        IO.Print(" '{0}'", index);
                    }

                    IO.PrintLine("");
                }
            }
            else
            {
                IO.PrintLine("..... None");
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        private GivesUpOwnershipAnalysisPass(PSharpAnalysisContext context)
            : base(context)
        {

        }

        /// <summary>
        /// Analyses all eligible methods of the given state-machine to compute the
        /// method summaries. This process repeats until it reaches a fix-point.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void SummarizeStateMachine(StateMachine machine)
        {
            int fixPoint = 0;
            
            foreach (var method in machine.Declaration.ChildNodes().OfType<MethodDeclarationSyntax>())
            {
                if (this.AnalysisContext.Summaries.ContainsKey(method) ||
                    method.Modifiers.Any(SyntaxKind.AbstractKeyword))
                {
                    continue;
                }

                this.SummarizeMethod(method, machine);
                if (!this.AnalysisContext.Summaries.ContainsKey(method))
                {
                    fixPoint++;
                }
            }

            if (fixPoint > 0)
            {
                // If fix-point has not been reached, repeat.
                this.SummarizeStateMachine(machine);
            }
        }

        /// <summary>
        /// Computes the summary for the given method.
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="machine">Machine</param>
        private void SummarizeMethod(MethodDeclarationSyntax method, StateMachine machine)
        {
            foreach (var call in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var model = this.AnalysisContext.Compilation.GetSemanticModel(call.SyntaxTree);
                var callSymbol = model.GetSymbolInfo(call).Symbol;
                if (callSymbol == null)
                {
                    continue;
                }

                var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol,
                    this.AnalysisContext.Solution).Result;
                if (definition == null)
                {
                    continue;
                }

                var callee = Querying.GetCalleeOfInvocation(call);
                var calleeMethod = definition.DeclaringSyntaxReferences.First().GetSyntax()
                    as BaseMethodDeclarationSyntax;

                if (machine.Declaration.ChildNodes().OfType<BaseMethodDeclarationSyntax>().
                    Contains(calleeMethod) &&
                    !this.AnalysisContext.Summaries.ContainsKey(calleeMethod) &&
                    !calleeMethod.Modifiers.Any(SyntaxKind.AbstractKeyword))
                {
                    return;
                }
            }
            
            var summary = PSharpMethodSummary.Create(this.AnalysisContext, method, machine);

            if (this.AnalysisContext.Configuration.ShowControlFlowInformation)
            {
                summary.PrintControlFlowInformation();
            }

            if (this.AnalysisContext.Configuration.ShowDataFlowInformation)
            {
                summary.PrintDataFlowInformation();
            }

            // Visits all control-flow graph nodes that are giving up ownership,
            // to compute the gives-up ownership set of the method.
            foreach (var givesUpNode in summary.GivesUpOwnershipNodes)
            {
                this.ComputeGivesUpOwnershipSetInCFGNode(givesUpNode, summary);
            }
        }

        #endregion

        #region gives-up ownership analysis methods

        /// <summary>
        /// Computes the gives-up ownership set in the given control-flow graph node.
        /// </summary>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean</returns>
        private void ComputeGivesUpOwnershipSetInCFGNode(PSharpCFGNode cfgNode, PSharpMethodSummary summary)
        {
            var localDecl = cfgNode.SyntaxNodes.First() as LocalDeclarationStatementSyntax;
            var expr = cfgNode.SyntaxNodes.First() as ExpressionStatementSyntax;

            InvocationExpressionSyntax invocation = null;
            if (localDecl != null)
            {
                invocation = localDecl.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().First();
            }
            else if (expr != null)
            {
                invocation = expr.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().First();
            }

            if (invocation == null || !(invocation.Expression is MemberAccessExpressionSyntax ||
                invocation.Expression is IdentifierNameSyntax))
            {
                return;
            }

            var model = this.AnalysisContext.Compilation.GetSemanticModel(invocation.SyntaxTree);
            ISymbol symbol = model.GetSymbolInfo(invocation).Symbol;
            string methodName = symbol.ContainingNamespace.ToString() + "." + symbol.Name;

            if (methodName.Equals("Microsoft.PSharp.Send"))
            {
                this.ComputeGivesUpOwnershipSetInSendCFGNode(invocation, cfgNode, summary);
            }
            else if (methodName.Equals("Microsoft.PSharp.CreateMachine"))
            {
                this.ComputeGivesUpOwnershipSetInCreateMachineCFGNode(invocation, cfgNode, summary);
            }
            else
            {
                this.ComputeGivesUpOwnershipSetInGenericCFGNode(invocation, cfgNode, summary);
            }
        }

        /// <summary>
        /// Computes the gives-up ownership set in the given control-flow graph node.
        /// </summary>
        /// <param name="send">InvocationExpressionSyntax</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean</returns>
        private void ComputeGivesUpOwnershipSetInSendCFGNode(InvocationExpressionSyntax send,
            PSharpCFGNode cfgNode, PSharpMethodSummary summary)
        {
            var expr = send.ArgumentList.Arguments[1].Expression;
            if (expr is ObjectCreationExpressionSyntax)
            {
                var objCreation = expr as ObjectCreationExpressionSyntax;
                foreach (var arg in objCreation.ArgumentList.Arguments)
                {
                    this.ComputeGivesUpOwnershipSetInArgument(arg.Expression, cfgNode, summary);
                }
            }
            else if (expr is BinaryExpressionSyntax && expr.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = expr as BinaryExpressionSyntax;
                if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                {
                    this.ComputeGivesUpOwnershipSetInArgument(binExpr.Left, cfgNode, summary);
                }
                else if (binExpr.Left is InvocationExpressionSyntax)
                {
                    var invocation = binExpr.Left as InvocationExpressionSyntax;
                    for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                    {
                        this.ComputeGivesUpOwnershipSetInArgument(invocation.ArgumentList.
                            Arguments[i].Expression, cfgNode, summary);
                    }
                }
            }
            else if (expr is IdentifierNameSyntax || expr is MemberAccessExpressionSyntax)
            {
                this.ComputeGivesUpOwnershipSetInArgument(expr, cfgNode, summary);
            }
        }

        /// <summary>
        /// Computes the gives-up ownership set in the given control-flow graph node.
        /// </summary>
        /// <param name="create">InvocationExpressionSyntax</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean</returns>
        private void ComputeGivesUpOwnershipSetInCreateMachineCFGNode(InvocationExpressionSyntax create,
            PSharpCFGNode cfgNode, PSharpMethodSummary summary)
        {
            if (create.ArgumentList.Arguments.Count != 2)
            {
                return;
            }

            var expr = create.ArgumentList.Arguments[1].Expression;
            if (expr is ObjectCreationExpressionSyntax)
            {
                var objCreation = expr as ObjectCreationExpressionSyntax;
                foreach (var arg in objCreation.ArgumentList.Arguments)
                {
                    this.ComputeGivesUpOwnershipSetInArgument(arg.Expression, cfgNode, summary);
                }
            }
            else if (expr is BinaryExpressionSyntax && expr.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = expr as BinaryExpressionSyntax;
                if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                {
                    this.ComputeGivesUpOwnershipSetInArgument(binExpr.Left, cfgNode, summary);
                }
                else if (binExpr.Left is InvocationExpressionSyntax)
                {
                    var invocation = binExpr.Left as InvocationExpressionSyntax;
                    for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                    {
                        this.ComputeGivesUpOwnershipSetInArgument(invocation.ArgumentList.
                            Arguments[i].Expression, cfgNode, summary);
                    }
                }
            }
            else if (expr is IdentifierNameSyntax || expr is MemberAccessExpressionSyntax)
            {
                this.ComputeGivesUpOwnershipSetInArgument(expr, cfgNode, summary);
            }
        }

        /// <summary>
        /// Computes the gives-up ownership set in the given control-flow graph node.
        /// </summary>
        /// <param name="call">InvocationExpressionSyntax</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean</returns>
        private void ComputeGivesUpOwnershipSetInGenericCFGNode(InvocationExpressionSyntax call,
            PSharpCFGNode cfgNode, PSharpMethodSummary summary)
        {
            if (call.ArgumentList.Arguments.Count == 0)
            {
                return;
            }

            var model = this.AnalysisContext.Compilation.GetSemanticModel(call.SyntaxTree);
            var callSymbol = model.GetSymbolInfo(call).Symbol;
            var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol,
                this.AnalysisContext.Solution).Result;
            var calleeMethod = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as BaseMethodDeclarationSyntax;
            var calleeSummary = PSharpMethodSummary.Create(this.AnalysisContext, calleeMethod);

            foreach (int idx in calleeSummary.GivesUpSet)
            {
                var expr = call.ArgumentList.Arguments[idx].Expression;
                if (expr is ObjectCreationExpressionSyntax)
                {
                    var objCreation = expr as ObjectCreationExpressionSyntax;
                    foreach (var arg in objCreation.ArgumentList.Arguments)
                    {
                        this.ComputeGivesUpOwnershipSetInArgument(arg.Expression, cfgNode, summary);
                    }
                }
                else if (expr is BinaryExpressionSyntax && expr.IsKind(SyntaxKind.AsExpression))
                {
                    var binExpr = expr as BinaryExpressionSyntax;
                    if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                    {
                        this.ComputeGivesUpOwnershipSetInArgument(binExpr.Left, cfgNode, summary);
                    }
                    else if (binExpr.Left is InvocationExpressionSyntax)
                    {
                        var invocation = binExpr.Left as InvocationExpressionSyntax;
                        for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                        {
                            this.ComputeGivesUpOwnershipSetInArgument(invocation.ArgumentList.
                                Arguments[i].Expression, cfgNode, summary);
                        }
                    }
                }
                else if (expr is IdentifierNameSyntax || expr is MemberAccessExpressionSyntax)
                {
                    this.ComputeGivesUpOwnershipSetInArgument(call.ArgumentList.Arguments[idx].
                        Expression, cfgNode, summary);
                }
            }
        }

        /// <summary>
        /// Computes the gives-up ownership set in the given argument.
        /// </summary>
        /// <param name="arg">Argument</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="summary">MethodSummary</param>
        private void ComputeGivesUpOwnershipSetInArgument(ExpressionSyntax arg,
            PSharpCFGNode cfgNode, PSharpMethodSummary summary)
        {
            var model = this.AnalysisContext.Compilation.GetSemanticModel(arg.SyntaxTree);
            if (arg is IdentifierNameSyntax || arg is MemberAccessExpressionSyntax)
            {
                for (int idx = 0; idx < summary.Method.ParameterList.Parameters.Count; idx++)
                {
                    TypeInfo typeInfo = model.GetTypeInfo(summary.Method.ParameterList.Parameters[idx].Type);
                    if (this.AnalysisContext.IsTypePassedByValueOrImmutable(typeInfo.Type))
                    {
                        continue;
                    }

                    var paramSymbol = model.GetDeclaredSymbol(summary.Method.ParameterList.Parameters[idx]);
                    if (DataFlowQuerying.FlowsFromTarget(arg, paramSymbol, summary.EntryNode.SyntaxNodes.First(),
                        summary.EntryNode, cfgNode.SyntaxNodes.First(), cfgNode, model, this.AnalysisContext))
                    {
                        summary.GivesUpSet.Add(idx);
                    }
                }
            }
            else if (arg is ObjectCreationExpressionSyntax)
            {
                var payload = arg as ObjectCreationExpressionSyntax;
                foreach (var item in payload.ArgumentList.Arguments)
                {
                    this.ComputeGivesUpOwnershipSetInArgument(item.Expression, cfgNode, summary);
                }
            }
        }

        #endregion
    }
}
