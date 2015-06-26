//-----------------------------------------------------------------------
// <copyright file="MethodDeclaration.cs">
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
    /// Method declaration syntax node.
    /// </summary>
    internal sealed class MethodDeclaration : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The access modifier.
        /// </summary>
        internal AccessModifier AccessModifier;

        /// <summary>
        /// The inheritance modifier.
        /// </summary>
        internal InheritanceModifier InheritanceModifier;

        /// <summary>
        /// Is the method async.
        /// </summary>
        internal bool IsAsync;

        /// <summary>
        /// The type identifier.
        /// </summary>
        internal Token TypeIdentifier;

        /// <summary>
        /// The identifier token.
        /// </summary>
        internal Token Identifier;

        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        internal Token LeftParenthesisToken;

        /// <summary>
        /// List of parameter tokens.
        /// </summary>
        internal List<Token> Parameters;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        internal Token RightParenthesisToken;

        /// <summary>
        /// The semicolon token.
        /// </summary>
        internal Token SemicolonToken;

        /// <summary>
        /// The statement block.
        /// </summary>
        internal BlockSyntax StatementBlock;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="isModel">Is a model</param>
        internal MethodDeclaration(IPSharpProgram program, bool isModel)
            : base(program, isModel)
        {
            this.Parameters = new List<Token>();
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite()
        {
            if (this.IsModel)
            {
                base.TextUnit = new TextUnit("", 0);
                return;
            }

            var text = this.GetRewrittenMethodDeclaration();
            base.TextUnit = new TextUnit(text, this.TypeIdentifier.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            var text = this.GetRewrittenMethodDeclaration();
            base.TextUnit = new TextUnit(text, this.TypeIdentifier.TextUnit.Line);
        }

        #endregion

        #region private API

        /// <summary>
        /// Returns the rewritten method declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenMethodDeclaration()
        {
            var text = "";

            if (base.IsModel)
            {
                text += "[Model]\n";
            }

            if (this.AccessModifier == AccessModifier.Protected)
            {
                text += "protected ";
            }
            else
            {
                text += "private ";
            }

            if (this.IsAsync)
            {
                text += "async ";
            }

            if (this.InheritanceModifier == InheritanceModifier.Abstract)
            {
                text += "abstract ";
            }
            else if (this.InheritanceModifier == InheritanceModifier.Virtual)
            {
                text += "virtual ";
            }
            else if (this.InheritanceModifier == InheritanceModifier.Override)
            {
                text += "override ";
            }

            text += this.TypeIdentifier.TextUnit.Text;
            text += " ";

            text += this.Identifier.TextUnit.Text;

            text += this.LeftParenthesisToken.TextUnit.Text;

            for (int idx = 0; idx < this.Parameters.Count; idx++)
            {
                text += this.Parameters[idx].TextUnit.Text;

                if (idx < this.Parameters.Count - 1)
                {
                    text += " ";
                }
            }

            text += this.RightParenthesisToken.TextUnit.Text;

            if (this.StatementBlock != null)
            {
                this.StatementBlock.Rewrite();
                text += StatementBlock.TextUnit.Text;
            }
            else
            {
                text += this.SemicolonToken.TextUnit.Text + "\n";
            }

            return text;
        }

        #endregion
    }
}
