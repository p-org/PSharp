//-----------------------------------------------------------------------
// <copyright file="TypeIdentifierNode.cs">
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
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Event declaration node.
    /// </summary>
    public sealed class TypeIdentifierNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The identifier token.
        /// </summary>
        public Token Identifier;

        /// <summary>
        /// The left angle bracket token.
        /// </summary>
        public Token LeftAngleBracket;

        /// <summary>
        /// The type tokens.
        /// </summary>
        public List<Token> TypeTokens;

        /// <summary>
        /// The right angle bracket token.
        /// </summary>
        public Token RightAngleBracket;

        #endregion

        #region public API

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TypeIdentifierNode()
        {
            this.TypeTokens = new List<Token>();
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
            base.RewrittenTextUnit = TextUnit.Clone(base.TextUnit, position);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            var text = this.Identifier.TextUnit.Text;

            if (this.LeftAngleBracket != null)
            {
                text += this.LeftAngleBracket.TextUnit.Text;

                foreach (var type in this.TypeTokens)
                {
                    text += type.TextUnit.Text;
                }

                text += this.RightAngleBracket.TextUnit.Text;

                int length = this.RightAngleBracket.TextUnit.End - this.Identifier.TextUnit.Start + 1;

                base.TextUnit = new TextUnit(text, length, this.Identifier.TextUnit.Start);
            }
            else
            {
                int length = this.Identifier.TextUnit.End - this.Identifier.TextUnit.Start + 1;

                base.TextUnit = new TextUnit(text, length, this.Identifier.TextUnit.Start);
            }
        }

        #endregion
    }
}
