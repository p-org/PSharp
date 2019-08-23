// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Microsoft.PSharp.DataFlowAnalysis
{
    /// <summary>
    /// Class implementing a symbol with given-up
    /// ownership symbol.
    /// </summary>
    public class GivenUpOwnershipSymbol
    {
        /// <summary>
        /// Containing symbol.
        /// </summary>
        public ISymbol ContainingSymbol { get; }

        /// <summary>
        /// Statement where the ownership is given up.
        /// </summary>
        public Statement Statement { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GivenUpOwnershipSymbol"/> class.
        /// </summary>
        internal GivenUpOwnershipSymbol(ISymbol symbol, Statement statement)
        {
            this.ContainingSymbol = symbol;
            this.Statement = statement;
        }
    }
}
