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
                base.AnalyzeOwnershipInCFG(target, cfgNode, givesUpCfgNode, originalMachine, model, trace);
            }

            if (!visited.Contains(cfgNode))
            {
                visited.Add(cfgNode);
                foreach (var successor in cfgNode.GetImmediateSuccessors())
                {
                    this.AnalyzeOwnershipInCFG(target, successor, givesUpCfgNode, giveUpSource,
                        visited, originalMachine, model, trace);
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
        protected override void AnalyzeOwnershipInLocalDeclaration(ISymbol target, VariableDeclarationSyntax varDecl,
            StatementSyntax stmt, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            foreach (var variable in varDecl.Variables.Where(v => v.Initializer != null))
            {
                this.AnalyzeOwnershipInExpression(target, variable.Initializer.Value, stmt, syntaxNode,
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
        protected override void AnalyzeOwnershipInAssignment(ISymbol target, AssignmentExpressionSyntax assignment,
            StatementSyntax stmt, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            var leftIdentifier = CodeAnalysis.CSharp.DataFlowAnalysis.AnalysisContext.
                GetTopLevelIdentifier(assignment.Left);
            ISymbol leftSymbol = model.GetSymbolInfo(leftIdentifier).Symbol;

            if (assignment.Right is IdentifierNameSyntax &&
                DataFlowQuerying.FlowsFromTarget(assignment.Right, target, syntaxNode, cfgNode,
                givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model))
            {
                var type = model.GetTypeInfo(assignment.Right).Type;
                var fieldSymbol = SymbolFinder.FindSourceDefinitionAsync(leftSymbol,
                    this.AnalysisContext.Solution).Result as IFieldSymbol;
                if (fieldSymbol != null && fieldSymbol.Kind == SymbolKind.Field &&
                    this.AnalysisContext.DoesFieldBelongToMachine(fieldSymbol, cfgNode.GetMethodSummary()) &&
                    base.IsFieldAccessedBeforeBeingReset(fieldSymbol, cfgNode.GetMethodSummary()) &&
                    !this.AnalysisContext.IsTypePassedByValueOrImmutable(type) &&
                    !this.AnalysisContext.IsTypeEnum(type))
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
                this.AnalyzeOwnershipInExpression(target, assignment.Right, stmt, syntaxNode,
                    cfgNode, givesUpCfgNode, originalMachine, model, trace);
            }

            if (assignment.Left is MemberAccessExpressionSyntax)
            {
                if (!DataFlowQuerying.DoesResetInControlFlowGraphNode(leftSymbol, syntaxNode, cfgNode) &&
                    DataFlowQuerying.FlowsFromTarget(assignment.Left, target, syntaxNode, cfgNode,
                    givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model))
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
        protected override void AnalyzeOwnershipInCandidateCallee(ISymbol target, PSharpMethodSummary calleeSummary,
            ExpressionSyntax call, SyntaxNode syntaxNode, PSharpCFGNode cfgNode, SyntaxNode givesUpSyntaxNode,
            PSharpCFGNode givesUpCfgNode, StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            var invocation = call as InvocationExpressionSyntax;
            ArgumentListSyntax argumentList = AnalysisContext.GetArgumentList(call);
            if (argumentList == null)
            {
                return;
            }

            for (int idx = 0; idx < argumentList.Arguments.Count; idx++)
            {
                var argType = model.GetTypeInfo(argumentList.Arguments[idx].Expression).Type;
                if (!this.AnalysisContext.IsTypeEnum(argType) &&
                    DataFlowQuerying.FlowsFromTarget(argumentList.Arguments[idx].Expression, target,
                    syntaxNode, cfgNode, givesUpSyntaxNode, givesUpCfgNode, model) &&
                    !DataFlowQuerying.DoesResetInLoop(argumentList.Arguments[idx].Expression,
                    syntaxNode, cfgNode, givesUpSyntaxNode, givesUpCfgNode, model))
                {
                    if (calleeSummary.ParameterAccessSet.ContainsKey(idx))
                    {
                        foreach (var access in calleeSummary.ParameterAccessSet[idx])
                        {
                            TraceInfo newTrace = new TraceInfo();
                            newTrace.Merge(trace);
                            newTrace.AddErrorTrace(access.ToString(), access.SyntaxTree.FilePath, access.SyntaxTree.
                                GetLineSpan(access.Span).StartLinePosition.Line + 1);
                            AnalysisErrorReporter.ReportPotentialDataRace(newTrace);
                        }
                    }
                    else if (invocation != null)
                    {
                        Console.WriteLine(calleeSummary.Method);
                        var paramSymbol = model.GetDeclaredSymbol(calleeSummary.Method.ParameterList.Parameters[idx]);
                        Console.WriteLine("flows arg: " + paramSymbol);
                        this.AnalyzeOwnershipInCFG(paramSymbol, calleeSummary.EntryNode as PSharpCFGNode,
                            givesUpCfgNode, invocation, new HashSet<ControlFlowGraphNode>() { givesUpCfgNode },
                            originalMachine, model, trace);
                    }

                    var fieldSymbols = calleeSummary.SideEffects.Where(v => v.Value.Contains(idx)).Select(v => v.Key);
                    foreach (var fieldSymbol in fieldSymbols)
                    {
                        if (this.AnalysisContext.DoesFieldBelongToMachine(fieldSymbol, cfgNode.GetMethodSummary()))
                        {
                            if (base.IsFieldAccessedBeforeBeingReset(fieldSymbol, cfgNode.GetMethodSummary()))
                            {
                                AnalysisErrorReporter.ReportGivenUpOwnershipFieldAssignment(trace, fieldSymbol);
                            }
                        }
                        else
                        {
                            AnalysisErrorReporter.ReportGivenUpOwnershipFieldAssignment(trace, fieldSymbol);
                        }
                    }

                    if (calleeSummary.GivesUpSet.Contains(idx))
                    {
                        AnalysisErrorReporter.ReportGivenUpOwnershipSending(trace);
                    }
                }
            }

            foreach (var fieldAccess in calleeSummary.FieldAccessSet)
            {
                if (DataFlowQuerying.FlowsFromTarget(fieldAccess.Key, target, syntaxNode,
                    cfgNode, givesUpSyntaxNode, givesUpCfgNode))
                {
                    foreach (var access in fieldAccess.Value)
                    {
                        TraceInfo newTrace = new TraceInfo();
                        newTrace.Merge(trace);
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
        /// <param name="target">Target</param>
        /// <param name="call">Gives-up call</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="givesUpSyntaxNode">Gives up syntaxNode</param>
        /// <param name="givesUpCfgNode">Gives up controlFlowGraphNode</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected override void AnalyzeOwnershipInGivesUpCall(ISymbol target, InvocationExpressionSyntax call,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, SyntaxNode givesUpSyntaxNode,
            PSharpCFGNode givesUpCfgNode, SemanticModel model, TraceInfo trace)
        {
            List<ExpressionSyntax> arguments = new List<ExpressionSyntax>();
            var opSymbol = model.GetSymbolInfo(call).Symbol;

            if (opSymbol.ContainingType.ToString().Equals("Microsoft.PSharp.Machine") &&
                opSymbol.Name.Equals("Send"))
            {
                var expr = call.ArgumentList.Arguments[1].Expression;
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
                if (call.ArgumentList.Arguments.Count != 2)
                {
                    return;
                }

                var expr = call.ArgumentList.Arguments[1].Expression;
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
                var argType = model.GetTypeInfo(arg).Type;
                if (!this.AnalysisContext.IsTypeEnum(argType) &&
                    DataFlowQuerying.FlowsFromTarget(arg, target, syntaxNode, cfgNode,
                    givesUpSyntaxNode, givesUpCfgNode, model) &&
                    !DataFlowQuerying.DoesResetInLoop(arg, syntaxNode, cfgNode,
                    givesUpSyntaxNode, givesUpCfgNode, model))
                {
                    AnalysisErrorReporter.ReportGivenUpOwnershipSending(trace);
                    return;
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
        /// Analyzes the ownership of references in the given expression.
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="expr">ExpressionSyntax</param>
        /// <param name="stmt">StatementSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void AnalyzeOwnershipInExpression(ISymbol target, ExpressionSyntax expr, StatementSyntax stmt,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            if (expr is MemberAccessExpressionSyntax)
            {
                if (DataFlowQuerying.FlowsFromTarget(expr, target, syntaxNode, cfgNode,
                    givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode, model))
                {
                    TraceInfo newTrace = new TraceInfo();
                    newTrace.Merge(trace);
                    newTrace.AddErrorTrace(stmt.ToString(), stmt.SyntaxTree.FilePath, stmt.SyntaxTree.
                        GetLineSpan(stmt.Span).StartLinePosition.Line + 1);
                    AnalysisErrorReporter.ReportPotentialDataRace(newTrace);
                }
            }
            else if (expr is InvocationExpressionSyntax ||
                expr is ObjectCreationExpressionSyntax)
            {
                trace.InsertCall(cfgNode.GetMethodSummary().Method, expr);
                base.AnalyzeOwnershipInCall(target, expr, syntaxNode, cfgNode,
                    givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode,
                    originalMachine, model, trace);
            }
        }

        #endregion
    }
}
