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
        /// <param name="target">Target</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="giveUpSource">Give up source</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected override void AnalyzeOwnershipInCFG(ISymbol target, PSharpCFGNode cfgNode,
            PSharpCFGNode givesUpCfgNode, InvocationExpressionSyntax giveUpSource,
            HashSet<ControlFlowGraphNode> visited, StateMachine originalMachine,
            SemanticModel model, TraceInfo trace)
        {
            if (!cfgNode.IsJumpNode && !cfgNode.IsLoopHeadNode &&
                visited.Contains(givesUpCfgNode))
            {
                this.AnalyzeOwnershipInCFG(target, cfgNode, givesUpCfgNode, originalMachine, model, trace);
            }

            if (!visited.Contains(cfgNode))
            {
                visited.Add(cfgNode);

                if (givesUpCfgNode != null)
                {
                    foreach (var predecessor in cfgNode.GetImmediatePredecessors())
                    {
                        this.AnalyzeOwnershipInCFG(target, predecessor, givesUpCfgNode,
                            giveUpSource, visited, originalMachine, model, trace);
                    }
                }
                else
                {
                    foreach (var successor in cfgNode.GetImmediateSuccessors())
                    {
                        this.AnalyzeOwnershipInCFG(target, successor, givesUpCfgNode,
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
        private void AnalyzeOwnershipInCFG(ISymbol target, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
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
                    else if (expr.Expression is InvocationExpressionSyntax)
                    {
                        var invocation = expr.Expression as InvocationExpressionSyntax;
                        trace.InsertCall(cfgNode.GetMethodSummary().Method, invocation);
                        this.AnalyzeOwnershipInInvocation(target, invocation, syntaxNode,
                            cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode,
                            originalMachine, model, trace);
                    }
                    else if (expr.Expression is ObjectCreationExpressionSyntax)
                    {
                        var objCreation = expr.Expression as ObjectCreationExpressionSyntax;
                        trace.InsertCall(cfgNode.GetMethodSummary().Method, objCreation);
                        var returnSymbols = this.AnalyzeOwnershipInObjectCreation(target,
                            objCreation, syntaxNode, cfgNode, givesUpCfgNode.SyntaxNodes.First(),
                            givesUpCfgNode, model, trace);
                    }
                }
            }
        }

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
        private void AnalyzeOwnershipInLocalDeclaration(ISymbol target, VariableDeclarationSyntax varDecl,
            StatementSyntax stmt, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            foreach (var variable in varDecl.Variables.Where(v => v.Initializer != null))
            {
                this.AnalyzeOwnershipInVariableDeclaration(target, variable, syntaxNode,
                    cfgNode, givesUpCfgNode, originalMachine, model, trace);
            }
        }

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
        private void AnalyzeOwnershipInAssignment(ISymbol target, AssignmentExpressionSyntax assignment,
            StatementSyntax stmt, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            if (base.IsPayloadIllegallyAccessed(assignment, stmt, model, trace))
            {
                return;
            }

            var leftIdentifier = DataFlowQuerying.GetTopLevelIdentifier(assignment.Left);
            ISymbol leftSymbol = model.GetSymbolInfo(leftIdentifier).Symbol;

            if (assignment.Right is IdentifierNameSyntax ||
                assignment.Right is MemberAccessExpressionSyntax)
            {
                if (DataFlowQuerying.FlowsIntoTarget(assignment.Left, target, syntaxNode,
                    cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model))
                {
                    IdentifierNameSyntax rightIdentifier = DataFlowQuerying.
                        GetTopLevelIdentifier(assignment.Right);

                    if (rightIdentifier != null)
                    {
                        var rightSymbol = model.GetSymbolInfo(rightIdentifier).Symbol;
                        this.AnalyzeFieldOwnershipInAssignment(leftSymbol, new HashSet<ISymbol> { rightSymbol },
                            target, assignment, stmt, syntaxNode, cfgNode, givesUpCfgNode, model, trace);
                    }
                }
            }
            else if (assignment.Right is InvocationExpressionSyntax ||
                assignment.Right is ObjectCreationExpressionSyntax)
            {
                HashSet<ISymbol> returnSymbols = null;
                if (assignment.Right is InvocationExpressionSyntax)
                {
                    var invocation = assignment.Right as InvocationExpressionSyntax;
                    trace.InsertCall(cfgNode.GetMethodSummary().Method, invocation);
                    returnSymbols = this.AnalyzeOwnershipInInvocation(target,
                        invocation, syntaxNode, cfgNode, givesUpCfgNode.SyntaxNodes.First(),
                        givesUpCfgNode, originalMachine, model, trace);
                }
                else if (assignment.Right is ObjectCreationExpressionSyntax)
                {
                    var objCreation = assignment.Right as ObjectCreationExpressionSyntax;
                    trace.InsertCall(cfgNode.GetMethodSummary().Method, objCreation);
                    returnSymbols = this.AnalyzeOwnershipInObjectCreation(target,
                        objCreation, syntaxNode, cfgNode, givesUpCfgNode.SyntaxNodes.First(),
                        givesUpCfgNode, model, trace);
                }

                if (DataFlowQuerying.FlowsIntoTarget(assignment.Left, target, syntaxNode,
                        cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model))
                {
                    this.AnalyzeFieldOwnershipInAssignment(leftSymbol, returnSymbols, target, assignment,
                        stmt, syntaxNode, cfgNode, givesUpCfgNode, model, trace);
                }
            }
        }

        /// <summary>
        /// Analyzes the ownership of references in the given variable declaration.
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="varDecl">VariableDeclarationSyntax</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void AnalyzeOwnershipInVariableDeclaration(ISymbol target, VariableDeclaratorSyntax variable,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            var expr = variable.Initializer.Value;

            if (expr is IdentifierNameSyntax ||
                expr is MemberAccessExpressionSyntax)
            {
                if (DataFlowQuerying.FlowsIntoTarget(variable, target, syntaxNode, cfgNode,
                    givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model))
                {
                    IdentifierNameSyntax identifier = DataFlowQuerying.GetTopLevelIdentifier(expr);
                    if (identifier != null)
                    {
                        var rightSymbol = model.GetSymbolInfo(identifier).Symbol;
                        this.AnalyzeFieldOwnershipInExpression(new HashSet<ISymbol> { rightSymbol },
                            target, expr, syntaxNode, cfgNode, model, trace);
                    }
                }
            }
            else if (expr is InvocationExpressionSyntax)
            {
                var invocation = expr as InvocationExpressionSyntax;
                trace.InsertCall(cfgNode.GetMethodSummary().Method, invocation);
                var returnSymbols = this.AnalyzeOwnershipInInvocation(target,
                    invocation, syntaxNode, cfgNode, givesUpCfgNode.SyntaxNodes.First(),
                    givesUpCfgNode, originalMachine, model, trace);

                if (DataFlowQuerying.FlowsIntoTarget(variable, target, syntaxNode, cfgNode,
                    givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model))
                {
                    this.AnalyzeFieldOwnershipInExpression(returnSymbols, target, expr,
                        syntaxNode, cfgNode, model, trace);
                }
            }
            else if (expr is ObjectCreationExpressionSyntax)
            {
                var objCreation = expr as ObjectCreationExpressionSyntax;
                trace.InsertCall(cfgNode.GetMethodSummary().Method, objCreation);
                var returnSymbols = this.AnalyzeOwnershipInObjectCreation(target,
                    objCreation, syntaxNode, cfgNode, givesUpCfgNode.SyntaxNodes.First(),
                    givesUpCfgNode, model, trace);

                if (DataFlowQuerying.FlowsIntoTarget(variable, target, syntaxNode, cfgNode,
                    givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model))
                {
                    this.AnalyzeFieldOwnershipInExpression(returnSymbols, target, expr,
                        syntaxNode, cfgNode, model, trace);
                }
            }
        }

        /// <summary>
        /// Analyzes the ownership of references in the given invocation.
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="call">Call</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="givesUpSyntaxNode">Gives up syntaxNode</param>
        /// <param name="givesUpCfgNode">Gives up controlFlowGraphNode</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        /// <returns>Set of return symbols</returns>
        private HashSet<ISymbol> AnalyzeOwnershipInInvocation(ISymbol target, InvocationExpressionSyntax call,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, SyntaxNode givesUpSyntaxNode,
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
                        syntaxNode, cfgNode, givesUpSyntaxNode, givesUpCfgNode, model))
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

        /// <summary>
        /// Analyzes the ownership of references in the given object creation.
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="call">Call</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="givesUpSyntaxNode">Gives up syntaxNode</param>
        /// <param name="givesUpCfgNode">Gives up controlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        /// <returns>Set of return symbols</returns>
        private HashSet<ISymbol> AnalyzeOwnershipInObjectCreation(ISymbol target, ObjectCreationExpressionSyntax call,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, SyntaxNode givesUpSyntaxNode,
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
                    syntaxNode, cfgNode, givesUpSyntaxNode, givesUpCfgNode, model))
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
        /// Analyzes the ownership of fields in the given assignment.
        /// </summary>
        /// <param name="leftSymbol">Left symbol</param>
        /// <param name="rightSymbols">Right symbols</param>
        /// <param name="target">Target</param>
        /// <param name="assignment">AssignmentExpressionSyntax</param>
        /// <param name="stmt">StatementSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void AnalyzeFieldOwnershipInAssignment(ISymbol leftSymbol, HashSet<ISymbol> rightSymbols,
            ISymbol target, AssignmentExpressionSyntax assignment, StatementSyntax stmt, SyntaxNode syntaxNode,
            PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode, SemanticModel model, TraceInfo trace)
        {
            foreach (var rightSymbol in rightSymbols)
            {
                if (target.Kind == SymbolKind.Field && rightSymbol.Equals(leftSymbol))
                {
                    return;
                }

                this.AnalyzeFieldOwnershipInExpression(rightSymbol, target, assignment.Right,
                        stmt, cfgNode, model, trace);

                if (leftSymbol != null && !rightSymbol.Equals(leftSymbol))
                {
                    if (DataFlowQuerying.FlowsIntoTarget(rightSymbol, target, syntaxNode,
                        cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode))
                    {
                        this.AnalyzeFieldOwnershipInExpression(leftSymbol, target, assignment.Left,
                            stmt, cfgNode, model, trace);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the ownership of fields in the given expression.
        /// </summary>
        /// <param name="fieldSymbols">Field symbols</param>
        /// <param name="target">Target</param>
        /// <param name="expr">ExpressionSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void AnalyzeFieldOwnershipInExpression(HashSet<ISymbol> fieldSymbols, ISymbol target, ExpressionSyntax expr,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, SemanticModel model, TraceInfo trace)
        {
            foreach (var symbol in fieldSymbols)
            {
                if (!symbol.Equals(target) && !expr.ToString().Equals(target.ToString()))
                {
                    this.AnalyzeFieldOwnershipInExpression(symbol, target, expr,
                        syntaxNode, cfgNode, model, trace);
                }
            }
        }

        /// <summary>
        /// Analyzes the ownership of fields in the given expression.
        /// </summary>
        /// <param name="fieldSymbol">Field symbol</param>
        /// <param name="target">Target</param>
        /// <param name="expr">ExpressionSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void AnalyzeFieldOwnershipInExpression(ISymbol fieldSymbol, ISymbol target, ExpressionSyntax expr,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, SemanticModel model, TraceInfo trace)
        {
            var definition = SymbolFinder.FindSourceDefinitionAsync(fieldSymbol,
                    this.AnalysisContext.Solution).Result;
            var type = model.GetTypeInfo(expr).Type;
            if (definition != null && definition.Kind == SymbolKind.Field &&
                this.AnalysisContext.DoesFieldBelongToMachine(definition, cfgNode.GetMethodSummary()) &&
                !this.AnalysisContext.IsTypePassedByValueOrImmutable(type) &&
                !this.AnalysisContext.IsExprEnum(expr, model) &&
                !DataFlowQuerying.DoesResetInSuccessorControlFlowGraphNodes(
                    fieldSymbol, target, syntaxNode, cfgNode) &&
                base.IsFieldAccessedBeforeBeingReset(definition, cfgNode.GetMethodSummary()))
            {
                TraceInfo newTrace = new TraceInfo();
                newTrace.Merge(trace);
                newTrace.AddErrorTrace(syntaxNode.ToString(), syntaxNode.SyntaxTree.FilePath,
                    syntaxNode.SyntaxTree.GetLineSpan(syntaxNode.Span).StartLinePosition.Line + 1);
                AnalysisErrorReporter.ReportGivenUpFieldOwnershipError(newTrace);
            }
        }

        #endregion 
    }
}
