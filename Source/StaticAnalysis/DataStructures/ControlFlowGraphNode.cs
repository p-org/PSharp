//-----------------------------------------------------------------------
// <copyright file="ControlFlowGraphNode.cs">
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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    internal class ControlFlowGraphNode
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        private AnalysisContext AnalysisContext;

        /// <summary>
        /// The unique ID of the node.
        /// </summary>
        internal int Id;

        /// <summary>
        /// Handle to the summary of the method which owns this node.
        /// </summary>
        internal MethodSummary Summary;

        /// <summary>
        /// List of syntax nodes.
        /// </summary>
        internal List<SyntaxNode> SyntaxNodes;

        /// <summary>
        /// Set of the immediate predecessors of the node.
        /// </summary>
        internal HashSet<ControlFlowGraphNode> IPredecessors;

        /// <summary>
        /// Set of the immediate successors of the node.
        /// </summary>
        internal HashSet<ControlFlowGraphNode> ISuccessors;

        /// <summary>
        /// The node after exiting the loop.
        /// </summary>
        internal ControlFlowGraphNode LoopExitNode;

        /// <summary>
        /// True if the node is a gives up node. False by default.
        /// </summary>
        internal bool IsGivesUpNode;

        /// <summary>
        /// True if the node is a jump node. False by default.
        /// </summary>
        internal bool IsJumpNode;

        /// <summary>
        /// True if the node is a loop head node. False by default.
        /// </summary>
        internal bool IsLoopHeadNode;

        /// <summary>
        /// A counter for creating unique IDs.
        /// </summary>
        private static int IdCounter = 0;

        #endregion

        #region public API

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="summary">MethodSummary</param>
        internal ControlFlowGraphNode(AnalysisContext context, MethodSummary summary)
        {
            this.AnalysisContext = context;
            this.Id = ControlFlowGraphNode.IdCounter++;
            this.Summary = summary;
            this.SyntaxNodes = new List<SyntaxNode>();
            this.IPredecessors = new HashSet<ControlFlowGraphNode>();
            this.ISuccessors = new HashSet<ControlFlowGraphNode>();
            this.IsGivesUpNode = false;
            this.IsJumpNode = false;
            this.IsLoopHeadNode = false;
        }

        /// <summary>
        /// Constructs the node from the given list of statements starting
        /// at the given index.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="index">Index</param>
        /// <param name="isBound">Processes only one statement</param>
        /// <param name="successor">Successor</param>
        internal void Construct(SyntaxList<StatementSyntax> stmtList, int index, bool isBound,
            ControlFlowGraphNode successor)
        {
            int boundary = stmtList.Count;
            if (isBound && index == stmtList.Count)
            {
                boundary = stmtList.Count;
            }
            else if (isBound)
            {
                boundary = index + 1;
            }

            for (int idx = index; idx < boundary; idx++)
            {
                ControlFlowGraphNode givesUpNode = null;
                ControlFlowGraphNode jumpNode = null;
                ControlFlowGraphNode succNode = null;

                if (stmtList[idx] is ExpressionStatementSyntax ||
                    stmtList[idx] is LocalDeclarationStatementSyntax)
                {
                    var invocations = stmtList[idx].DescendantNodesAndSelf().
                        OfType<InvocationExpressionSyntax>();
                    if (invocations.Count() > 0)
                    {
                        var call = invocations.First();
                        if (this.IsGivesUpOperation(call))
                        {
                            if (this.SyntaxNodes.Count == 0)
                            {
                                givesUpNode = this;
                            }
                            else
                            {
                                givesUpNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                                this.ISuccessors.Add(givesUpNode);
                                givesUpNode.IPredecessors.Add(this);
                            }
                            
                            givesUpNode.IsGivesUpNode = true;
                            this.Summary.GivesUpNodes.Add(givesUpNode);
                            givesUpNode.SyntaxNodes.Add(stmtList[idx]);

                            if (idx < boundary - 1 &&
                                stmtList[idx + 1] is BreakStatementSyntax)
                            {
                                if (successor != null &&
                                    successor.LoopExitNode != null)
                                {
                                    givesUpNode.ISuccessors.Add(successor.LoopExitNode);
                                    successor.LoopExitNode.IPredecessors.Add(givesUpNode);
                                }
                            }
                            else if (idx < boundary - 1 &&
                                stmtList[idx + 1] is ContinueStatementSyntax)
                            {
                                if (successor != null)
                                {
                                    givesUpNode.ISuccessors.Add(successor);
                                    successor.IPredecessors.Add(givesUpNode);
                                }
                            }
                            else if (idx < boundary - 1)
                            {
                                succNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                                givesUpNode.ISuccessors.Add(succNode);
                                succNode.IPredecessors.Add(givesUpNode);
                                succNode.Construct(stmtList, idx + 1, false, successor);
                            }
                            else if (successor != null)
                            {
                                givesUpNode.ISuccessors.Add(successor);
                                successor.IPredecessors.Add(givesUpNode);
                            }

                            return;
                        }
                    }

                    this.SyntaxNodes.Add(stmtList[idx]);
                    continue;
                }
                else if (stmtList[idx] is BreakStatementSyntax)
                {
                    if (successor != null && successor.LoopExitNode != null)
                    {
                        this.ISuccessors.Add(successor.LoopExitNode);
                        successor.LoopExitNode.IPredecessors.Add(this);
                    }

                    return;
                }
                else if (stmtList[idx] is ContinueStatementSyntax)
                {
                    if (successor != null)
                    {
                        this.ISuccessors.Add(successor);
                        successor.IPredecessors.Add(this);
                    }

                    return;
                }
                else if (stmtList[idx] is ReturnStatementSyntax)
                {
                    this.SyntaxNodes.Add(stmtList[idx]);
                    continue;
                }

                if (this.SyntaxNodes.Count == 0)
                {
                    jumpNode = this;
                }
                else
                {
                    jumpNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                    this.ISuccessors.Add(jumpNode);
                    jumpNode.IPredecessors.Add(this);
                }

                if (stmtList[idx] is IfStatementSyntax)
                {
                    if (idx < boundary - 1)
                    {
                        succNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleIfStatement(stmtList[idx] as IfStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, false, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleIfStatement(stmtList[idx] as IfStatementSyntax, successor);
                    }
                }
                else if (stmtList[idx] is ForStatementSyntax)
                {
                    if (idx < boundary - 1)
                    {
                        succNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleForStatement(stmtList[idx] as ForStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, false, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleForStatement(stmtList[idx] as ForStatementSyntax, successor);
                    }
                }
                else if (stmtList[idx] is WhileStatementSyntax)
                {
                    if (idx < boundary - 1)
                    {
                        succNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleWhileStatement(stmtList[idx] as WhileStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, false, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleWhileStatement(stmtList[idx] as WhileStatementSyntax, successor);
                    }
                }
                else if (stmtList[idx] is DoStatementSyntax)
                {
                    if (idx < boundary - 1)
                    {
                        succNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleDoStatement(stmtList[idx] as DoStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, false, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleDoStatement(stmtList[idx] as DoStatementSyntax, successor);
                    }
                }
                else if (stmtList[idx] is ForEachStatementSyntax)
                {
                    if (idx < boundary - 1)
                    {
                        succNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleForeachStatement(stmtList[idx] as ForEachStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, false, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleForeachStatement(stmtList[idx] as ForEachStatementSyntax, successor);
                    }
                }
                else if (stmtList[idx] is SwitchStatementSyntax)
                {
                    if (idx < boundary - 1)
                    {
                        succNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleSwitchStatement(stmtList[idx] as SwitchStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, false, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleSwitchStatement(stmtList[idx] as SwitchStatementSyntax, successor);
                    }
                }
                else if (stmtList[idx] is TryStatementSyntax)
                {
                    if (idx < boundary - 1)
                    {
                        succNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleTryStatement(stmtList[idx] as TryStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, false, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleTryStatement(stmtList[idx] as TryStatementSyntax, successor);
                    }
                }
                else if (stmtList[idx] is UsingStatementSyntax)
                {
                    if (idx < boundary - 1)
                    {
                        succNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleUsingStatement(stmtList[idx] as UsingStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, false, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleUsingStatement(stmtList[idx] as UsingStatementSyntax, successor);
                    }
                }
            }

            if (successor != null && (this.IsJumpNode ||
                (!this.IsJumpNode && this.ISuccessors.Count == 0)))
            {
                this.ISuccessors.Add(successor);
                successor.IPredecessors.Add(this);
            }

            if (this.SyntaxNodes.Count == 0)
            {
                foreach (var pred in this.IPredecessors)
                {
                    pred.ISuccessors.Remove(this);
                }
            }
        }

        /// <summary>
        /// Cleans empty successors.
        /// </summary>
        internal void CleanEmptySuccessors()
        {
            this.CleanEmptySuccessors(new HashSet<ControlFlowGraphNode>());
        }

        /// <summary>
        /// Returns all predecessors of the node.
        /// </summary>
        /// <param name="node">Node</param>
        /// <returns>Set of predecessor nodes</returns>
        internal HashSet<ControlFlowGraphNode> GetPredecessors()
        {
            var predecessors = this.IPredecessors;
            foreach (var predecessor in predecessors)
            {
                var nodes = predecessor.GetPredecessors();
                foreach (var node in nodes)
                {
                    predecessors.Add(node);
                }
            }

            return predecessors;
        }

        /// <summary>
        /// Returns true if the node is a predecessor of the given node.
        /// Returns false if not.
        /// </summary>
        /// <param name="node">ControlFlowGraphNode</param>
        /// <returns>Boolean</returns>
        internal bool IsPredecessorOf(ControlFlowGraphNode node)
        {
            return this.IsPredecessorOf(node, new HashSet<ControlFlowGraphNode>());
        }

        /// <summary>
        /// Returns all exit nodes in the control flow graph.
        /// </summary>
        /// <returns>Set of exit nodes</returns>
        internal HashSet<ControlFlowGraphNode> GetExitNodes()
        {
            return this.GetExitNodes(new HashSet<ControlFlowGraphNode>());
        }

        #endregion

        #region private methods

        /// <summary>
        /// Handles the given if statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleIfStatement(IfStatementSyntax stmt, ControlFlowGraphNode successor)
        {
            this.SyntaxNodes.Add(stmt.Condition);
            this.IsJumpNode = true;

            if (successor != null)
            {
                this.ISuccessors.Add(successor);
                successor.IPredecessors.Add(this);
                this.LoopExitNode = successor;
            }

            var ifNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
            this.ISuccessors.Add(ifNode);
            ifNode.IPredecessors.Add(this);

            if (stmt.Statement is BlockSyntax)
            {
                ifNode.Construct((stmt.Statement as BlockSyntax).Statements, 0, false, successor);
            }
            else
            {
                ifNode.Construct(new SyntaxList<StatementSyntax> { stmt.Statement }, 0, false, successor);
            }

            if (stmt.Else != null)
            {
                var elseNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                this.ISuccessors.Add(elseNode);
                elseNode.IPredecessors.Add(this);

                if (stmt.Else.Statement is IfStatementSyntax)
                {
                    elseNode.HandleIfStatement(stmt.Else.Statement as IfStatementSyntax, successor);
                }
                else
                {
                    if (stmt.Else.Statement is BlockSyntax)
                    {
                        elseNode.Construct((stmt.Else.Statement as BlockSyntax).Statements, 0, false, successor);
                    }
                    else
                    {
                        elseNode.Construct(new SyntaxList<StatementSyntax> { stmt.Else.Statement }, 0, false, successor);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the given for statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleForStatement(ForStatementSyntax stmt, ControlFlowGraphNode successor)
        {
            this.SyntaxNodes.Add(stmt.Condition);
            this.IsLoopHeadNode = true;

            if (successor != null)
            {
                this.ISuccessors.Add(successor);
                successor.IPredecessors.Add(this);
                this.LoopExitNode = successor;
            }

            var forNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
            this.ISuccessors.Add(forNode);
            forNode.IPredecessors.Add(this);

            if (stmt.Statement is BlockSyntax)
            {
                forNode.Construct((stmt.Statement as BlockSyntax).Statements, 0, false, this);
            }
            else
            {
                forNode.Construct(new SyntaxList<StatementSyntax> { stmt.Statement }, 0, false, this);
            }
        }

        /// <summary>
        /// Handles the given while statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleWhileStatement(WhileStatementSyntax stmt, ControlFlowGraphNode successor)
        {
            this.SyntaxNodes.Add(stmt.Condition);
            this.IsLoopHeadNode = true;

            if (successor != null)
            {
                this.ISuccessors.Add(successor);
                successor.IPredecessors.Add(this);
                this.LoopExitNode = successor;
            }

            var whileNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
            this.ISuccessors.Add(whileNode);
            whileNode.IPredecessors.Add(this);

            if (stmt.Statement is BlockSyntax)
            {
                whileNode.Construct((stmt.Statement as BlockSyntax).Statements, 0, false, this);
            }
            else
            {
                whileNode.Construct(new SyntaxList<StatementSyntax> { stmt.Statement }, 0, false, this);
            }
        }

        /// <summary>
        /// Handles the given do statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleDoStatement(DoStatementSyntax stmt, ControlFlowGraphNode successor)
        {
            this.SyntaxNodes.Add(stmt.Condition);
            this.IsLoopHeadNode = true;

            if (successor != null)
            {
                this.ISuccessors.Add(successor);
                successor.IPredecessors.Add(this);
                this.LoopExitNode = successor;
            }

            var doNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
            this.ISuccessors.Add(doNode);
            doNode.IPredecessors.Add(this);

            if (stmt.Statement is BlockSyntax)
            {
                doNode.Construct((stmt.Statement as BlockSyntax).Statements, 0, false, this);
            }
            else
            {
                doNode.Construct(new SyntaxList<StatementSyntax> { stmt.Statement }, 0, false, this);
            }
        }

        /// <summary>
        /// Handles the given foreach statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleForeachStatement(ForEachStatementSyntax stmt, ControlFlowGraphNode successor)
        {
            this.SyntaxNodes.Add(stmt.Expression);
            this.IsLoopHeadNode = true;

            if (successor != null)
            {
                this.ISuccessors.Add(successor);
                successor.IPredecessors.Add(this);
                this.LoopExitNode = successor;
            }

            var foreachNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
            this.ISuccessors.Add(foreachNode);
            foreachNode.IPredecessors.Add(this);

            if (stmt.Statement is BlockSyntax)
            {
                foreachNode.Construct((stmt.Statement as BlockSyntax).Statements, 0, false, this);
            }
            else
            {
                foreachNode.Construct(new SyntaxList<StatementSyntax> { stmt.Statement }, 0, false, this);
            }
        }

        /// <summary>
        /// Handles the given switch statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleSwitchStatement(SwitchStatementSyntax stmt, ControlFlowGraphNode successor)
        {
            this.SyntaxNodes.Add(stmt.Expression);
            this.IsJumpNode = true;

            if (stmt.Sections.Count == 0 &&
                successor != null)
            {
                this.ISuccessors.Add(successor);
                successor.IPredecessors.Add(this);
                return;
            }

            for (int idx = 0; idx < stmt.Sections.Count; idx++)
            {
                var statements = stmt.Sections[idx].Statements;
                bool containsBreak = false;
                foreach (var s in statements)
                {
                    if (s is BreakStatementSyntax)
                    {
                        containsBreak = true;
                        break;
                    }
                }

                var switchNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                this.ISuccessors.Add(switchNode);
                switchNode.IPredecessors.Add(this);

                if (containsBreak || idx == stmt.Sections.Count - 1)
                {
                    switchNode.Construct(statements, 0, false, successor);
                }
                else
                {
                    switchNode.Construct(statements, 0, false, null);
                }
            }
        }

        /// <summary>
        /// Handles the given try statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleTryStatement(TryStatementSyntax stmt, ControlFlowGraphNode successor)
        {
            if (this.AnalysisContext.Configuration.AnalyzeExceptionHandling)
            {
                var catchSuccessors = new List<ControlFlowGraphNode>();
                foreach (var catchBlock in stmt.Catches)
                {
                    var catchSucc = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                    catchSucc.Construct(catchBlock.Block.Statements, 0, false, successor);
                    catchSuccessors.Add(catchSucc);
                }

                ControlFlowGraphNode pred = null;
                for (int idx = 0; idx < stmt.Block.Statements.Count; idx++)
                {
                    var tryNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
                    tryNode.IsJumpNode = true;

                    if (idx == 0)
                    {
                        tryNode = this;
                    }

                    if (idx + 1 == stmt.Block.Statements.Count)
                    {
                        tryNode.Construct(stmt.Block.Statements, idx, true, successor);
                    }
                    else
                    {
                        tryNode.Construct(stmt.Block.Statements, idx, true, null);
                    }

                    foreach (var catchNode in catchSuccessors)
                    {
                        tryNode.ISuccessors.Add(catchNode);
                        catchNode.IPredecessors.Add(tryNode);
                    }

                    if (pred != null)
                    {
                        pred.ISuccessors.Add(tryNode);
                        tryNode.IPredecessors.Add(pred);
                    }

                    pred = tryNode;
                }
            }
            else
            {
                this.Construct(stmt.Block.Statements, 0, false, successor);
            }
        }

        /// <summary>
        /// Handles the given using statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleUsingStatement(UsingStatementSyntax stmt, ControlFlowGraphNode successor)
        {
            this.SyntaxNodes.Add(stmt.Declaration);
            this.IsJumpNode = true;

            var usingNode = new ControlFlowGraphNode(this.AnalysisContext, this.Summary);
            this.ISuccessors.Add(usingNode);
            usingNode.IPredecessors.Add(this);

            if (stmt.Statement is BlockSyntax)
            {
                usingNode.Construct((stmt.Statement as BlockSyntax).Statements, 0, false, successor);
            }
            else
            {
                usingNode.Construct(new SyntaxList<StatementSyntax> { stmt.Statement }, 0, false, successor);
            }
        }

        /// <summary>
        /// Returns true if the node is a predecessor of the given node.
        /// Returns false if not.
        /// </summary>
        /// <param name="node">ControlFlowGraphNode</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <returns>Boolean</returns>
        private bool IsPredecessorOf(ControlFlowGraphNode node, HashSet<ControlFlowGraphNode> visited)
        {
            visited.Add(this);

            if (this.ISuccessors.Contains(node))
            {
                return true;
            }

            foreach (var successor in this.ISuccessors.Where(v => !visited.Contains(v)))
            {
                if (successor.IsPredecessorOf(node, visited))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Cleans empty successors.
        /// </summary>
        /// <param name="visited">Already visited cfgNodes</param>
        private void CleanEmptySuccessors(HashSet<ControlFlowGraphNode> visited)
        {
            visited.Add(this);

            var toRemove = new List<ControlFlowGraphNode>();
            foreach (var successor in this.ISuccessors)
            {
                if (successor.SyntaxNodes.Count == 0)
                {
                    toRemove.Add(successor);
                }
            }

            foreach (var successor in toRemove)
            {
                this.ISuccessors.Remove(successor);
            }

            foreach (var successor in this.ISuccessors.Where(v => !visited.Contains(v)))
            {
                successor.CleanEmptySuccessors(visited);
            }
        }

        /// <summary>
        /// Returns all exit nodes in the control flow graph.
        /// </summary>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <returns>Set of exit nodes</returns>
        private HashSet<ControlFlowGraphNode> GetExitNodes(HashSet<ControlFlowGraphNode> visited)
        {
            visited.Add(this);

            var exitNodes = new HashSet<ControlFlowGraphNode>();
            if (this.ISuccessors.Count == 0)
            {
                exitNodes.Add(this);
            }
            else
            {
                foreach (var successor in this.ISuccessors.Where(v => !visited.Contains(v)))
                {
                    var nodes = successor.GetExitNodes(visited);
                    foreach (var node in nodes)
                    {
                        exitNodes.Add(node);
                    }
                }
            }

            return exitNodes;
        }

        /// <summary>
        /// Returns true if the given invocation is a gives up operation.
        /// Returns false if it is not.
        /// </summary>
        /// <param name="call">Call</param>
        /// <returns>Boolean</returns>
        private bool IsGivesUpOperation(InvocationExpressionSyntax call)
        {
            var callee = Querying.GetCalleeOfInvocation(call);
            var model = this.AnalysisContext.Compilation.GetSemanticModel(call.SyntaxTree);
            var callSymbol = model.GetSymbolInfo(call).Symbol;
            if (callSymbol == null)
            {
                return false;
            }

            if (Querying.IsEventSenderInvocation(call, callee, model))
            {
                return true;
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol,
                this.AnalysisContext.Solution).Result;
            if (definition == null)
            {
                return false;
            }

            var calleeMethod = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as BaseMethodDeclarationSyntax;
            if (this.AnalysisContext.Summaries.ContainsKey(calleeMethod) &&
                MethodSummary.Factory.Summarize(this.AnalysisContext, calleeMethod).GivesUpSet.Count > 0)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region debug methods

        internal void DebugPrint(HashSet<ControlFlowGraphNode> visited = null)
        {
            if (visited == null)
            {
                visited = new HashSet<ControlFlowGraphNode> { this };
            }
            else if (visited.Contains(this))
            {
                return;
            }
            else
            {
                visited.Add(this);
            }

            if (this.IsGivesUpNode)
            {
                IO.PrintLine("printing [GivesUp] node {0}:", this.Id);
            }
            else if (this.IsJumpNode)
            {
                IO.PrintLine("printing [Jump] node {0}:", this.Id);
            }
            else if (this.IsLoopHeadNode)
            {
                IO.PrintLine("printing [LoopHead] node {0}:", this.Id);
            }
            else
            {
                IO.PrintLine("printing node {0}:", this.Id);
            }

            foreach (var node in this.SyntaxNodes)
            {
                IO.PrintLine(" > syntax node: " + node);
            }

            foreach (var node in this.ISuccessors)
            {
                node.DebugPrint(visited);
            }
        }

        internal void DebugPrintPredecessors(HashSet<ControlFlowGraphNode> visited = null)
        {
            if (visited == null)
            {
                visited = new HashSet<ControlFlowGraphNode> { this };
            }
            else if (visited.Contains(this))
            {
                return;
            }
            else
            {
                visited.Add(this);
            }

            Console.Write("predecessors of {0}:", this.Id);
            foreach (var node in this.IPredecessors)
            {
                IO.Print(" " + node.Id);
            }

            IO.PrintLine("");
            foreach (var node in this.ISuccessors)
            {
                node.DebugPrintPredecessors(visited);
            }
        }

        internal void DebugPrintSuccessors(HashSet<ControlFlowGraphNode> visited = null)
        {
            if (visited == null)
            {
                visited = new HashSet<ControlFlowGraphNode> { this };
            }
            else if (visited.Contains(this))
            {
                return;
            }
            else
            {
                visited.Add(this);
            }

            Console.Write("successors of {0}:", this.Id);
            foreach (var node in this.ISuccessors)
            {
                IO.Print(" " + node.Id);
            }

            IO.PrintLine("");
            foreach (var node in this.ISuccessors)
            {
                node.DebugPrintSuccessors(visited);
            }
        }

        #endregion
    }
}
