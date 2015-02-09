//-----------------------------------------------------------------------
// <copyright file="FieldUsageAnalysis.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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
using Microsoft.CodeAnalysis;

namespace PSharp
{
    internal static class FieldUsageAnalysis
    {
        #region internal API

        /// <summary>
        /// Returns true if the given field symbol is being accessed
        /// before being reset.
        /// </summary>
        /// <param name="field">Field</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean value</returns>
        internal static bool IsAccessedBeforeBeingReset(ISymbol field, MethodSummary summary)
        {
            StateTransitionGraphNode stateTransitionNode = null;
            if (!AnalysisContext.StateTransitionGraphs.ContainsKey(summary.Machine))
            {
                return true;
            }
            
            stateTransitionNode = AnalysisContext.StateTransitionGraphs[summary.Machine].
                GetGraphNodeForSummary(summary);
            if (stateTransitionNode == null)
            {
                return true;
            }

            var result = stateTransitionNode.VisitSelfAndSuccessors(IsAccessedBeforeBeingReset,
                new Tuple<MethodSummary, ISymbol>(summary, field));

            return false;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Query checking if the field is accessed before being reset
        /// in the given state transition node.
        /// </summary>
        /// <param name="node">StateTransitionGraphNode</param>
        /// <param name="input">Input</param>
        /// <param name="isFirstVisit">True if first node to visit</param>
        /// <returns>Boolean value</returns>
        private static bool IsAccessedBeforeBeingReset(StateTransitionGraphNode node,
            object input, bool isFirstVisit)
        {
            var summary = ((Tuple<MethodSummary, ISymbol>)input).Item1;
            var fieldSymbol = ((Tuple<MethodSummary, ISymbol>)input).Item2;
            var result = false;

            if (isFirstVisit && node.OnExit != null && !summary.Equals(node.OnExit))
            {
                foreach (var action in node.Actions)
                {
                    result = action.FieldAccessSet.ContainsKey(fieldSymbol as IFieldSymbol);
                    if (result)
                    {
                        break;
                    }
                }

                if (!result && node.OnExit != null)
                {
                    result = node.OnExit.FieldAccessSet.ContainsKey(fieldSymbol as IFieldSymbol);
                }
            }
            else if (!isFirstVisit)
            {
                if (node.OnEntry != null)
                {
                    result = node.OnEntry.FieldAccessSet.ContainsKey(fieldSymbol as IFieldSymbol);
                }

                if (!result)
                {
                    foreach (var action in node.Actions)
                    {
                        result = action.FieldAccessSet.ContainsKey(fieldSymbol as IFieldSymbol);
                        if (result)
                        {
                            break;
                        }
                    }
                }

                if (!result && node.OnExit != null)
                {
                    result = node.OnExit.FieldAccessSet.ContainsKey(fieldSymbol as IFieldSymbol);
                }
            }

            return result;
        }

        #endregion
    }
}
