//-----------------------------------------------------------------------
// <copyright file="EventDeclaration.cs">
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

using System.Collections.Generic;
using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Event declaration syntax node.
    /// </summary>
    internal sealed class EventDeclaration : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The event keyword.
        /// </summary>
        internal Token EventKeyword;

        /// <summary>
        /// The access modifier.
        /// </summary>
        internal AccessModifier AccessModifier;

        /// <summary>
        /// The identifier token.
        /// </summary>
        internal Token Identifier;

        /// <summary>
        /// The left parenthesis token.
        /// </summary>
        internal Token LeftParenthesis;

        /// <summary>
        /// The payload types.
        /// </summary>
        internal List<Token> PayloadTypes;

        /// <summary>
        /// The payload identifiers.
        /// </summary>
        internal List<Token> PayloadIdentifiers;

        /// <summary>
        /// The right parenthesis token.
        /// </summary>
        internal Token RightParenthesis;

        /// <summary>
        /// The assert keyword.
        /// </summary>
        internal Token AssertKeyword;

        /// <summary>
        /// The assume keyword.
        /// </summary>
        internal Token AssumeKeyword;

        /// <summary>
        /// The assert value.
        /// </summary>
        internal int AssertValue;

        /// <summary>
        /// The assume value.
        /// </summary>
        internal int AssumeValue;

        /// <summary>
        /// The semicolon token.
        /// </summary>
        internal Token SemicolonToken;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        internal EventDeclaration(IPSharpProgram program)
            : base(program, false)
        {
            this.PayloadTypes = new List<Token>();
            this.PayloadIdentifiers = new List<Token>();
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite()
        {
            var text = this.GetRewrittenEventDeclaration();
            base.TextUnit = new TextUnit(text, this.EventKeyword.TextUnit.Line);
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation using any given program models.
        /// </summary>
        internal override void Model()
        {
            var text = this.GetRewrittenEventDeclaration();
            base.TextUnit = new TextUnit(text, this.EventKeyword.TextUnit.Line);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the rewritten event declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenEventDeclaration()
        {
            var text = "";

            if ((this.Program as AbstractPSharpProgram).Project.CompilationContext.
                ActiveCompilationTarget == CompilationTarget.Remote)
            {
                text += "[System.Runtime.Serialization.DataContract]\n";
            }

            if (this.AccessModifier == AccessModifier.Public)
            {
                text += "public ";
            }
            else if (this.AccessModifier == AccessModifier.Internal)
            {
                text += "internal ";
            }

            text += "class " + this.Identifier.TextUnit.Text + " : Event";

            text += "\n";
            text += "{\n";

            for (int i = 0; i < this.PayloadIdentifiers.Count; i++)
            {
                text += " public ";
                text += this.PayloadTypes[i].TextUnit.Text + " ";
                text += this.PayloadIdentifiers[i].TextUnit.Text + ";\n";
            }

            if (this.AccessModifier == AccessModifier.Internal)
            {
                text += " internal ";
            }
            else
            {
                text += " public ";
            }

            text += this.Identifier.TextUnit.Text + "(";

            for (int i = 0; i < this.PayloadIdentifiers.Count; i++)
            {
                if (i == this.PayloadIdentifiers.Count - 1)
                {
                    text += this.PayloadTypes[i].TextUnit.Text + " ";
                    text += this.PayloadIdentifiers[i].TextUnit.Text;
                }
                else
                {
                    text += this.PayloadTypes[i].TextUnit.Text + " ";
                    text += this.PayloadIdentifiers[i].TextUnit.Text + ", ";
                }
            }

            text += ")\n";
            text += "  : base(";

            if (this.AssertKeyword != null)
            {
                text += this.AssertValue + ", -1";
            }
            else if (this.AssumeKeyword != null)
            {
                text += "-1, " + this.AssumeValue;
            }

            text += ")\n";
            text += " {\n";

            for (int i = 0; i < this.PayloadIdentifiers.Count; i++)
            {
                text += "  this." + this.PayloadIdentifiers[i].TextUnit.Text + " = ";
                text += this.PayloadIdentifiers[i].TextUnit.Text + ";\n";
            }

            text += " }\n";
            text += "}\n";

            return text;
        }

        #endregion
    }
}
