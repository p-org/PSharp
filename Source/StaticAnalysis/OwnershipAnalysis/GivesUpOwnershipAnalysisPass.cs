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
                base.AnalyzeOwnershipInCFG(target, cfgNode, givesUpCfgNode, originalMachine, model, trace);
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
                ExpressionSyntax expr = variable.Initializer.Value;
                ISymbol leftSymbol = model.GetDeclaredSymbol(variable);
                this.AnalyzeOwnershipInAssignment(target, leftSymbol, expr, expr, stmt, syntaxNode,
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
            this.AnalyzeOwnershipInAssignment(target, leftSymbol, assignment.Left, assignment.Right,
                stmt, syntaxNode, cfgNode, givesUpCfgNode, originalMachine, model, trace);
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
            ArgumentListSyntax argumentList = AnalysisContext.GetArgumentList(call);
            if (argumentList == null)
            {
                return;
            }

            for (int idx = 0; idx < argumentList.Arguments.Count; idx++)
            {
                if (DataFlowQuerying.FlowsIntoTarget(argumentList.Arguments[idx].Expression, target,
                    syntaxNode, cfgNode, givesUpSyntaxNode, givesUpCfgNode, model))
                {
                    if (calleeSummary.SideEffects.Any(v => v.Value.Contains(idx) &&
                        this.AnalysisContext.DoesFieldBelongToMachine(v.Key, cfgNode.GetMethodSummary()) &&
                        base.IsFieldAccessedBeforeBeingReset(v.Key, cfgNode.GetMethodSummary())))
                    {
                        AnalysisErrorReporter.ReportGivenUpFieldOwnershipError(trace);
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
            // Does nothing.
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
        /// <param name="target">Target</param>
        /// <param name="leftSymbol">Left symbol</param>
        /// <param name="leftExpr">ExpressionSyntax</param>
        /// <param name="rightExpr">ExpressionSyntax</param>
        /// <param name="stmt">StatementSyntax</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="originalMachine">Original machine</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void AnalyzeOwnershipInAssignment(ISymbol target, ISymbol leftSymbol,
            ExpressionSyntax leftExpr, ExpressionSyntax rightExpr, StatementSyntax stmt,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, PSharpCFGNode givesUpCfgNode,
            StateMachine originalMachine, SemanticModel model, TraceInfo trace)
        {
            if (rightExpr is IdentifierNameSyntax ||
                rightExpr is MemberAccessExpressionSyntax)
            {
                if (DataFlowQuerying.FlowsIntoTarget(leftSymbol, target, syntaxNode, cfgNode,
                    givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode))
                {
                    IdentifierNameSyntax identifier = CodeAnalysis.CSharp.DataFlowAnalysis.
                        AnalysisContext.GetTopLevelIdentifier(rightExpr);
                    if (identifier != null)
                    {
                        var symbol = model.GetSymbolInfo(identifier).Symbol;
                        this.AnalyzeFieldOwnershipInAssignment(target, leftSymbol,
                            new HashSet<ISymbol> { symbol }, leftExpr, rightExpr,
                            stmt, syntaxNode, cfgNode, givesUpCfgNode, model, trace);
                    }
                }
            }
            else if (rightExpr is InvocationExpressionSyntax ||
                rightExpr is ObjectCreationExpressionSyntax)
            {
                trace.InsertCall(cfgNode.GetMethodSummary().Method, rightExpr);

                HashSet<ISymbol> returnSymbols = base.AnalyzeOwnershipInCall(target, rightExpr,
                    syntaxNode, cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode,
                    originalMachine, model, trace);

                if (DataFlowQuerying.FlowsIntoTarget(leftSymbol, target, syntaxNode, cfgNode,
                    givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode))
                {
                    this.AnalyzeFieldOwnershipInAssignment(target, leftSymbol, returnSymbols,
                        leftExpr, rightExpr, stmt, syntaxNode, cfgNode, givesUpCfgNode,
                        model, trace);
                }
            }
        }

        /// <summary>
        /// Analyzes the ownership of fields in the given assignment.
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="leftSymbol">Left symbol</param>
        /// <param name="rightSymbols">Right symbols</param>
        /// <param name="leftExpr">ExpressionSyntax</param>
        /// <param name="rightExpr">ExpressionSyntax</param>
        /// <param name="stmt">StatementSyntax</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="givesUpCfgNode">Gives-up CFG node</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="trace">TraceInfo</param>
        private void AnalyzeFieldOwnershipInAssignment(ISymbol target, ISymbol leftSymbol,
            HashSet<ISymbol> rightSymbols, ExpressionSyntax leftExpr, ExpressionSyntax rightExpr,
            StatementSyntax stmt, SyntaxNode syntaxNode, PSharpCFGNode cfgNode,
            PSharpCFGNode givesUpCfgNode, SemanticModel model, TraceInfo trace)
        {
            foreach (var rightSymbol in rightSymbols)
            {
                if (target.Kind == SymbolKind.Field && rightSymbol.Equals(leftSymbol))
                {
                    return;
                }

                var type = model.GetTypeInfo(rightExpr).Type;
                this.AnalyzeFieldOwnership(target, rightSymbol, type, stmt, cfgNode, trace);

                if (leftSymbol != null && !rightSymbol.Equals(leftSymbol))
                {
                    if (DataFlowQuerying.FlowsIntoTarget(rightSymbol, target, syntaxNode,
                        cfgNode, givesUpCfgNode.SyntaxNodes.First(), givesUpCfgNode))
                    {
                        type = model.GetTypeInfo(leftExpr).Type;
                        this.AnalyzeFieldOwnership(target, leftSymbol, type,
                            stmt, cfgNode, trace);
                    }
                }
            }
        }

        /// <summary>
        /// Analyzes the ownership of fields in the given expression.
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="fieldSymbol">Field symbol</param>
        /// <param name="fieldType">Field type</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">Control flow graph node</param>
        /// <param name="trace">TraceInfo</param>
        private void AnalyzeFieldOwnership(ISymbol target, ISymbol fieldSymbol, ITypeSymbol fieldType,
            SyntaxNode syntaxNode, PSharpCFGNode cfgNode, TraceInfo trace)
        {
            var definition = SymbolFinder.FindSourceDefinitionAsync(fieldSymbol,
                    this.AnalysisContext.Solution).Result;
            
            if (definition != null && definition.Kind == SymbolKind.Field &&
                this.AnalysisContext.DoesFieldBelongToMachine(definition, cfgNode.GetMethodSummary()) &&
                !this.AnalysisContext.IsTypePassedByValueOrImmutable(fieldType) &&
                !this.AnalysisContext.IsTypeEnum(fieldType) &&
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
