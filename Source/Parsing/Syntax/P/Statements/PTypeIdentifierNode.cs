//-----------------------------------------------------------------------
// <copyright file="PTypeIdentifierNode.cs">
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
    /// Type identifier node.
    /// </summary>
    public sealed class PTypeIdentifierNode : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The type tokens.
        /// </summary>
        public List<Token> TypeTokens;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        public PTypeIdentifierNode()
            : base()
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
            if (this.TypeTokens.Count == 0)
            {
                return;
            }

            var start = position;
            var text = "";

            this.RewriteTypeTokens();
            foreach (var tok in this.TypeTokens)
            {
                text += tok.TextUnit.Text;
            }

            base.RewrittenTextUnit = new TextUnit(text, this.TypeTokens.First().TextUnit.Line, start);
            position = base.RewrittenTextUnit.End + 1;
        }

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal override void GenerateTextUnit()
        {
            if (this.TypeTokens.Count == 0)
            {
                return;
            }

            var text = "";

            foreach (var tok in this.TypeTokens)
            {
                text += tok.TextUnit.Text;
            }

            base.TextUnit = new TextUnit(text, this.TypeTokens.First().TextUnit.Line,
                this.TypeTokens.First().TextUnit.Start);
        }

        #endregion

        #region private API

        /// <summary>
        /// Rewrites the type tokens.
        /// </summary>
        private void RewriteTypeTokens()
        {
            for (int idx = 0; idx < this.TypeTokens.Count; idx++)
            {
                var token = this.TypeTokens[idx];
                if (token.Type == TokenType.MachineDecl)
                {
                    var textUnit = new TextUnit("Machine", token.TextUnit.Line, token.TextUnit.Start);
                    this.TypeTokens[idx] = new Token(textUnit, token.Type);
                }
            }
        }

        #endregion
    }
}
