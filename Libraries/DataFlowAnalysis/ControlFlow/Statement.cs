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
        public readonly IControlFlowNode ControlFlowNode;

        /// <summary>
        /// Handle to the summary of the method
        /// that contains the statement.
        /// </summary>
        public MethodSummary Summary { get; }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="node">IControlFlowNode</param>
        /// <param name="summary">MethodSummary</param>
        private Statement(SyntaxNode syntaxNode, IControlFlowNode node,
            MethodSummary summary)
        {
            this.SyntaxNode = syntaxNode;
            this.ControlFlowNode = node;
            this.Summary = summary;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Creates a new statement.
        /// </summary>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="node">IControlFlowNode</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Statement</returns>
        public static Statement Create(SyntaxNode syntaxNode, IControlFlowNode node,
            MethodSummary summary)
        {
            return new Statement(syntaxNode, node, summary);
        }

        /// <summary>
        /// Checks if the statement is in the same
        /// method as the specified statement.
        /// </summary>
        /// <param name="statement">Statement</param>
        /// <returns>Boolean</returns>
        public bool IsInSameMethodAs(Statement statement)
        {
            return this.Summary.Id == statement.Summary.Id;
        }

        /// <summary>
        /// Checks if the statement is in the same method as
        /// the specified control-flow graph node.
        /// </summary>
        /// <param name="node">IControlFlowNode</param>
        /// <returns>Boolean</returns>
        public bool IsInSameMethodAs(IControlFlowNode node)
        {
            return this.Summary.Id == node.Summary.Id;
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
                !this.ControlFlowNode.Equals(stmt.ControlFlowNode))
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
                hash = hash + 31 * this.ControlFlowNode.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>Text</returns>
        public override string ToString()
        {
            return "[" + this.SyntaxNode + "]::[cfg::" + this.ControlFlowNode + "]";
        }

        #endregion
    }
}
