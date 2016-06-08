//-----------------------------------------------------------------------
// <copyright file="ITraversable.cs">
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
    /// Interface for a traversable node.
    /// </summary>
    public interface ITraversable<T> where T : INode
    {
        #region properties

        /// <summary>
        /// Set of the immediate successors.
        /// </summary>
        ISet<T> ISuccessors { get; }

        /// <summary>
        /// Set of the immediate predecessors.
        /// </summary>
        ISet<T> IPredecessors { get; }

        #endregion

        #region methods

        /// <summary>
        /// Returns true if the node is a successor
        /// of the specified node.
        /// </summary>
        /// <param name="node">INode</param>
        /// <returns>Boolean</returns>
        bool IsSuccessorOf(T node);

        /// <summary>
        /// Returns true if the node is a predecessor
        /// of the specified node.
        /// </summary>
        /// <param name="node">INode</param>
        /// <returns>Boolean</returns>
        bool IsPredecessorOf(T node);

        #endregion
    }
}
