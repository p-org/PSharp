//-----------------------------------------------------------------------
// <copyright file="MethodSummaryAnalysis.cs">
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
using Microsoft.CodeAnalysis.FindSymbols;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis computes the method summaries for every method
    /// in each machine of a P# program.
    /// </summary>
    public sealed class MethodSummaryAnalysis
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        private AnalysisContext AnalysisContext;

        #endregion

        #region public API

        /// <summary>
        /// Creates a new method summary analysis pass.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <returns>MethodSummaryAnalysis</returns>
        public static MethodSummaryAnalysis Create(AnalysisContext context)
        {
            return new MethodSummaryAnalysis(context);
        }

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        public void Run()
        {
            // Starts profiling the data flow analysis.
            if (this.AnalysisContext.Configuration.ShowDFARuntimeResults &&
                !this.AnalysisContext.Configuration.ShowRuntimeResults &&
                !this.AnalysisContext.Configuration.ShowROARuntimeResults)
            {
                Profiler.StartMeasuringExecutionTime();
            }

            foreach (var machine in this.AnalysisContext.Machines)
            {
                this.AnalyseMethodsInMachine(machine);
            }

            // Stops profiling the data flow analysis.
            if (this.AnalysisContext.Configuration.ShowDFARuntimeResults &&
                !this.AnalysisContext.Configuration.ShowRuntimeResults &&
                !this.AnalysisContext.Configuration.ShowROARuntimeResults)
            {
                Profiler.StopMeasuringExecutionTime();
            }
        }

        /// <summary>
        /// Prints the results of the analysis.
        /// </summary>
        public void PrintGivesUpResults()
        {
            IO.PrintLine("\n > Printing gives up ownership information:\n");
            foreach (var summary in this.AnalysisContext.Summaries)
            {
                if (summary.Value.GivesUpSet.Count == 0)
                {
                    continue;
                }

                string methodName = null;
                if (summary.Key is MethodDeclarationSyntax)
                {
                    methodName = (summary.Key as MethodDeclarationSyntax).Identifier.ValueText;
                }
                else if (summary.Key is ConstructorDeclarationSyntax)
                {
                    methodName = (summary.Key as ConstructorDeclarationSyntax).Identifier.ValueText;
                }

                Console.Write("   > Method '{0}' gives_up set:", methodName);
                foreach (var index in summary.Value.GivesUpSet)
                {
                    Console.Write(" '{0}'", index);
                }

                Console.Write("\n");
            }

            Console.Write("\n");
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        private MethodSummaryAnalysis(AnalysisContext context)
        {
            this.AnalysisContext = context;
        }

        /// <summary>
        /// Analyses all the eligible methods of the given machine to compute each
        /// method summary. This process continues until it reaches a fix point.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void AnalyseMethodsInMachine(ClassDeclarationSyntax machine)
        {
            int fixPoint = 0;

            foreach (var nestedClass in machine.ChildNodes().OfType<ClassDeclarationSyntax>())
            {
                foreach (var method in nestedClass.ChildNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (!this.AnalysisContext.ShouldAnalyseMethod(method) ||
                        this.AnalysisContext.Summaries.ContainsKey(method))
                    {
                        continue;
                    }

                    this.ComputeSummaryForMethod(method, machine, nestedClass);
                    if (!this.AnalysisContext.Summaries.ContainsKey(method))
                    {
                        fixPoint++;
                    }
                }
            }

            foreach (var method in machine.ChildNodes().OfType<MethodDeclarationSyntax>())
            {
                if (!this.AnalysisContext.ShouldAnalyseMethod(method) ||
                    this.AnalysisContext.Summaries.ContainsKey(method))
                {
                    continue;
                }

                this.ComputeSummaryForMethod(method, machine, null);
                if (!this.AnalysisContext.Summaries.ContainsKey(method))
                {
                    fixPoint++;
                }
            }

            if (fixPoint > 0)
            {
                this.AnalyseMethodsInMachine(machine);
            }
        }

        /// <summary>
        /// Computes the summary for the given method.
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">State</param>
        private void ComputeSummaryForMethod(MethodDeclarationSyntax method,
            ClassDeclarationSyntax machine, ClassDeclarationSyntax state)
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

                var callee = this.AnalysisContext.GetCallee(call);
                var calleeMethod = definition.DeclaringSyntaxReferences.First().GetSyntax()
                    as BaseMethodDeclarationSyntax;

                if (this.AnalysisContext.IsSourceOfGivingUpOwnership(call, model, callee) ||
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

            MethodSummary summary = MethodSummary.Factory.Summarize(this.AnalysisContext, method, machine, state);
            foreach (var givesUpNode in summary.GivesUpNodes)
            {
                this.TryComputeGivesUpSetForSendControlFlowGraphNode(givesUpNode, summary);
                this.TryComputeGivesUpSetForCreateControlFlowGraphNode(givesUpNode, summary);
                this.TryComputeGivesUpSetForGenericControlFlowGraphNode(givesUpNode, summary);
            }
        }

        #endregion

        #region give up ownership source analysis methods

        /// <summary>
        /// Tries to compute the 'gives_up' set of indexes for the given control flow graph node.
        /// If the node does not contain a 'Send' operation, then it returns false.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean value</returns>
        private bool TryComputeGivesUpSetForSendControlFlowGraphNode(ControlFlowGraphNode cfgNode,
            MethodSummary summary)
        {
            var sendExpr = cfgNode.SyntaxNodes.First() as ExpressionStatementSyntax;
            if (sendExpr == null)
            {
                return false;
            }

            var send = sendExpr.Expression as InvocationExpressionSyntax;
            if (send == null || !((send.Expression is MemberAccessExpressionSyntax) ||
                (send.Expression is IdentifierNameSyntax)))
            {
                return false;
            }

            if (((send.Expression is MemberAccessExpressionSyntax) &&
                !(send.Expression as MemberAccessExpressionSyntax).
                Name.Identifier.ValueText.Equals("Send")) ||
                ((send.Expression is IdentifierNameSyntax) &&
                !(send.Expression as IdentifierNameSyntax).
                Identifier.ValueText.Equals("Send")))
            {
                return false;
            }

            if (send.ArgumentList.Arguments[1].Expression is ObjectCreationExpressionSyntax)
            {
                var objCreation = send.ArgumentList.Arguments[1].Expression
                    as ObjectCreationExpressionSyntax;
                foreach (var arg in objCreation.ArgumentList.Arguments)
                {
                    this.ComputeGivesUpSetForArgument(arg.Expression, cfgNode, summary);
                }
            }
            else if (send.ArgumentList.Arguments[1].Expression is BinaryExpressionSyntax &&
                send.ArgumentList.Arguments[1].Expression.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = send.ArgumentList.Arguments[1].Expression
                    as BinaryExpressionSyntax;
                if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                {
                    this.ComputeGivesUpSetForArgument(binExpr.Left, cfgNode, summary);
                }
                else if (binExpr.Left is InvocationExpressionSyntax)
                {
                    var invocation = binExpr.Left as InvocationExpressionSyntax;
                    for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                    {
                        this.ComputeGivesUpSetForArgument(invocation.ArgumentList.
                            Arguments[i].Expression, cfgNode, summary);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Tries to compute the 'gives_up' set of indexes for the given control flow graph node.
        /// If the node does not contain a 'Create' operation, then it returns false.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean value</returns>
        private bool TryComputeGivesUpSetForCreateControlFlowGraphNode(ControlFlowGraphNode cfgNode,
            MethodSummary summary)
        {
            var createExpr = cfgNode.SyntaxNodes.First() as ExpressionStatementSyntax;
            if (createExpr == null)
            {
                return false;
            }

            var create = createExpr.Expression as InvocationExpressionSyntax;
            if (create == null || !((create.Expression is MemberAccessExpressionSyntax) ||
                (create.Expression is IdentifierNameSyntax)))
            {
                return false;
            }

            if (((create.Expression is MemberAccessExpressionSyntax) &&
                !(create.Expression as MemberAccessExpressionSyntax).
                Name.Identifier.ValueText.Equals("CreateMachine")) ||
                ((create.Expression is IdentifierNameSyntax) &&
                !(create.Expression as IdentifierNameSyntax).
                Identifier.ValueText.Equals("CreateMachine")))
            {
                return false;
            }

            if (create.ArgumentList.Arguments.Count == 0)
            {
                return true;
            }

            if (create.ArgumentList.Arguments[0].Expression is ObjectCreationExpressionSyntax)
            {
                var objCreation = create.ArgumentList.Arguments[0].Expression
                    as ObjectCreationExpressionSyntax;
                foreach (var arg in objCreation.ArgumentList.Arguments)
                {
                    this.ComputeGivesUpSetForArgument(arg.Expression, cfgNode, summary);
                }
            }
            else if (create.ArgumentList.Arguments[0].Expression is BinaryExpressionSyntax &&
                create.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = create.ArgumentList.Arguments[0].Expression
                    as BinaryExpressionSyntax;
                if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                {
                    this.ComputeGivesUpSetForArgument(binExpr.Left, cfgNode, summary);
                }
                else if (binExpr.Left is InvocationExpressionSyntax)
                {
                    var invocation = binExpr.Left as InvocationExpressionSyntax;
                    for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                    {
                        this.ComputeGivesUpSetForArgument(invocation.ArgumentList.
                            Arguments[i].Expression, cfgNode, summary);
                    }
                }
            }
            else if ((create.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax) ||
                (create.ArgumentList.Arguments[0].Expression is MemberAccessExpressionSyntax))
            {
                this.ComputeGivesUpSetForArgument(create.ArgumentList.
                    Arguments[0].Expression, cfgNode, summary);
            }

            return true;
        }

        /// <summary>
        /// Tries to compute the 'gives_up' set of indexes for the given control flow graph node.
        /// If the node does not contain a generic 'gives_up' operation, then it returns false.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean value</returns>
        private bool TryComputeGivesUpSetForGenericControlFlowGraphNode(ControlFlowGraphNode cfgNode,
            MethodSummary summary)
        {
            var callLocalDecl = cfgNode.SyntaxNodes.First() as LocalDeclarationStatementSyntax;
            var callExpr = cfgNode.SyntaxNodes.First() as ExpressionStatementSyntax;

            InvocationExpressionSyntax call = null;
            if (callLocalDecl != null)
            {
                call = callLocalDecl.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().First();
            }
            else if (callExpr != null)
            {
                call = callExpr.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().First();
            }
            else if (call == null || !((call.Expression is MemberAccessExpressionSyntax) ||
                (call.Expression is IdentifierNameSyntax)))
            {
                return false;
            }

            var model = this.AnalysisContext.Compilation.GetSemanticModel(call.SyntaxTree);

            if (call.Expression is MemberAccessExpressionSyntax)
            {
                var callStmt = call.Expression as MemberAccessExpressionSyntax;
                if (callStmt.Name.Identifier.ValueText.Equals("Send") ||
                    callStmt.Name.Identifier.ValueText.Equals("CreateMachine"))
                {
                    return false;
                }
            }
            else if (call.Expression is IdentifierNameSyntax)
            {
                var callStmt = call.Expression as IdentifierNameSyntax;
                if (callStmt.Identifier.ValueText.Equals("Send") ||
                    callStmt.Identifier.ValueText.Equals("CreateMachine"))
                {
                    return false;
                }
            }

            if (call.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            var callSymbol = model.GetSymbolInfo(call).Symbol;
            var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol,
                this.AnalysisContext.Solution).Result;
            var calleeMethod = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as BaseMethodDeclarationSyntax;
            var calleeSummary = MethodSummary.Factory.Summarize(this.AnalysisContext, calleeMethod);

            foreach (int idx in calleeSummary.GivesUpSet)
            {
                if (call.ArgumentList.Arguments[idx].Expression is ObjectCreationExpressionSyntax)
                {
                    var objCreation = call.ArgumentList.Arguments[idx].Expression
                        as ObjectCreationExpressionSyntax;
                    foreach (var arg in objCreation.ArgumentList.Arguments)
                    {
                        this.ComputeGivesUpSetForArgument(arg.Expression, cfgNode, summary);
                    }
                }
                else if (call.ArgumentList.Arguments[idx].Expression is BinaryExpressionSyntax &&
                    call.ArgumentList.Arguments[idx].Expression.IsKind(SyntaxKind.AsExpression))
                {
                    var binExpr = call.ArgumentList.Arguments[idx].Expression
                        as BinaryExpressionSyntax;
                    if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                    {
                        this.ComputeGivesUpSetForArgument(binExpr.Left, cfgNode, summary);
                    }
                    else if (binExpr.Left is InvocationExpressionSyntax)
                    {
                        var invocation = binExpr.Left as InvocationExpressionSyntax;
                        for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                        {
                            this.ComputeGivesUpSetForArgument(invocation.ArgumentList.
                                Arguments[i].Expression, cfgNode, summary);
                        }
                    }
                }
                else if ((call.ArgumentList.Arguments[idx].Expression is IdentifierNameSyntax) ||
                    (call.ArgumentList.Arguments[idx].Expression is MemberAccessExpressionSyntax))
                {
                    this.ComputeGivesUpSetForArgument(call.ArgumentList.Arguments[idx].
                        Expression, cfgNode, summary);
                }
            }

            return true;
        }

        /// <summary>
        /// Computes the 'gives_up' set for the given argument.
        /// </summary>
        /// <param name="arg">Argument</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="summary">MethodSummary</param>
        private void ComputeGivesUpSetForArgument(ExpressionSyntax arg, ControlFlowGraphNode cfgNode,
            MethodSummary summary)
        {
            var model = this.AnalysisContext.Compilation.GetSemanticModel(arg.SyntaxTree);
            if (arg is IdentifierNameSyntax || arg is MemberAccessExpressionSyntax)
            {
                for (int idx = 0; idx < summary.Method.ParameterList.Parameters.Count; idx++)
                {
                    if (this.AnalysisContext.IsTypeAllowedToBeSend(summary.Method.ParameterList.
                        Parameters[idx].Type, model))
                    {
                        continue;
                    }

                    var paramSymbol = model.GetDeclaredSymbol(summary.Method.ParameterList.Parameters[idx]);
                    if (DataFlowAnalysis.FlowsFromTarget(arg, paramSymbol, summary.Node.SyntaxNodes.First(),
                        summary.Node, cfgNode.SyntaxNodes.First(), cfgNode, model, this.AnalysisContext))
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
                    this.ComputeGivesUpSetForArgument(item.Expression, cfgNode, summary);
                }
            }
        }

        #endregion
    }
}
