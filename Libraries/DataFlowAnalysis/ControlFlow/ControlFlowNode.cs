//-----------------------------------------------------------------------
// <copyright file="ControlFlowNode.cs">
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
    internal class ControlFlowNode : IControlFlowNode
    {
        #region fields

        /// <summary>
        /// The unique id of the node.
        /// </summary>
        internal int Id;

        /// <summary>
        /// Graph that contains this node.
        /// </summary>
        public IGraph<IControlFlowNode> Graph { get; private set; }

        /// <summary>
        /// Method summary that contains this node.
        /// </summary>
        public MethodSummary Summary { get; private set; }

        /// <summary>
        /// List of statements contained in the node.
        /// </summary>
        public IList<Statement> Statements { get; private set; }

        /// <summary>
        /// Set of the immediate successors.
        /// </summary>
        public ISet<IControlFlowNode> ISuccessors { get; private set; }

        /// <summary>
        /// Set of the immediate predecessors.
        /// </summary>
        public ISet<IControlFlowNode> IPredecessors { get; private set; }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cfg">ControlFlowGraph</param>
        /// <param name="summary">MethodSummary</param>
        protected ControlFlowNode(IGraph<IControlFlowNode> cfg, MethodSummary summary)
        {
            this.Id = cfg.Nodes.Count;
            this.Graph = cfg;
            this.Summary = summary;
            this.Statements = new List<Statement>();
            this.ISuccessors = new HashSet<IControlFlowNode>();
            this.IPredecessors = new HashSet<IControlFlowNode>();
            cfg.Nodes.Add(this);
        }

        /// <summary>
        /// Creates the control-flow graph nodes of the
        /// specified method summary.
        /// </summary>
        /// <param name="cfg">ControlFlowGraph</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>IControlFlowNode</returns>
        internal static IControlFlowNode Create(ControlFlowGraph cfg, MethodSummary summary)
        {
            var entryNode = new ControlFlowNode(cfg, summary);
            entryNode.Statements.Add(Statement.Create(summary.Method.ParameterList,
                entryNode, summary));
            entryNode.Construct(entryNode.GetStatements(summary.Method.Body), null, null);
            return entryNode;
        }

        #endregion

        #region public methods
        
        /// <summary>
        /// Checks the node is a successor of the specified node.
        /// </summary>
        /// <param name="node">INode</param>
        /// <returns>Boolean</returns>
        public bool IsSuccessorOf(IControlFlowNode node)
        {
            return this.Graph.IsSuccessorOf(this, node);
        }

        /// <summary>
        /// Checks the node is a predecessor of the specified node.
        /// </summary>
        /// <param name="node">INode</param>
        /// <returns>Boolean</returns>
        public bool IsPredecessorOf(IControlFlowNode node)
        {
            return this.Graph.IsPredecessorOf(this, node);
        }

        /// <summary>
        /// Checks if the node contains the specified item.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool Contains<T>(T item)
        {
            return false;
        }

        /// <summary>
        /// Checks if the node is empty.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsEmpty()
        {
            if (this.Statements.Count == 0)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructs the control-flow graph of the node.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">ControlFlowNode</param>
        /// <param name="innerLoopHead">LoopHeadControlFlowNode</param>
        private void Construct(List<StatementSyntax> stmtList, ControlFlowNode successor,
            LoopHeadControlFlowNode innerLoopHead)
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
                    this.ConstructForLoop(stmtList, successor, innerLoopHead);
                    return;
                }
                else if (stmtList[0] is WhileStatementSyntax)
                {
                    this.ConstructWhileLoop(stmtList, successor, innerLoopHead);
                    return;
                }
                else if (stmtList[0] is ForEachStatementSyntax)
                {
                    this.ConstructForeachLoop(stmtList, successor, innerLoopHead);
                    return;
                }
                else if (stmtList[0] is DoStatementSyntax)
                {
                    this.ConstructDoWhileLoop(stmtList, successor, innerLoopHead);
                    return;
                }
                else if (stmtList[0] is SwitchStatementSyntax)
                {
                    this.ConstructSwitchBlock(stmtList, successor, innerLoopHead);
                    return;
                }
                else if (stmtList[0] is UsingStatementSyntax)
                {
                    this.ConstructUsingBlock(stmtList, successor, innerLoopHead);
                    return;
                }
                else if (stmtList[0] is BlockSyntax)
                {
                    this.ConstructNakedCodeBlock(stmtList, successor, innerLoopHead);
                    return;
                }
                else if (stmtList[0] is ReturnStatementSyntax)
                {
                    this.Statements.Add(Statement.Create(stmtList[0], this, this.Summary));
                    stmtList.RemoveAt(0);
                    return;
                }
                else if (stmtList[0] is ContinueStatementSyntax)
                {
                    stmtList.RemoveAt(0);
                    this.ISuccessors.Add(innerLoopHead);
                    innerLoopHead.IPredecessors.Add(this);
                    return;
                }
                else if (stmtList[0] is BreakStatementSyntax)
                {
                    stmtList.RemoveAt(0);
                    if (innerLoopHead != null)
                    {
                        this.ISuccessors.Add(innerLoopHead.LoopExitNode);
                        innerLoopHead.LoopExitNode.IPredecessors.Add(this);
                        return;
                    }
                    else
                    {
                        stmtList.Clear();
                    }
                }
                else if (stmtList[0] is ExpressionStatementSyntax ||
                    stmtList[0] is LocalDeclarationStatementSyntax)
                {
                    this.Statements.Add(Statement.Create(stmtList[0], this, this.Summary));
                    stmtList.RemoveAt(0);
                }
                // NOTE: exceptions not supported yet
                else if (stmtList[0] is ThrowStatementSyntax)
                {
                    this.Statements.Add(Statement.Create(stmtList[0], this, this.Summary));
                    stmtList.RemoveAt(0);
                }
                else if (stmtList[0] is EmptyStatementSyntax)
                {
                    stmtList.RemoveAt(0);
                }
                else
                {
                    throw new NotSupportedException($"CFG construction for statement '{stmtList[0]}' " +
                        "is not yet supported. Please report this to the developers.");
                }
            }

            if (stmtList.Count == 0 && successor != null)
            {
                this.ISuccessors.Add(successor);
                successor.IPredecessors.Add(this);
            }
        }

        /// <summary>
        /// Constructs an if-then-else branch.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">ControlFlowNode</param>
        /// <param name="innerLoopHead">LoopHeadControlFlowNode</param>
        private void ConstructIfThenElseBranch(List<StatementSyntax> stmtList,
            ControlFlowNode successor, LoopHeadControlFlowNode innerLoopHead)
        {
            var ifStmt = stmtList[0] as IfStatementSyntax;
            stmtList.RemoveAt(0);

            var ifBranchNode = new ControlFlowNode(this.Graph, this.Summary);
            ifBranchNode.Statements.Add(Statement.Create(ifStmt.Condition,
                ifBranchNode, this.Summary));
            this.ISuccessors.Add(ifBranchNode);
            ifBranchNode.IPredecessors.Add(this);

            ControlFlowNode elseBranchNode = null;
            ControlFlowNode falseNode = null;

            if (ifStmt.Else != null)
            {
                elseBranchNode = new ControlFlowNode(this.Graph, this.Summary);
                falseNode = new ControlFlowNode(this.Graph, this.Summary);

                falseNode.Construct(stmtList, successor, innerLoopHead);
                elseBranchNode.Construct(this.GetStatements(ifStmt.Else.Statement),
                    falseNode, innerLoopHead);
                ifBranchNode.ISuccessors.Add(elseBranchNode);
                elseBranchNode.IPredecessors.Add(ifBranchNode);
            }
            else
            {
                falseNode = new ControlFlowNode(this.Graph, this.Summary);
                falseNode.Construct(stmtList, successor, innerLoopHead);
                ifBranchNode.ISuccessors.Add(falseNode);
                falseNode.IPredecessors.Add(ifBranchNode);
            }

            var trueNode = new ControlFlowNode(this.Graph, this.Summary);
            trueNode.Construct(this.GetStatements(ifStmt.Statement),
                falseNode, innerLoopHead);
            ifBranchNode.ISuccessors.Add(trueNode);
            trueNode.IPredecessors.Add(ifBranchNode);
        }

        /// <summary>
        /// Constructs a for loop.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">ControlFlowNode</param>
        /// <param name="innerLoopHead">LoopHeadControlFlowNode</param>
        private void ConstructForLoop(List<StatementSyntax> stmtList, ControlFlowNode successor,
            LoopHeadControlFlowNode innerLoopHead)
        {
            var loopHeadStmt = stmtList[0] as ForStatementSyntax;
            stmtList.RemoveAt(0);

            this.ConstructLoop(loopHeadStmt.Condition, loopHeadStmt.Statement,
                stmtList, successor, innerLoopHead);
        }

        /// <summary>
        /// Constructs a while loop.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">ControlFlowNode</param>
        /// <param name="innerLoopHead">LoopHeadControlFlowNode</param>
        private void ConstructWhileLoop(List<StatementSyntax> stmtList, ControlFlowNode successor,
            LoopHeadControlFlowNode innerLoopHead)
        {
            var loopHeadStmt = stmtList[0] as WhileStatementSyntax;
            stmtList.RemoveAt(0);

            this.ConstructLoop(loopHeadStmt.Condition, loopHeadStmt.Statement,
                stmtList, successor, innerLoopHead);
        }

        /// <summary>
        /// Constructs a foreach loop.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">ControlFlowNode</param>
        /// <param name="innerLoopHead">LoopHeadControlFlowNode</param>
        private void ConstructForeachLoop(List<StatementSyntax> stmtList, ControlFlowNode successor,
            LoopHeadControlFlowNode innerLoopHead)
        {
            var loopHeadStmt = stmtList[0] as ForEachStatementSyntax;
            stmtList.RemoveAt(0);

            this.ConstructLoop(loopHeadStmt.Expression, loopHeadStmt.Statement,
                stmtList, successor, innerLoopHead);
        }

        /// <summary>
        /// Constructs a loop from the specified loop guard and body.
        /// </summary>
        /// <param name="loopGuard">ExpressionSyntax</param>
        /// <param name="loopBody">StatementSyntax</param>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">ControlFlowNode</param>
        /// <param name="innerLoopHead">LoopHeadControlFlowNode</param>
        private void ConstructLoop(ExpressionSyntax loopGuard, StatementSyntax loopBody,
            List<StatementSyntax> stmtList, ControlFlowNode successor,
            LoopHeadControlFlowNode innerLoopHead)
        {
            var falseNode = new ControlFlowNode(this.Graph, this.Summary);
            var trueNode = new ControlFlowNode(this.Graph, this.Summary);
            var loopHeadNode = new LoopHeadControlFlowNode(this.Graph, this.Summary, falseNode);

            loopHeadNode.Statements.Add(Statement.Create(loopGuard,
                loopHeadNode, this.Summary));
            falseNode.Construct(stmtList, successor, innerLoopHead);
            trueNode.Construct(this.GetStatements(loopBody), loopHeadNode, loopHeadNode);

            this.ISuccessors.Add(loopHeadNode);
            loopHeadNode.IPredecessors.Add(this);

            loopHeadNode.ISuccessors.Add(falseNode);
            falseNode.IPredecessors.Add(loopHeadNode);

            loopHeadNode.ISuccessors.Add(trueNode);
            trueNode.IPredecessors.Add(loopHeadNode);
        }

        /// <summary>
        /// Constructs a do-while loop.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">ControlFlowNode</param>
        /// <param name="innerLoopHead">LoopHeadControlFlowNode</param>
        private void ConstructDoWhileLoop(List<StatementSyntax> stmtList, ControlFlowNode successor,
            LoopHeadControlFlowNode innerLoopHead)
        {
            var loopHeadStmt = stmtList[0] as DoStatementSyntax;
            stmtList.RemoveAt(0);

            var falseNode = new ControlFlowNode(this.Graph, this.Summary);
            var trueNode = new ControlFlowNode(this.Graph, this.Summary);
            var loopHeadNode = new LoopHeadControlFlowNode(this.Graph, this.Summary, falseNode);

            trueNode.Construct(this.GetStatements(loopHeadStmt.Statement),
                loopHeadNode, loopHeadNode);
            loopHeadNode.Statements.Add(Statement.Create(loopHeadStmt.Condition,
                loopHeadNode, this.Summary));
            falseNode.Construct(stmtList, successor, innerLoopHead);

            this.ISuccessors.Add(trueNode);
            trueNode.IPredecessors.Add(this);

            loopHeadNode.ISuccessors.Add(falseNode);
            falseNode.IPredecessors.Add(loopHeadNode);

            loopHeadNode.ISuccessors.Add(trueNode);
            trueNode.IPredecessors.Add(loopHeadNode);
        }

        /// <summary>
        /// Constructs a switch block.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">ControlFlowNode</param>
        /// <param name="innerLoopHead">LoopHeadControlFlowNode</param>
        private void ConstructSwitchBlock(List<StatementSyntax> stmtList,
            ControlFlowNode successor, LoopHeadControlFlowNode innerLoopHead)
        {
            var switchStmt = stmtList[0] as SwitchStatementSyntax;
            stmtList.RemoveAt(0);

            var switchNode = new ControlFlowNode(this.Graph, this.Summary);
            switchNode.Statements.Add(Statement.Create(switchStmt.Expression,
                switchNode, this.Summary));

            var afterSwitchNode = new ControlFlowNode(this.Graph, this.Summary);
            afterSwitchNode.Construct(stmtList, successor, innerLoopHead);

            foreach (var section in switchStmt.Sections)
            {
                var sectionNode = new ControlFlowNode(this.Graph, this.Summary);
                sectionNode.Construct(section.Statements.ToList(),
                    afterSwitchNode, null);

                switchNode.ISuccessors.Add(sectionNode);
                sectionNode.IPredecessors.Add(switchNode);
            }

            this.ISuccessors.Add(switchNode);
            switchNode.IPredecessors.Add(this);
        }

        /// <summary>
        /// Constructs a using block.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">ControlFlowNode</param>
        /// <param name="innerLoopHead">LoopHeadControlFlowNode</param>
        private void ConstructUsingBlock(List<StatementSyntax> stmtList,
            ControlFlowNode successor, LoopHeadControlFlowNode innerLoopHead)
        {
            var usingStmt = stmtList[0] as UsingStatementSyntax;
            stmtList.RemoveAt(0);

            var usingNode = new ControlFlowNode(this.Graph, this.Summary);

            if (usingStmt.Declaration != null)
            {
                usingNode.Statements.Add(Statement.Create(usingStmt.Declaration,
                    usingNode, this.Summary));
            }
            else
            {
                usingNode.Statements.Add(Statement.Create(usingStmt.Expression,
                    usingNode, this.Summary));
            }

            var afterUsingNode = new ControlFlowNode(this.Graph, this.Summary);
            afterUsingNode.Construct(stmtList, successor, innerLoopHead);

            var bodyNode = new ControlFlowNode(this.Graph, this.Summary);
            bodyNode.Construct(this.GetStatements(usingStmt.Statement),
                afterUsingNode, innerLoopHead);

            this.ISuccessors.Add(usingNode);
            usingNode.IPredecessors.Add(this);

            usingNode.ISuccessors.Add(bodyNode);
            bodyNode.IPredecessors.Add(usingNode);
        }

        /// <summary>
        /// Constructs a naked code block.
        /// </summary>
        /// <param name="stmtList">List of statements</param>
        /// <param name="successor">ControlFlowNode</param>
        /// <param name="innerLoopHead">LoopHeadControlFlowNode</param>
        private void ConstructNakedCodeBlock(List<StatementSyntax> stmtList,
            ControlFlowNode successor, LoopHeadControlFlowNode innerLoopHead)
        {
            var blockStmt = stmtList[0] as BlockSyntax;
            stmtList.RemoveAt(0);

            var afterBlockNode = new ControlFlowNode(this.Graph, this.Summary);
            afterBlockNode.Construct(stmtList, successor, innerLoopHead);

            var blockNode = new ControlFlowNode(this.Graph, this.Summary);
            blockNode.Construct(this.GetStatements(blockStmt), afterBlockNode, innerLoopHead);

            this.ISuccessors.Add(blockNode);
            blockNode.IPredecessors.Add(this);
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Returns a list containing all statements in the specified block.
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

        #endregion

        #region printing methods

        public override string ToString()
        {
            return $"{this.Id}";
        }

        #endregion
    }
}
