//-----------------------------------------------------------------------
// <copyright file="PSharpDataFlowAnalysis.cs">
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
using Microsoft.CodeAnalysis.FindSymbols;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Class implementing the P# data-flow analysis.
    /// </summary>
    internal sealed class PSharpDataFlowAnalysis : DataFlowAnalysis
    {
        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">AnalysisContext</param>
        /// <param name="model">SemanticModel</param>
        private PSharpDataFlowAnalysis(AnalysisContext context, SemanticModel model)
            : base(context, model)
        {

        }

        #endregion

        #region public analysis API

        /// <summary>
        /// Analyzes the data-flow of the given method.
        /// </summary>
        /// <param name="methodSummary">MethodSummary</param>
        /// <param name="context">AnalysisContext</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>DataFlowAnalysis</returns>
        public new static PSharpDataFlowAnalysis Analyze(MethodSummary methodSummary, AnalysisContext context, SemanticModel model)
        {
            var dataFlowAnalysis = new PSharpDataFlowAnalysis(context, model);
            dataFlowAnalysis.Analyze(methodSummary);
            return dataFlowAnalysis;
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Maps symbols in the invocation.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="cfgNode">CFGNode</param>
        protected override void MapSymbolsInInvocation(InvocationExpressionSyntax call, CFGNode cfgNode)
        {
            if (!(cfgNode is PSharpCFGNode) ||
                !(cfgNode as PSharpCFGNode).IsGivesUpNode)
            {
                return;
            }

            List<MemberAccessExpressionSyntax> accesses;
            if ((cfgNode.GetMethodSummary() as PSharpMethodSummary).GivesUpSet.Count == 0 && call.Expression.DescendantNodesAndSelf().
                OfType<IdentifierNameSyntax>().Last().ToString().Equals("Send"))
            {
                accesses = call.ArgumentList.Arguments[1].DescendantNodesAndSelf().
                    OfType<MemberAccessExpressionSyntax>().ToList();
            }
            else
            {
                accesses = call.ArgumentList.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().ToList();
            }

            foreach (var access in accesses)
            {
                IdentifierNameSyntax id = this.AnalysisContext.GetTopLevelIdentifier(access, this.SemanticModel);
                if (id == null)
                {
                    continue;
                }

                var accessSymbol = this.SemanticModel.GetSymbolInfo(id).Symbol;
                this.MapSymbol(accessSymbol, cfgNode.SyntaxNodes[0], cfgNode);
            }
        }

        #endregion
    }
}
