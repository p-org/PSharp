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

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// A control-flow graph node.
    /// </summary>
    public class CFGNode
    {
        #region fields

        /// <summary>
        /// The unique id of the node.
        /// </summary>
        public int Id;

        /// <summary>
        /// Handle to the control-flow graph which contains this node.
        /// </summary>
        public ControlFlowGraph ControlFlowGraph;

        /// <summary>
        /// List of statements.
        /// </summary>
        public List<Statement> Statements;

        /// <summary>
        /// Set of the immediate successors of the node.
        /// </summary>
        internal HashSet<CFGNode> ISuccessors;

        /// <summary>
        /// Set of the immediate predecessors of the node.
        /// </summary>
        internal HashSet<CFGNode> IPredecessors;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cfg">ControlFlowGraph</param>
        internal CFGNode(ControlFlowGraph cfg)
        {
            this.Id = cfg.Nodes.Count;
            this.ControlFlowGraph = cfg;
            this.Statements = new List<Statement>();
            this.ISuccessors = new HashSet<CFGNode>();
            this.IPredecessors = new HashSet<CFGNode>();

            cfg.Nodes.Add(this);
        }

        #endregion

        #region public API

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
        /// Returns the immediate predecessors of this
        /// control-flow graph node.
        /// </summary>
        /// <returns>Predecessors</returns>
        public IEnumerable<CFGNode> GetImmediatePredecessors()
        {
            return this.IPredecessors.ToList();
        }

        /// <summary>
        /// Returns true if the node is a successor of the given node.
        /// Returns false if not.
        /// </summary>
        /// <param name="node">CFGNode</param>
        /// <returns>Boolean</returns>
        public bool IsSuccessorOf(CFGNode node)
        {
            return this.IsSuccessorOf(node, new HashSet<CFGNode>());
        }

        /// <summary>
        /// Returns true if the node is a predecessor of the given node.
        /// Returns false if not.
        /// </summary>
        /// <param name="node">CFGNode</param>
        /// <returns>Boolean</returns>
        public bool IsPredecessorOf(CFGNode node)
        {
            return this.IsPredecessorOf(node, new HashSet<CFGNode>());
        }

        /// <summary>
        /// Returns all exit nodes in the control-flow graph.
        /// </summary>
        /// <returns>Set of exit nodes</returns>
        internal HashSet<CFGNode> GetExitNodes()
        {
            return this.GetExitNodes(new HashSet<CFGNode>());
        }

        #endregion

        #region internal API

        /// <summary>
        /// Constructs the control-flow graph of the given method.
        /// </summary>
        /// <param name="method">Method</param>
        internal void Construct(BaseMethodDeclarationSyntax method)
        {
            this.Statements.Add(Statement.Create(method.ParameterList, this));
            this.Construct(this.GetStatements(method.Body), null, null);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructs the control-flow graph of the node.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">CFGNode</param>
        /// <param name="innerLoopHead">LoopHeadCFGNode</param>
        private void Construct(List<StatementSyntax> stmtList, CFGNode successor,
            LoopHeadCFGNode innerLoopHead)
        {
            while (stmtList.Count > 0)
            {
                if (stmtList[0] is IfStatementSyntax)
                {
                    this.ConstructIfThenElseBranch(stmtList, successor, innerLoopHead);
                    return;
                }
                else if (stmtList[0] is ForStatementSyntax)
                {
                    this.ConstructForLoop(stmtList, successor);
                    return;
                }
                else if (stmtList[0] is WhileStatementSyntax)
                {
                    this.ConstructWhileLoop(stmtList, successor);
                    return;
                }
                else if (stmtList[0] is ForEachStatementSyntax)
                {
                    this.ConstructForeachLoop(stmtList, successor);
                    return;
                }
                else if (stmtList[0] is DoStatementSyntax)
                {
                    this.ConstructDoWhileLoop(stmtList, successor);
                    return;
                }
                else if (stmtList[0] is UsingStatementSyntax)
                {
                    this.ConstructUsingBlock(stmtList, successor, innerLoopHead);
                    return;
                }
                else if (stmtList[0] is ReturnStatementSyntax)
                {
                    this.Statements.Add(Statement.Create(stmtList[0], this));
                    stmtList.RemoveAt(0);
                    return;
                }
                else if (stmtList[0] is ContinueStatementSyntax)
                {
                    stmtList.RemoveAt(0);
                    this.ControlFlowGraph.AddEdge(this, innerLoopHead);
                    return;
                }
                else if (stmtList[0] is BreakStatementSyntax)
                {
                    stmtList.RemoveAt(0);
                    this.ControlFlowGraph.AddEdge(this, innerLoopHead.LoopExitNode);
                    return;
                }
                else if (stmtList[0] is ExpressionStatementSyntax ||
                    stmtList[0] is LocalDeclarationStatementSyntax)
                {
                    this.Statements.Add(Statement.Create(stmtList[0], this));
                    stmtList.RemoveAt(0);
                }
            }

            if (stmtList.Count == 0 && successor != null)
            {
                this.ControlFlowGraph.AddEdge(this, successor);
            }
        }

        /// <summary>
        /// Constructs an if-then-else branch.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">CFGNode</param>
        /// <param name="innerLoopHead">LoopHeadCFGNode</param>
        private void ConstructIfThenElseBranch(List<StatementSyntax> stmtList,
            CFGNode successor, LoopHeadCFGNode innerLoopHead)
        {
            var ifStmt = stmtList[0] as IfStatementSyntax;
            stmtList.RemoveAt(0);

            CFGNode ifBranchNode = new CFGNode(this.ControlFlowGraph);
            ifBranchNode.Statements.Add(Statement.Create(ifStmt.Condition, ifBranchNode));
            this.ControlFlowGraph.AddEdge(this, ifBranchNode);

            CFGNode elseBranchNode = null;
            CFGNode falseNode = null;

            if (ifStmt.Else != null)
            {
                elseBranchNode = new CFGNode(this.ControlFlowGraph);
                falseNode = new CFGNode(this.ControlFlowGraph);

                falseNode.Construct(stmtList, successor, innerLoopHead);
                elseBranchNode.Construct(this.GetStatements(ifStmt.Else.Statement),
                    falseNode, innerLoopHead);
                this.ControlFlowGraph.AddEdge(ifBranchNode, elseBranchNode);
            }
            else
            {
                falseNode = new CFGNode(this.ControlFlowGraph);
                falseNode.Construct(stmtList, successor, innerLoopHead);
                this.ControlFlowGraph.AddEdge(ifBranchNode, falseNode);
            }

            CFGNode trueNode = new CFGNode(this.ControlFlowGraph);
            trueNode.Construct(this.GetStatements(ifStmt.Statement),
                falseNode, innerLoopHead);
            this.ControlFlowGraph.AddEdge(ifBranchNode, trueNode);
        }

        /// <summary>
        /// Constructs a for loop.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">CFGNode</param>
        private void ConstructForLoop(List<StatementSyntax> stmtList, CFGNode successor)
        {
            var loopHeadStmt = stmtList[0] as ForStatementSyntax;
            stmtList.RemoveAt(0);

            this.ConstructLoop(loopHeadStmt.Condition, loopHeadStmt.Statement,
                stmtList, successor);
        }

        /// <summary>
        /// Constructs a while loop.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">CFGNode</param>
        private void ConstructWhileLoop(List<StatementSyntax> stmtList, CFGNode successor)
        {
            var loopHeadStmt = stmtList[0] as WhileStatementSyntax;
            stmtList.RemoveAt(0);

            this.ConstructLoop(loopHeadStmt.Condition, loopHeadStmt.Statement,
                stmtList, successor);
        }

        /// <summary>
        /// Constructs a foreach loop.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">CFGNode</param>
        private void ConstructForeachLoop(List<StatementSyntax> stmtList, CFGNode successor)
        {
            var loopHeadStmt = stmtList[0] as ForEachStatementSyntax;
            stmtList.RemoveAt(0);

            this.ConstructLoop(loopHeadStmt.Expression, loopHeadStmt.Statement,
                stmtList, successor);
        }

        /// <summary>
        /// Constructs a loop from the specified loop guard and body.
        /// </summary>
        /// <param name="loopGuard">ExpressionSyntax</param>
        /// <param name="loopBody">StatementSyntax</param>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">CFGNode</param>
        private void ConstructLoop(ExpressionSyntax loopGuard, StatementSyntax loopBody,
            List<StatementSyntax> stmtList, CFGNode successor)
        {
            CFGNode falseNode = new CFGNode(this.ControlFlowGraph);
            CFGNode trueNode = new CFGNode(this.ControlFlowGraph);
            LoopHeadCFGNode loopHeadNode = new LoopHeadCFGNode(this.ControlFlowGraph, falseNode);

            loopHeadNode.Statements.Add(Statement.Create(loopGuard, loopHeadNode));
            falseNode.Construct(stmtList, successor, loopHeadNode);
            trueNode.Construct(this.GetStatements(loopBody), loopHeadNode, loopHeadNode);

            this.ControlFlowGraph.AddEdge(this, loopHeadNode);
            this.ControlFlowGraph.AddEdge(loopHeadNode, falseNode);
            this.ControlFlowGraph.AddEdge(loopHeadNode, trueNode);
        }

        /// <summary>
        /// Constructs a do-while loop.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">CFGNode</param>
        private void ConstructDoWhileLoop(List<StatementSyntax> stmtList, CFGNode successor)
        {
            var loopHeadStmt = stmtList[0] as DoStatementSyntax;
            stmtList.RemoveAt(0);

            CFGNode falseNode = new CFGNode(this.ControlFlowGraph);
            CFGNode trueNode = new CFGNode(this.ControlFlowGraph);
            LoopHeadCFGNode loopHeadNode = new LoopHeadCFGNode(this.ControlFlowGraph, falseNode);

            trueNode.Construct(this.GetStatements(loopHeadStmt.Statement),
                loopHeadNode, loopHeadNode);
            loopHeadNode.Statements.Add(Statement.Create(loopHeadStmt.Condition, loopHeadNode));
            falseNode.Construct(stmtList, successor, loopHeadNode);

            this.ControlFlowGraph.AddEdge(this, trueNode);
            this.ControlFlowGraph.AddEdge(loopHeadNode, falseNode);
            this.ControlFlowGraph.AddEdge(loopHeadNode, trueNode);
        }

        /// <summary>
        /// Constructs a using block.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">CFGNode</param>
        /// <param name="innerLoopHead">LoopHeadCFGNode</param>
        private void ConstructUsingBlock(List<StatementSyntax> stmtList,
            CFGNode successor, LoopHeadCFGNode innerLoopHead)
        {
            var usingStmt = stmtList[0] as UsingStatementSyntax;
            stmtList.RemoveAt(0);

            CFGNode usingNode = new CFGNode(this.ControlFlowGraph);

            if (usingStmt.Declaration != null)
            {
                usingNode.Statements.Add(Statement.Create(usingStmt.Declaration, usingNode));
            }
            else
            {
                usingNode.Statements.Add(Statement.Create(usingStmt.Expression, usingNode));
            }

            CFGNode afterUsingNode = new CFGNode(this.ControlFlowGraph);
            afterUsingNode.Construct(stmtList, successor, innerLoopHead);

            CFGNode bodyNode = new CFGNode(this.ControlFlowGraph);
            bodyNode.Construct(this.GetStatements(usingStmt.Statement),
                afterUsingNode, innerLoopHead);

            this.ControlFlowGraph.AddEdge(this, usingNode);
            this.ControlFlowGraph.AddEdge(usingNode, bodyNode);
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Returns a list containing all statements in the given block.
        /// </summary>
        /// <param name="statement">StatementSyntax</param>
        /// <returns>SyntaxList</returns>
        private List<StatementSyntax> GetStatements(StatementSyntax statement)
        {
            if (statement is BlockSyntax)
            {
                return (statement as BlockSyntax).Statements.ToList();
            }

            return new List<StatementSyntax> { statement };
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

        #endregion

        #region printing methods

        public override string ToString()
        {
            return $"[cfg::{this.Id}]";
        }

        /// <summary>
        /// Prints the nodes.
        /// </summary>
        internal void PrintNodes()
        {
            this.PrintNodes(null);
        }

        /// <summary>
        /// Prints the nodes.
        /// </summary>
        /// <param name="visited">Set of visited CFG nodes</param>
        private void PrintNodes(HashSet<CFGNode> visited)
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

            Console.WriteLine("... |");
            Console.WriteLine("... | . CFG '{0}'", this.Id);

            foreach (var node in this.Statements)
            {
                Console.WriteLine("... | ... {0}", node.SyntaxNode);
            }
            
            string successors = "... | ..... successors:";
            foreach (var node in this.ISuccessors)
            {
                successors += $" '{node.Id}'";
            }

            if (this.ISuccessors.Count == 0)
            {
                successors += " 'Exit'";
            }

            string predecessors = "... | ..... predecessors:";
            foreach (var node in this.IPredecessors)
            {
                predecessors += $" '{node.Id}'";
            }

            if (this.IPredecessors.Count == 0)
            {
                predecessors += " 'Entry'";
            }

            Console.WriteLine(successors);
            Console.WriteLine(predecessors);

            foreach (var node in this.ISuccessors)
            {
                node.PrintNodes(visited);
            }
        }

        #endregion
    }
}
