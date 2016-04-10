//-----------------------------------------------------------------------
// <copyright file="IDataFlowAnalysis.cs">
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
    /// Interface for performing data-flow analysis.
    /// </summary>
    public interface IDataFlowAnalysis
    {
        /// <summary>
        /// Checks if the target symbol flows from the entry of the method.
        /// </summary>
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
        bool FlowsFromMethodEntry(ISymbol targetSymbol, Statement targetStatement);

        /// <summary>
        /// Checks if the target symbol flows from the parameter list.
        /// </summary>
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
        bool FlowsFromParameterList(ISymbol targetSymbol, Statement targetStatement);

        /// <summary>
        /// Checks if the target symbol flows from the parameter symbol.
        /// </summary>
        /// <param name="paramSymbol">Parameter Symbol</param>
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
        bool FlowsFromParameter(IParameterSymbol paramSymbol, ISymbol targetSymbol,
            Statement targetStatement);

        /// <summary>
        /// Checks if the symbol flows into the target symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="targetSymbol">Target Symbol</param>
        /// <param name="statement">Statement</param>
        /// <param name="targetStatement">Target Statement</param>
        /// <returns>Boolean</returns>
        bool FlowsIntoSymbol(ISymbol symbol, ISymbol targetSymbol,
            Statement statement, Statement targetStatement);
    }
}
