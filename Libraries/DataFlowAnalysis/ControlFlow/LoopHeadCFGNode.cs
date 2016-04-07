//-----------------------------------------------------------------------
// <copyright file="LoopHeadCFGNode.cs">
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
    /// A loop head control-flow graph node.
    /// </summary>
    internal class LoopHeadCFGNode : CFGNode
    {
        #region fields

        /// <summary>
        /// The node after exiting the loop.
        /// </summary>
        public CFGNode LoopExitNode;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cfg">ControlFlowGraph</param>
        /// <param name="loopExitNode">CFGNode</param>
        internal LoopHeadCFGNode(ControlFlowGraph cfg, CFGNode loopExitNode)
            : base(cfg)
        {
            this.LoopExitNode = loopExitNode;
        }

        #endregion
    }
}
