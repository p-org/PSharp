//-----------------------------------------------------------------------
// <copyright file="CFGNode.cs">
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

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// A control-flow graph node.
    /// </summary>
    public class CFGNode
    {
        #region fields

        /// <summary>
        /// The analysis context.
        /// </summary>
        protected AnalysisContext AnalysisContext;

        /// <summary>
        /// The unique ID of the node.
        /// </summary>
        internal int Id;

        /// <summary>
        /// Handle to the summary of the method which owns this node.
        /// </summary>
        protected MethodSummary Summary;

        /// <summary>
        /// List of syntax nodes.
        /// </summary>
        internal List<SyntaxNode> SyntaxNodes;

        /// <summary>
        /// Set of the immediate predecessors of the node.
        /// </summary>
        protected HashSet<CFGNode> IPredecessors;

        /// <summary>
        /// Set of the immediate successors of the node.
        /// </summary>
        protected HashSet<CFGNode> ISuccessors;

        /// <summary>
        /// The node after exiting the loop.
        /// </summary>
        private CFGNode LoopExitNode;

        /// <summary>
        /// True if the node is a jump node. False by default.
        /// </summary>
        internal bool IsJumpNode;

        /// <summary>
        /// True if the node is a loop head node. False by default.
        /// </summary>
        internal bool IsLoopHeadNode;

        /// <summary>
        /// The description of the node.
        /// </summary>
        protected string Description;

        /// <summary>
        /// A counter for creating unique IDs.
        /// </summary>
        private static int IdCounter = 0;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="summary">MethodSummary</param>
        public CFGNode(AnalysisContext context, MethodSummary summary)
        {
            this.AnalysisContext = context;
            this.Id = CFGNode.IdCounter++;
            this.Summary = summary;
            this.SyntaxNodes = new List<SyntaxNode>();
            this.IPredecessors = new HashSet<CFGNode>();
            this.ISuccessors = new HashSet<CFGNode>();
            this.IsJumpNode = false;
            this.IsLoopHeadNode = false;
            this.Description = "";
        }

        /// <summary>
        /// Creates a control-flow graph node.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>CFGNode</returns>
        protected virtual CFGNode CreateNode(AnalysisContext context, MethodSummary summary)
        {
            return this.CreateNode(context, summary);
        }

        #endregion

        #region public API

        /// <summary>
        /// Constructs the control-flow graph of the given method.
        /// </summary>
        /// <param name="method">Method</param>
        public void Construct(BaseMethodDeclarationSyntax method)
        {
            this.Construct(method.Body.Statements, 0, null);
        }

        /// <summary>
        /// Returns the method summary that contains this
        /// control-flow graph node.
        /// </summary>
        /// <returns>MethodSummary</returns>
        public MethodSummary GetMethodSummary()
        {
            return this.Summary;
        }

        /// <summary>
        /// Returns the immediate predecessors of this
        /// control-flow graph node.
        /// </summary>
        /// <returns>Predecessors</returns>
        public IEnumerable<CFGNode> GetImmediatePredecessors()
        {
            return this.IPredecessors.ToList();
        }

        /// <summary>
        /// Returns the immediate successors of this
        /// control-flow graph node.
        /// </summary>
        /// <returns>Successors</returns>
        public IEnumerable<CFGNode> GetImmediateSuccessors()
        {
            return this.ISuccessors.ToList();
        }

        /// <summary>
        /// Returns true if the node is a predecessor of the given node.
        /// Returns false if not.
        /// </summary>
        /// <param name="node">CFGNode</param>
        /// <returns>Boolean</returns>
        internal bool IsPredecessorOf(CFGNode node)
        {
            return this.IsPredecessorOf(node, new HashSet<CFGNode>());
        }

        /// <summary>
        /// Returns true if the node is a successor of the given node.
        /// Returns false if not.
        /// </summary>
        /// <param name="node">CFGNode</param>
        /// <returns>Boolean</returns>
        internal bool IsSuccessorOf(CFGNode node)
        {
            return this.IsSuccessorOf(node, new HashSet<CFGNode>());
        }

        /// <summary>
        /// Returns all exit nodes in the control-flow graph.
        /// </summary>
        /// <returns>Set of exit nodes</returns>
        internal HashSet<CFGNode> GetExitNodes()
        {
            return this.GetExitNodes(new HashSet<CFGNode>());
        }

        /// <summary>
        /// Cleans empty successors.
        /// </summary>
        internal void CleanEmptySuccessors()
        {
            this.CleanEmptySuccessors(new HashSet<CFGNode>());
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Constructs the node from the given list of statements starting
        /// at the given index.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="index">Statement index</param>
        /// <param name="successor">Successor</param>
        protected void Construct(SyntaxList<StatementSyntax> stmtList, int index, CFGNode successor)
        {
            for (int idx = index; idx < stmtList.Count; idx++)
            {
                CFGNode jumpNode = null;
                CFGNode succNode = null;
                
                CFGNode specialNode = this.CreateSingleStatementCFGNode(stmtList[idx]);
                if (specialNode != null)
                {
                    if (idx < stmtList.Count - 1 &&
                        stmtList[idx + 1] is BreakStatementSyntax)
                    {
                        if (successor != null &&
                            successor.LoopExitNode != null)
                        {
                            specialNode.ISuccessors.Add(successor.LoopExitNode);
                            successor.LoopExitNode.IPredecessors.Add(specialNode);
                        }
                    }
                    else if (idx < stmtList.Count - 1 &&
                        stmtList[idx + 1] is ContinueStatementSyntax)
                    {
                        if (successor != null)
                        {
                            specialNode.ISuccessors.Add(successor);
                            successor.IPredecessors.Add(specialNode);
                        }
                    }
                    else if (idx < stmtList.Count - 1)
                    {
                        succNode = this.CreateNode(this.AnalysisContext, this.Summary);
                        specialNode.ISuccessors.Add(succNode);
                        succNode.IPredecessors.Add(specialNode);
                        succNode.Construct(stmtList, idx + 1, successor);
                    }
                    else if (successor != null)
                    {
                        specialNode.ISuccessors.Add(successor);
                        successor.IPredecessors.Add(specialNode);
                    }

                    return;
                }

                if (stmtList[idx] is ExpressionStatementSyntax ||
                    stmtList[idx] is LocalDeclarationStatementSyntax)
                {
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
                    jumpNode = this.CreateNode(this.AnalysisContext, this.Summary);
                    this.ISuccessors.Add(jumpNode);
                    jumpNode.IPredecessors.Add(this);
                }

                if (stmtList[idx] is IfStatementSyntax)
                {
                    if (idx < stmtList.Count - 1)
                    {
                        succNode = this.CreateNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleIfStatement(stmtList[idx] as IfStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleIfStatement(stmtList[idx] as IfStatementSyntax, successor);
                    }
                }
                else if (stmtList[idx] is ForStatementSyntax)
                {
                    if (idx < stmtList.Count - 1)
                    {
                        succNode = this.CreateNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleForStatement(stmtList[idx] as ForStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleForStatement(stmtList[idx] as ForStatementSyntax, successor);
                    }
                }
                else if (stmtList[idx] is WhileStatementSyntax)
                {
                    if (idx < stmtList.Count - 1)
                    {
                        succNode = this.CreateNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleWhileStatement(stmtList[idx] as WhileStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleWhileStatement(stmtList[idx] as WhileStatementSyntax, successor);
                    }
                }
                else if (stmtList[idx] is DoStatementSyntax)
                {
                    if (idx < stmtList.Count - 1)
                    {
                        succNode = this.CreateNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleDoStatement(stmtList[idx] as DoStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleDoStatement(stmtList[idx] as DoStatementSyntax, successor);
                    }
                }
                else if (stmtList[idx] is ForEachStatementSyntax)
                {
                    if (idx < stmtList.Count - 1)
                    {
                        succNode = this.CreateNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleForeachStatement(stmtList[idx] as ForEachStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleForeachStatement(stmtList[idx] as ForEachStatementSyntax, successor);
                    }
                }
                else if (stmtList[idx] is SwitchStatementSyntax)
                {
                    if (idx < stmtList.Count - 1)
                    {
                        succNode = this.CreateNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleSwitchStatement(stmtList[idx] as SwitchStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleSwitchStatement(stmtList[idx] as SwitchStatementSyntax, successor);
                    }
                }
                else if (stmtList[idx] is TryStatementSyntax)
                {
                    if (idx < stmtList.Count - 1)
                    {
                        succNode = this.CreateNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleTryStatement(stmtList[idx] as TryStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, successor);
                        return;
                    }
                    else
                    {
                        jumpNode.HandleTryStatement(stmtList[idx] as TryStatementSyntax, successor);
                    }
                }
                else if (stmtList[idx] is UsingStatementSyntax)
                {
                    if (idx < stmtList.Count - 1)
                    {
                        succNode = this.CreateNode(this.AnalysisContext, this.Summary);
                        jumpNode.HandleUsingStatement(stmtList[idx] as UsingStatementSyntax, succNode);
                        succNode.Construct(stmtList, idx + 1, successor);
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
        /// Creates a single statement control-flow graph node
        /// using the given statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <returns>CFGNode</returns>
        protected virtual CFGNode CreateSingleStatementCFGNode(StatementSyntax stmt)
        {
            return null;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Handles the given if statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleIfStatement(IfStatementSyntax stmt, CFGNode successor)
        {
            this.SyntaxNodes.Add(stmt.Condition);
            this.IsJumpNode = true;
            this.Description = "Jump";

            var ifNode = this.CreateNode(this.AnalysisContext, this.Summary);
            this.ISuccessors.Add(ifNode);
            ifNode.IPredecessors.Add(this);

            if (stmt.Statement is BlockSyntax)
            {
                ifNode.Construct((stmt.Statement as BlockSyntax).Statements, 0, successor);
            }
            else
            {
                ifNode.Construct(new SyntaxList<StatementSyntax> { stmt.Statement }, 0, successor);
            }

            if (stmt.Else != null)
            {
                var elseNode = this.CreateNode(this.AnalysisContext, this.Summary);
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
                        elseNode.Construct((stmt.Else.Statement as BlockSyntax).Statements, 0, successor);
                    }
                    else
                    {
                        elseNode.Construct(new SyntaxList<StatementSyntax> { stmt.Else.Statement }, 0, successor);
                    }
                }
            }
            else if (successor != null)
            {
                this.ISuccessors.Add(successor);
                successor.IPredecessors.Add(this);
            }
        }

        /// <summary>
        /// Handles the given for statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleForStatement(ForStatementSyntax stmt, CFGNode successor)
        {
            this.SyntaxNodes.Add(stmt.Condition);
            this.IsLoopHeadNode = true;
            this.Description = "LoopHead";

            if (successor != null)
            {
                this.ISuccessors.Add(successor);
                successor.IPredecessors.Add(this);
                this.LoopExitNode = successor;
            }

            var forNode = this.CreateNode(this.AnalysisContext, this.Summary);
            this.ISuccessors.Add(forNode);
            forNode.IPredecessors.Add(this);

            if (stmt.Statement is BlockSyntax)
            {
                forNode.Construct((stmt.Statement as BlockSyntax).Statements, 0, this);
            }
            else
            {
                forNode.Construct(new SyntaxList<StatementSyntax> { stmt.Statement }, 0, this);
            }
        }

        /// <summary>
        /// Handles the given while statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleWhileStatement(WhileStatementSyntax stmt, CFGNode successor)
        {
            this.SyntaxNodes.Add(stmt.Condition);
            this.IsLoopHeadNode = true;
            this.Description = "LoopHead";

            if (successor != null)
            {
                this.ISuccessors.Add(successor);
                successor.IPredecessors.Add(this);
                this.LoopExitNode = successor;
            }

            var whileNode = this.CreateNode(this.AnalysisContext, this.Summary);
            this.ISuccessors.Add(whileNode);
            whileNode.IPredecessors.Add(this);

            if (stmt.Statement is BlockSyntax)
            {
                whileNode.Construct((stmt.Statement as BlockSyntax).Statements, 0, this);
            }
            else
            {
                whileNode.Construct(new SyntaxList<StatementSyntax> { stmt.Statement }, 0, this);
            }
        }

        /// <summary>
        /// Handles the given do statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleDoStatement(DoStatementSyntax stmt, CFGNode successor)
        {
            this.SyntaxNodes.Add(stmt.Condition);
            this.IsLoopHeadNode = true;
            this.Description = "LoopHead";

            if (successor != null)
            {
                this.ISuccessors.Add(successor);
                successor.IPredecessors.Add(this);
                this.LoopExitNode = successor;
            }

            var doNode = this.CreateNode(this.AnalysisContext, this.Summary);
            this.ISuccessors.Add(doNode);
            doNode.IPredecessors.Add(this);

            if (stmt.Statement is BlockSyntax)
            {
                doNode.Construct((stmt.Statement as BlockSyntax).Statements, 0, this);
            }
            else
            {
                doNode.Construct(new SyntaxList<StatementSyntax> { stmt.Statement }, 0, this);
            }
        }

        /// <summary>
        /// Handles the given foreach statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleForeachStatement(ForEachStatementSyntax stmt, CFGNode successor)
        {
            this.SyntaxNodes.Add(stmt.Expression);
            this.IsLoopHeadNode = true;
            this.Description = "LoopHead";

            if (successor != null)
            {
                this.ISuccessors.Add(successor);
                successor.IPredecessors.Add(this);
                this.LoopExitNode = successor;
            }

            var foreachNode = this.CreateNode(this.AnalysisContext, this.Summary);
            this.ISuccessors.Add(foreachNode);
            foreachNode.IPredecessors.Add(this);

            if (stmt.Statement is BlockSyntax)
            {
                foreachNode.Construct((stmt.Statement as BlockSyntax).Statements, 0, this);
            }
            else
            {
                foreachNode.Construct(new SyntaxList<StatementSyntax> { stmt.Statement }, 0, this);
            }
        }

        /// <summary>
        /// Handles the given switch statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleSwitchStatement(SwitchStatementSyntax stmt, CFGNode successor)
        {
            this.SyntaxNodes.Add(stmt.Expression);
            this.IsJumpNode = true;
            this.Description = "Jump";

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

                var switchNode = this.CreateNode(this.AnalysisContext, this.Summary);
                this.ISuccessors.Add(switchNode);
                switchNode.IPredecessors.Add(this);

                if (containsBreak || idx == stmt.Sections.Count - 1)
                {
                    switchNode.Construct(statements, 0, successor);
                }
                else
                {
                    switchNode.Construct(statements, 0, null);
                }
            }
        }

        /// <summary>
        /// Handles the given try statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleTryStatement(TryStatementSyntax stmt, CFGNode successor)
        {
            this.Construct(stmt.Block.Statements, 0, successor);
        }

        /// <summary>
        /// Handles the given using statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="successor">Successor</param>
        private void HandleUsingStatement(UsingStatementSyntax stmt, CFGNode successor)
        {
            this.SyntaxNodes.Add(stmt.Declaration);
            this.IsJumpNode = true;
            this.Description = "Jump";

            var usingNode = this.CreateNode(this.AnalysisContext, this.Summary);
            this.ISuccessors.Add(usingNode);
            usingNode.IPredecessors.Add(this);

            if (stmt.Statement is BlockSyntax)
            {
                usingNode.Construct((stmt.Statement as BlockSyntax).Statements, 0, successor);
            }
            else
            {
                usingNode.Construct(new SyntaxList<StatementSyntax> { stmt.Statement }, 0, successor);
            }
        }

        /// <summary>
        /// Returns true if the node is a predecessor of the given node.
        /// Returns false if not.
        /// </summary>
        /// <param name="node">CFGNode</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <returns>Boolean</returns>
        private bool IsPredecessorOf(CFGNode node, HashSet<CFGNode> visited)
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
        /// Returns true if the node is a successor of the given node.
        /// Returns false if not.
        /// </summary>
        /// <param name="node">CFGNode</param>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <returns>Boolean</returns>
        private bool IsSuccessorOf(CFGNode node, HashSet<CFGNode> visited)
        {
            visited.Add(this);

            if (this.IPredecessors.Contains(node))
            {
                return true;
            }

            foreach (var predecessor in this.IPredecessors.Where(v => !visited.Contains(v)))
            {
                if (predecessor.IsSuccessorOf(node, visited))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns all exit nodes in the control-flow graph.
        /// </summary>
        /// <param name="visited">Already visited cfgNodes</param>
        /// <returns>Set of exit nodes</returns>
        private HashSet<CFGNode> GetExitNodes(HashSet<CFGNode> visited)
        {
            visited.Add(this);

            var exitNodes = new HashSet<CFGNode>();
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
        /// Cleans empty successors.
        /// </summary>
        /// <param name="visited">Already visited cfgNodes</param>
        private void CleanEmptySuccessors(HashSet<CFGNode> visited)
        {
            visited.Add(this);

            var toRemove = new List<CFGNode>();
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

        #endregion

        #region control-flow summary printing methods

        /// <summary>
        /// Prints the control-flow graph nodes.
        /// </summary>
        internal void PrintCFGNodes()
        {
            IO.PrintLine("... |");
            IO.PrintLine("... | . List of CFG nodes");
            this.PrintCFGNodes(null);
        }

        /// <summary>
        /// Prints the control-flow graph successor nodes.
        /// </summary>
        internal void PrintCFGSuccessors()
        {
            IO.PrintLine("... |");
            IO.PrintLine("... | . Successor CFG nodes");
            this.PrintCFGSuccessors(null);
        }

        /// <summary>
        /// Prints the control-flow graph predecessor nodes.
        /// </summary>
        internal void PrintCFGPredecessors()
        {
            IO.PrintLine("... |");
            IO.PrintLine("... | . Predecessor CFG nodes");
            this.PrintCFGPredecessors(null);
        }

        /// <summary>
        /// Prints the control-flow graph nodes.
        /// </summary>
        /// <param name="visited">Set of visited CFG nodes</param>
        private void PrintCFGNodes(HashSet<CFGNode> visited)
        {
            if (visited == null)
            {
                visited = new HashSet<CFGNode> { this };
            }
            else if (visited.Contains(this))
            {
                return;
            }
            else
            {
                visited.Add(this);
            }

            if (this.Description.Length > 0)
            {
                IO.PrintLine("... | ... [{0}] '{1}'", this.Description, this.Id);
            }
            else
            {
                IO.PrintLine("... | ... '{0}'", this.Id);
            }

            foreach (var node in this.SyntaxNodes)
            {
                IO.PrintLine("... | ..... '{0}'", node);
            }

            foreach (var node in this.ISuccessors)
            {
                node.PrintCFGNodes(visited);
            }
        }

        /// <summary>
        /// Prints the control-flow graph successor nodes.
        /// </summary>
        /// <param name="visited">Set of visited CFG nodes</param>
        private void PrintCFGSuccessors(HashSet<CFGNode> visited)
        {
            if (visited == null)
            {
                visited = new HashSet<CFGNode> { this };
            }
            else if (visited.Contains(this))
            {
                return;
            }
            else
            {
                visited.Add(this);
            }

            IO.Print("... | ... cfg '{0}': ", this.Id);
            foreach (var node in this.ISuccessors)
            {
                IO.Print(" '{0}'", node.Id);
            }

            IO.PrintLine("");
            foreach (var node in this.ISuccessors)
            {
                node.PrintCFGSuccessors(visited);
            }
        }

        /// <summary>
        /// Prints the control-flow graph predecessor nodes.
        /// </summary>
        /// <param name="visited">Set of visited CFG nodes</param>
        private void PrintCFGPredecessors(HashSet<CFGNode> visited)
        {
            if (visited == null)
            {
                visited = new HashSet<CFGNode> { this };
            }
            else if (visited.Contains(this))
            {
                return;
            }
            else
            {
                visited.Add(this);
            }

            IO.Print("... | ... cfg '{0}': ", this.Id);
            foreach (var node in this.IPredecessors)
            {
                IO.Print(" '{0}'", node.Id);
            }

            IO.PrintLine("");
            foreach (var node in this.ISuccessors)
            {
                node.PrintCFGPredecessors(visited);
            }
        }

        #endregion
    }
}
