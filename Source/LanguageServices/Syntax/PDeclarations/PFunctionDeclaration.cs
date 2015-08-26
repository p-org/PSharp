//-----------------------------------------------------------------------
// <copyright file="PFunctionDeclaration.cs">
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

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Function declaration syntax node.
    /// </summary>
    internal sealed class PFunctionDeclaration : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The function keyword.
        /// </summary>
        internal Token FunctionKeyword;

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
        /// List of parameter types.
        /// </summary>
        internal List<PBaseType> ParameterTypes;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        internal Token RightParenthesisToken;

        /// <summary>
        /// The colon token.
        /// </summary>
        internal Token ColonToken;

        /// <summary>
        /// The return type.
        /// </summary>
        internal PBaseType ReturnType;

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
        internal PFunctionDeclaration(IPSharpProgram program, bool isModel)
            : base(program, isModel)
        {
            this.Parameters = new List<Token>();
            this.ParameterTypes = new List<PBaseType>();
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

            var text = this.GetRewrittenFunctionDeclaration();
            base.TextUnit = new TextUnit(text, this.FunctionKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            var text = this.GetRewrittenFunctionDeclaration();
            base.TextUnit = new TextUnit(text, this.FunctionKeyword.TextUnit.Line);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the rewritten function declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenFunctionDeclaration()
        {
            var text = "";

            if (base.IsModel)
            {
                text += "[Model]\n";
            }

            if (this.ColonToken != null)
            {
                this.ReturnType.Rewrite();
                text += this.ReturnType.GetRewrittenText();
            }
            else
            {
                text += "void";
            }

            text += " ";
            text += this.Identifier.TextUnit.Text;

            text += this.LeftParenthesisToken.TextUnit.Text;

            for (int idx = 0; idx < this.Parameters.Count; idx++)
            {
                if (idx > 0)
                {
                    text += ", ";
                }

                this.ParameterTypes[idx].Rewrite();
                text += this.ParameterTypes[idx].GetRewrittenText();
                text += " " + this.Parameters[idx].TextUnit.Text;
            }

            text += this.RightParenthesisToken.TextUnit.Text;

            this.StatementBlock.Rewrite();
            text += StatementBlock.TextUnit.Text;

            return text;
        }

        #endregion
    }
}
