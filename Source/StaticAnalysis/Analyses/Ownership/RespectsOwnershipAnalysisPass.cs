// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.DataFlowAnalysis;
using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis checks that all methods in each machine of a P#
    /// program respect given-up ownerships.
    /// </summary>
    internal sealed class RespectsOwnershipAnalysisPass : OwnershipAnalysisPass
    {
        /// <summary>
        /// Creates a new respects ownership analysis pass.
        /// </summary>
        internal static RespectsOwnershipAnalysisPass Create(AnalysisContext context, Configuration configuration,
            ILogger logger, ErrorReporter errorReporter)
        {
            return new RespectsOwnershipAnalysisPass(context, configuration, logger, errorReporter);
        }

        /// <summary>
        /// Analyzes the ownership of the given-up symbol in the control-flow graph.
        /// </summary>
        protected override void AnalyzeOwnershipInControlFlowGraph(GivenUpOwnershipSymbol givenUpSymbol, StateMachine machine,
            SemanticModel model, TraceInfo trace)
        {
            var queue = new Queue<IControlFlowNode>();
            queue.Enqueue(givenUpSymbol.Statement.ControlFlowNode);

            var visitedNodes = new HashSet<IControlFlowNode>();
            visitedNodes.Add(givenUpSymbol.Statement.ControlFlowNode);

            bool repeatGivesUpNode = false;
            while (queue.Count > 0)
            {
                IControlFlowNode node = queue.Dequeue();

                var statements = new List<Statement>();
                if (!repeatGivesUpNode &&
                    node.Equals(givenUpSymbol.Statement.ControlFlowNode))
                {
                    statements.AddRange(node.Statements.SkipWhile(
                        val => !val.Equals(givenUpSymbol.Statement)));
                }
                else if (repeatGivesUpNode &&
                    node.Equals(givenUpSymbol.Statement.ControlFlowNode))
                {
                    statements.AddRange(node.Statements.TakeWhile(
                        val => !val.Equals(givenUpSymbol.Statement)));
                    statements.Add(givenUpSymbol.Statement);
                }
                else
                {
                    statements.AddRange(node.Statements);
                }

                foreach (var statement in statements)
                {
                    this.AnalyzeOwnershipInStatement(givenUpSymbol, statement,
                        machine, model, trace);
                }

                foreach (var successor in node.ISuccessors)
                {
                    if (!repeatGivesUpNode &&
                        successor.Equals(givenUpSymbol.Statement.ControlFlowNode))
                    {
                        repeatGivesUpNode = true;
                        visitedNodes.Remove(givenUpSymbol.Statement.ControlFlowNode);
                    }

                    if (!visitedNodes.Contains(successor))
                    {
                        queue.Enqueue(successor);
                        visitedNodes.Add(successor);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the ownership of the given-up symbol in the variable declaration.
        /// </summary>
        protected override void AnalyzeOwnershipInLocalDeclaration(GivenUpOwnershipSymbol givenUpSymbol, VariableDeclarationSyntax varDecl,
            Statement statement, StateMachine machine, SemanticModel model, TraceInfo trace)
        {
            foreach (var variable in varDecl.Variables.Where(v => v.Initializer != null))
            {
                var expr = variable.Initializer.Value;
                if (expr is IdentifierNameSyntax ||
                    expr is MemberAccessExpressionSyntax)
                {
                    this.AnalyzeOwnershipInExpression(givenUpSymbol, expr, statement, model, trace);
                }
                else if (expr is InvocationExpressionSyntax ||
                    expr is ObjectCreationExpressionSyntax)
                {
                    trace.InsertCall(statement.Summary.Method, expr);
                    this.AnalyzeOwnershipInCall(givenUpSymbol, expr, statement,
                        machine, model, trace);
                }
            }
        }

        /// <summary>
        /// Analyzes the ownership of the given-up symbol in the assignment expression.
        /// </summary>
        protected override void AnalyzeOwnershipInAssignment(GivenUpOwnershipSymbol givenUpSymbol, AssignmentExpressionSyntax assignment,
            Statement statement, StateMachine machine, SemanticModel model, TraceInfo trace)
        {
            var leftIdentifier = AnalysisContext.GetRootIdentifier(assignment.Left);
            ISymbol leftSymbol = model.GetSymbolInfo(leftIdentifier).Symbol;

            if (assignment.Right is IdentifierNameSyntax)
            {
                var rightIdentifier = AnalysisContext.GetRootIdentifier(assignment.Right);
                ISymbol rightSymbol = model.GetSymbolInfo(rightIdentifier).Symbol;

                if (statement.Summary.DataFlowAnalysis.FlowsIntoSymbol(rightSymbol, givenUpSymbol.ContainingSymbol, statement, givenUpSymbol.Statement))
                {
                    var type = model.GetTypeInfo(assignment.Right).Type;
                    if (leftSymbol != null && leftSymbol.Kind == SymbolKind.Field &&
                        this.IsFieldAccessedInSuccessor(leftSymbol as IFieldSymbol, statement.Summary, machine) &&
                        !this.AnalysisContext.IsTypePassedByValueOrImmutable(type))
                    {
                        TraceInfo newTrace = new TraceInfo();
                        newTrace.Merge(trace);
                        newTrace.AddErrorTrace(statement.SyntaxNode);

                        this.ErrorReporter.ReportGivenUpOwnershipFieldAssignment(newTrace, leftSymbol);
                    }

                    return;
                }
            }
            else if (assignment.Right is MemberAccessExpressionSyntax)
            {
                this.AnalyzeOwnershipInExpression(givenUpSymbol, assignment.Right, statement, model, trace);
            }
            else if (assignment.Right is InvocationExpressionSyntax ||
                assignment.Right is ObjectCreationExpressionSyntax)
            {
                trace.InsertCall(statement.Summary.Method, assignment.Right);
                this.AnalyzeOwnershipInCall(givenUpSymbol, assignment.Right, statement,
                    machine, model, trace);
            }

            if (assignment.Left is MemberAccessExpressionSyntax)
            {
                ISymbol outerLeftMemberSymbol = model.GetSymbolInfo(assignment.Left).Symbol;
                if (!outerLeftMemberSymbol.Equals(leftSymbol) &&
                    statement.Summary.DataFlowAnalysis.FlowsIntoSymbol(givenUpSymbol.ContainingSymbol, leftSymbol, givenUpSymbol.Statement, statement))
                {
                    TraceInfo newTrace = new TraceInfo();
                    newTrace.Merge(trace);
                    newTrace.AddErrorTrace(statement.SyntaxNode);

                    this.ErrorReporter.ReportGivenUpOwnershipAccess(newTrace);
                }
            }
        }

        /// <summary>
        /// Analyzes the ownership of the given-up symbol in the candidate callee.
        /// </summary>
        protected override void AnalyzeOwnershipInCandidateCallee(GivenUpOwnershipSymbol givenUpSymbol, MethodSummary calleeSummary,
            ExpressionSyntax call, Statement statement, StateMachine machine, SemanticModel model, TraceInfo trace)
        {
            if (statement.Equals(givenUpSymbol.Statement) &&
                !statement.ControlFlowNode.IsSuccessorOf(givenUpSymbol.Statement.ControlFlowNode))
            {
                return;
            }

            if (call is InvocationExpressionSyntax invocation)
            {
                this.AnalyzeOwnershipInExpression(givenUpSymbol, invocation.Expression, statement, model, trace);
            }

            ArgumentListSyntax argumentList = AnalysisContext.GetArgumentList(call);
            if (argumentList != null)
            {
                for (int idx = 0; idx < argumentList.Arguments.Count; idx++)
                {
                    var argType = model.GetTypeInfo(argumentList.Arguments[idx].Expression).Type;
                    if (this.AnalysisContext.IsTypePassedByValueOrImmutable(argType))
                    {
                        continue;
                    }

                    var argIdentifier = AnalysisContext.GetRootIdentifier(
                        argumentList.Arguments[idx].Expression);
                    ISymbol argSymbol = model.GetSymbolInfo(argIdentifier).Symbol;

                    if (statement.Summary.DataFlowAnalysis.FlowsIntoSymbol(argSymbol, givenUpSymbol.ContainingSymbol,
                        statement, givenUpSymbol.Statement))
                    {
                        if (calleeSummary.SideEffectsInfo.ParameterAccesses.ContainsKey(idx))
                        {
                            foreach (var access in calleeSummary.SideEffectsInfo.ParameterAccesses[idx])
                            {
                                TraceInfo newTrace = new TraceInfo();
                                newTrace.Merge(trace);
                                newTrace.AddErrorTrace(access.SyntaxNode);

                                this.ErrorReporter.ReportGivenUpOwnershipAccess(newTrace);
                            }
                        }

                        var fieldSymbols = calleeSummary.SideEffectsInfo.FieldFlowParamIndexes.Where(
                            v => v.Value.Contains(idx)).Select(v => v.Key);
                        foreach (var fieldSymbol in fieldSymbols)
                        {
                            if (this.IsFieldAccessedInSuccessor(fieldSymbol, statement.Summary, machine))
                            {
                                this.ErrorReporter.ReportGivenUpOwnershipFieldAssignment(trace, fieldSymbol);
                            }
                        }

                        if (calleeSummary.SideEffectsInfo.GivesUpOwnershipParamIndexes.Contains(idx))
                        {
                            this.ErrorReporter.ReportGivenUpOwnershipSending(trace, argSymbol);
                        }
                    }
                }
            }

            foreach (var fieldAccess in calleeSummary.SideEffectsInfo.FieldAccesses)
            {
                foreach (var access in fieldAccess.Value)
                {
                    if (statement.Summary.DataFlowAnalysis.FlowsIntoSymbol(givenUpSymbol.ContainingSymbol, fieldAccess.Key,
                        givenUpSymbol.Statement, statement))
                    {
                        TraceInfo newTrace = new TraceInfo();
                        newTrace.Merge(trace);
                        newTrace.AddErrorTrace(access.SyntaxNode);

                        this.ErrorReporter.ReportGivenUpOwnershipFieldAccess(newTrace, fieldAccess.Key);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the ownership of the given-up symbol in the gives-up operation.
        /// </summary>
        protected override void AnalyzeOwnershipInGivesUpCall(GivenUpOwnershipSymbol givenUpSymbol, InvocationExpressionSyntax call,
            Statement statement, StateMachine machine, SemanticModel model, TraceInfo trace)
        {
            if (statement.Equals(givenUpSymbol.Statement) &&
                !statement.ControlFlowNode.IsSuccessorOf(
                    givenUpSymbol.Statement.ControlFlowNode))
            {
                return;
            }

            var opSymbol = model.GetSymbolInfo(call).Symbol;
            if ((!opSymbol.Name.Equals("Send") &&
                !opSymbol.Name.Equals("CreateMachine")) ||
                (opSymbol.Name.Equals("CreateMachine") &&
                call.ArgumentList.Arguments.Count != 2))
            {
                return;
            }

            ExpressionSyntax argExpr = call.ArgumentList.Arguments[1].Expression;
            var arguments = new List<ExpressionSyntax>();

            if (argExpr is ObjectCreationExpressionSyntax)
            {
                var objCreation = argExpr as ObjectCreationExpressionSyntax;
                foreach (var arg in objCreation.ArgumentList.Arguments)
                {
                    arguments.Add(arg.Expression);
                }
            }
            else if (argExpr is BinaryExpressionSyntax &&
                argExpr.IsKind(SyntaxKind.AsExpression))
            {
                var binExpr = argExpr as BinaryExpressionSyntax;
                if (binExpr.Left is IdentifierNameSyntax ||
                    binExpr.Left is MemberAccessExpressionSyntax)
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
            else if (argExpr is IdentifierNameSyntax ||
                argExpr is MemberAccessExpressionSyntax)
            {
                arguments.Add(argExpr);
            }

            var extractedArgs = this.ExtractArguments(arguments);
            foreach (var arg in extractedArgs)
            {
                IdentifierNameSyntax argIdentifier = AnalysisContext.GetRootIdentifier(arg);
                ITypeSymbol argType = model.GetTypeInfo(argIdentifier).Type;
                if (this.AnalysisContext.IsTypePassedByValueOrImmutable(argType))
                {
                    continue;
                }

                ISymbol argSymbol = model.GetSymbolInfo(argIdentifier).Symbol;
                if (statement.Summary.DataFlowAnalysis.FlowsIntoSymbol(argSymbol, givenUpSymbol.ContainingSymbol,
                    statement, givenUpSymbol.Statement))
                {
                    this.ErrorReporter.ReportGivenUpOwnershipSending(trace, argSymbol);
                    return;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RespectsOwnershipAnalysisPass"/> class.
        /// </summary>
        private RespectsOwnershipAnalysisPass(AnalysisContext context, Configuration configuration, ILogger logger, ErrorReporter errorReporter)
            : base(context, configuration, logger, errorReporter)
        {
        }

        /// <summary>
        /// Analyzes the ownership of the given-up symbol in the expression.
        /// </summary>
        private void AnalyzeOwnershipInExpression(GivenUpOwnershipSymbol givenUpSymbol, ExpressionSyntax expr,
            Statement statement, SemanticModel model, TraceInfo trace)
        {
            if (expr is MemberAccessExpressionSyntax)
            {
                var identifier = AnalysisContext.GetRootIdentifier(expr);
                ISymbol symbol = model.GetSymbolInfo(identifier).Symbol;
                if (statement.Summary.DataFlowAnalysis.FlowsIntoSymbol(symbol, givenUpSymbol.ContainingSymbol,
                    statement, givenUpSymbol.Statement))
                {
                    TraceInfo newTrace = new TraceInfo();
                    newTrace.Merge(trace);
                    newTrace.AddErrorTrace(statement.SyntaxNode);

                    this.ErrorReporter.ReportGivenUpOwnershipAccess(newTrace);
                }
            }
        }

        /// <summary>
        /// Prints profiling results.
        /// </summary>
        protected override void PrintProfilingResults()
        {
            this.Logger.WriteLine($"... Respects ownership analysis runtime: '{this.Profiler.Results()}' seconds.");
        }
    }
}
