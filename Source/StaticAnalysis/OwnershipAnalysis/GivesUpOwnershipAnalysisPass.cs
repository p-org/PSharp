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
    /// This analysis checks if any method in each machine of a P# program
    /// is erroneously giving up ownership of references.
    /// </summary>
    internal sealed class GivesUpOwnershipAnalysisPass : OwnershipAnalysisPass
    {
        #region internal API

        /// <summary>
        /// Creates a new gives-up ownership analysis pass.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <returns>GivesUpOwnershipAnalysisPass</returns>
        internal static GivesUpOwnershipAnalysisPass Create(PSharpAnalysisContext context)
        {
            return new GivesUpOwnershipAnalysisPass(context);
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
                            this.AnalyzeOwnershipInAssignmentExpression(assignment, stmt,
                                syntaxNode, cfgNode, givesUpCfgNode, target, originalMachine, model, trace);
                        }
                        else if (expr.Expression is InvocationExpressionSyntax)
                        {
                            var invocation = expr.Expression as InvocationExpressionSyntax;
                            trace.InsertCall(cfgNode.GetMethodSummary().Method, invocation);
                            this.AnalyzeOwnershipInInvocation(invocation, target, syntaxNode,
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
                        this.AnalyzeOwnershipInCFG(predecessor, givesUpCfgNode, target,
                            giveUpSource, visited, originalMachine, model, trace);
                    }
                }
                else
                {
                    foreach (var successor in cfgNode.GetImmediateSuccessors())
                    {
                        this.AnalyzeOwnershipInCFG(successor, givesUpCfgNode, target,
                            giveUpSource, visited, originalMachine, model, trace);
                    }
                }
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
                this.AnalyzeOwnershipInVariableDeclaration(variable, syntaxNode,
                    cfgNode, givesUpCfgNode, target, originalMachine, model, trace);
            }
        }

        /// <summary>
        /// Analyzes the ownership of references in the given variable declaration.
        /// </summary>
        /// <param name="varDecl">VariableDeclarationSyntax</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="target">Target</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void AnalyzeOwnershipInVariableDeclaration(VariableDeclaratorSyntax variable,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            ISymbol target, StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            var expr = variable.Initializer.Value;

            var rightSymbols = new HashSet<ISymbol>();
            if (variable.Initializer.Value is IdentifierNameSyntax ||
                variable.Initializer.Value is MemberAccessExpressionSyntax)
            {
                if (DataFlowQuerying.FlowsIntoTarget(variable, target, syntaxNode, cfgNode,
                    givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model))
                {
                    IdentifierNameSyntax identifier = this.AnalysisContext.
                        GetTopLevelIdentifier(variable.Initializer.Value);

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
                var returnSymbols = this.AnalyzeOwnershipInInvocation(
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
                var returnSymbols = this.AnalyzeOwnershipInObjectCreation(
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
                        base.IsFieldAccessedBeforeBeingReset(rightDef, cfgNode.GetMethodSummary()))
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

            var rightSymbols = new HashSet<ISymbol>();
            if (assignment.Right is IdentifierNameSyntax ||
                assignment.Right is MemberAccessExpressionSyntax)
            {
                if (DataFlowQuerying.FlowsIntoTarget(assignment.Left, target, syntaxNode,
                    cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode,
                    model, this.AnalysisContext))
                {
                    IdentifierNameSyntax rightIdentifier = this.AnalysisContext.
                        GetTopLevelIdentifier(assignment.Right);

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
                var returnSymbols = this.AnalyzeOwnershipInInvocation(
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
                var returnSymbols = this.AnalyzeOwnershipInObjectCreation(
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
                    base.IsFieldAccessedBeforeBeingReset(rightDef, cfgNode.GetMethodSummary()))
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
                            base.IsFieldAccessedBeforeBeingReset(leftDef, cfgNode.GetMethodSummary()))
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
        /// Analyzes the ownership of references in the given object creation.
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
        private HashSet<ISymbol> AnalyzeOwnershipInObjectCreation(ObjectCreationExpressionSyntax call,
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
                        base.IsFieldAccessedBeforeBeingReset(v.Key, cfgNode.GetMethodSummary())))
                    {
                        AnalysisErrorReporter.ReportGivenUpFieldOwnershipError(callTrace);
                    }
                }
            }

            return constructorSummary.GetResolvedReturnSymbols(call.ArgumentList, model);
        }

        /// <summary>
        /// Analyzes the ownership of references in the given invocation.
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
        private HashSet<ISymbol> AnalyzeOwnershipInInvocation(InvocationExpressionSyntax call,
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
                if (!DataFlowQuerying.TryGetPotentialMethodOverriders(out overriders, call, syntaxNode,
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
                            base.IsFieldAccessedBeforeBeingReset(v.Key, cfgNode.GetMethodSummary())))
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
    }
}
