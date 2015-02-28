//-----------------------------------------------------------------------
// <copyright file="MethodDeclarationNode.cs">
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
    /// Method declaration node.
    /// </summary>
    public sealed class MethodDeclarationNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The modifier token.
        /// </summary>
        public Token Modifier;

        /// <summary>
        /// The inheritance modifier token.
        /// </summary>
        public Token InheritanceModifier;

        /// <summary>
        /// The type identifier.
        /// </summary>
        public Token TypeIdentifier;

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
        /// The semicolon token.
        /// </summary>
        public Token SemicolonToken;

        /// <summary>
        /// The statement block.
        /// </summary>
        public StatementBlockNode StatementBlock;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        public MethodDeclarationNode()
            : base()
        {
            this.Parameters = new List<Token>();
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

            if (this.Modifier != null)
            {
                text += this.Modifier.TextUnit.Text;
                text += " ";
            }

            if (this.InheritanceModifier != null)
            {
                text += this.InheritanceModifier.TextUnit.Text;
                text += " ";
            }

            text += this.TypeIdentifier.TextUnit.Text;
            text += " ";

            text += this.Identifier.TextUnit.Text;

            text += this.LeftParenthesisToken.TextUnit.Text;

            foreach (var param in this.Parameters)
            {
                text += param.TextUnit.Text;
            }

            text += this.RightParenthesisToken.TextUnit.Text;

            if (this.StatementBlock != null)
            {
                this.StatementBlock.Rewrite(ref position);
                text += StatementBlock.GetRewrittenText();
            }
            else
            {
                text += this.SemicolonToken.TextUnit.Text + "\n";
            }

            base.TextUnit = new TextUnit(text, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            var text = "";
            Token initToken = null;

            if (this.InheritanceModifier != null)
            {
                initToken = this.InheritanceModifier;
                text += this.InheritanceModifier.TextUnit.Text;
                text += " ";
            }

            if (this.Modifier != null)
            {
                initToken = this.Modifier;
                text += this.Modifier.TextUnit.Text;
                text += " ";
            }

            text += this.TypeIdentifier.TextUnit.Text;
            text += " ";

            text += this.Identifier.TextUnit.Text;

            text += this.LeftParenthesisToken.TextUnit.Text;

            foreach (var param in this.Parameters)
            {
                text += param.TextUnit.Text;
            }

            text += this.RightParenthesisToken.TextUnit.Text;

            if (this.StatementBlock != null)
            {
                this.StatementBlock.GenerateTextUnit();
                text += this.StatementBlock.GetFullText();

                if (initToken != null)
                {
                    int length = this.StatementBlock.TextUnit.End - initToken.TextUnit.Start + 1;

                    base.TextUnit = new TextUnit(text, initToken.TextUnit.Start);
                }
                else
                {
                    int length = this.StatementBlock.TextUnit.End - this.TypeIdentifier.TextUnit.Start + 1;

                    base.TextUnit = new TextUnit(text, this.TypeIdentifier.TextUnit.Start);
                }
            }
            else
            {
                text += this.SemicolonToken.TextUnit.Text + "\n";

                if (initToken != null)
                {
                    base.TextUnit = new TextUnit(text, initToken.TextUnit.Start);
                }
                else
                {
                    base.TextUnit = new TextUnit(text, this.TypeIdentifier.TextUnit.Start);
                }
            }
        }

        #endregion
    }
}
