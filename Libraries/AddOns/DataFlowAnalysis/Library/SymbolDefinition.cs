//-----------------------------------------------------------------------
// <copyright file="SymbolDefinition.cs">
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

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// Class implementing a symbol definition.
    /// </summary>
    public class SymbolDefinition
    {
        #region fields
        
        /// <summary>
        /// Node that defined the symbol.
        /// </summary>
        private readonly DataFlowNode DataFlowNode;

        /// <summary>
        /// The symbol.
        /// </summary>
        public readonly ISymbol Symbol;

        /// <summary>
        /// Candidate types of the symbol.
        /// </summary>
        public readonly ISet<ITypeSymbol> CandidateTypes;

        /// <summary>
        /// Kind of the symbol.
        /// </summary>
        public readonly SymbolKind Kind;

        /// <summary>
        /// Name of the symbol definition.
        /// </summary>
        public readonly string Name;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="symbol">ISymbol</param>
        /// <param name="dfgNode">DataFlowNode</param>
        internal SymbolDefinition(ISymbol symbol, DataFlowNode dfgNode)
        {
            this.DataFlowNode = dfgNode;
            this.Symbol = symbol;
            this.CandidateTypes = new HashSet<ITypeSymbol>();
            this.Kind = symbol.Kind;
            this.Name = $"[{this.DataFlowNode.Id},{this.Kind}]::{this.Symbol.Name}";
        }

        #endregion

        #region public methods

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>Text</returns>
        public override string ToString()
        {
            return this.Name;
        }

        #endregion
    }
}
