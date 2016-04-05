//-----------------------------------------------------------------------
// <copyright file="Statement.cs">
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
    /// Class implementing a statement.
    /// </summary>
    public class Statement
    {
        #region fields

        /// <summary>
        /// The syntax node of the statement.
        /// </summary>
        public readonly SyntaxNode SyntaxNode;

        /// <summary>
        /// The control-flow graph node that contains
        /// the statement.
        /// </summary>
        public readonly ControlFlowGraphNode ControlFlowGraphNode;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        private Statement(SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            this.SyntaxNode = syntaxNode;
            this.ControlFlowGraphNode = cfgNode;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Creates a new statement.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <returns>Statement</returns>
        public static Statement Create(SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode)
        {
            return new Statement(syntaxNode, cfgNode);
        }

        /// <summary>
        /// Returns the method summary that contains this
        /// syntax node location.
        /// </summary>
        /// <returns>MethodSummary</returns>
        public MethodSummary GetMethodSummary()
        {
            return this.ControlFlowGraphNode.GetMethodSummary();
        }

        /// <summary>
        /// Checks if the statement is in the same
        /// method as the specified statement.
        /// </summary>
        /// <param name="statement">Statement</param>
        /// <returns>Boolean</returns>
        public bool IsInSameMethodAs(Statement statement)
        {
            return this.GetMethodSummary().Id == statement.GetMethodSummary().Id;
        }

        /// <summary>
        /// Checks if the statement is in the same method as
        /// the specified control-flow graph node.
        /// </summary>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <returns>Boolean</returns>
        public bool IsInSameMethodAs(ControlFlowGraphNode cfgNode)
        {
            return this.GetMethodSummary().Id == cfgNode.GetMethodSummary().Id;
        }

        /// <summary>
        /// Determines if the specified object is equal
        /// to the current object.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            Statement stmt = obj as Statement;
            if (stmt == null)
            {
                return false;
            }

            if (!this.SyntaxNode.Equals(stmt.SyntaxNode) ||
                !this.ControlFlowGraphNode.Equals(stmt.ControlFlowGraphNode))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>HashValue</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 19;

                hash = hash + 31 * this.SyntaxNode.GetHashCode();
                hash = hash + 31 * this.ControlFlowGraphNode.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>Text</returns>
        public override string ToString()
        {
            return "[" + this.SyntaxNode + "]::[cfg::" +
                this.ControlFlowGraphNode.Id + "]";
        }

        #endregion
    }
}
