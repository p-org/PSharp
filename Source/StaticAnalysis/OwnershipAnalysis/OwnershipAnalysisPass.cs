//-----------------------------------------------------------------------
// <copyright file="OwnershipAnalysisPass.cs">
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
using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Implementation of an abstract ownership analysis pass.
    /// </summary>
    internal abstract class OwnershipAnalysisPass : AnalysisPass
    {
        #region internal API

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        internal override void Run()
        {
            // Starts profiling the data-flow analysis.
            if (this.AnalysisContext.Configuration.ShowROARuntimeResults &&
                !this.AnalysisContext.Configuration.ShowRuntimeResults &&
                !this.AnalysisContext.Configuration.ShowDFARuntimeResults)
            {
                Profiler.StartMeasuringExecutionTime();
            }

            foreach (var machine in this.AnalysisContext.Machines)
            {
                this.AnalyzeMethodsInMachine(machine);
            }

            // Stops profiling the data-flow analysis.
            if (this.AnalysisContext.Configuration.ShowROARuntimeResults &&
                !this.AnalysisContext.Configuration.ShowRuntimeResults &&
                !this.AnalysisContext.Configuration.ShowDFARuntimeResults)
            {
                Profiler.StopMeasuringExecutionTime();
            }
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        protected OwnershipAnalysisPass(PSharpAnalysisContext context)
            : base(context)
        {

        }

        /// <summary>
        /// Analyzes the ownership of references in the given control-flow graph node.
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="giveUpSource">Give up source</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected abstract void AnalyzeOwnershipInCFG(ISymbol target, PSharpCFGNode cfgNode,
            PSharpCFGNode givesUpCfgNode, InvocationExpressionSyntax giveUpSource,
            HashSet<ControlFlowGraphNode> visited, StateMachine originalMachine,
            SemanticModel model, TraceInfo trace);

        /// <summary>
        /// Analyzes the ownership of references in the given variable declaration.
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="varDecl">VariableDeclarationSyntax</param>
        /// <param name="stmt">StatementSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected abstract void AnalyzeOwnershipInLocalDeclaration(ISymbol target, VariableDeclarationSyntax varDecl,
            StatementSyntax stmt, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            StateMachine originalMachine, SemanticModel model, TraceInfo trace);

        /// <summary>
        /// Analyzes the ownership of references in the given assignment expression.
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="assignment">AssignmentExpressionSyntax</param>
        /// <param name="stmt">StatementSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected abstract void AnalyzeOwnershipInAssignment(ISymbol target, AssignmentExpressionSyntax assignment,
            StatementSyntax stmt, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            StateMachine originalMachine, SemanticModel model, TraceInfo trace);

        /// <summary>
        /// Analyzes the ownership of references in the given candidate callee.
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="calleeSummary">PSharpMethodSummary</param>
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="givesUpSyntaxNode">Gives up syntaxNode</param>
        /// <param name="givesUpCfgNode">Gives up controlFlowGraphNode</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected abstract void AnalyzeOwnershipInCandidateCallee(ISymbol target, PSharpMethodSummary calleeSummary,
            ExpressionSyntax call, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, SyntaxNode givesUpSyntaxNode,
            PSharpCFGNode givesUpCfgNode, StateMachine originalMachine, SemanticModel model, TraceInfo trace);

        /// <summary>
        /// Analyzes the ownership of references in the gives-up operation.
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="call">Gives-up call</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="givesUpSyntaxNode">Gives up syntaxNode</param>
        /// <param name="givesUpCfgNode">Gives up controlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected abstract void AnalyzeOwnershipInGivesUpCall(ISymbol target, InvocationExpressionSyntax call,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, SyntaxNode givesUpSyntaxNode,
            PSharpCFGNode givesUpCfgNode, SemanticModel model, TraceInfo trace);

        /// <summary>
        /// Analyzes the ownership of references in the given control-flow graph node.
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="giveUpSource">Give up source</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected void AnalyzeOwnershipInCFG(ISymbol target, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            foreach (var syntaxNode in cfgNode.SyntaxNodes)
            {
                var stmt = syntaxNode as StatementSyntax;
                var localDecl = stmt.DescendantNodesAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
                var expr = stmt.DescendantNodesAndSelf().OfType<ExpressionStatementSyntax>().FirstOrDefault();

                if (localDecl != null)
                {
                    var varDecl = localDecl.Declaration;
                    this.AnalyzeOwnershipInLocalDeclaration(target, varDecl, stmt, syntaxNode,
                        cfgNode, givesUpCfgNode, originalMachine, model, trace);
                }
                else if (expr != null)
                {
                    if (expr.Expression is AssignmentExpressionSyntax)
                    {
                        var assignment = expr.Expression as AssignmentExpressionSyntax;
                        this.AnalyzeOwnershipInAssignment(target, assignment, stmt, syntaxNode,
                            cfgNode, givesUpCfgNode, originalMachine, model, trace);
                    }
                    else if (expr.Expression is InvocationExpressionSyntax ||
                        expr.Expression is ObjectCreationExpressionSyntax)
                    {
                        trace.InsertCall(cfgNode.GetMethodSummary().Method, expr.Expression);
                        this.AnalyzeOwnershipInCall(target, expr.Expression, syntaxNode, cfgNode,
                            givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode,
                            originalMachine, model, trace);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the ownership of references in the given call.
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="givesUpSyntaxNode">Gives up syntaxNode</param>
        /// <param name="givesUpCfgNode">Gives up controlFlowGraphNode</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        /// <returns>Set of return symbols</returns>
        protected HashSet<ISymbol> AnalyzeOwnershipInCall(ISymbol target, ExpressionSyntax call,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, SyntaxNode givesUpSyntaxNode,
            PSharpCFGNode givesUpCfgNode, StateMachine originalMachine, SemanticModel model,
            TraceInfo trace)
        {
            HashSet<ISymbol> potentialReturnSymbols = new HashSet<ISymbol>();

            var invocation = call as InvocationExpressionSyntax;
            var objCreation = call as ObjectCreationExpressionSyntax;
            if (invocation == null && objCreation == null)
            {
                return potentialReturnSymbols;
            }

            TraceInfo callTrace = new TraceInfo();
            callTrace.Merge(trace);
            callTrace.AddErrorTrace(call.ToString(), call.SyntaxTree.FilePath, call.SyntaxTree.
                GetLineSpan(call.Span).StartLinePosition.Line + 1);

            var calleeSymbol = model.GetSymbolInfo(call).Symbol;
            if (calleeSymbol == null)
            {
                AnalysisErrorReporter.ReportExternalInvocation(callTrace);
                return potentialReturnSymbols;
            }

            if (invocation != null &&
                calleeSymbol.ContainingType.ToString().Equals("Microsoft.PSharp.Machine"))
            {
                this.AnalyzeOwnershipInGivesUpCall(target, invocation, syntaxNode, cfgNode,
                    givesUpSyntaxNode, givesUpCfgNode, model, callTrace);
                return potentialReturnSymbols;
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(calleeSymbol,
                this.AnalysisContext.Solution).Result;
            if (definition == null || definition.DeclaringSyntaxReferences.IsEmpty)
            {
                AnalysisErrorReporter.ReportExternalInvocation(callTrace);
                return potentialReturnSymbols;
            }

            var candidateCallees = new HashSet<BaseMethodDeclarationSyntax>();
            if (invocation != null)
            {
                candidateCallees.UnionWith(this.AnalysisContext.ResolveCandidateMethodsAtCallSite(
                    invocation, syntaxNode, cfgNode, model));
            }
            else
            {
                var callee = this.AnalysisContext.ResolveConstructor(objCreation, model);
                candidateCallees.Add(callee);
            }

            foreach (var candidateCallee in candidateCallees)
            {
                var calleeSummary = PSharpMethodSummary.Create(this.AnalysisContext, candidateCallee);
                this.AnalyzeOwnershipInCandidateCallee(target, calleeSummary, call, syntaxNode,
                    cfgNode, givesUpSyntaxNode, givesUpCfgNode, originalMachine, model, callTrace);

                var resolvedReturnSymbols = calleeSummary.GetResolvedReturnSymbols(call, model);
                foreach (var rrs in resolvedReturnSymbols)
                {
                    potentialReturnSymbols.Add(rrs);
                }
            }

            return potentialReturnSymbols;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Analyzes the methods of the given machine to check if each method
        /// respects given-up ownerships.
        /// </summary>
        /// <param name="machine">StateMachine</param>
        private void AnalyzeMethodsInMachine(StateMachine machine)
        {
            var machinesToAnalyze = new List<StateMachine> { machine };

            HashSet<StateMachine> baseMachines;
            if (this.AnalysisContext.MachineInheritanceMap.TryGetValue(machine, out baseMachines))
            {
                machinesToAnalyze.AddRange(baseMachines);
            }

            foreach (var machineToAnalyze in machinesToAnalyze)
            {
                foreach (var method in machineToAnalyze.Declaration.ChildNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (!method.Modifiers.Any(SyntaxKind.AbstractKeyword))
                    {
                        this.AnalyzeMethod(method, machineToAnalyze, null, machine);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the given method to check if it respects
        /// the given-up ownerships.
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">MachineState</param>
        /// <param name="originalMachine">Original machine</param>
        private void AnalyzeMethod(MethodDeclarationSyntax method, StateMachine machine,
            MachineState state, StateMachine originalMachine)
        {
            var summary = PSharpMethodSummary.Create(this.AnalysisContext, method, machine);
            foreach (var givesUpCfgNode in summary.GivesUpOwnershipNodes)
            {
                this.AnalyzeOwnershipInGivesUpCFGNode(givesUpCfgNode, summary, machine, state, originalMachine);
            }
        }

        #endregion

        #region ownership analysis methods

        /// <summary>
        /// Analyzes the ownership of references in the gives-up
        /// control-flow graph node.
        /// </summary>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="summary">MethodSummary</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">MachineState</param>
        /// <param name="originalMachine">Original machine</param>
        private void AnalyzeOwnershipInGivesUpCFGNode(PSharpCFGNode givesUpCfgNode, PSharpMethodSummary summary,
            StateMachine machine, MachineState state, StateMachine originalMachine)
        {
            var givesUpSource = givesUpCfgNode.SyntaxNodes[0].DescendantNodesAndSelf().
                OfType<InvocationExpressionSyntax>().FirstOrDefault();

            if (givesUpSource == null || !(givesUpSource.Expression is MemberAccessExpressionSyntax ||
                givesUpSource.Expression is IdentifierNameSyntax))
            {
                return;
            }

            var model = this.AnalysisContext.Compilation.GetSemanticModel(givesUpSource.SyntaxTree);
            ISymbol symbol = model.GetSymbolInfo(givesUpSource).Symbol;
            string methodName = symbol.ContainingNamespace.ToString() + "." + symbol.Name;

            if (methodName.Equals("Microsoft.PSharp.Send"))
            {
                this.AnalyzeOwnershipInSendExpression(givesUpSource, givesUpCfgNode, machine,
                    state, originalMachine);
            }
            else if (methodName.Equals("Microsoft.PSharp.CreateMachine"))
            {
                this.AnalyzeOwnershipInCreateMachineExpression(givesUpSource, givesUpCfgNode,
                    machine, state, originalMachine);
            }
            else
            {
                this.AnalyzeOwnershipInGenericCallExpression(givesUpSource, givesUpCfgNode,
                    machine, state, originalMachine);
            }
        }

        /// <summary>
        /// Analyzes the ownership of references in the given 'Send' expression.
        /// </summary>
        /// <param name="send">Send call</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">MachineState</param>
        /// <param name="originalMachine">Original machine</param>
        private void AnalyzeOwnershipInSendExpression(InvocationExpressionSyntax send, PSharpCFGNode givesUpCfgNode,
            StateMachine machine, MachineState state, StateMachine originalMachine)
        {
            var expr = send.ArgumentList.Arguments[1].Expression;
            if (expr is ObjectCreationExpressionSyntax)
            {
                var objCreation = expr as ObjectCreationExpressionSyntax;
                foreach (var arg in objCreation.ArgumentList.Arguments)
                {
                    this.AnalyzeOwnershipInArgumentSyntax(arg.Expression, send, givesUpCfgNode,
                        machine, state, originalMachine);
                }
            }
            else if (expr is BinaryExpressionSyntax && expr.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = expr as BinaryExpressionSyntax;
                if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                {
                    this.AnalyzeOwnershipInArgumentSyntax(binExpr.Left, send, givesUpCfgNode,
                        machine, state, originalMachine);
                }
                else if (binExpr.Left is InvocationExpressionSyntax)
                {
                    var invocation = binExpr.Left as InvocationExpressionSyntax;
                    for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                    {
                        this.AnalyzeOwnershipInArgumentSyntax(invocation.ArgumentList.Arguments[i].
                            Expression, send, givesUpCfgNode, machine, state, originalMachine);
                    }
                }
            }
            else if (expr is IdentifierNameSyntax || expr is MemberAccessExpressionSyntax)
            {
                this.AnalyzeOwnershipInArgumentSyntax(expr, send, givesUpCfgNode,
                    machine, state, originalMachine);
            }
        }

        /// <summary>
        /// Analyzes the ownership of references in the given 'CreateMachine' expression.
        /// </summary>
        /// <param name="send">Create call</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">MachineState</param>
        /// <param name="originalMachine">Original machine</param>
        private void AnalyzeOwnershipInCreateMachineExpression(InvocationExpressionSyntax create,
            PSharpCFGNode givesUpCfgNode, StateMachine machine, MachineState state,
            StateMachine originalMachine)
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
                    this.AnalyzeOwnershipInArgumentSyntax(arg.Expression, create,
                        givesUpCfgNode, machine, state, originalMachine);
                }
            }
            else if (expr is BinaryExpressionSyntax && expr.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = expr as BinaryExpressionSyntax;
                if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                {
                    this.AnalyzeOwnershipInArgumentSyntax(binExpr.Left, create,
                        givesUpCfgNode, machine, state, originalMachine);
                }
                else if (binExpr.Left is InvocationExpressionSyntax)
                {
                    var invocation = binExpr.Left as InvocationExpressionSyntax;
                    for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                    {
                        this.AnalyzeOwnershipInArgumentSyntax(invocation.ArgumentList.Arguments[i].
                            Expression, create, givesUpCfgNode, machine, state, originalMachine);
                    }
                }
            }
            else if (expr is IdentifierNameSyntax || expr is MemberAccessExpressionSyntax)
            {
                this.AnalyzeOwnershipInArgumentSyntax(expr, create, givesUpCfgNode,
                    machine, state, originalMachine);
            }
        }

        /// <summary>
        /// Analyzes the ownership of references in the given call expression.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">MachineState</param>
        /// <param name="originalMachine">Original machine</param>
        private void AnalyzeOwnershipInGenericCallExpression(InvocationExpressionSyntax call, PSharpCFGNode givesUpCfgNode,
            StateMachine machine, MachineState state, StateMachine originalMachine)
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
                        this.AnalyzeOwnershipInArgumentSyntax(arg.Expression, call, givesUpCfgNode,
                            machine, state, originalMachine);
                    }
                }
                else if (expr is BinaryExpressionSyntax && expr.IsKind(SyntaxKind.AsExpression))
                {
                    var binExpr = expr as BinaryExpressionSyntax;
                    if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                    {
                        this.AnalyzeOwnershipInArgumentSyntax(binExpr.Left, call, givesUpCfgNode,
                            machine, state, originalMachine);
                    }
                    else if (binExpr.Left is InvocationExpressionSyntax)
                    {
                        var invocation = binExpr.Left as InvocationExpressionSyntax;
                        for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                        {
                            this.AnalyzeOwnershipInArgumentSyntax(invocation.ArgumentList.Arguments[i].Expression,
                                call, givesUpCfgNode, machine, state, originalMachine);
                        }
                    }
                }
                else if (expr is IdentifierNameSyntax || expr is MemberAccessExpressionSyntax)
                {
                    this.AnalyzeOwnershipInArgumentSyntax(expr, call, givesUpCfgNode,
                        machine, state, originalMachine);
                }
            }
        }

        /// <summary>
        /// Analyzes the ownership of references in the given argument.
        /// </summary>
        /// <param name="arg">Argument</param>
        /// <param name="call">Call</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">MachineState</param>
        /// <param name="originalMachine">Original machine</param>
        private void AnalyzeOwnershipInArgumentSyntax(ExpressionSyntax arg, InvocationExpressionSyntax call,
            PSharpCFGNode givesUpCfgNode, StateMachine machine, MachineState state, StateMachine originalMachine)
        {
            var model = this.AnalysisContext.Compilation.GetSemanticModel(arg.SyntaxTree);

            if (arg is MemberAccessExpressionSyntax || arg is IdentifierNameSyntax)
            {
                TraceInfo trace = new TraceInfo(givesUpCfgNode.GetMethodSummary().Method
                    as MethodDeclarationSyntax, machine, state, arg);
                trace.AddErrorTrace(call.ToString(), call.SyntaxTree.FilePath, call.SyntaxTree.
                    GetLineSpan(call.Span).StartLinePosition.Line + 1);

                if (this.IsArgumentSafeToAccess(arg, givesUpCfgNode, model, trace, true))
                {
                    return;
                }

                ISymbol argSymbol = model.GetSymbolInfo(arg).Symbol;
                this.AnalyzeOwnershipInCFG(argSymbol, givesUpCfgNode, givesUpCfgNode, call,
                    new HashSet<ControlFlowGraphNode>(), originalMachine, model, trace);
            }
            else if (arg is ObjectCreationExpressionSyntax)
            {
                var payload = arg as ObjectCreationExpressionSyntax;
                foreach (var item in payload.ArgumentList.Arguments)
                {
                    this.AnalyzeOwnershipInArgumentSyntax(item.Expression, call, givesUpCfgNode,
                        machine, state, originalMachine);
                }
            }
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Returns true if the given field symbol is being accessed
        /// before being reset.
        /// </summary>
        /// <param name="field">Field</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean</returns>
        protected bool IsFieldAccessedBeforeBeingReset(ISymbol field, MethodSummary summary)
        {
            StateTransitionGraphNode stateTransitionNode = null;
            if (!this.AnalysisContext.StateTransitionGraphs.ContainsKey((summary as PSharpMethodSummary).Machine))
            {
                return true;
            }

            stateTransitionNode = this.AnalysisContext.StateTransitionGraphs[(summary as PSharpMethodSummary).Machine].
                GetGraphNodeForSummary(summary);
            if (stateTransitionNode == null)
            {
                return true;
            }

            var result = stateTransitionNode.VisitSelfAndSuccessors(
                this.IsFieldAccessedBeforeBeingReset, new Tuple<MethodSummary, ISymbol>(summary, field));

            return false;
        }

        /// <summary>
        /// Query checking if the field is accessed before being reset
        /// in the given state transition node.
        /// </summary>
        /// <param name="node">StateTransitionGraphNode</param>
        /// <param name="input">Input</param>
        /// <param name="isFirstVisit">True if first node to visit</param>
        /// <returns>Boolean</returns>
        protected bool IsFieldAccessedBeforeBeingReset(StateTransitionGraphNode node,
            object input, bool isFirstVisit)
        {
            var summary = ((Tuple<MethodSummary, ISymbol>)input).Item1;
            var fieldSymbol = ((Tuple<MethodSummary, ISymbol>)input).Item2;
            var result = false;

            if (isFirstVisit && node.OnExit != null && !summary.Equals(node.OnExit))
            {
                foreach (var action in node.Actions)
                {
                    result = action.FieldAccessSet.ContainsKey(fieldSymbol as IFieldSymbol);
                    if (result)
                    {
                        break;
                    }
                }

                if (!result && node.OnExit != null)
                {
                    result = node.OnExit.FieldAccessSet.ContainsKey(fieldSymbol as IFieldSymbol);
                }
            }
            else if (!isFirstVisit)
            {
                if (node.OnEntry != null)
                {
                    result = node.OnEntry.FieldAccessSet.ContainsKey(fieldSymbol as IFieldSymbol);
                }

                if (!result)
                {
                    foreach (var action in node.Actions)
                    {
                        result = action.FieldAccessSet.ContainsKey(fieldSymbol as IFieldSymbol);
                        if (result)
                        {
                            break;
                        }
                    }
                }

                if (!result && node.OnExit != null)
                {
                    result = node.OnExit.FieldAccessSet.ContainsKey(fieldSymbol as IFieldSymbol);
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts arguments from the given list of arguments.
        /// </summary>
        /// <param name="arguments">List of arguments</param>
        /// <returns>List of arguments</returns>
        protected List<ExpressionSyntax> ExtractArguments(List<ExpressionSyntax> arguments)
        {
            var args = new List<ExpressionSyntax>();

            foreach (var arg in arguments)
            {
                if (arg is ObjectCreationExpressionSyntax)
                {
                    var objCreation = arg as ObjectCreationExpressionSyntax;
                    List<ExpressionSyntax> argExprs = new List<ExpressionSyntax>();
                    foreach (var objCreationArg in objCreation.ArgumentList.Arguments)
                    {
                        argExprs.Add(objCreationArg.Expression);
                    }

                    var objCreationArgs = this.ExtractArguments(argExprs);
                    foreach (var objCreationArg in objCreationArgs)
                    {
                        args.Add(objCreationArg);
                    }
                }
                else if (arg is InvocationExpressionSyntax)
                {
                    var invocation = arg as InvocationExpressionSyntax;
                    List<ExpressionSyntax> argExprs = new List<ExpressionSyntax>();
                    foreach (var invocationArg in invocation.ArgumentList.Arguments)
                    {
                        argExprs.Add(invocationArg.Expression);
                    }

                    var invocationArgs = this.ExtractArguments(argExprs);
                    foreach (var invocationArg in invocationArgs)
                    {
                        args.Add(invocationArg);
                    }
                }
                else
                {
                    args.Add(arg);
                }
            }

            return args;
        }

        /// <summary>
        /// Returns true if the argument can be accessed after it is sent.
        /// Returns false if it cannot be accessed.
        /// </summary>
        /// <param name="arg">Argument</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        /// <param name="shouldReportError">Should report error</param>
        /// <returns>Boolean</returns>
        private bool IsArgumentSafeToAccess(ExpressionSyntax arg, PSharpCFGNode givesUpCfgNode,
            SemanticModel model, TraceInfo trace, bool shouldReportError)
        {
            ISymbol symbol = null;
            ITypeSymbol typeSymbol = null;
            if (arg is MemberAccessExpressionSyntax)
            {
                symbol = model.GetSymbolInfo((arg as MemberAccessExpressionSyntax).Name).Symbol;
                typeSymbol = model.GetTypeInfo(arg).Type;
            }
            else if (arg is IdentifierNameSyntax)
            {
                symbol = model.GetSymbolInfo(arg).Symbol;
                typeSymbol = model.GetTypeInfo(arg).Type;
            }

            if (symbol.ToString().Equals("Microsoft.PSharp.Machine.Payload"))
            {
                return false;
            }
            else if (typeSymbol.ToString().Equals("Microsoft.PSharp.Machine") ||
                symbol.ToString().Equals("Microsoft.PSharp.Machine"))
            {
                return true;
            }
            else if (symbol.ContainingType.Name.Equals("Tuple"))
            {
                var str = (arg as MemberAccessExpressionSyntax).Name.ToString();
                int idx = Int32.Parse(str.Substring(4));
                return this.AnalysisContext.IsTypePassedByValueOrImmutable(
                    symbol.ContainingType.TypeArguments[idx - 1]);
            }
            else
            {
                var definition = SymbolFinder.FindSourceDefinitionAsync(symbol,
                    this.AnalysisContext.Solution).Result;
                if (definition == null)
                {
                    return false;
                }

                if (definition.DeclaringSyntaxReferences.First().GetSyntax().Parent is ParameterListSyntax)
                {
                    var parameterList = definition.DeclaringSyntaxReferences.First().
                        GetSyntax().Parent as ParameterListSyntax;
                    var parameter = parameterList.Parameters.First(v =>
                        v.Identifier.ValueText.Equals(arg.ToString()));
                    TypeInfo typeInfo = model.GetTypeInfo(parameter.Type);
                    return this.AnalysisContext.IsTypePassedByValueOrImmutable(typeInfo.Type);
                }
                else if (definition.DeclaringSyntaxReferences.First().GetSyntax().Parent is VariableDeclarationSyntax)
                {
                    var varDecl = definition.DeclaringSyntaxReferences.First().GetSyntax().Parent
                        as VariableDeclarationSyntax;
                    TypeInfo typeInfo = model.GetTypeInfo(varDecl.Type);
                    var argType = model.GetTypeInfo(arg).Type;

                    if (shouldReportError && definition != null && definition.Kind == SymbolKind.Field &&
                        !this.AnalysisContext.IsTypePassedByValueOrImmutable(typeInfo.Type) &&
                        !this.AnalysisContext.IsTypeEnum(argType) &&
                        !DataFlowQuerying.DoesResetInSuccessorControlFlowGraphNodes(
                            symbol, symbol, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode) &&
                        this.IsFieldAccessedBeforeBeingReset(definition, givesUpCfgNode.GetMethodSummary()))
                    {
                        AnalysisErrorReporter.ReportGivenUpFieldOwnershipError(trace);
                    }

                    return this.AnalysisContext.IsTypePassedByValueOrImmutable(typeInfo.Type);
                }
            }

            return true;
        }

        #endregion
    }
}
