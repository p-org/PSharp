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

using Microsoft.PSharp.Tooling;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis computes the method summaries for every method
    /// in each machine of a P# program.
    /// </summary>
    public static class MethodSummaryAnalysis
    {
        #region public API

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        public static void Run()
        {
            // Starts profiling the data flow analysis.
            if (Configuration.ShowDFARuntimeResults &&
                !Configuration.ShowRuntimeResults &&
                !Configuration.ShowROARuntimeResults)
            {
                Profiler.StartMeasuringExecutionTime();
            }

            foreach (var machine in AnalysisContext.Machines)
            {
                MethodSummaryAnalysis.AnalyseMethodsInMachine(machine);
            }

            // Stops profiling the data flow analysis.
            if (Configuration.ShowDFARuntimeResults &&
                !Configuration.ShowRuntimeResults &&
                !Configuration.ShowROARuntimeResults)
            {
                Profiler.StopMeasuringExecutionTime();
            }
        }

        /// <summary>
        /// Prints the results of the analysis.
        /// </summary>
        public static void PrintGivesUpResults()
        {
            Console.WriteLine("\n > Printing gives up ownership information:\n");
            foreach (var summary in AnalysisContext.Summaries)
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
        /// Analyses all the eligible methods of the given machine to compute each
        /// method summary. This process continues until it reaches a fix point.
        /// </summary>
        /// <param name="machine">Machine</param>
        private static void AnalyseMethodsInMachine(ClassDeclarationSyntax machine)
        {
            int fixPoint = 0;

            foreach (var nestedClass in machine.ChildNodes().OfType<ClassDeclarationSyntax>())
            {
                foreach (var method in nestedClass.ChildNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (!Utilities.ShouldAnalyseMethod(method) ||
                        AnalysisContext.Summaries.ContainsKey(method))
                    {
                        continue;
                    }

                    MethodSummaryAnalysis.ComputeSummaryForMethod(method, machine, nestedClass);
                    if (!AnalysisContext.Summaries.ContainsKey(method))
                    {
                        fixPoint++;
                    }
                }
            }

            foreach (var method in machine.ChildNodes().OfType<MethodDeclarationSyntax>())
            {
                if (!Utilities.ShouldAnalyseMethod(method) ||
                    AnalysisContext.Summaries.ContainsKey(method))
                {
                    continue;
                }

                MethodSummaryAnalysis.ComputeSummaryForMethod(method, machine, null);
                if (!AnalysisContext.Summaries.ContainsKey(method))
                {
                    fixPoint++;
                }
            }

            if (fixPoint > 0)
            {
                MethodSummaryAnalysis.AnalyseMethodsInMachine(machine);
            }
        }

        /// <summary>
        /// Computes the summary for the given method.
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">State</param>
        private static void ComputeSummaryForMethod(MethodDeclarationSyntax method,
            ClassDeclarationSyntax machine, ClassDeclarationSyntax state)
        {
            List<InvocationExpressionSyntax> givesUpSources = new List<InvocationExpressionSyntax>();
            foreach (var call in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var model = AnalysisContext.Compilation.GetSemanticModel(call.SyntaxTree);

                var callSymbol = model.GetSymbolInfo(call).Symbol;
                if (callSymbol == null)
                {
                    continue;
                }

                var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol, ProgramInfo.Solution).Result;
                if (definition == null)
                {
                    continue;
                }

                var callee = Utilities.GetCallee(call);
                var calleeMethod = definition.DeclaringSyntaxReferences.First().GetSyntax()
                    as BaseMethodDeclarationSyntax;

                if (Utilities.IsSourceOfGivingUpOwnership(call, model, callee) ||
                    AnalysisContext.Summaries.ContainsKey(calleeMethod))
                {
                    givesUpSources.Add(call);
                }
                else if (machine.ChildNodes().OfType<BaseMethodDeclarationSyntax>().Contains(calleeMethod) &&
                    !AnalysisContext.Summaries.ContainsKey(calleeMethod) &&
                    !calleeMethod.Modifiers.Any(SyntaxKind.AbstractKeyword))
                {
                    return;
                }
            }

            MethodSummary summary = MethodSummary.Factory.Summarize(method, machine, state);
            foreach (var givesUpNode in summary.GivesUpNodes)
            {
                MethodSummaryAnalysis.TryComputeGivesUpSetForSendControlFlowGraphNode(
                    givesUpNode, summary);
                MethodSummaryAnalysis.TryComputeGivesUpSetForInvokeControlFlowGraphNode(
                    givesUpNode, summary);
                MethodSummaryAnalysis.TryComputeGivesUpSetForFactoryControlFlowGraphNode(
                    givesUpNode, summary);
                MethodSummaryAnalysis.TryComputeGivesUpSetForGenericControlFlowGraphNode(
                    givesUpNode, summary);
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
        private static bool TryComputeGivesUpSetForSendControlFlowGraphNode(ControlFlowGraphNode cfgNode,
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
                    MethodSummaryAnalysis.ComputeGivesUpSetForArgument(arg.Expression,
                        cfgNode, summary);
                }
            }
            else if (send.ArgumentList.Arguments[1].Expression is BinaryExpressionSyntax &&
                send.ArgumentList.Arguments[1].Expression.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = send.ArgumentList.Arguments[1].Expression
                    as BinaryExpressionSyntax;
                if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                {
                    MethodSummaryAnalysis.ComputeGivesUpSetForArgument(binExpr.Left,
                        cfgNode, summary);
                }
                else if (binExpr.Left is InvocationExpressionSyntax)
                {
                    var invocation = binExpr.Left as InvocationExpressionSyntax;
                    for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                    {
                        MethodSummaryAnalysis.ComputeGivesUpSetForArgument(invocation.ArgumentList.
                            Arguments[i].Expression, cfgNode, summary);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Tries to compute the 'gives_up' set of indexes for the given control flow graph node.
        /// If the node does not contain an 'Invoke' operation, then it returns false.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean value</returns>
        private static bool TryComputeGivesUpSetForInvokeControlFlowGraphNode(ControlFlowGraphNode cfgNode,
            MethodSummary summary)
        {
            var invokeExpr = cfgNode.SyntaxNodes.First() as ExpressionStatementSyntax;
            if (invokeExpr == null)
            {
                return false;
            }

            var invoke = invokeExpr.Expression as InvocationExpressionSyntax;
            if (invoke == null || !((invoke.Expression is MemberAccessExpressionSyntax) ||
                (invoke.Expression is IdentifierNameSyntax)))
            {
                return false;
            }

            if (((invoke.Expression is MemberAccessExpressionSyntax) &&
                !(invoke.Expression as MemberAccessExpressionSyntax).
                Name.Identifier.ValueText.Equals("Invoke")) ||
                ((invoke.Expression is IdentifierNameSyntax) &&
                !(invoke.Expression as IdentifierNameSyntax).
                Identifier.ValueText.Equals("Invoke")))
            {
                return false;
            }

            if (invoke.ArgumentList.Arguments[0].Expression is ObjectCreationExpressionSyntax)
            {
                var objCreation = invoke.ArgumentList.Arguments[0].Expression
                    as ObjectCreationExpressionSyntax;
                foreach (var arg in objCreation.ArgumentList.Arguments)
                {
                    MethodSummaryAnalysis.ComputeGivesUpSetForArgument(
                        arg.Expression, cfgNode, summary);
                }
            }
            else if (invoke.ArgumentList.Arguments[0].Expression is BinaryExpressionSyntax &&
                invoke.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = invoke.ArgumentList.Arguments[0].Expression
                    as BinaryExpressionSyntax;
                if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                {
                    MethodSummaryAnalysis.ComputeGivesUpSetForArgument(binExpr.Left,
                        cfgNode, summary);
                }
                else if (binExpr.Left is InvocationExpressionSyntax)
                {
                    var invocation = binExpr.Left as InvocationExpressionSyntax;
                    for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                    {
                        MethodSummaryAnalysis.ComputeGivesUpSetForArgument(invocation.ArgumentList.
                            Arguments[i].Expression, cfgNode, summary);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Tries to compute the 'gives_up' set of indexes for the given control flow graph node.
        /// If the node does not contain a 'Factory' operation, then it returns false.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean value</returns>
        private static bool TryComputeGivesUpSetForFactoryControlFlowGraphNode(ControlFlowGraphNode cfgNode,
            MethodSummary summary)
        {
            var factoryLocalDecl = cfgNode.SyntaxNodes.First() as LocalDeclarationStatementSyntax;
            var factoryExpr = cfgNode.SyntaxNodes.First() as ExpressionStatementSyntax;

            InvocationExpressionSyntax factory = null;
            if (factoryLocalDecl != null)
            {
                factory = factoryLocalDecl.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().First();
            }
            else if (factoryExpr != null)
            {
                factory = factoryExpr.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().First();
            }
            else if (factory == null || !((factory.Expression is MemberAccessExpressionSyntax) ||
                (factory.Expression is IdentifierNameSyntax)))
            {
                return false;
            }
            
            if (((factory.Expression is MemberAccessExpressionSyntax) &&
                !((factory.Expression as MemberAccessExpressionSyntax).
                Name.Identifier.ValueText.Equals("CreateMachine") ||
                (factory.Expression as MemberAccessExpressionSyntax).
                Name.Identifier.ValueText.Equals("CreateMonitor"))) ||
                ((factory.Expression is IdentifierNameSyntax) &&
                !((factory.Expression as IdentifierNameSyntax).
                Identifier.ValueText.Equals("CreateMachine") ||
                (factory.Expression as IdentifierNameSyntax).
                Identifier.ValueText.Equals("CreateMonitor"))))
            {
                return false;
            }

            if (factory.ArgumentList.Arguments.Count == 0)
            {
                return true;
            }

            if (factory.ArgumentList.Arguments[0].Expression is ObjectCreationExpressionSyntax)
            {
                var objCreation = factory.ArgumentList.Arguments[0].Expression
                    as ObjectCreationExpressionSyntax;
                foreach (var arg in objCreation.ArgumentList.Arguments)
                {
                    MethodSummaryAnalysis.ComputeGivesUpSetForArgument(
                        arg.Expression, cfgNode, summary);
                }
            }
            else if (factory.ArgumentList.Arguments[0].Expression is BinaryExpressionSyntax &&
                factory.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = factory.ArgumentList.Arguments[0].Expression
                    as BinaryExpressionSyntax;
                if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                {
                    MethodSummaryAnalysis.ComputeGivesUpSetForArgument(binExpr.Left,
                        cfgNode, summary);
                }
                else if (binExpr.Left is InvocationExpressionSyntax)
                {
                    var invocation = binExpr.Left as InvocationExpressionSyntax;
                    for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                    {
                        MethodSummaryAnalysis.ComputeGivesUpSetForArgument(invocation.ArgumentList.
                            Arguments[i].Expression, cfgNode, summary);
                    }
                }
            }
            else if ((factory.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax) ||
                (factory.ArgumentList.Arguments[0].Expression is MemberAccessExpressionSyntax))
            {
                MethodSummaryAnalysis.ComputeGivesUpSetForArgument(factory.ArgumentList.
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
        private static bool TryComputeGivesUpSetForGenericControlFlowGraphNode(ControlFlowGraphNode cfgNode,
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

            if (call.Expression is MemberAccessExpressionSyntax)
            {
                var callStmt = call.Expression as MemberAccessExpressionSyntax;
                if (callStmt.Name.Identifier.ValueText.Equals("Send") ||
                    callStmt.Name.Identifier.ValueText.Equals("Invoke") ||
                    callStmt.Name.Identifier.ValueText.Equals("CreateMachine") ||
                    callStmt.Name.Identifier.ValueText.Equals("CreateMonitor"))
                {
                    return false;
                }
            }
            else if (call.Expression is IdentifierNameSyntax)
            {
                var callStmt = call.Expression as IdentifierNameSyntax;
                if (callStmt.Identifier.ValueText.Equals("Send") ||
                    callStmt.Identifier.ValueText.Equals("Invoke") ||
                    callStmt.Identifier.ValueText.Equals("CreateMachine") ||
                    callStmt.Identifier.ValueText.Equals("CreateMonitor"))
                {
                    return false;
                }
            }

            if (call.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            var model = AnalysisContext.Compilation.GetSemanticModel(call.SyntaxTree);
            var callSymbol = model.GetSymbolInfo(call).Symbol;
            var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol, ProgramInfo.Solution).Result;
            var calleeMethod = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as BaseMethodDeclarationSyntax;
            var calleeSummary = MethodSummary.Factory.Summarize(calleeMethod);

            foreach (int idx in calleeSummary.GivesUpSet)
            {
                if (call.ArgumentList.Arguments[idx].Expression is ObjectCreationExpressionSyntax)
                {
                    var objCreation = call.ArgumentList.Arguments[idx].Expression
                        as ObjectCreationExpressionSyntax;
                    foreach (var arg in objCreation.ArgumentList.Arguments)
                    {
                        MethodSummaryAnalysis.ComputeGivesUpSetForArgument(
                            arg.Expression, cfgNode, summary);
                    }
                }
                else if (call.ArgumentList.Arguments[idx].Expression is BinaryExpressionSyntax &&
                    call.ArgumentList.Arguments[idx].Expression.IsKind(SyntaxKind.AsExpression))
                {
                    var binExpr = call.ArgumentList.Arguments[idx].Expression
                        as BinaryExpressionSyntax;
                    if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                    {
                        MethodSummaryAnalysis.ComputeGivesUpSetForArgument(binExpr.Left,
                            cfgNode, summary);
                    }
                    else if (binExpr.Left is InvocationExpressionSyntax)
                    {
                        var invocation = binExpr.Left as InvocationExpressionSyntax;
                        for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                        {
                            MethodSummaryAnalysis.ComputeGivesUpSetForArgument(invocation.ArgumentList.
                                Arguments[i].Expression, cfgNode, summary);
                        }
                    }
                }
                else if ((call.ArgumentList.Arguments[idx].Expression is IdentifierNameSyntax) ||
                    (call.ArgumentList.Arguments[idx].Expression is MemberAccessExpressionSyntax))
                {
                    MethodSummaryAnalysis.ComputeGivesUpSetForArgument(call.ArgumentList.
                        Arguments[idx].Expression, cfgNode, summary);
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
        private static void ComputeGivesUpSetForArgument(ExpressionSyntax arg, ControlFlowGraphNode cfgNode,
            MethodSummary summary)
        {
            var model = AnalysisContext.Compilation.GetSemanticModel(arg.SyntaxTree);
            if (arg is IdentifierNameSyntax || arg is MemberAccessExpressionSyntax)
            {
                for (int idx = 0; idx < summary.Method.ParameterList.Parameters.Count; idx++)
                {
                    if (Utilities.IsTypeAllowedToBeSend(summary.Method.ParameterList.Parameters[idx].Type, model))
                    {
                        continue;
                    }

                    var paramSymbol = model.GetDeclaredSymbol(summary.Method.ParameterList.Parameters[idx]);
                    if (DataFlowAnalysis.FlowsFromTarget(arg, paramSymbol, summary.Node.SyntaxNodes.First(),
                        summary.Node, cfgNode.SyntaxNodes.First(), cfgNode, model))
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
                    MethodSummaryAnalysis.ComputeGivesUpSetForArgument(item.Expression,
                        cfgNode, summary);
                }
            }
        }

        #endregion
    }
}
