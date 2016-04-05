//-----------------------------------------------------------------------
// <copyright file="GivenUpOwnershipSymbol.cs">
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
    /// Class implementing a symbol with given-up
    /// ownership symbol.
    /// </summary>
    public class GivenUpOwnershipSymbol
    {
        #region fields

        /// <summary>
        /// Containing symbol.
        /// </summary>
        public ISymbol ContainingSymbol { get; }

        /// <summary>
        /// Statement where the ownership is given up.
        /// </summary>
        public Statement Statement { get; }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="symbol">ISymbol</param>
        /// <param name="statement">Statement</param>
        internal GivenUpOwnershipSymbol(ISymbol symbol,
            Statement statement)
        {
            this.ContainingSymbol = symbol;
            this.Statement = statement;
        }

        #endregion
    }
}
