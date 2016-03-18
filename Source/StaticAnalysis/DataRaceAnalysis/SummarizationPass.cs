//-----------------------------------------------------------------------
// <copyright file="SummarizationPass.cs">
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
using Microsoft.CodeAnalysis.FindSymbols;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis pass computes the summaries
    /// for each machine of a P# program.
    /// </summary>
    public sealed class SummarizationPass : AnalysisPass
    {
        #region public API

        /// <summary>
        /// Creates a new method summary analysis pass.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <returns>SummarizationPass</returns>
        public static SummarizationPass Create(PSharpAnalysisContext context)
        {
            return new SummarizationPass(context);
        }

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        /// <returns>SummarizationPass</returns>
        public SummarizationPass Run()
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

            return this;
        }

        /// <summary>
        /// Prints the results of the analysis.
        /// </summary>
        public void PrintGivesUpResults()
        {
            IO.PrintLine("... Gives-up ownership summaries");
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

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        private SummarizationPass(PSharpAnalysisContext context)
            : base(context)
        {

        }

        /// <summary>
        /// Analyses all eligible methods of the given state-machine to compute the
        /// method summaries. This process repeats until it reaches a fix-point.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void SummarizeStateMachine(ClassDeclarationSyntax machine)
        {
            int fixPoint = 0;
            
            foreach (var method in machine.ChildNodes().OfType<MethodDeclarationSyntax>())
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
        private void SummarizeMethod(MethodDeclarationSyntax method, ClassDeclarationSyntax machine)
        {
            List<InvocationExpressionSyntax> givesUpSources = new List<InvocationExpressionSyntax>();
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

                if (Querying.IsEventSenderInvocation(call, callee, model) ||
                    this.AnalysisContext.Summaries.ContainsKey(calleeMethod))
                {
                    givesUpSources.Add(call);
                }
                else if (machine.ChildNodes().OfType<BaseMethodDeclarationSyntax>().Contains(calleeMethod) &&
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
        private void ComputeGivesUpOwnershipSetInCFGNode(CFGNode cfgNode, PSharpMethodSummary summary)
        {
            var localDecl = cfgNode.SyntaxNodes.First() as LocalDeclarationStatementSyntax;
            var expr = cfgNode.SyntaxNodes.First() as ExpressionStatementSyntax;

            InvocationExpressionSyntax call = null;
            if (localDecl != null)
            {
                call = localDecl.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().First();
            }
            else if (expr != null)
            {
                call = expr.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().First();
            }
            else if (call == null || !((call.Expression is MemberAccessExpressionSyntax) ||
                (call.Expression is IdentifierNameSyntax)))
            {
                return;
            }

            var model = this.AnalysisContext.Compilation.GetSemanticModel(call.SyntaxTree);
            var callSymbol = model.GetSymbolInfo(call).Symbol;

            if (callSymbol.ContainingNamespace.ToString().Equals("Microsoft.PSharp") &&
                callSymbol.Name.Equals("Send"))
            {
                this.ComputeGivesUpOwnershipSetInSendCFGNode(call, cfgNode, summary);
            }
            else if (callSymbol.ContainingNamespace.ToString().Equals("Microsoft.PSharp") &&
                callSymbol.Name.Equals("CreateMachine"))
            {
                this.ComputeGivesUpOwnershipSetInCreateMachineCFGNode(call, cfgNode, summary);
            }
            else
            {
                this.ComputeGivesUpOwnershipSetInGenericCFGNode(call, cfgNode, summary);
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
            CFGNode cfgNode, PSharpMethodSummary summary)
        {
            if (send.ArgumentList.Arguments[1].Expression is ObjectCreationExpressionSyntax)
            {
                var objCreation = send.ArgumentList.Arguments[1].Expression
                    as ObjectCreationExpressionSyntax;
                foreach (var arg in objCreation.ArgumentList.Arguments)
                {
                    this.ComputeGivesUpOwnershipSetInArgument(arg.Expression, cfgNode, summary);
                }
            }
            else if (send.ArgumentList.Arguments[1].Expression is BinaryExpressionSyntax &&
                send.ArgumentList.Arguments[1].Expression.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = send.ArgumentList.Arguments[1].Expression
                    as BinaryExpressionSyntax;
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
        }

        /// <summary>
        /// Computes the gives-up ownership set in the given control-flow graph node.
        /// </summary>
        /// <param name="create">InvocationExpressionSyntax</param>
        /// <param name="cfgNode">CFGNode</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean</returns>
        private void ComputeGivesUpOwnershipSetInCreateMachineCFGNode(InvocationExpressionSyntax create,
            CFGNode cfgNode, PSharpMethodSummary summary)
        {
            if (create.ArgumentList.Arguments.Count == 0)
            {
                return;
            }

            if (create.ArgumentList.Arguments[0].Expression is ObjectCreationExpressionSyntax)
            {
                var objCreation = create.ArgumentList.Arguments[0].Expression
                    as ObjectCreationExpressionSyntax;
                foreach (var arg in objCreation.ArgumentList.Arguments)
                {
                    this.ComputeGivesUpOwnershipSetInArgument(arg.Expression, cfgNode, summary);
                }
            }
            else if (create.ArgumentList.Arguments[0].Expression is BinaryExpressionSyntax &&
                create.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = create.ArgumentList.Arguments[0].Expression
                    as BinaryExpressionSyntax;
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
            else if ((create.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax) ||
                (create.ArgumentList.Arguments[0].Expression is MemberAccessExpressionSyntax))
            {
                this.ComputeGivesUpOwnershipSetInArgument(create.ArgumentList.
                    Arguments[0].Expression, cfgNode, summary);
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
            CFGNode cfgNode, PSharpMethodSummary summary)
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
                if (call.ArgumentList.Arguments[idx].Expression is ObjectCreationExpressionSyntax)
                {
                    var objCreation = call.ArgumentList.Arguments[idx].Expression
                        as ObjectCreationExpressionSyntax;
                    foreach (var arg in objCreation.ArgumentList.Arguments)
                    {
                        this.ComputeGivesUpOwnershipSetInArgument(arg.Expression, cfgNode, summary);
                    }
                }
                else if (call.ArgumentList.Arguments[idx].Expression is BinaryExpressionSyntax &&
                    call.ArgumentList.Arguments[idx].Expression.IsKind(SyntaxKind.AsExpression))
                {
                    var binExpr = call.ArgumentList.Arguments[idx].Expression
                        as BinaryExpressionSyntax;
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
                else if ((call.ArgumentList.Arguments[idx].Expression is IdentifierNameSyntax) ||
                    (call.ArgumentList.Arguments[idx].Expression is MemberAccessExpressionSyntax))
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
        private void ComputeGivesUpOwnershipSetInArgument(ExpressionSyntax arg, CFGNode cfgNode,
            PSharpMethodSummary summary)
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
