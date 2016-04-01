//-----------------------------------------------------------------------
// <copyright file="PSharpMethodSummary.cs">
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
    /// Class implementing a P# method summary.
    /// </summary>
    internal class PSharpMethodSummary : MethodSummary
    {
        #region fields

        /// <summary>
        /// Machine that the method of this summary belongs to.
        /// If the method does not belong to a machine, the
        /// object is null.
        /// </summary>
        internal StateMachine Machine;

        /// <summary>
        /// Set of all gives-up ownership nodes in the control-flow
        /// graph of the method of this summary.
        /// </summary>
        internal HashSet<PSharpCFGNode> GivesUpOwnershipNodes;

        /// <summary>
        /// Set of the indexes of parameters that the original method
        /// gives up during its execution.
        /// </summary>
        internal HashSet<int> GivesUpSet;

        #endregion

        #region constructors

        /// <summary>
        /// Creates the summary of the given method.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="method">Method</param>
        /// <returns>MethodSummary</returns>
        public static PSharpMethodSummary Create(PSharpAnalysisContext context,
            BaseMethodDeclarationSyntax method)
        {
            if (context.Summaries.ContainsKey(method))
            {
                return context.Summaries[method] as PSharpMethodSummary;
            }

            var summary = new PSharpMethodSummary(context, method);
            summary.BuildSummary();

            return summary;
        }

        /// <summary>
        /// Creates the summary of the given method.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="method">Method</param>
        /// <param name="machine">Machine</param>
        /// <returns>MethodSummary</returns>
        public static PSharpMethodSummary Create(PSharpAnalysisContext context,
            BaseMethodDeclarationSyntax method, StateMachine machine)
        {
            if (context.Summaries.ContainsKey(method))
            {
                return context.Summaries[method] as PSharpMethodSummary;
            }

            var summary = new PSharpMethodSummary(context, method, machine);
            summary.BuildSummary();

            return summary;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="method">Method</param>
        private PSharpMethodSummary(AnalysisContext context, BaseMethodDeclarationSyntax method)
            :base (context, method)
        {
            this.Machine = null;
            this.GivesUpOwnershipNodes = new HashSet<PSharpCFGNode>();
            this.GivesUpSet = new HashSet<int>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="method">Method</param>
        /// <param name="machine">Machine</param>
        private PSharpMethodSummary(AnalysisContext context, BaseMethodDeclarationSyntax method,
            StateMachine machine)
            : base(context, method)
        {
            this.Machine = machine;
            this.GivesUpOwnershipNodes = new HashSet<PSharpCFGNode>();
            this.GivesUpSet = new HashSet<int>();
        }

        #endregion

        #region public methods

        /// <summary>
        /// Tries to get the method summary of the given object creation. Returns
        /// null if such summary cannot be found.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>MethodSummary</returns>
        public static PSharpMethodSummary TryGet(ObjectCreationExpressionSyntax call,
            SemanticModel model, PSharpAnalysisContext context)
        {
            var callSymbol = model.GetSymbolInfo(call).Symbol;
            if (callSymbol == null)
            {
                return null;
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol, context.Solution).Result;
            if (definition == null)
            {
                return null;
            }

            if (definition.DeclaringSyntaxReferences.IsEmpty)
            {
                return null;
            }

            var constructorCall = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as ConstructorDeclarationSyntax;
            return PSharpMethodSummary.Create(context, constructorCall);
        }

        /// <summary>
        /// Tries to get the method summary of the given invocation. Returns
        /// null if such summary cannot be found.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>MethodSummary</returns>
        public static PSharpMethodSummary TryGet(InvocationExpressionSyntax call,
            SemanticModel model, PSharpAnalysisContext context)
        {
            var callSymbol = model.GetSymbolInfo(call).Symbol;
            if (callSymbol == null)
            {
                return null;
            }

            if (callSymbol.ContainingType.ToString().Equals("Microsoft.PSharp.Machine"))
            {
                return null;
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(callSymbol, context.Solution).Result;
            if (definition == null || definition.DeclaringSyntaxReferences.IsEmpty)
            {
                return null;
            }

            var invocationCall = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as MethodDeclarationSyntax;
            return PSharpMethodSummary.Create(context, invocationCall);
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Creates a new control-flow graph node.
        /// </summary>
        /// <returns>CFGNode</returns>
        protected override ControlFlowGraphNode CreateNewControlFlowGraphNode()
        {
            return new PSharpCFGNode(this.AnalysisContext, this);
        }

        #endregion
    }
}
