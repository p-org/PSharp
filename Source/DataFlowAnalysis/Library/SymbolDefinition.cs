// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// Class implementing a symbol definition.
    /// </summary>
    public class SymbolDefinition
    {
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolDefinition"/> class.
        /// </summary>
        internal SymbolDefinition(ISymbol symbol, DataFlowNode dfgNode)
        {
            this.DataFlowNode = dfgNode;
            this.Symbol = symbol;
            this.CandidateTypes = new HashSet<ITypeSymbol>();
            this.Kind = symbol.Kind;
            this.Name = $"[{this.DataFlowNode.Id},{this.Kind}]::{this.Symbol.Name}";
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString() => this.Name;
    }
}
