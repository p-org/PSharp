//-----------------------------------------------------------------------
// <copyright file="StateTransitionGraphNode.cs">
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
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.StaticAnalysis
{
    internal class StateTransitionGraphNode
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
        /// Handle to the state which owns this node.
        /// </summary>
        internal ClassDeclarationSyntax State;

        /// <summary>
        /// Handle to the machine which owns this node.
        /// </summary>
        internal ClassDeclarationSyntax Machine;

        /// <summary>
        /// The summary of the OnEntry method of the state
        /// that this node represents.
        /// </summary>
        internal MethodSummary OnEntry;

        /// <summary>
        /// The summary of the OnExit method of the state
        /// that this node represents.
        /// </summary>
        internal MethodSummary OnExit;

        /// <summary>
        /// Set of action handlers in the state that this
        /// node represents.
        /// </summary>
        internal HashSet<MethodSummary> Actions;

        /// <summary>
        /// Set of the immediate predecessors of the node.
        /// </summary>
        internal HashSet<StateTransitionGraphNode> IPredecessors;

        /// <summary>
        /// Set of the immediate successors of the node.
        /// </summary>
        internal HashSet<StateTransitionGraphNode> ISuccessors;

        /// <summary>
        /// True if the node represents the start state.
        /// False by default.
        /// </summary>
        internal bool IsStartNode;

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
        /// <param name="machine">Machine</param>
        internal StateTransitionGraphNode(AnalysisContext context, ClassDeclarationSyntax state,
            ClassDeclarationSyntax machine)
        {
            this.AnalysisContext = context;
            this.Id = StateTransitionGraphNode.IdCounter++;
            this.State = state;
            this.Machine = machine;
            this.Actions = new HashSet<MethodSummary>();
            this.IPredecessors = new HashSet<StateTransitionGraphNode>();
            this.ISuccessors = new HashSet<StateTransitionGraphNode>();
            this.IsStartNode = false;
        }

        /// <summary>
        /// Constructs the node using information from the given state transitions
        /// and action bindings.
        /// </summary>
        /// <param name="stateTransitions">State transitions</param>
        /// <param name="actionBindings">Action bindings</param>
        internal void Construct(Dictionary<ClassDeclarationSyntax, HashSet<ClassDeclarationSyntax>> stateTransitions,
            Dictionary<ClassDeclarationSyntax, HashSet<MethodDeclarationSyntax>> actionBindings)
        {
            this.Construct(stateTransitions, actionBindings, new HashSet<StateTransitionGraphNode>());
        }

        /// <summary>
        /// Visits the state transition graph to find and return
        /// the node for the given summary.
        /// </summary>
        /// <param name="summary">MethodSummary</param>
        /// <returns>StateTransitionGraphNode</returns>
        internal StateTransitionGraphNode GetGraphNodeForSummary(MethodSummary summary)
        {
            return this.GetGraphNodeForSummary(summary, new HashSet<StateTransitionGraphNode>());
        }

        /// <summary>
        /// Visits the state transition graph node and its successors
        /// to apply the given query.
        /// </summary>
        /// <param name="query">Query</param>
        /// <param name="input">Query input</param>
        /// <returns>Boolean</returns>
        internal bool VisitSelfAndSuccessors(Func<StateTransitionGraphNode, object, bool, bool> query, object input)
        {
            return this.VisitSelfAndSuccessors(query, input, true, new HashSet<StateTransitionGraphNode>());
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructs the node using information from the given state transitions
        /// and action bindings.
        /// </summary>
        /// <param name="stateTransitions">State transitions</param>
        /// <param name="actionBindings">Action bindings</param>
        /// <param name="visited">Already visited nodes</param>
        private void Construct(Dictionary<ClassDeclarationSyntax, HashSet<ClassDeclarationSyntax>> stateTransitions,
            Dictionary<ClassDeclarationSyntax, HashSet<MethodDeclarationSyntax>> actionBindings,
            HashSet<StateTransitionGraphNode> visited)
        {
            visited.Add(this);

            foreach (var method in this.State.ChildNodes().OfType<MethodDeclarationSyntax>())
            {
                var summary = MethodSummary.Create(this.AnalysisContext, method);
                if (method.Modifiers.Any(SyntaxKind.OverrideKeyword) &&
                    method.Identifier.ValueText.Equals("OnEntry"))
                {
                    this.OnEntry = summary;
                }
                else if (method.Modifiers.Any(SyntaxKind.OverrideKeyword) &&
                    method.Identifier.ValueText.Equals("OnExit"))
                {
                    this.OnExit = summary;
                }
            }

            var actions = new HashSet<MethodDeclarationSyntax>();
            if (actionBindings.ContainsKey(this.State))
            {
                actions = actionBindings[this.State];
            }

            foreach (var action in actions)
            {
                var actionSummary = MethodSummary.Create(this.AnalysisContext, action);
                this.Actions.Add(actionSummary);
            }

            var transitions = new HashSet<ClassDeclarationSyntax>();
            if (stateTransitions.ContainsKey(this.State))
            {
                transitions = stateTransitions[this.State];
            }

            foreach (var successorState in transitions)
            {
                var successor = visited.FirstOrDefault(v => v.State.Equals(successorState));
                if (successor != null)
                {
                    this.ISuccessors.Add(successor);
                    successor.IPredecessors.Add(this);
                }
                else
                {
                    successor = new StateTransitionGraphNode(this.AnalysisContext, successorState, this.Machine);
                    this.ISuccessors.Add(successor);
                    successor.IPredecessors.Add(this);
                    successor.Construct(stateTransitions, actionBindings, visited);
                }
            }
        }

        /// <summary>
        /// Visits the state transition graph to find and return
        /// the node for the given summary.
        /// </summary>
        /// <param name="summary">MethodSummary</param>
        /// <param name="visited">Already visited nodes</param>
        /// <returns>StateTransitionGraphNode</returns>
        private StateTransitionGraphNode GetGraphNodeForSummary(MethodSummary summary,
            HashSet<StateTransitionGraphNode> visited)
        {
            visited.Add(this);

            StateTransitionGraphNode stateNode = null;
            if ((this.OnEntry != null && this.OnEntry.Equals(summary)) ||
                (this.OnExit != null && this.OnExit.Equals(summary)) ||
                this.Actions.Any(v => v.Equals(summary)))
            {
                stateNode = this;
            }
            else
            {
                foreach (var successor in this.ISuccessors.Where(v => !visited.Contains(v)))
                {
                    var node = successor.GetGraphNodeForSummary(summary, visited);
                    if (node != null)
                    {
                        stateNode = node;
                        break;
                    }
                }
            }

            return stateNode;
        }

        /// <summary>
        /// Visits the state transition graph node and its successors
        /// to apply the given query.
        /// </summary>
        /// <param name="query">Query</param>
        /// <param name="input">Query input</param>
        /// <param name="isFirstVisit">True if first node to visit</param>
        /// <param name="visited">Already visited nodes</param>
        /// <returns>Boolean</returns>
        private bool VisitSelfAndSuccessors(Func<StateTransitionGraphNode, object, bool, bool> query,
            object input, bool isFirstVisit, HashSet<StateTransitionGraphNode> visited)
        {
            bool result = query(this, input, isFirstVisit);
            if (!result)
            {
                foreach (var successor in this.ISuccessors.Where(v => !visited.Contains(v)))
                {
                    visited.Add(successor);
                    result = successor.VisitSelfAndSuccessors(query, input, false, visited);
                    if (result)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
