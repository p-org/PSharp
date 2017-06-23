//-----------------------------------------------------------------------
// <copyright file="Edge.cs">
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

using QuickGraph;

namespace Microsoft.PSharp.DynamicRaceDetection
{
    internal class Edge : IEdge<Node>
    {
        private Node _source;
        private Node _target;

        /// <summary>
        /// The source node.
        /// </summary>
        public Node Source
        {
            get
            {
                return this._source;
            }
        }

        /// <summary>
        /// The target node.
        /// </summary>
        public Node Target
        {
            get
            {
                return this._target;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">Source node</param>
        /// <param name="target">Target node</param>
        public Edge(Node source, Node target)
        {
            this._source = source;
            this._target = target;
        }
    }
}
