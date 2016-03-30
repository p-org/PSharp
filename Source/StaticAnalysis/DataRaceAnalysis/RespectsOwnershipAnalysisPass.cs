//-----------------------------------------------------------------------
// <copyright file="RespectsOwnershipAnalysisPass.cs">
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
    /// This analysis checks that all methods in each machine of a P#
    /// program respect given-up ownerships. In more detail:
    /// 
    /// - The analysis checks that no object can be accessed by a method
    /// after the call to another method, if the callee has given-up
    /// ownership for that object.
    /// 
    /// - The ananlysis is performed in a modular fashion and does not
    /// focus specifically on 'send' operations, but more generally on
    /// operations that give up ownership.
    /// </summary>
    public sealed class RespectsOwnershipAnalysisPass : AnalysisPass
    {
        #region public API

        /// <summary>
        /// Creates a new respects ownership analysis pass.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <returns>RespectsOwnershipAnalysisPass</returns>
        public static RespectsOwnershipAnalysisPass Create(PSharpAnalysisContext context)
        {
            return new RespectsOwnershipAnalysisPass(context);
        }

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        /// <returns>RespectsOwnershipAnalysisPass</returns>
        public override void Run()
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

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        private RespectsOwnershipAnalysisPass(PSharpAnalysisContext context)
            : base(context)
        {

        }

        /// <summary>
        /// Analyzes the methods of the given machine to check if each method
        /// respects given-up ownerships.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="machineToAnalyze">Machine to analyse</param>
        private void AnalyzeMethodsInMachine(StateMachine machine, StateMachine machineToAnalyze = null)
        {
            if (machineToAnalyze == null)
            {
                machineToAnalyze = machine;
            }

            foreach (var method in machineToAnalyze.Declaration.ChildNodes().OfType<MethodDeclarationSyntax>())
            {
                if (!method.Modifiers.Any(SyntaxKind.AbstractKeyword))
                {
                    this.AnalyzeMethod(method, machineToAnalyze, null, machine);
                }
            }

            if (this.AnalysisContext.MachineInheritanceMap.ContainsKey(machineToAnalyze))
            {
                this.AnalyzeMethodsInMachine(machine, this.AnalysisContext.
                    MachineInheritanceMap[machineToAnalyze]);
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
            var summary = PSharpMethodSummary.Create(this.AnalysisContext, method);
            foreach (var givesUpCfgNode in summary.GivesUpOwnershipNodes)
            {
                this.AnalyzeGivesUpCFGNode(givesUpCfgNode, summary, machine, state, originalMachine);
            }
        }

        #endregion

        #region gives-up ownership analysis methods

        /// <summary>
        /// Analyzes the given control-flow graph node to check
        /// if it respects the given-up ownerships.
        /// </summary>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="summary">MethodSummary</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">MachineState</param>
        /// <param name="originalMachine">Original machine</param>
        private void AnalyzeGivesUpCFGNode(PSharpCFGNode givesUpCfgNode, PSharpMethodSummary summary,
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
                this.AnalyzeSendExpression(givesUpSource, givesUpCfgNode, machine,
                    state, originalMachine);
            }
            else if (methodName.Equals("Microsoft.PSharp.CreateMachine"))
            {
                this.AnalyzeCreateExpression(givesUpSource, givesUpCfgNode, machine,
                    state, originalMachine);
            }
            else
            {
                this.AnalyzeGenericCallExpression(givesUpSource, givesUpCfgNode, machine,
                    state, originalMachine);
            }
        }
        
        /// <summary>
        /// Analyzes the given 'Send' expression to check if it respects the
        /// given-up ownerships.
        /// </summary>
        /// <param name="send">Send call</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">MachineState</param>
        /// <param name="originalMachine">Original machine</param>
        private void AnalyzeSendExpression(InvocationExpressionSyntax send, PSharpCFGNode givesUpCfgNode,
            StateMachine machine, MachineState state, StateMachine originalMachine)
        {
            var expr = send.ArgumentList.Arguments[1].Expression;
            if (expr is ObjectCreationExpressionSyntax)
            {
                var objCreation = expr as ObjectCreationExpressionSyntax;
                foreach (var arg in objCreation.ArgumentList.Arguments)
                {
                    this.AnalyzeArgumentSyntax(arg.Expression,
                        send, givesUpCfgNode, machine, state, originalMachine);
                }
            }
            else if (expr is BinaryExpressionSyntax && expr.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = expr as BinaryExpressionSyntax;
                if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                {
                    this.AnalyzeArgumentSyntax(binExpr.Left,
                        send, givesUpCfgNode, machine, state, originalMachine);
                }
                else if (binExpr.Left is InvocationExpressionSyntax)
                {
                    var invocation = binExpr.Left as InvocationExpressionSyntax;
                    for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                    {
                        this.AnalyzeArgumentSyntax(invocation.ArgumentList.Arguments[i].
                            Expression, send, givesUpCfgNode, machine, state, originalMachine);
                    }
                }
            }
            else if (expr is IdentifierNameSyntax || expr is MemberAccessExpressionSyntax)
            {
                this.AnalyzeArgumentSyntax(expr, send, givesUpCfgNode,
                    machine, state, originalMachine);
            }
        }

        /// <summary>
        /// Analyzes the given 'Create' expression to check if it respects the
        /// given-up ownerships.
        /// </summary>
        /// <param name="send">Create call</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">MachineState</param>
        /// <param name="originalMachine">Original machine</param>
        private void AnalyzeCreateExpression(InvocationExpressionSyntax create, PSharpCFGNode givesUpCfgNode,
            StateMachine machine, MachineState state, StateMachine originalMachine)
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
                    this.AnalyzeArgumentSyntax(arg.Expression,
                        create, givesUpCfgNode, machine, state, originalMachine);
                }
            }
            else if (expr is BinaryExpressionSyntax && expr.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = expr as BinaryExpressionSyntax;
                if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                {
                    this.AnalyzeArgumentSyntax(binExpr.Left,
                        create, givesUpCfgNode, machine, state, originalMachine);
                }
                else if (binExpr.Left is InvocationExpressionSyntax)
                {
                    var invocation = binExpr.Left as InvocationExpressionSyntax;
                    for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                    {
                        this.AnalyzeArgumentSyntax(invocation.ArgumentList.Arguments[i].
                            Expression, create, givesUpCfgNode, machine, state, originalMachine);
                    }
                }
            }
            else if (expr is IdentifierNameSyntax || expr is MemberAccessExpressionSyntax)
            {
                this.AnalyzeArgumentSyntax(expr, create, givesUpCfgNode,
                    machine, state, originalMachine);
            }
        }

        /// <summary>
        /// Analyzes the given call expression to check if it respects the
        /// given-up ownerships.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">MachineState</param>
        /// <param name="originalMachine">Original machine</param>
        private void AnalyzeGenericCallExpression(InvocationExpressionSyntax call, PSharpCFGNode givesUpCfgNode,
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
                        this.AnalyzeArgumentSyntax(arg.Expression, call,
                            givesUpCfgNode, machine, state, originalMachine);
                    }
                }
                else if (expr is BinaryExpressionSyntax && expr.IsKind(SyntaxKind.AsExpression))
                {
                    var binExpr = expr as BinaryExpressionSyntax;
                    if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                    {
                        this.AnalyzeArgumentSyntax(binExpr.Left, call,
                            givesUpCfgNode, machine, state, originalMachine);
                    }
                    else if (binExpr.Left is InvocationExpressionSyntax)
                    {
                        var invocation = binExpr.Left as InvocationExpressionSyntax;
                        for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                        {
                            this.AnalyzeArgumentSyntax(invocation.ArgumentList.Arguments[i].
                                Expression, call, givesUpCfgNode, machine, state, originalMachine);
                        }
                    }
                }
                else if (expr is IdentifierNameSyntax || expr is MemberAccessExpressionSyntax)
                {
                    this.AnalyzeArgumentSyntax(expr, call, givesUpCfgNode,
                        machine, state, originalMachine);
                }
            }
        }

        /// <summary>
        /// Analyzes the given argument to see if it respects the given-up ownerships.
        /// </summary>
        /// <param name="arg">Argument</param>
        /// <param name="call">Call</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">MachineState</param>
        /// <param name="originalMachine">Original machine</param>
        private void AnalyzeArgumentSyntax(ExpressionSyntax arg, InvocationExpressionSyntax call,
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
                
                ISymbol argSymbol = null;
                if (arg is IdentifierNameSyntax)
                {
                    argSymbol = model.GetSymbolInfo(arg as IdentifierNameSyntax).Symbol;
                }
                else if (arg is MemberAccessExpressionSyntax)
                {
                    argSymbol = model.GetSymbolInfo((arg as MemberAccessExpressionSyntax).Name).Symbol;
                }

                this.DetectGivenUpFieldOwnershipInCFG(givesUpCfgNode, givesUpCfgNode, argSymbol,
                    call, new HashSet<ControlFlowGraphNode>(), originalMachine, model, trace);
                this.DetectPotentialDataRacesInCFG(givesUpCfgNode, givesUpCfgNode, argSymbol,
                    call, new HashSet<ControlFlowGraphNode>(), originalMachine, model, trace);
            }
            else if (arg is ObjectCreationExpressionSyntax)
            {
                var payload = arg as ObjectCreationExpressionSyntax;
                foreach (var item in payload.ArgumentList.Arguments)
                {
                    this.AnalyzeArgumentSyntax(item.Expression, call, givesUpCfgNode,
                        machine, state, originalMachine);
                }
            }
        }

        #endregion

        #region predecessor analysis methods

        /// <summary>
        /// Analyzes the given control-flow graph node to find if it gives up ownership of
        /// data from a machine field.
        /// </summary>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="target">Target</param>
        /// <param name="giveUpSource">Give up source</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void DetectGivenUpFieldOwnershipInCFG(PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            ISymbol target, InvocationExpressionSyntax giveUpSource, HashSet<ControlFlowGraphNode> visited,
            StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            if (!cfgNode.IsJumpNode && !cfgNode.IsLoopHeadNode &&
                visited.Contains(givesUpCfgNode))
            {
                foreach (var syntaxNode in cfgNode.SyntaxNodes)
                {
                    var stmt = syntaxNode as StatementSyntax;
                    var localDecl = stmt.DescendantNodesAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
                    var expr = stmt.DescendantNodesAndSelf().OfType<ExpressionStatementSyntax>().FirstOrDefault();

                    if (localDecl != null)
                    {
                        var varDecl = localDecl.Declaration;
                        this.DetectGivenUpFieldOwnershipInVariableDeclaration(varDecl, syntaxNode, cfgNode,
                            givesUpCfgNode, target, originalMachine, model, trace);
                    }
                    else if (expr != null)
                    {
                        if (expr.Expression is AssignmentExpressionSyntax)
                        {
                            var assignment = expr.Expression as AssignmentExpressionSyntax;
                            this.DetectGivenUpFieldOwnershipInAssignmentExpression(assignment, stmt,
                                syntaxNode, cfgNode, givesUpCfgNode, target, originalMachine, model, trace);
                        }
                        else if (expr.Expression is InvocationExpressionSyntax)
                        {
                            var invocation = expr.Expression as InvocationExpressionSyntax;
                            trace.InsertCall(cfgNode.GetMethodSummary().Method, invocation);
                            this.DetectGivenUpFieldOwnershipInInvocation(invocation, target, syntaxNode,
                                cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode,
                                originalMachine, model, trace);
                        }
                    }
                }
            }

            if (!visited.Contains(cfgNode))
            {
                visited.Add(cfgNode);

                if (givesUpCfgNode != null)
                {
                    foreach (var predecessor in cfgNode.GetImmediatePredecessors())
                    {
                        this.DetectGivenUpFieldOwnershipInCFG(predecessor,
                            givesUpCfgNode, target, giveUpSource, visited, originalMachine, model, trace);
                    }
                }
                else
                {
                    foreach (var successor in cfgNode.GetImmediateSuccessors())
                    {
                        this.DetectGivenUpFieldOwnershipInCFG(successor,
                            givesUpCfgNode, target, giveUpSource, visited, originalMachine, model, trace);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the given variable declaration to find if it
        /// gives up ownership of data from a machine field.
        /// </summary>
        /// <param name="varDecl">VariableDeclarationSyntax</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="target">Target</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void DetectGivenUpFieldOwnershipInVariableDeclaration(VariableDeclarationSyntax varDecl,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            ISymbol target, StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            foreach (var variable in varDecl.Variables.Where(v => v.Initializer != null))
            {
                var rightSymbols = new HashSet<ISymbol>();
                if (variable.Initializer.Value is IdentifierNameSyntax ||
                    variable.Initializer.Value is MemberAccessExpressionSyntax)
                {
                    if (DataFlowQuerying.FlowsIntoTarget(variable, target, syntaxNode, cfgNode,
                        givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model))
                    {
                        IdentifierNameSyntax identifier = null;
                        if (variable.Initializer.Value is IdentifierNameSyntax)
                        {
                            identifier = variable.Initializer.Value as IdentifierNameSyntax;
                        }
                        else if (variable.Initializer.Value is MemberAccessExpressionSyntax)
                        {
                            identifier = this.AnalysisContext.GetTopLevelIdentifier(
                                variable.Initializer.Value);
                        }

                        if (identifier != null)
                        {
                            var rightSymbol = model.GetSymbolInfo(identifier).Symbol;
                            rightSymbols.Add(rightSymbol);
                        }
                    }
                }
                else if (variable.Initializer.Value is InvocationExpressionSyntax)
                {
                    var invocation = variable.Initializer.Value as InvocationExpressionSyntax;
                    trace.InsertCall(cfgNode.GetMethodSummary().Method, invocation);
                    var returnSymbols = this.DetectGivenUpFieldOwnershipInInvocation(
                        invocation, target, syntaxNode, cfgNode, givesUpCfgNode.SyntaxNodes.First(),
                        givesUpCfgNode, originalMachine, model, trace);

                    if (DataFlowQuerying.FlowsIntoTarget(variable, target, syntaxNode, cfgNode,
                        givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model))
                    {
                        rightSymbols = returnSymbols;
                    }
                }
                else if (variable.Initializer.Value is ObjectCreationExpressionSyntax)
                {
                    var objCreation = variable.Initializer.Value as ObjectCreationExpressionSyntax;
                    trace.InsertCall(cfgNode.GetMethodSummary().Method, objCreation);
                    var returnSymbols = this.DetectGivenUpFieldOwnershipInObjectCreation(
                        objCreation, target, syntaxNode, cfgNode, givesUpCfgNode.SyntaxNodes.First(),
                        givesUpCfgNode, model, trace);

                    if (DataFlowQuerying.FlowsIntoTarget(variable, target, syntaxNode, cfgNode,
                        givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model))
                    {
                        rightSymbols = returnSymbols;
                    }
                }

                foreach (var rightSymbol in rightSymbols)
                {
                    if (!rightSymbol.Equals(target) && !variable.Initializer.Value.
                        ToString().Equals(target.ToString()))
                    {
                        var rightDef = SymbolFinder.FindSourceDefinitionAsync(rightSymbol,
                            this.AnalysisContext.Solution).Result;
                        var type = model.GetTypeInfo(variable.Initializer.Value).Type;
                        if (rightDef != null && rightDef.Kind == SymbolKind.Field &&
                            this.AnalysisContext.DoesFieldBelongToMachine(rightDef, cfgNode.GetMethodSummary()) &&
                            !this.AnalysisContext.IsTypePassedByValueOrImmutable(type) &&
                            !this.AnalysisContext.IsExprEnum(variable.Initializer.Value, model) &&
                            !DataFlowQuerying.DoesResetInSuccessorControlFlowGraphNodes(
                                rightSymbol, target, syntaxNode, cfgNode) &&
                            this.IsFieldAccessedBeforeBeingReset(rightDef, cfgNode.GetMethodSummary()))
                        {
                            TraceInfo newTrace = new TraceInfo();
                            newTrace.Merge(trace);
                            newTrace.AddErrorTrace(syntaxNode.ToString(), syntaxNode.SyntaxTree.FilePath,
                                syntaxNode.SyntaxTree.GetLineSpan(syntaxNode.Span).StartLinePosition.Line + 1);
                            AnalysisErrorReporter.ReportGivenUpFieldOwnershipError(newTrace);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the given assignment expression to find if it
        /// gives up ownership of data from a machine field.
        /// </summary>
        /// <param name="assignment">AssignmentExpressionSyntax</param>
        /// <param name="stmt">StatementSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="target">Target</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void DetectGivenUpFieldOwnershipInAssignmentExpression(AssignmentExpressionSyntax assignment,
            StatementSyntax stmt, SyntaxNode syntaxNode, PSharpCFGNode cfgNode,
            PSharpCFGNode givesUpCfgNode, ISymbol target, StateMachine originalMachine,
            SemanticModel model, TraceInfo trace)
        {
            if (this.IsPayloadIllegallyAccessed(assignment, stmt, model, trace))
            {
                return;
            }

            ISymbol leftSymbol = null;
            if (assignment.Left is IdentifierNameSyntax)
            {
                leftSymbol = model.GetSymbolInfo(assignment.Left
                    as IdentifierNameSyntax).Symbol;
            }
            else if (assignment.Left is MemberAccessExpressionSyntax)
            {
                var leftIdentifier = this.AnalysisContext.GetTopLevelIdentifier(
                    assignment.Left);
                if (leftIdentifier != null)
                {
                    leftSymbol = model.GetSymbolInfo(leftIdentifier).Symbol;
                }
            }

            var rightSymbols = new HashSet<ISymbol>();
            if (assignment.Right is IdentifierNameSyntax ||
                assignment.Right is MemberAccessExpressionSyntax)
            {
                if (DataFlowQuerying.FlowsIntoTarget(assignment.Left, target, syntaxNode,
                    cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode,
                    model, this.AnalysisContext))
                {
                    IdentifierNameSyntax rightIdentifier = null;
                    if (assignment.Right is IdentifierNameSyntax)
                    {
                        rightIdentifier = assignment.Right as IdentifierNameSyntax;
                    }
                    else if (assignment.Right is MemberAccessExpressionSyntax)
                    {
                        rightIdentifier = this.AnalysisContext.GetTopLevelIdentifier(
                            assignment.Right);
                    }

                    if (rightIdentifier != null)
                    {
                        var rightSymbol = model.GetSymbolInfo(rightIdentifier).Symbol;
                        rightSymbols.Add(rightSymbol);
                    }
                }
            }
            else if (assignment.Right is InvocationExpressionSyntax)
            {
                var invocation = assignment.Right as InvocationExpressionSyntax;
                trace.InsertCall(cfgNode.GetMethodSummary().Method, invocation);
                var returnSymbols = this.DetectGivenUpFieldOwnershipInInvocation(
                    invocation, target, syntaxNode, cfgNode, givesUpCfgNode.SyntaxNodes.First(),
                    givesUpCfgNode, originalMachine, model, trace);

                if (DataFlowQuerying.FlowsIntoTarget(assignment.Left, target, syntaxNode,
                    cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode,
                    model, this.AnalysisContext))
                {
                    rightSymbols = returnSymbols;
                    if (rightSymbols.Count == 0 && leftSymbol != null)
                    {
                        rightSymbols.Add(leftSymbol);
                    }
                }
            }
            else if (assignment.Right is ObjectCreationExpressionSyntax)
            {
                var objCreation = assignment.Right as ObjectCreationExpressionSyntax;
                trace.InsertCall(cfgNode.GetMethodSummary().Method, objCreation);
                var returnSymbols = this.DetectGivenUpFieldOwnershipInObjectCreation(
                    objCreation, target, syntaxNode, cfgNode, givesUpCfgNode.SyntaxNodes.First(),
                    givesUpCfgNode, model, trace);

                if (DataFlowQuerying.FlowsIntoTarget(assignment.Left, target, syntaxNode,
                    cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode,
                    model, this.AnalysisContext))
                {
                    rightSymbols = returnSymbols;
                    if (rightSymbols.Count == 0 && leftSymbol != null)
                    {
                        rightSymbols.Add(leftSymbol);
                    }
                }
            }

            foreach (var rightSymbol in rightSymbols)
            {
                if (target.Kind == SymbolKind.Field &&
                    rightSymbol.Equals(leftSymbol))
                {
                    return;
                }

                var rightDef = SymbolFinder.FindSourceDefinitionAsync(rightSymbol,
                    this.AnalysisContext.Solution).Result;
                var rightType = model.GetTypeInfo(assignment.Right).Type;
                if (rightDef != null && rightDef.Kind == SymbolKind.Field &&
                    this.AnalysisContext.DoesFieldBelongToMachine(rightDef, cfgNode.GetMethodSummary()) &&
                    !this.AnalysisContext.IsTypePassedByValueOrImmutable(rightType) &&
                    !this.AnalysisContext.IsExprEnum(assignment.Right, model) &&
                    !DataFlowQuerying.DoesResetInSuccessorControlFlowGraphNodes(
                        rightSymbol, target, syntaxNode, cfgNode) &&
                    this.IsFieldAccessedBeforeBeingReset(rightDef, cfgNode.GetMethodSummary()))
                {
                    TraceInfo newTrace = new TraceInfo();
                    newTrace.Merge(trace);
                    newTrace.AddErrorTrace(stmt.ToString(), stmt.SyntaxTree.FilePath, stmt.SyntaxTree.
                        GetLineSpan(stmt.Span).StartLinePosition.Line + 1);
                    AnalysisErrorReporter.ReportGivenUpFieldOwnershipError(newTrace);
                }

                if (leftSymbol != null && !rightSymbol.Equals(leftSymbol))
                {
                    if (DataFlowQuerying.FlowsIntoTarget(rightSymbol, target, syntaxNode,
                        cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode))
                    {
                        var leftDef = SymbolFinder.FindSourceDefinitionAsync(leftSymbol,
                            this.AnalysisContext.Solution).Result;
                        var leftType = model.GetTypeInfo(assignment.Left).Type;
                        if (leftDef != null && leftDef.Kind == SymbolKind.Field &&
                            !this.AnalysisContext.IsTypePassedByValueOrImmutable(leftType) &&
                            !this.AnalysisContext.IsExprEnum(assignment.Left, model) &&
                            !DataFlowQuerying.DoesResetInSuccessorControlFlowGraphNodes(
                                leftSymbol, target, syntaxNode, cfgNode) &&
                            this.IsFieldAccessedBeforeBeingReset(leftDef, cfgNode.GetMethodSummary()))
                        {
                            TraceInfo newTrace = new TraceInfo();
                            newTrace.Merge(trace);
                            newTrace.AddErrorTrace(stmt.ToString(), stmt.SyntaxTree.FilePath, stmt.SyntaxTree.
                                GetLineSpan(stmt.Span).StartLinePosition.Line + 1);
                            AnalysisErrorReporter.ReportGivenUpFieldOwnershipError(newTrace);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the summary of the given object creation to find if it gives up ownership
        /// of data from a machine field.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="givesUpSyntaxNode">Gives up syntaxNode</param>
        /// <param name="givesUpCfgNode">Gives up controlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        /// <returns>Set of return symbols</returns>
        private HashSet<ISymbol> DetectGivenUpFieldOwnershipInObjectCreation(ObjectCreationExpressionSyntax call,
            ISymbol target, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, SyntaxNode givesUpSyntaxNode,
            PSharpCFGNode givesUpCfgNode, SemanticModel model, TraceInfo trace)
        {
            TraceInfo callTrace = new TraceInfo();
            callTrace.Merge(trace);
            callTrace.AddErrorTrace(call.ToString(), call.SyntaxTree.FilePath, call.SyntaxTree.
                GetLineSpan(call.Span).StartLinePosition.Line + 1);

            var callSymbol = model.GetSymbolInfo(call).Symbol;
            if (callSymbol == null)
            {
                if (call.ArgumentList != null && call.ArgumentList.Arguments.Count > 0)
                {
                    AnalysisErrorReporter.ReportUnknownInvocation(callTrace);
                }
                
                return new HashSet<ISymbol>();
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol,
                this.AnalysisContext.Solution).Result;
            if (definition == null || definition.DeclaringSyntaxReferences.IsEmpty)
            {
                if (call.ArgumentList != null && call.ArgumentList.Arguments.Count > 0)
                {
                    AnalysisErrorReporter.ReportUnknownInvocation(callTrace);
                }

                return new HashSet<ISymbol>();
            }

            var constructorCall = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as ConstructorDeclarationSyntax;
            var constructorSummary = PSharpMethodSummary.Create(this.AnalysisContext, constructorCall);
            var arguments = call.ArgumentList.Arguments;

            for (int idx = 0; idx < arguments.Count; idx++)
            {
                if (DataFlowQuerying.FlowsIntoTarget(arguments[idx].Expression, target,
                    syntaxNode, cfgNode, givesUpSyntaxNode, givesUpCfgNode,
                    model, this.AnalysisContext))
                {
                    if (constructorSummary.SideEffects.Any(v => v.Value.Contains(idx) &&
                        this.AnalysisContext.DoesFieldBelongToMachine(v.Key, cfgNode.GetMethodSummary()) &&
                        this.IsFieldAccessedBeforeBeingReset(v.Key, cfgNode.GetMethodSummary())))
                    {
                        AnalysisErrorReporter.ReportGivenUpFieldOwnershipError(callTrace);
                    }
                }
            }

            return constructorSummary.GetResolvedReturnSymbols(call.ArgumentList, model);
        }

        /// <summary>
        /// Analyzes the summary of the given invocation to find if it gives up ownership
        /// of data from a machine field.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="givesUpSyntaxNode">Gives up syntaxNode</param>
        /// <param name="givesUpCfgNode">Gives up controlFlowGraphNode</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        /// <returns>Set of return symbols</returns>
        private HashSet<ISymbol> DetectGivenUpFieldOwnershipInInvocation(InvocationExpressionSyntax call,
            ISymbol target, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, SyntaxNode givesUpSyntaxNode,
            PSharpCFGNode givesUpCfgNode, StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            TraceInfo callTrace = new TraceInfo();
            callTrace.Merge(trace);
            callTrace.AddErrorTrace(call.ToString(), call.SyntaxTree.FilePath, call.SyntaxTree.
                GetLineSpan(call.Span).StartLinePosition.Line + 1);

            var callSymbol = model.GetSymbolInfo(call).Symbol;
            if (callSymbol.ContainingType.ToString().Equals("Microsoft.PSharp.Machine"))
            {
                return new HashSet<ISymbol>();
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol,
                this.AnalysisContext.Solution).Result;
            if (definition == null || definition.DeclaringSyntaxReferences.IsEmpty)
            {
                AnalysisErrorReporter.ReportUnknownInvocation(callTrace);
                return new HashSet<ISymbol>();
            }

            HashSet<MethodDeclarationSyntax> potentialCalls = new HashSet<MethodDeclarationSyntax>();
            var invocationCall = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as MethodDeclarationSyntax;
            if ((invocationCall.Modifiers.Any(SyntaxKind.AbstractKeyword) &&
                !originalMachine.Declaration.Modifiers.Any(SyntaxKind.AbstractKeyword)) ||
                invocationCall.Modifiers.Any(SyntaxKind.VirtualKeyword) ||
                invocationCall.Modifiers.Any(SyntaxKind.OverrideKeyword))
            {
                HashSet<MethodDeclarationSyntax> overriders = null;
                if (!VirtualDispatchQuerying.TryGetPotentialMethodOverriders(out overriders, call, syntaxNode,
                    cfgNode, originalMachine.Declaration, model, this.AnalysisContext))
                {
                    AnalysisErrorReporter.ReportUnknownVirtualCall(callTrace);
                }

                foreach (var overrider in overriders)
                {
                    potentialCalls.Add(overrider);
                }
            }

            if (potentialCalls.Count == 0)
            {
                potentialCalls.Add(invocationCall);
            }

            HashSet<ISymbol> potentialReturnSymbols = new HashSet<ISymbol>();
            foreach (var potentialCall in potentialCalls)
            {
                var invocationSummary = PSharpMethodSummary.Create(this.AnalysisContext, potentialCall);
                var arguments = call.ArgumentList.Arguments;

                for (int idx = 0; idx < arguments.Count; idx++)
                {
                    if (DataFlowQuerying.FlowsIntoTarget(arguments[idx].Expression, target,
                        syntaxNode, cfgNode, givesUpSyntaxNode, givesUpCfgNode,
                        model, this.AnalysisContext))
                    {
                        if (invocationSummary.SideEffects.Any(v => v.Value.Contains(idx) &&
                            this.AnalysisContext.DoesFieldBelongToMachine(v.Key, cfgNode.GetMethodSummary()) &&
                            this.IsFieldAccessedBeforeBeingReset(v.Key, cfgNode.GetMethodSummary())))
                        {
                            AnalysisErrorReporter.ReportGivenUpFieldOwnershipError(callTrace);
                        }
                    }
                }

                var resolvedReturnSymbols = invocationSummary.GetResolvedReturnSymbols(
                    call.ArgumentList, model);
                foreach (var rrs in resolvedReturnSymbols)
                {
                    potentialReturnSymbols.Add(rrs);
                }
            }

            return potentialReturnSymbols;
        }

        #endregion

        #region successor analysis methods

        /// <summary>
        /// Analyzes the given control-flow graph node to find if it respects the
        /// given-up ownerships and reports any potential data races.
        /// </summary>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="target">Target</param>
        /// <param name="giveUpSource">Give up source</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void DetectPotentialDataRacesInCFG(PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            ISymbol target, InvocationExpressionSyntax giveUpSource, HashSet<ControlFlowGraphNode> visited,
            StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            if (!cfgNode.IsJumpNode && !cfgNode.IsLoopHeadNode &&
                visited.Contains(givesUpCfgNode))
            {
                foreach (var syntaxNode in cfgNode.SyntaxNodes)
                {
                    var stmt = syntaxNode as StatementSyntax;
                    var localDecl = stmt.DescendantNodesAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
                    var expr = stmt.DescendantNodesAndSelf().OfType<ExpressionStatementSyntax>().FirstOrDefault();

                    if (localDecl != null)
                    {
                        var varDecl = localDecl.Declaration;
                        this.DetectPotentialDataRacesInLocalDeclaration(varDecl, stmt, syntaxNode, cfgNode,
                            givesUpCfgNode, target, originalMachine, model, trace);
                    }
                    else if (expr != null)
                    {
                        if (expr.Expression is AssignmentExpressionSyntax)
                        {
                            var assignment = expr.Expression as AssignmentExpressionSyntax;
                            this.DetectPotentialDataRacesInAssignmentExpression(assignment, stmt, syntaxNode,
                                cfgNode, givesUpCfgNode, target, originalMachine, model, trace);
                        }
                        else if (expr.Expression is InvocationExpressionSyntax)
                        {
                            var invocation = expr.Expression as InvocationExpressionSyntax;
                            trace.InsertCall(cfgNode.GetMethodSummary().Method, invocation);
                            this.DetectPotentialDataRaceInInvocation(invocation, target, syntaxNode,
                                cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode,
                                originalMachine, model, trace);
                        }
                    }
                }
            }

            if (!visited.Contains(cfgNode))
            {
                visited.Add(cfgNode);
                foreach (var successor in cfgNode.GetImmediateSuccessors())
                {
                    this.DetectPotentialDataRacesInCFG(successor, givesUpCfgNode, target,
                        giveUpSource, visited, originalMachine, model, trace);
                }
            }
        }

        /// <summary>
        /// Analyzes the given variable declaration to find if it respects the
        /// given-up ownerships and reports any potential data races.
        /// </summary>
        /// <param name="varDecl">VariableDeclarationSyntax</param>
        /// <param name="stmt">StatementSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="target">Target</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void DetectPotentialDataRacesInLocalDeclaration(VariableDeclarationSyntax varDecl,
            StatementSyntax stmt, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            ISymbol target, StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            foreach (var variable in varDecl.Variables.Where(v => v.Initializer != null))
            {
                this.DetectPotentialDataRacesInRightExpression(variable.Initializer.Value, stmt, syntaxNode,
                    cfgNode, givesUpCfgNode, target, originalMachine, model, trace);
            }
        }

        /// <summary>
        /// Analyzes the given assignment expression to find if it respects the
        /// given-up ownerships and reports any potential data races.
        /// </summary>
        /// <param name="assignment">AssignmentExpressionSyntax</param>
        /// <param name="stmt">StatementSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="target">Target</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void DetectPotentialDataRacesInAssignmentExpression(AssignmentExpressionSyntax assignment,
            StatementSyntax stmt, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            ISymbol target, StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            if (this.IsPayloadIllegallyAccessed(assignment, stmt, model, trace))
            {
                return;
            }

            ISymbol leftSymbol = model.GetSymbolInfo(assignment.Left).Symbol;
            if (assignment.Right is IdentifierNameSyntax &&
                DataFlowQuerying.FlowsFromTarget(assignment.Right, target, syntaxNode, cfgNode,
                givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model, this.AnalysisContext))
            {
                var type = model.GetTypeInfo(assignment.Right).Type;
                var fieldSymbol = SymbolFinder.FindSourceDefinitionAsync(leftSymbol,
                    this.AnalysisContext.Solution).Result as IFieldSymbol;
                if (fieldSymbol != null && fieldSymbol.Kind == SymbolKind.Field &&
                    this.AnalysisContext.DoesFieldBelongToMachine(fieldSymbol, cfgNode.GetMethodSummary()) &&
                    this.IsFieldAccessedBeforeBeingReset(fieldSymbol, cfgNode.GetMethodSummary()) &&
                    !this.AnalysisContext.IsTypePassedByValueOrImmutable(type) &&
                    !this.AnalysisContext.IsExprEnum(assignment.Right, model))
                {
                    TraceInfo newTrace = new TraceInfo();
                    newTrace.Merge(trace);
                    newTrace.AddErrorTrace(stmt.ToString(), stmt.SyntaxTree.FilePath, stmt.SyntaxTree.
                        GetLineSpan(stmt.Span).StartLinePosition.Line + 1);
                    AnalysisErrorReporter.ReportGivenUpOwnershipFieldAssignment(newTrace, fieldSymbol);
                }

                return;
            }
            else if (assignment.Right is MemberAccessExpressionSyntax ||
                assignment.Right is InvocationExpressionSyntax ||
                assignment.Right is ObjectCreationExpressionSyntax)
            {
                this.DetectPotentialDataRacesInRightExpression(assignment.Right, stmt, syntaxNode,
                    cfgNode, givesUpCfgNode, target, originalMachine, model, trace);
            }

            if (assignment.Left is MemberAccessExpressionSyntax)
            {
                if (!DataFlowQuerying.DoesResetInControlFlowGraphNode(leftSymbol, syntaxNode, cfgNode) &&
                    DataFlowQuerying.FlowsFromTarget(assignment.Left, target, syntaxNode, cfgNode,
                    givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model, this.AnalysisContext))
                {
                    TraceInfo newTrace = new TraceInfo();
                    newTrace.Merge(trace);
                    newTrace.AddErrorTrace(stmt.ToString(), stmt.SyntaxTree.FilePath, stmt.SyntaxTree.
                        GetLineSpan(stmt.Span).StartLinePosition.Line + 1);
                    AnalysisErrorReporter.ReportPotentialDataRace(newTrace);
                }
            }
        }

        /// <summary>
        /// Analyzes the given right-hand side expression to find if it respects
        /// the given-up ownerships and reports any potential data races.
        /// </summary>
        /// <param name="expr">ExpressionSyntax</param>
        /// <param name="stmt">StatementSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="target">Target</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void DetectPotentialDataRacesInRightExpression(ExpressionSyntax expr, StatementSyntax stmt,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode, ISymbol target,
            StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            if (expr is MemberAccessExpressionSyntax)
            {
                var access = expr as MemberAccessExpressionSyntax;
                if (DataFlowQuerying.FlowsFromTarget(access, target, syntaxNode, cfgNode,
                    givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model, this.AnalysisContext))
                {
                    TraceInfo newTrace = new TraceInfo();
                    newTrace.Merge(trace);
                    newTrace.AddErrorTrace(stmt.ToString(), stmt.SyntaxTree.FilePath, stmt.SyntaxTree.
                        GetLineSpan(stmt.Span).StartLinePosition.Line + 1);
                    AnalysisErrorReporter.ReportPotentialDataRace(newTrace);
                }
            }
            else if (expr is InvocationExpressionSyntax)
            {
                var invocation = expr as InvocationExpressionSyntax;
                trace.InsertCall(cfgNode.GetMethodSummary().Method, invocation);
                this.DetectPotentialDataRaceInInvocation(invocation, target, syntaxNode,
                    cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode,
                    originalMachine, model, trace);
            }
            else if (expr is ObjectCreationExpressionSyntax)
            {
                var objCreation = expr as ObjectCreationExpressionSyntax;
                trace.InsertCall(cfgNode.GetMethodSummary().Method, objCreation);
                this.DetectPotentialDataRaceInObjectCreation(objCreation, target, syntaxNode,
                    cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model, trace);
            }
        }

        /// <summary>
        /// Analyzes the summary of the given invocation to find if it respects the
        /// given-up ownerships and reports any potential data races.
        /// </summary>
        /// <param name="invocation">Invocation</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="givesUpSyntaxNode">Gives up syntaxNode</param>
        /// <param name="givesUpCfgNode">Gives up controlFlowGraphNode</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void DetectPotentialDataRaceInInvocation(InvocationExpressionSyntax call, ISymbol target,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, SyntaxNode givesUpSyntaxNode, PSharpCFGNode givesUpCfgNode,
            StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            TraceInfo callTrace = new TraceInfo();
            callTrace.Merge(trace);
            callTrace.AddErrorTrace(call.ToString(), call.SyntaxTree.FilePath, call.SyntaxTree.
                GetLineSpan(call.Span).StartLinePosition.Line + 1);
            
            var callSymbol = model.GetSymbolInfo(call).Symbol;
            if (callSymbol.ContainingType.ToString().Equals("Microsoft.PSharp.Machine"))
            {
                this.DetectPotentialDataRaceInGivesUpOperation(call, target,
                    syntaxNode, cfgNode, givesUpSyntaxNode, givesUpCfgNode, model, callTrace);
                return;
            }
            
            var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol,
                this.AnalysisContext.Solution).Result;
            if (definition == null || definition.DeclaringSyntaxReferences.IsEmpty)
            {
                if (call.Expression is MemberAccessExpressionSyntax)
                {
                    var callee = (call.Expression as MemberAccessExpressionSyntax).Expression;
                    if (DataFlowQuerying.FlowsFromTarget(callee, target, syntaxNode, cfgNode,
                        givesUpSyntaxNode, givesUpCfgNode, model, this.AnalysisContext) &&
                        !DataFlowQuerying.DoesResetInLoop(callee, syntaxNode, cfgNode,
                        givesUpSyntaxNode, givesUpCfgNode, model, this.AnalysisContext))
                    {
                        var typeSymbol = model.GetTypeInfo(callee).Type;
                        if (typeSymbol.ContainingNamespace.ToString().Equals("System.Collections.Generic"))
                        {
                            TraceInfo newTrace = new TraceInfo();
                            newTrace.Merge(callTrace);
                            newTrace.AddErrorTrace(callee.ToString(), callee.SyntaxTree.FilePath, callee.SyntaxTree.
                                GetLineSpan(callee.Span).StartLinePosition.Line + 1);
                            AnalysisErrorReporter.ReportPotentialDataRace(newTrace);
                            return;
                        }
                    }
                }

                AnalysisErrorReporter.ReportUnknownInvocation(callTrace);
                return;
            }

            HashSet<MethodDeclarationSyntax> potentialCalls = new HashSet<MethodDeclarationSyntax>();
            var invocationCall = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as MethodDeclarationSyntax;
            if ((invocationCall.Modifiers.Any(SyntaxKind.AbstractKeyword) &&
                !originalMachine.Declaration.Modifiers.Any(SyntaxKind.AbstractKeyword)) ||
                invocationCall.Modifiers.Any(SyntaxKind.VirtualKeyword) ||
                invocationCall.Modifiers.Any(SyntaxKind.OverrideKeyword))
            {
                HashSet<MethodDeclarationSyntax> overriders = null;
                if (!VirtualDispatchQuerying.TryGetPotentialMethodOverriders(out overriders,
                    call, syntaxNode, cfgNode, originalMachine.Declaration, model, this.AnalysisContext))
                {
                    AnalysisErrorReporter.ReportUnknownVirtualCall(callTrace);
                }

                foreach (var overrider in overriders)
                {
                    potentialCalls.Add(overrider);
                }
            }

            if (potentialCalls.Count == 0)
            {
                potentialCalls.Add(invocationCall);
            }

            foreach (var potentialCall in potentialCalls)
            {
                var invocationSummary = PSharpMethodSummary.Create(this.AnalysisContext, potentialCall);
                var arguments = call.ArgumentList.Arguments;

                for (int idx = 0; idx < arguments.Count; idx++)
                {
                    if (!this.AnalysisContext.IsExprEnum(arguments[idx].Expression, model) &&
                        DataFlowQuerying.FlowsFromTarget(arguments[idx].Expression, target, syntaxNode,
                        cfgNode, givesUpSyntaxNode, givesUpCfgNode, model, this.AnalysisContext) &&
                        !DataFlowQuerying.DoesResetInLoop(arguments[idx].Expression, syntaxNode, cfgNode,
                        givesUpSyntaxNode, givesUpCfgNode, model, this.AnalysisContext))
                    {
                        if (invocationSummary.ParameterAccessSet.ContainsKey(idx))
                        {
                            foreach (var access in invocationSummary.ParameterAccessSet[idx])
                            {
                                TraceInfo newTrace = new TraceInfo();
                                newTrace.Merge(callTrace);
                                newTrace.AddErrorTrace(access.ToString(), access.SyntaxTree.FilePath, access.SyntaxTree.
                                    GetLineSpan(access.Span).StartLinePosition.Line + 1);
                                AnalysisErrorReporter.ReportPotentialDataRace(newTrace);
                            }
                        }

                        var fieldSymbols = invocationSummary.SideEffects.
                            Where(v => v.Value.Contains(idx)).Select(v => v.Key);
                        foreach (var fieldSymbol in fieldSymbols)
                        {
                            if (this.AnalysisContext.DoesFieldBelongToMachine(fieldSymbol, cfgNode.GetMethodSummary()))
                            {
                                if (this.IsFieldAccessedBeforeBeingReset(fieldSymbol, cfgNode.GetMethodSummary()))
                                {
                                    AnalysisErrorReporter.ReportGivenUpOwnershipFieldAssignment(callTrace, fieldSymbol);
                                }
                            }
                            else
                            {
                                AnalysisErrorReporter.ReportGivenUpOwnershipFieldAssignment(callTrace, fieldSymbol);
                            }
                        }

                        if (invocationSummary.GivesUpSet.Contains(idx))
                        {
                            AnalysisErrorReporter.ReportGivenUpOwnershipSending(callTrace);
                        }
                    }
                }

                foreach (var fieldAccess in invocationSummary.FieldAccessSet)
                {
                    if (DataFlowQuerying.FlowsFromTarget(fieldAccess.Key, target, syntaxNode,
                        cfgNode, givesUpSyntaxNode, givesUpCfgNode))
                    {
                        foreach (var access in fieldAccess.Value)
                        {
                            TraceInfo newTrace = new TraceInfo();
                            newTrace.Merge(callTrace);
                            newTrace.AddErrorTrace(access.ToString(), access.SyntaxTree.FilePath, access.SyntaxTree.
                                GetLineSpan(access.Span).StartLinePosition.Line + 1);
                            AnalysisErrorReporter.ReportPotentialDataRace(newTrace);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the summary of the given object creation to find if it respects the
        /// given-up ownerships and reports any potential data races.
        /// </summary>
        /// <param name="invocation">Invocation</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="givesUpSyntaxNode">Gives up syntaxNode</param>
        /// <param name="givesUpCfgNode">Gives up controlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void DetectPotentialDataRaceInObjectCreation(ObjectCreationExpressionSyntax call,
            ISymbol target, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, SyntaxNode givesUpSyntaxNode,
            PSharpCFGNode givesUpCfgNode, SemanticModel model, TraceInfo trace)
        {
            TraceInfo callTrace = new TraceInfo();
            callTrace.Merge(trace);
            callTrace.AddErrorTrace(call.ToString(), call.SyntaxTree.FilePath, call.SyntaxTree.
                GetLineSpan(call.Span).StartLinePosition.Line + 1);

            var callSymbol = model.GetSymbolInfo(call).Symbol;
            var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol,
                this.AnalysisContext.Solution).Result;
            if (definition == null || definition.DeclaringSyntaxReferences.IsEmpty)
            {
                AnalysisErrorReporter.ReportUnknownInvocation(callTrace);
                return;
            }

            var constructorCall = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as ConstructorDeclarationSyntax;
            var constructorSummary = PSharpMethodSummary.Create(this.AnalysisContext, constructorCall);
            var arguments = call.ArgumentList.Arguments;

            for (int idx = 0; idx < arguments.Count; idx++)
            {
                if (!this.AnalysisContext.IsExprEnum(arguments[idx].Expression, model) &&
                    DataFlowQuerying.FlowsFromTarget(arguments[idx].Expression, target, syntaxNode,
                    cfgNode, givesUpSyntaxNode, givesUpCfgNode, model, this.AnalysisContext) &&
                    !DataFlowQuerying.DoesResetInLoop(arguments[idx].Expression, syntaxNode, cfgNode,
                    givesUpSyntaxNode, givesUpCfgNode, model, this.AnalysisContext))
                {
                    if (constructorSummary.ParameterAccessSet.ContainsKey(idx))
                    {
                        foreach (var access in constructorSummary.ParameterAccessSet[idx])
                        {
                            TraceInfo newTrace = new TraceInfo();
                            newTrace.Merge(callTrace);
                            newTrace.AddErrorTrace(access.ToString(), access.SyntaxTree.FilePath, access.SyntaxTree.
                                GetLineSpan(access.Span).StartLinePosition.Line + 1);
                            AnalysisErrorReporter.ReportPotentialDataRace(newTrace);
                        }
                    }

                    var fieldSymbols = constructorSummary.SideEffects.
                            Where(v => v.Value.Contains(idx)).Select(v => v.Key);
                    foreach (var fieldSymbol in fieldSymbols)
                    {
                        if (this.AnalysisContext.DoesFieldBelongToMachine(fieldSymbol, cfgNode.GetMethodSummary()))
                        {
                            if (this.IsFieldAccessedBeforeBeingReset(fieldSymbol, cfgNode.GetMethodSummary()))
                            {
                                AnalysisErrorReporter.ReportGivenUpOwnershipFieldAssignment(callTrace, fieldSymbol);
                            }
                        }
                        else
                        {
                            AnalysisErrorReporter.ReportGivenUpOwnershipFieldAssignment(callTrace, fieldSymbol);
                        }
                    }

                    if (constructorSummary.GivesUpSet.Contains(idx))
                    {
                        AnalysisErrorReporter.ReportGivenUpOwnershipSending(callTrace);
                    }
                }
            }

            foreach (var fieldAccess in constructorSummary.FieldAccessSet)
            {
                if (DataFlowQuerying.FlowsFromTarget(fieldAccess.Key, target, syntaxNode,
                    cfgNode, givesUpSyntaxNode, givesUpCfgNode))
                {
                    foreach (var access in fieldAccess.Value)
                    {
                        TraceInfo newTrace = new TraceInfo();
                        newTrace.Merge(callTrace);
                        newTrace.AddErrorTrace(access.ToString(), access.SyntaxTree.FilePath, access.SyntaxTree.
                            GetLineSpan(access.Span).StartLinePosition.Line + 1);
                        AnalysisErrorReporter.ReportPotentialDataRace(newTrace);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the arguments of the gives up operation to find if it respects
        /// the given-up ownerships and reports any potential data races.
        /// </summary>
        /// <param name="operation">Send-related operation</param>
        /// <param name="target">Target</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="givesUpSyntaxNode">Gives up syntaxNode</param>
        /// <param name="givesUpCfgNode">Gives up controlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void DetectPotentialDataRaceInGivesUpOperation(InvocationExpressionSyntax operation,
            ISymbol target, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, SyntaxNode givesUpSyntaxNode,
            PSharpCFGNode givesUpCfgNode, SemanticModel model, TraceInfo trace)
        {
            List<ExpressionSyntax> arguments = new List<ExpressionSyntax>();
            var opSymbol = model.GetSymbolInfo(operation).Symbol;

            if (opSymbol.ContainingType.ToString().Equals("Microsoft.PSharp.Machine") &&
                opSymbol.Name.Equals("Send"))
            {
                var expr = operation.ArgumentList.Arguments[1].Expression;
                if (expr is ObjectCreationExpressionSyntax)
                {
                    var objCreation = expr as ObjectCreationExpressionSyntax;
                    foreach (var arg in objCreation.ArgumentList.Arguments)
                    {
                        arguments.Add(arg.Expression);
                    }
                }
                else if (expr is BinaryExpressionSyntax && expr.IsKind(SyntaxKind.AsExpression))
                {
                    var binExpr = expr as BinaryExpressionSyntax;
                    if ((binExpr.Left is IdentifierNameSyntax) ||
                        (binExpr.Left is MemberAccessExpressionSyntax))
                    {
                        arguments.Add(binExpr.Left);
                    }
                    else if (binExpr.Left is InvocationExpressionSyntax)
                    {
                        var invocation = binExpr.Left as InvocationExpressionSyntax;
                        for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                        {
                            arguments.Add(invocation.ArgumentList.Arguments[i].Expression);
                        }
                    }
                }
            }
            else if (opSymbol.ContainingType.ToString().Equals("Microsoft.PSharp.Machine") &&
                opSymbol.Name.Equals("CreateMachine"))
            {
                if (operation.ArgumentList.Arguments.Count != 2)
                {
                    return;
                }

                var expr = operation.ArgumentList.Arguments[1].Expression;
                if (expr is ObjectCreationExpressionSyntax)
                {
                    var objCreation = expr
                        as ObjectCreationExpressionSyntax;
                    foreach (var arg in objCreation.ArgumentList.Arguments)
                    {
                        arguments.Add(arg.Expression);
                    }
                }
                else if (expr is BinaryExpressionSyntax &&
                    expr.IsKind(SyntaxKind.AsExpression))
                {
                    var binExpr = expr
                        as BinaryExpressionSyntax;
                    if ((binExpr.Left is IdentifierNameSyntax) || (binExpr.Left is MemberAccessExpressionSyntax))
                    {
                        arguments.Add(binExpr.Left);
                    }
                    else if (binExpr.Left is InvocationExpressionSyntax)
                    {
                        var invocation = binExpr.Left as InvocationExpressionSyntax;
                        for (int i = 1; i < invocation.ArgumentList.Arguments.Count; i++)
                        {
                            arguments.Add(invocation.ArgumentList.Arguments[i].Expression);
                        }
                    }
                }
                else if ((expr is IdentifierNameSyntax) ||
                    (expr is MemberAccessExpressionSyntax))
                {
                    arguments.Add(expr);
                }
            }

            var extractedArgs = this.ExtractArguments(arguments);

            foreach (var arg in extractedArgs)
            {
                if (!this.AnalysisContext.IsExprEnum(arg, model) &&
                    DataFlowQuerying.FlowsFromTarget(arg, target, syntaxNode, cfgNode,
                    givesUpSyntaxNode, givesUpCfgNode, model, this.AnalysisContext) &&
                    !DataFlowQuerying.DoesResetInLoop(arg, syntaxNode, cfgNode,
                    givesUpSyntaxNode, givesUpCfgNode, model, this.AnalysisContext))
                {
                    AnalysisErrorReporter.ReportGivenUpOwnershipSending(trace);
                    return;
                }
            }
        }

        #endregion

        #region helper methods

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

                    if (shouldReportError && definition != null && definition.Kind == SymbolKind.Field &&
                        !this.AnalysisContext.IsTypePassedByValueOrImmutable(typeInfo.Type) &&
                        !this.AnalysisContext.IsExprEnum(arg, model) &&
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

        /// <summary>
        /// Returns true if the given field symbol is being accessed
        /// before being reset.
        /// </summary>
        /// <param name="field">Field</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean</returns>
        private bool IsFieldAccessedBeforeBeingReset(ISymbol field, MethodSummary summary)
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
        private bool IsFieldAccessedBeforeBeingReset(StateTransitionGraphNode node,
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
        /// Returns true and reports an error if the payload was illegally accessed.
        /// </summary>
        /// <param name="assignment">AssignmentExpressionSyntax</param>
        /// <param name="stmt">Statement</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        /// <returns></returns>
        private bool IsPayloadIllegallyAccessed(AssignmentExpressionSyntax assignment,
            StatementSyntax stmt, SemanticModel model, TraceInfo trace)
        {
            ISymbol payloadSymbol = null;
            if (assignment.Right is MemberAccessExpressionSyntax)
            {
                payloadSymbol = model.GetSymbolInfo((assignment.Right as MemberAccessExpressionSyntax).
                    Name).Symbol;
            }
            else if (assignment.Right is IdentifierNameSyntax)
            {
                payloadSymbol = model.GetSymbolInfo(assignment.Right).Symbol;
            }
            else
            {
                return false;
            }

            if (payloadSymbol != null &&
                payloadSymbol.ToString().Equals("Microsoft.PSharp.Machine.Payload"))
            {
                ISymbol leftSymbol = null;
                if (assignment.Left is IdentifierNameSyntax)
                {
                    leftSymbol = model.GetSymbolInfo(assignment.Left
                        as IdentifierNameSyntax).Symbol;
                }
                else if (assignment.Left is MemberAccessExpressionSyntax)
                {
                    leftSymbol = model.GetSymbolInfo((assignment.Left
                        as MemberAccessExpressionSyntax).Name).Symbol;
                }

                var leftDef = SymbolFinder.FindSourceDefinitionAsync(leftSymbol,
                    this.AnalysisContext.Solution).Result;
                if (leftDef != null && leftDef.Kind == SymbolKind.Field)
                {
                    TraceInfo newTrace = new TraceInfo();
                    newTrace.Merge(trace);
                    newTrace.AddErrorTrace(stmt.ToString(), stmt.SyntaxTree.FilePath, stmt.SyntaxTree.
                        GetLineSpan(stmt.Span).StartLinePosition.Line + 1);
                    AnalysisErrorReporter.ReportPayloadFieldAssignment(newTrace);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Extracts arguments from the given list of arguments.
        /// </summary>
        /// <param name="arguments">List of arguments</param>
        /// <returns>List of arguments</returns>
        private List<ExpressionSyntax> ExtractArguments(List<ExpressionSyntax> arguments)
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

        #endregion
    }
}
