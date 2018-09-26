// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PSharp.IO;

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
        /// <param name="configuration">Configuration</param>
        /// <param name="logger">ILogger</param>
        /// <param name="errorReporter">ErrorReporter</param>
        /// <returns>GivesUpOwnershipAnalysisPass</returns>
        internal static GivesUpOwnershipAnalysisPass Create(AnalysisContext context,
            Configuration configuration, ILogger logger, ErrorReporter errorReporter)
        {
            return new GivesUpOwnershipAnalysisPass(context, configuration, logger, errorReporter);
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Analyzes the ownership of the given-up symbol
        /// in the control-flow graph.
        /// </summary>
        /// <param name="givenUpSymbol">GivenUpOwnershipSymbol</param>
        /// <param name="machine">StateMachine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected override void AnalyzeOwnershipInControlFlowGraph(GivenUpOwnershipSymbol givenUpSymbol,
            StateMachine machine, SemanticModel model, TraceInfo trace)
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
                    statements.AddRange(node.Statements.TakeWhile(
                        val => !val.Equals(givenUpSymbol.Statement)));
                    statements.Add(givenUpSymbol.Statement);
                }
                else if (repeatGivesUpNode &&
                    node.Equals(givenUpSymbol.Statement.ControlFlowNode))
                {
                    statements.AddRange(node.Statements.SkipWhile(
                        val => !val.Equals(givenUpSymbol.Statement)));
                }
                else
                {
                    statements.AddRange(node.Statements);
                }

                foreach (var statement in statements)
                {
                    base.AnalyzeOwnershipInStatement(givenUpSymbol, statement,
                        machine, model, trace);
                }

                foreach (var predecessor in node.IPredecessors)
                {
                    if (!repeatGivesUpNode &&
                        predecessor.Equals(givenUpSymbol.Statement.ControlFlowNode))
                    {
                        repeatGivesUpNode = true;
                        visitedNodes.Remove(givenUpSymbol.Statement.ControlFlowNode);
                    }

                    if (!visitedNodes.Contains(predecessor))
                    {
                        queue.Enqueue(predecessor);
                        visitedNodes.Add(predecessor);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the ownership of the given-up symbol
        /// in the variable declaration.
        /// </summary>
        /// <param name="givenUpSymbol">GivenUpOwnershipSymbol</param>
        /// <param name="varDecl">VariableDeclarationSyntax</param>
        /// <param name="statement">Statement</param>
        /// <param name="machine">StateMachine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected override void AnalyzeOwnershipInLocalDeclaration(GivenUpOwnershipSymbol givenUpSymbol,
            VariableDeclarationSyntax varDecl, Statement statement, StateMachine machine,
            SemanticModel model, TraceInfo trace)
        {
            foreach (var variable in varDecl.Variables.Where(v => v.Initializer != null))
            {
                ExpressionSyntax expr = variable.Initializer.Value;
                ISymbol leftSymbol = model.GetDeclaredSymbol(variable);

                this.AnalyzeGivingUpFieldOwnership(givenUpSymbol, leftSymbol, statement, machine, trace);
                this.AnalyzeOwnershipInExpression(givenUpSymbol, expr, statement,
                    machine, model, trace);
            }
        }

        /// <summary>
        /// Analyzes the ownership of the given-up symbol
        /// in the assignment expression.
        /// </summary>
        /// <param name="givenUpSymbol">GivenUpOwnershipSymbol</param>
        /// <param name="assignment">AssignmentExpressionSyntax</param>
        /// <param name="statement">Statement</param>
        /// <param name="machine">StateMachine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected override void AnalyzeOwnershipInAssignment(GivenUpOwnershipSymbol givenUpSymbol,
            AssignmentExpressionSyntax assignment, Statement statement, StateMachine machine,
            SemanticModel model, TraceInfo trace)
        {
            IdentifierNameSyntax leftIdentifier = base.AnalysisContext.GetRootIdentifier(assignment.Left);
            ISymbol leftSymbol = model.GetSymbolInfo(leftIdentifier).Symbol;
            
            this.AnalyzeGivingUpFieldOwnership(givenUpSymbol, leftSymbol, statement, machine, trace);
            this.AnalyzeOwnershipInExpression(givenUpSymbol, assignment.Right,
                statement, machine, model, trace);
        }

        /// <summary>
        /// Analyzes the ownership of the given-up symbol
        /// in the candidate callee.
        /// </summary>
        /// <param name="givenUpSymbol">GivenUpOwnershipSymbol</param>
        /// <param name="calleeSummary">MethodSummary</param>
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="statement">Statement</param>
        /// <param name="machine">StateMachine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected override void AnalyzeOwnershipInCandidateCallee(GivenUpOwnershipSymbol givenUpSymbol,
            MethodSummary calleeSummary, ExpressionSyntax call, Statement statement,
            StateMachine machine, SemanticModel model, TraceInfo trace)
        {
            ArgumentListSyntax argumentList = base.AnalysisContext.GetArgumentList(call);
            if (argumentList == null)
            {
                return;
            }

            for (int idx = 0; idx < argumentList.Arguments.Count; idx++)
            {
                var argIdentifier = base.AnalysisContext.GetRootIdentifier(
                    argumentList.Arguments[idx].Expression);
                if (argIdentifier == null)
                {
                    continue;
                }
                
                ISymbol argSymbol = model.GetSymbolInfo(argIdentifier).Symbol;
                if (statement.Summary.DataFlowAnalysis.FlowsIntoSymbol(argSymbol,
                    givenUpSymbol.ContainingSymbol, statement, givenUpSymbol.Statement))
                {
                    if (calleeSummary.SideEffectsInfo.FieldFlowParamIndexes.Any(v => v.Value.Contains(idx) &&
                        base.IsFieldAccessedInSuccessor(v.Key, statement.Summary, machine)))
                    {
                        base.ErrorReporter.ReportGivenUpFieldOwnershipError(trace, argSymbol);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the ownership of the given-up symbol
        /// in the gives-up operation.
        /// </summary>
        /// <param name="givenUpSymbol">GivenUpOwnershipSymbol</param>
        /// <param name="call">Gives-up call</param>
        /// <param name="statement">Statement</param>
        /// <param name="machine">StateMachine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected override void AnalyzeOwnershipInGivesUpCall(GivenUpOwnershipSymbol givenUpSymbol,
            InvocationExpressionSyntax call, Statement statement, StateMachine machine,
            SemanticModel model, TraceInfo trace)
        {
            if (givenUpSymbol.Statement.Equals(statement) &&
                givenUpSymbol.ContainingSymbol.Kind == SymbolKind.Field &&
                base.IsFieldAccessedInSuccessor(givenUpSymbol.ContainingSymbol as IFieldSymbol, statement.Summary, machine))
            {
                base.ErrorReporter.ReportGivenUpFieldOwnershipError(trace, givenUpSymbol.ContainingSymbol);
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="logger">ILogger</param>
        /// <param name="errorReporter">ErrorReporter</param>
        private GivesUpOwnershipAnalysisPass(AnalysisContext context, Configuration configuration,
            ILogger logger, ErrorReporter errorReporter)
            : base(context, configuration, logger, errorReporter)
        {

        }

        /// <summary>
        /// Analyzes the ownership of the given-up symbol
        /// in the expression.
        /// </summary>
        /// <param name="givenUpSymbol">GivenUpOwnershipSymbol</param>
        /// <param name="expr">ExpressionSyntax</param>
        /// <param name="statement">Statement</param>
        /// <param name="machine">StateMachine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void AnalyzeOwnershipInExpression(GivenUpOwnershipSymbol givenUpSymbol,
            ExpressionSyntax expr, Statement statement, StateMachine machine,
            SemanticModel model, TraceInfo trace)
        {
            if (expr is IdentifierNameSyntax ||
                expr is MemberAccessExpressionSyntax)
            {
                IdentifierNameSyntax rightIdentifier = base.AnalysisContext.GetRootIdentifier(expr);
                if (rightIdentifier != null)
                {
                    var rightSymbol = model.GetSymbolInfo(rightIdentifier).Symbol;
                    this.AnalyzeGivingUpFieldOwnership(givenUpSymbol, rightSymbol, statement, machine, trace);
                }
            }
            else if (expr is InvocationExpressionSyntax ||
                expr is ObjectCreationExpressionSyntax)
            {
                trace.InsertCall(statement.Summary.Method, expr);

                HashSet<ISymbol> returnSymbols = base.AnalyzeOwnershipInCall(givenUpSymbol,
                    expr, statement, machine, model, trace);
                foreach (var returnSymbol in returnSymbols)
                {
                    this.AnalyzeGivingUpFieldOwnership(givenUpSymbol, returnSymbol, statement, machine, trace);
                }
            }
        }

        /// <summary>
        /// Analyzes the given-up ownership of fields in the expression.
        /// </summary>
        /// <param name="givenUpSymbol">GivenUpOwnershipSymbol</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="statement">Statement</param>
        /// <param name="machine">StateMachine</param>
        /// <param name="trace">TraceInfo</param>
        private void AnalyzeGivingUpFieldOwnership(GivenUpOwnershipSymbol givenUpSymbol,
            ISymbol symbol, Statement statement, StateMachine machine, TraceInfo trace)
        {
            if (!statement.Summary.DataFlowAnalysis.FlowsIntoSymbol(symbol,
                givenUpSymbol.ContainingSymbol, statement, givenUpSymbol.Statement))
            {
                return;
            }

            if (symbol.Kind == SymbolKind.Field &&
                base.IsFieldAccessedInSuccessor(symbol as IFieldSymbol, statement.Summary, machine))
            {
                TraceInfo newTrace = new TraceInfo();
                newTrace.Merge(trace);
                newTrace.AddErrorTrace(statement.SyntaxNode);

                base.ErrorReporter.ReportGivenUpFieldOwnershipError(newTrace, symbol);
            }
        }

        #endregion

        #region profiling methods

        /// <summary>
        /// Prints profiling results.
        /// </summary>
        protected override void PrintProfilingResults()
        {
            base.Logger.WriteLine("... Gives-up ownership analysis runtime: '" +
                base.Profiler.Results() + "' seconds.");
        }

        #endregion
    }
}
