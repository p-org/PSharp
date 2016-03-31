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

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis checks that all methods in each machine of a P#
    /// program respect given-up ownerships.
    /// </summary>
    internal sealed class RespectsOwnershipAnalysisPass : OwnershipAnalysisPass
    {
        #region internal API

        /// <summary>
        /// Creates a new respects ownership analysis pass.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <returns>RespectsOwnershipAnalysisPass</returns>
        internal static RespectsOwnershipAnalysisPass Create(PSharpAnalysisContext context)
        {
            return new RespectsOwnershipAnalysisPass(context);
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Analyzes the ownership of references in the given control-flow graph node.
        /// </summary>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="target">Target</param>
        /// <param name="giveUpSource">Give up source</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected override void AnalyzeOwnershipInCFG(PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
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
                        this.AnalyzeOwnershipInLocalDeclaration(varDecl, stmt, syntaxNode, cfgNode,
                            givesUpCfgNode, target, originalMachine, model, trace);
                    }
                    else if (expr != null)
                    {
                        if (expr.Expression is AssignmentExpressionSyntax)
                        {
                            var assignment = expr.Expression as AssignmentExpressionSyntax;
                            this.AnalyzeOwnershipInAssignmentExpression(assignment, stmt, syntaxNode,
                                cfgNode, givesUpCfgNode, target, originalMachine, model, trace);
                        }
                        else if (expr.Expression is InvocationExpressionSyntax ||
                            expr.Expression is ObjectCreationExpressionSyntax)
                        {
                            this.AnalyzeOwnershipInExpression(expr.Expression, stmt, syntaxNode,
                                cfgNode, givesUpCfgNode, target, originalMachine, model, trace);
                        }
                    }
                }
            }

            if (!visited.Contains(cfgNode))
            {
                visited.Add(cfgNode);
                foreach (var successor in cfgNode.GetImmediateSuccessors())
                {
                    this.AnalyzeOwnershipInCFG(successor, givesUpCfgNode, target, giveUpSource,
                        visited, originalMachine, model, trace);
                }
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
        /// Analyzes the ownership of references in the given variable declaration.
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
        private void AnalyzeOwnershipInLocalDeclaration(VariableDeclarationSyntax varDecl,
            StatementSyntax stmt, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            ISymbol target, StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            foreach (var variable in varDecl.Variables.Where(v => v.Initializer != null))
            {
                this.AnalyzeOwnershipInExpression(variable.Initializer.Value, stmt, syntaxNode,
                    cfgNode, givesUpCfgNode, target, originalMachine, model, trace);
            }
        }

        /// <summary>
        /// Analyzes the ownership of references in the given assignment expression.
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
        private void AnalyzeOwnershipInAssignmentExpression(AssignmentExpressionSyntax assignment,
            StatementSyntax stmt, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            ISymbol target, StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            if (base.IsPayloadIllegallyAccessed(assignment, stmt, model, trace))
            {
                return;
            }

            var leftIdentifier = this.AnalysisContext.GetTopLevelIdentifier(assignment.Left);
            ISymbol leftSymbol = model.GetSymbolInfo(leftIdentifier).Symbol;
            
            if (assignment.Right is IdentifierNameSyntax &&
                DataFlowQuerying.FlowsFromTarget(assignment.Right, target, syntaxNode, cfgNode,
                givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model, this.AnalysisContext))
            {
                var type = model.GetTypeInfo(assignment.Right).Type;
                var fieldSymbol = SymbolFinder.FindSourceDefinitionAsync(leftSymbol,
                    this.AnalysisContext.Solution).Result as IFieldSymbol;
                if (fieldSymbol != null && fieldSymbol.Kind == SymbolKind.Field &&
                    this.AnalysisContext.DoesFieldBelongToMachine(fieldSymbol, cfgNode.GetMethodSummary()) &&
                    base.IsFieldAccessedBeforeBeingReset(fieldSymbol, cfgNode.GetMethodSummary()) &&
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
                this.AnalyzeOwnershipInExpression(assignment.Right, stmt, syntaxNode,
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
        /// Analyzes the ownership of references in the given expression.
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
        private void AnalyzeOwnershipInExpression(ExpressionSyntax expr, StatementSyntax stmt,
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
        /// Analyzes the ownership of references in the given invocation.
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
            
            var calleeSymbol = model.GetSymbolInfo(call).Symbol;
            if (calleeSymbol.ContainingType.ToString().Equals("Microsoft.PSharp.Machine"))
            {
                this.DetectPotentialDataRaceInGivesUpOperation(call, target,
                    syntaxNode, cfgNode, givesUpSyntaxNode, givesUpCfgNode, model, callTrace);
                return;
            }
            
            var definition = SymbolFinder.FindSourceDefinitionAsync(calleeSymbol,
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
            
            HashSet<MethodDeclarationSyntax> potentialCallees = new HashSet<MethodDeclarationSyntax>();
            var invocationCallee = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as MethodDeclarationSyntax;
            if ((invocationCallee.Modifiers.Any(SyntaxKind.AbstractKeyword) &&
                !originalMachine.Declaration.Modifiers.Any(SyntaxKind.AbstractKeyword)) ||
                invocationCallee.Modifiers.Any(SyntaxKind.VirtualKeyword) ||
                invocationCallee.Modifiers.Any(SyntaxKind.OverrideKeyword))
            {
                HashSet<MethodDeclarationSyntax> overriders = null;
                if (!DataFlowQuerying.TryGetPotentialMethodOverriders(out overriders, call, syntaxNode,
                    cfgNode, originalMachine.Declaration, model, this.AnalysisContext))
                {
                    AnalysisErrorReporter.ReportUnknownVirtualCall(callTrace);
                }

                foreach (var overrider in overriders)
                {
                    potentialCallees.Add(overrider);
                }
            }

            if (potentialCallees.Count == 0)
            {
                potentialCallees.Add(invocationCallee);
            }

            foreach (var potentialCallee in potentialCallees)
            {
                var invocationSummary = PSharpMethodSummary.Create(this.AnalysisContext, potentialCallee);
                var arguments = call.ArgumentList.Arguments;
                invocationSummary.PrintDataFlowInformation();
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
                        else
                        {
                            Console.WriteLine(invocationSummary.Method);
                            var paramSymbol = model.GetDeclaredSymbol(invocationSummary.Method.ParameterList.Parameters[idx]);
                            Console.WriteLine("flows arg: " + paramSymbol);
                            this.AnalyzeOwnershipInCFG(invocationSummary.EntryNode as PSharpCFGNode, givesUpCfgNode,
                                paramSymbol, call, new HashSet<ControlFlowGraphNode>() { givesUpCfgNode },
                                originalMachine, model, callTrace);
                        }

                        var fieldSymbols = invocationSummary.SideEffects.Where(v => v.Value.Contains(idx)).Select(v => v.Key);
                        foreach (var fieldSymbol in fieldSymbols)
                        {
                            if (this.AnalysisContext.DoesFieldBelongToMachine(fieldSymbol, cfgNode.GetMethodSummary()))
                            {
                                if (base.IsFieldAccessedBeforeBeingReset(fieldSymbol, cfgNode.GetMethodSummary()))
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
        /// Analyzes the ownership of references in the given object creation.
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

            var calleeSymbol = model.GetSymbolInfo(call).Symbol;
            var definition = SymbolFinder.FindSourceDefinitionAsync(calleeSymbol,
                this.AnalysisContext.Solution).Result;
            if (definition == null || definition.DeclaringSyntaxReferences.IsEmpty)
            {
                AnalysisErrorReporter.ReportUnknownInvocation(callTrace);
                return;
            }

            var constructorCallee = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as ConstructorDeclarationSyntax;
            var constructorSummary = PSharpMethodSummary.Create(this.AnalysisContext, constructorCallee);
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
                            if (base.IsFieldAccessedBeforeBeingReset(fieldSymbol, cfgNode.GetMethodSummary()))
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
        /// Analyzes the ownership of references in the gives-up operation.
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

            var extractedArgs = base.ExtractArguments(arguments);

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
    }
}
