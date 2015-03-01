//-----------------------------------------------------------------------
// <copyright file="PFunctionDeclarationNode.cs">
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

namespace Microsoft.PSharp.Parsing.Syntax.P
{
    /// <summary>
    /// Function declaration node.
    /// </summary>
    public sealed class PFunctionDeclarationNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// True if the function is a model.
        /// </summary>
        public bool IsModel;

        /// <summary>
        /// The function keyword.
        /// </summary>
        public Token FunctionKeyword;

        /// <summary>
        /// The identifier token.
        /// </summary>
        public Token Identifier;

        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        public Token LeftParenthesisToken;

        /// <summary>
        /// List of parameter tokens.
        /// </summary>
        public List<Token> Parameters;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        public Token RightParenthesisToken;

        /// <summary>
        /// The colon token.
        /// </summary>
        public Token ColonToken;

        /// <summary>
        /// The return type tokens.
        /// </summary>
        public List<Token> ReturnTypeTokens;

        /// <summary>
        /// The statement block.
        /// </summary>
        public PStatementBlockNode StatementBlock;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        public PFunctionDeclarationNode()
            : base()
        {
            this.Parameters = new List<Token>();
            this.ReturnTypeTokens = new List<Token>();
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
            var text = "";

            foreach (var node in this.ReturnTypeTokens)
            {
                text += node.TextUnit.Text;
            }

            if (this.ReturnTypeTokens.Count == 0)
            {
                text += "void";
            }

            text += " ";
            text += this.Identifier.TextUnit.Text;

            text += this.LeftParenthesisToken.TextUnit.Text;

            foreach (var param in this.Parameters)
            {
                text += param.TextUnit.Text;
            }

            text += this.RightParenthesisToken.TextUnit.Text;

            this.StatementBlock.Rewrite(ref position);
            text += StatementBlock.GetRewrittenText();

            base.RewrittenTextUnit = new TextUnit(text, this.FunctionKeyword.TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            var text = "";

            text += this.FunctionKeyword.TextUnit.Text;
            text += " ";

            text += this.Identifier.TextUnit.Text;

            text += this.LeftParenthesisToken.TextUnit.Text;

            foreach (var param in this.Parameters)
            {
                text += param.TextUnit.Text;
            }

            text += this.RightParenthesisToken.TextUnit.Text;

            this.StatementBlock.GenerateTextUnit();
            text += this.StatementBlock.GetFullText();

            base.TextUnit = new TextUnit(text, this.FunctionKeyword.TextUnit.Line,
                this.FunctionKeyword.TextUnit.Start);
        }

        #endregion
    }
}
