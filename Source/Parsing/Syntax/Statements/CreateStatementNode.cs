//-----------------------------------------------------------------------
// <copyright file="CreateStatementNode.cs">
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
    /// Create statement node.
    /// </summary>
    internal sealed class CreateStatementNode : StatementNode
    {
        #region fields

        /// <summary>
        /// The create keyword.
        /// </summary>
        internal Token CreateKeyword;

        /// <summary>
        /// The machine identifier.
        /// </summary>
        internal List<Token> MachineIdentifier;

        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        internal Token LeftParenthesisToken;

        /// <summary>
        /// The machine creation payload.
        /// </summary>
        internal ExpressionNode Payload;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        internal Token RightParenthesisToken;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">Node</param>
        internal CreateStatementNode(StatementBlockNode node)
            : base(node)
        {
            this.MachineIdentifier = new List<Token>();
        }

        /// <summary>
        /// Returns the full text.
        /// </summary>
        /// <returns>string</returns>
        internal override string GetFullText()
        {
            return base.TextUnit.Text;
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        internal override string GetRewrittenText()
        {
            return base.RewrittenTextUnit.Text;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite(IPSharpProgram program)
        {
            var machineId = "";
            foreach (var id in this.MachineIdentifier)
            {
                machineId += id.TextUnit.Text;
            }

            var text = "this.";

            bool isMonitor = false;
            if (program is PSharpProgram)
            {
                var psProgram = program as PSharpProgram;
                isMonitor = psProgram.NamespaceDeclarations.Any(ns => ns.MachineDeclarations.Any(md =>
                    md.IsMonitor && md.Identifier.TextUnit.Text.Equals(machineId)));
            }
            else
            {
                var pProgram = program as PProgram;
                isMonitor = pProgram.MachineDeclarations.Any(md =>
                    md.IsMonitor && md.Identifier.TextUnit.Text.Equals(machineId));
            }

            if (isMonitor)
            {
                text += "CreateMonitor<";
            }
            else
            {
                text += "Create<";
            }

            text += machineId;

            text += ">(";

            this.Payload.Rewrite(program);
            text += this.Payload.GetRewrittenText();

            text += ")";

            text += this.SemicolonToken.TextUnit.Text + "\n";

            base.RewrittenTextUnit = new TextUnit(text, this.CreateKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            var text = this.CreateKeyword.TextUnit.Text;
            text += " ";

            foreach (var id in this.MachineIdentifier)
            {
                text += id.TextUnit.Text;
            }

            if (this.LeftParenthesisToken != null &&
                this.RightParenthesisToken != null)
            {
                text += this.LeftParenthesisToken.TextUnit.Text;
            }

            this.Payload.GenerateTextUnit();
            text += this.Payload.GetFullText();

            if (this.LeftParenthesisToken != null &&
                this.RightParenthesisToken != null)
            {
                text += this.RightParenthesisToken.TextUnit.Text;
            }
            
            text += this.SemicolonToken.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.CreateKeyword.TextUnit.Line);
        }

        #endregion
    }
}
