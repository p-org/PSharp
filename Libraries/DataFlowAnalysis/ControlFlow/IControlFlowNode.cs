//-----------------------------------------------------------------------
// <copyright file="IControlFlowNode.cs">
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
    /// Interface for a control-flow graph node.
    /// </summary>
    public interface IControlFlowNode : INode, ITraversable<IControlFlowNode>
    {
        #region properties

        /// <summary>
        /// List of statements contained in the node.
        /// </summary>
        IList<Statement> Statements { get; }

        /// <summary>
        /// Graph that contains this node.
        /// </summary>
        IGraph<IControlFlowNode> Graph { get; }

        /// <summary>
        /// Method summary that contains this node.
        /// </summary>
        MethodSummary Summary { get; }

        #endregion
    }
}
