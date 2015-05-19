//-----------------------------------------------------------------------
// <copyright file="StatementBlockNode.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Statement block node.
    /// </summary>
    public sealed class StatementBlockNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The machine parent node.
        /// </summary>
        public readonly MachineDeclarationNode Machine;

        /// <summary>
        /// The state parent node.
        /// </summary>
        public readonly StateDeclarationNode State;

        /// <summary>
        /// The left curly bracket token.
        /// </summary>
        public Token LeftCurlyBracketToken;

        /// <summary>
        /// List of statement nodes.
        /// </summary>
        public List<StatementNode> Statements;

        /// <summary>
        /// The right curly bracket token.
        /// </summary>
        public Token RightCurlyBracketToken;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="machineNode">MachineDeclarationNode</param>
        /// <param name="stateNode">StateDeclarationNode</param>
        public StatementBlockNode(MachineDeclarationNode machineNode, StateDeclarationNode stateNode)
        {
            this.Machine = machineNode;
            this.State = stateNode;
            this.Statements = new List<StatementNode>();
        }

        /// <summary>
        /// Returns the full text.
        /// </summary>
        /// <returns>string</returns>
        public override string GetFullText()
        {
            return base.TextUnit.Text;
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        public override string GetRewrittenText()
        {
            return base.RewrittenTextUnit.Text;
        }

        #endregion

        #region internal API

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="position">Position</param>
        internal override void Rewrite(ref int position)
        {
            var start = position;

            foreach (var stmt in this.Statements)
            {
                stmt.Rewrite(ref position);
            }

            var text = "\n";

            if (this.LeftCurlyBracketToken != null &&
                this.RightCurlyBracketToken != null)
            {
                text += this.LeftCurlyBracketToken.TextUnit.Text + "\n";
            }

            foreach (var stmt in this.Statements)
            {
                text += stmt.GetRewrittenText();
            }

            if (this.LeftCurlyBracketToken != null &&
                this.RightCurlyBracketToken != null)
            {
                text += this.RightCurlyBracketToken.TextUnit.Text + "\n";
            }

            if (this.LeftCurlyBracketToken != null &&
                this.RightCurlyBracketToken != null)
            {
                base.RewrittenTextUnit = new TextUnit(text, this.LeftCurlyBracketToken.TextUnit.Line, start);
            }
            else
            {
                base.RewrittenTextUnit = new TextUnit(text, this.Statements.First().TextUnit.Line, start);
            }

            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            foreach (var stmt in this.Statements)
            {
                stmt.GenerateTextUnit();
            }

            var text = "\n";

            if (this.LeftCurlyBracketToken != null &&
                this.RightCurlyBracketToken != null)
            {
                text += this.LeftCurlyBracketToken.TextUnit.Text + "\n";
            }

            foreach (var stmt in this.Statements)
            {
                text += stmt.GetFullText();
            }

            if (this.LeftCurlyBracketToken != null &&
                this.RightCurlyBracketToken != null)
            {
                text += this.RightCurlyBracketToken.TextUnit.Text + "\n";
            }

            if (this.LeftCurlyBracketToken != null &&
                this.RightCurlyBracketToken != null)
            {
                base.TextUnit = new TextUnit(text, this.LeftCurlyBracketToken.TextUnit.Line,
                    this.LeftCurlyBracketToken.TextUnit.Start);
            }
            else
            {
                base.TextUnit = new TextUnit(text, this.Statements.First().TextUnit.Line,
                    this.Statements.First().TextUnit.Start);
            }
        }

        #endregion
    }
}
