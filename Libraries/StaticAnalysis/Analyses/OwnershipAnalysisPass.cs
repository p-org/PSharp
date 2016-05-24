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
    internal abstract class OwnershipAnalysisPass : StateMachineAnalysisPass
    {
        #region internal API

        /// <summary>
        /// Runs the analysis on the specified machines.
        /// </summary>
        /// <param name="machines">StateMachines</param>
        internal override void Run(ISet<StateMachine> machines)
        {
            // Starts profiling the ownership analysis.
            if (base.Configuration.TimeStaticAnalysis)
            {
                base.Profiler.StartMeasuringExecutionTime();
            }

            foreach (var machine in machines)
            {
                this.AnalyzeMethodSummariesInMachine(machine);
            }

            // Stops profiling the ownership analysis.
            if (base.Configuration.TimeStaticAnalysis)
            {
                base.Profiler.StopMeasuringExecutionTime();
                this.PrintProfilingResults();
            }
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="configuration">Configuration</param>
        protected OwnershipAnalysisPass(AnalysisContext context, Configuration configuration)
            : base(context, configuration)
        {

        }

        /// <summary>
        /// Analyzes the ownership of the given-up symbol
        /// in the control-flow graph.
        /// </summary>
        /// <param name="givenUpSymbol">GivenUpOwnershipSymbol</param>
        /// <param name="machine">StateMachine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected abstract void AnalyzeOwnershipInControlFlowGraph(GivenUpOwnershipSymbol givenUpSymbol,
            StateMachine machine, SemanticModel model, TraceInfo trace);

        /// <summary>
        /// Analyzes the ownership of the given-up symbol
        /// in the control-flow graph node.
        /// </summary>
        /// <param name="givenUpSymbol">GivenUpOwnershipSymbol</param>
        /// <param name="statement">Statement</param>
        /// <param name="visited">ControlFlowGraphNodes</param>
        /// <param name="machine">StateMachine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        protected void AnalyzeOwnershipInStatement(GivenUpOwnershipSymbol givenUpSymbol,
            Statement statement, StateMachine machine, SemanticModel model,
            TraceInfo trace)
        {
            var localDecl = statement.SyntaxNode.DescendantNodesAndSelf().
                OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
            var expr = statement.SyntaxNode.DescendantNodesAndSelf().
                OfType<ExpressionStatementSyntax>().FirstOrDefault();

            if (localDecl != null)
            {
                var varDecl = localDecl.Declaration;
                this.AnalyzeOwnershipInLocalDeclaration(givenUpSymbol, varDecl,
                    statement, machine, model, trace);
            }
            else if (expr != null)
            {
                if (expr.Expression is AssignmentExpressionSyntax)
                {
                    var assignment = expr.Expression as AssignmentExpressionSyntax;
                    this.AnalyzeOwnershipInAssignment(givenUpSymbol, assignment,
                        statement, machine, model, trace);
                }
                else if (expr.Expression is InvocationExpressionSyntax ||
                    expr.Expression is ObjectCreationExpressionSyntax)
                {
                    trace.InsertCall(statement.Summary.Method, expr.Expression);
                    this.AnalyzeOwnershipInCall(givenUpSymbol, expr.Expression,
                        statement, machine, model, trace);
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
        protected abstract void AnalyzeOwnershipInLocalDeclaration(GivenUpOwnershipSymbol givenUpSymbol,
            VariableDeclarationSyntax varDecl, Statement statement, StateMachine machine,
            SemanticModel model, TraceInfo trace);

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
        protected abstract void AnalyzeOwnershipInAssignment(GivenUpOwnershipSymbol givenUpSymbol,
            AssignmentExpressionSyntax assignment, Statement statement, StateMachine machine,
            SemanticModel model, TraceInfo trace);

        /// <summary>
        /// Analyzes the ownership of the given-up symbol
        /// in the call.
        /// </summary>
        /// <param name="givenUpSymbol">GivenUpOwnershipSymbol</param>
        /// <param name="call">ExpressionSyntax</param>
        /// <param name="statement">Statement</param>
        /// <param name="machine">StateMachine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        /// <returns>Set of return symbols</returns>
        protected HashSet<ISymbol> AnalyzeOwnershipInCall(GivenUpOwnershipSymbol givenUpSymbol,
            ExpressionSyntax call, Statement statement, StateMachine machine,
            SemanticModel model, TraceInfo trace)
        {
            var potentialReturnSymbols = new HashSet<ISymbol>();

            var invocation = call as InvocationExpressionSyntax;
            var objCreation = call as ObjectCreationExpressionSyntax;
            if ((invocation == null && objCreation == null))
            {
                return potentialReturnSymbols;
            }

            TraceInfo callTrace = new TraceInfo();
            callTrace.Merge(trace);
            callTrace.AddErrorTrace(call);

            var callSymbol = model.GetSymbolInfo(call).Symbol;
            if (callSymbol == null)
            {
                AnalysisErrorReporter.ReportExternalInvocation(callTrace);
                return potentialReturnSymbols;
            }

            if (callSymbol.ContainingType.ToString().Equals("Microsoft.PSharp.Machine"))
            {
                this.AnalyzeOwnershipInGivesUpCall(givenUpSymbol, invocation,
                    statement, machine, model, callTrace);
                return potentialReturnSymbols;
            }

            if (SymbolFinder.FindSourceDefinitionAsync(callSymbol,
                this.AnalysisContext.Solution).Result == null)
            {
                AnalysisErrorReporter.ReportExternalInvocation(callTrace);
                return potentialReturnSymbols;
            }

            var candidateSummaries = MethodSummary.GetCachedSummaries(callSymbol, statement);
            foreach (var candidateSummary in candidateSummaries)
            {
                this.AnalyzeOwnershipInCandidateCallee(givenUpSymbol, candidateSummary,
                    call, statement, machine, model, callTrace);

                if (invocation != null)
                {
                    var resolvedReturnSymbols = candidateSummary.GetResolvedReturnSymbols(invocation, model);
                    foreach (var resolvedReturnSymbol in resolvedReturnSymbols)
                    {
                        potentialReturnSymbols.Add(resolvedReturnSymbol);
                    }
                }
            }

            return potentialReturnSymbols;
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
        protected abstract void AnalyzeOwnershipInCandidateCallee(GivenUpOwnershipSymbol givenUpSymbol,
            MethodSummary calleeSummary, ExpressionSyntax call, Statement statement,
            StateMachine machine, SemanticModel model, TraceInfo trace);

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
        protected abstract void AnalyzeOwnershipInGivesUpCall(GivenUpOwnershipSymbol givenUpSymbol,
            InvocationExpressionSyntax call, Statement statement, StateMachine machine,
            SemanticModel model, TraceInfo trace);

        #endregion

        #region private methods

        /// <summary>
        /// Analyzes the method summaries of the machine to check if
        /// each summary respects given-up ownerships.
        /// </summary>
        /// <param name="machine">StateMachine</param>
        private void AnalyzeMethodSummariesInMachine(StateMachine machine)
        {
            foreach (var summary in machine.MethodSummaries.Values)
            {
                foreach (var givenUpSymbol in summary.GetSymbolsWithGivenUpOwnership())
                {
                    TraceInfo trace = new TraceInfo(summary.Method, machine, null, givenUpSymbol.ContainingSymbol);
                    trace.AddErrorTrace(givenUpSymbol.Statement.SyntaxNode);

                    var model = this.AnalysisContext.Compilation.GetSemanticModel(
                        givenUpSymbol.Statement.SyntaxNode.SyntaxTree);
                    this.AnalyzeOwnershipInControlFlowGraph(givenUpSymbol, machine, model, trace);
                }
            }
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Returns true if the field symbol is being accessed
        /// in a successor summary.
        /// </summary>
        /// <param name="fieldSymbol">IFieldSymbol</param>
        /// <param name="summary">MethodSummary</param>
        /// <param name="machine">StateMachine</param>
        /// <returns>Boolean</returns>
        protected bool IsFieldAccessedInSuccessor(IFieldSymbol fieldSymbol, MethodSummary summary,
            StateMachine machine)
        {
            if (!base.Configuration.DoStateTransitionAnalysis)
            {
                return true;
            }

            var successors = machine.GetSuccessorSummaries(summary);

            foreach (var successor in successors)
            {
                if (successor.SideEffectsInfo.FieldAccesses.ContainsKey(fieldSymbol))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Extracts arguments from the list of arguments.
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
                else if (!(arg is LiteralExpressionSyntax))
                {
                    args.Add(arg);
                }
            }

            return args;
        }

        #endregion
    }
}
