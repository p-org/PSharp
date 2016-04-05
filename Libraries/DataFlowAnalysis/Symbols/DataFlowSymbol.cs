//-----------------------------------------------------------------------
// <copyright file="DataFlowSymbol.cs">
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

namespace Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis
{
    /// <summary>
    /// Class implementing a data-flow symbol.
    /// </summary>
    internal class DataFlowSymbol
    {
        #region fields

        /// <summary>
        /// The unique id of the symbol.
        /// </summary>
        internal readonly int Id;

        /// <summary>
        /// Containing assembly
        /// </summary>
        public IAssemblySymbol ContainingAssembly { get; }

        /// <summary>
        /// Containing module.
        /// </summary>
        public IModuleSymbol ContainingModule { get; }

        /// <summary>
        /// Containing namespace.
        /// </summary>
        public INamespaceSymbol ContainingNamespace { get; }

        /// <summary>
        /// Containing symbol.
        /// </summary>
        public ISymbol ContainingSymbol { get; }

        /// <summary>
        /// Kind of the symbol.
        /// </summary>
        public SymbolKind Kind { get; }

        /// <summary>
        /// Name of the data-flow symbol.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Has symbol been reset after method entry.
        /// </summary>
        internal bool HasResetAfterMethodEntry;

        /// <summary>
        /// A counter for creating unique IDs.
        /// </summary>
        private static int IdCounter;

        #endregion

        #region constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static DataFlowSymbol()
        {
            DataFlowSymbol.IdCounter = 0;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="symbol">ISymbol</param>
        internal DataFlowSymbol(ISymbol symbol)
        {
            this.Id = DataFlowSymbol.IdCounter++;
            this.ContainingAssembly = symbol.ContainingAssembly;
            this.ContainingModule = symbol.ContainingModule;
            this.ContainingNamespace = symbol.ContainingNamespace;
            this.ContainingSymbol = symbol;
            this.Kind = symbol.Kind;

            this.HasResetAfterMethodEntry = false;

            string type = "";
            if (this.Kind == SymbolKind.Parameter)
            {
                type = "Param";
            }
            else if (this.Kind == SymbolKind.Field)
            {
                type = "Field";
            }
            else
            {
                type = "LocalVar";
            }

            this.Name = "[" + this.Id + "," + type + "]::" + symbol.Name;
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
