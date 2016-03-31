//-----------------------------------------------------------------------
// <copyright file="PSharpCFGNode.cs">
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
using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

using Microsoft.PSharp.LanguageServices;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// A P# control-flow graph node.
    /// </summary>
    internal class PSharpCFGNode : ControlFlowGraphNode
    {
        #region fields

        /// <summary>
        /// True if the node is a gives up node. False by default.
        /// </summary>
        internal bool IsGivesUpNode;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="summary">MethodSummary</param>
        public PSharpCFGNode(AnalysisContext context, MethodSummary summary)
            : base(context, summary)
        {
            this.IsGivesUpNode = false;
        }

        /// <summary>
        /// Creates a control-flow graph node.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>ControlFlowGraphNode</returns>
        protected override ControlFlowGraphNode CreateNode(AnalysisContext context, MethodSummary summary)
        {
            return new PSharpCFGNode(context, summary);
        }

        #endregion

        #region public API

        /// <summary>
        /// Returns the method summary that contains this
        /// control-flow graph node.
        /// </summary>
        /// <returns>MethodSummary</returns>
        public new PSharpMethodSummary GetMethodSummary()
        {
            return this.Summary as PSharpMethodSummary;
        }

        /// <summary>
        /// Returns the immediate predecessors of this
        /// control-flow graph node.
        /// </summary>
        /// <returns>Predecessors</returns>
        public new IEnumerable<PSharpCFGNode> GetImmediatePredecessors()
        {
            return this.IPredecessors.Cast<PSharpCFGNode>();
        }

        /// <summary>
        /// Returns the immediate successors of this
        /// control-flow graph node.
        /// </summary>
        /// <returns>Successors</returns>
        public new IEnumerable<PSharpCFGNode> GetImmediateSuccessors()
        {
            return this.ISuccessors.Cast<PSharpCFGNode>();
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Creates a single statement control-flow graph node
        /// using the given statement.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <returns>ControlFlowGraphNode</returns>
        protected override ControlFlowGraphNode CreateSingleStatementControlFlowGraphNode(StatementSyntax stmt)
        {
            PSharpCFGNode givesUpNode = null;

            if (stmt is ExpressionStatementSyntax ||
                stmt is LocalDeclarationStatementSyntax)
            {
                var invocations = stmt.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>();
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
                            givesUpNode = this.CreateNode(this.AnalysisContext, this.Summary)
                                as PSharpCFGNode;
                            givesUpNode.Description = "GivesUp";

                            this.ISuccessors.Add(givesUpNode);
                            givesUpNode.IPredecessors.Add(this);
                        }

                        givesUpNode.IsGivesUpNode = true;
                        (this.Summary as PSharpMethodSummary).GivesUpOwnershipNodes.Add(givesUpNode);
                        givesUpNode.SyntaxNodes.Add(stmt);
                    }
                }
            }

            return givesUpNode;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns true if the given invocation is a gives up operation.
        /// Returns false if it is not.
        /// </summary>
        /// <param name="call">Call</param>
        /// <returns>Boolean</returns>
        private bool IsGivesUpOperation(InvocationExpressionSyntax call)
        {
            var callee = AnalysisContext.GetCalleeOfInvocation(call);
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
                PSharpMethodSummary.Create(this.AnalysisContext as PSharpAnalysisContext,
                calleeMethod).GivesUpSet.Count > 0)
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
