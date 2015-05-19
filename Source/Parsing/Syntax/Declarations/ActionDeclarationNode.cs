//-----------------------------------------------------------------------
// <copyright file="ActionDeclarationNode.cs">
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
    /// Action declaration node.
    /// </summary>
    public sealed class ActionDeclarationNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The action keyword.
        /// </summary>
        public Token ActionKeyword;

        /// <summary>
        /// The modifier token.
        /// </summary>
        public Token Modifier;

        /// <summary>
        /// The inheritance modifier token.
        /// </summary>
        public Token InheritanceModifier;

        /// <summary>
        /// The identifier token.
        /// </summary>
        public Token Identifier;

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
        public ActionDeclarationNode()
            : base()
        {
            
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
            
            if (this.StatementBlock != null)
            {
                this.StatementBlock.Rewrite(ref position);
            }

            var text = "";
            var initToken = this.ActionKeyword;

            if (this.Modifier != null)
            {
                initToken = this.Modifier;
                text += this.Modifier.TextUnit.Text;
                text += " ";
            }

            if (this.InheritanceModifier != null)
            {
                initToken = this.InheritanceModifier;
                text += this.InheritanceModifier.TextUnit.Text;
                text += " ";
            }

            text += "void " + this.Identifier.TextUnit.Text + "()";

            if (this.StatementBlock != null)
            {
                text += StatementBlock.GetRewrittenText();
            }
            else
            {
                text += this.SemicolonToken.TextUnit.Text + "\n";
            }

            base.RewrittenTextUnit = new TextUnit(text, initToken.TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            if (this.StatementBlock != null)
            {
                this.StatementBlock.GenerateTextUnit();
            }

            var text = "";
            var initToken = this.ActionKeyword;

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

            text += this.ActionKeyword.TextUnit.Text;
            text += " ";

            text += this.Identifier.TextUnit.Text;

            if (this.StatementBlock != null)
            {
                text += this.StatementBlock.GetFullText();

                base.TextUnit = new TextUnit(text, initToken.TextUnit.Line, initToken.TextUnit.Start);
            }
            else
            {
                text += this.SemicolonToken.TextUnit.Text + "\n";

                base.TextUnit = new TextUnit(text, initToken.TextUnit.Line, initToken.TextUnit.Start);
            }
        }

        #endregion
    }
}
