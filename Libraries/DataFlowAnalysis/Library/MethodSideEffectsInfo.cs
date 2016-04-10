//-----------------------------------------------------------------------
// <copyright file="MethodSideEffectsInfo.cs">
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
    /// Method side effects information.
    /// </summary>
    public sealed class MethodSideEffectsInfo
    {
        #region fields

        /// <summary>
        /// Method summary that contains these side effects.
        /// </summary>
        private MethodSummary Summary;

        /// <summary>
        /// Dictionary containing all the indexes of parameters
        /// that flow into fields in the method.
        /// </summary>
        public IDictionary<IFieldSymbol, ISet<int>> FieldFlowParamIndexes;

        /// <summary>
        /// Dictionary containing all read and write
        /// field accesses in the method.
        /// </summary>
        public IDictionary<IFieldSymbol, ISet<Statement>> FieldAccesses;

        /// <summary>
        /// Dictionary containing all read and write parameters
        /// accesses in the method.
        /// </summary>
        public IDictionary<int, ISet<Statement>> ParameterAccesses;
        
        /// <summary>
        /// Set of fields and associated types that are
        /// returned to the caller of the method.
        /// </summary>
        public ISet<Tuple<IFieldSymbol, ITypeSymbol>> ReturnedFields;

        /// <summary>
        /// Set of parameter indexes and associated types that are
        /// returned to the caller of the method.
        /// </summary>
        public ISet<Tuple<int, ITypeSymbol>> ReturnedParameters;

        /// <summary>
        /// Set of the indexes of parameters that the method
        /// gives up during its execution.
        /// </summary>
        public ISet<int> GivesUpOwnershipParamIndexes;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="summary">MethodSummary</param>
        internal MethodSideEffectsInfo(MethodSummary summary)
        {
            this.Summary = summary;
            this.FieldFlowParamIndexes = new Dictionary<IFieldSymbol, ISet<int>>();
            this.FieldAccesses = new Dictionary<IFieldSymbol, ISet<Statement>>();
            this.ParameterAccesses = new Dictionary<int, ISet<Statement>>();
            this.ReturnedFields = new HashSet<Tuple<IFieldSymbol, ITypeSymbol>>();
            this.ReturnedParameters = new HashSet<Tuple<int, ITypeSymbol>>();
            this.GivesUpOwnershipParamIndexes = new HashSet<int>();
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Resolves and returns all possible return symbols at
        /// the point of the specified invocation.
        /// </summary>
        /// <param name="invocation">InvocationExpressionSyntax</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Set of return symbols</returns>
        internal ISet<Tuple<ISymbol, ITypeSymbol>> GetResolvedReturnSymbols(
            InvocationExpressionSyntax invocation, SemanticModel model)
        {
            var returnSymbols = new HashSet<Tuple<ISymbol, ITypeSymbol>>();

            foreach (var parameter in this.ReturnedParameters)
            {
                var argExpr = invocation.ArgumentList.Arguments[parameter.Item1].Expression;
                var returnSymbol = model.GetSymbolInfo(argExpr).Symbol;
                returnSymbols.Add(Tuple.Create(returnSymbol, parameter.Item2));
            }

            foreach (var field in this.ReturnedFields)
            {
                returnSymbols.Add(Tuple.Create(field.Item1 as ISymbol, field.Item2));
            }

            return returnSymbols;
        }

        #endregion
    }
}
