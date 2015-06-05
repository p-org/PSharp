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

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Function declaration node.
    /// </summary>
    internal sealed class PFunctionDeclarationNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// True if the function is a model.
        /// </summary>
        internal bool IsModel;

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
        internal StatementBlockNode StatementBlock;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        internal PFunctionDeclarationNode()
            : base()
        {
            this.Parameters = new List<Token>();
            this.ParameterTypes = new List<PBaseType>();
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        internal override string GetRewrittenText()
        {
            return base.TextUnit.Text;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="program">Program</param>
        internal override void Rewrite(IPSharpProgram program)
        {
            var text = "";

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

            this.StatementBlock.Rewrite(program);
            text += StatementBlock.GetRewrittenText();

            base.TextUnit = new TextUnit(text, this.FunctionKeyword.TextUnit.Line);
        }

        #endregion
    }
}
