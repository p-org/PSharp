//-----------------------------------------------------------------------
// <copyright file="PSharpSyntaxNode.cs">
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
    /// P# syntax node.
    /// </summary>
    public abstract class PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The text unit.
        /// </summary>
        internal TextUnit TextUnit;

        /// <summary>
        /// The rewritten text unit.
        /// </summary>
        internal TextUnit RewrittenTextUnit;

        /// <summary>
        /// The rewritten tokens.
        /// </summary>
        internal List<Token> RewrittenTokens;

        #endregion

        #region public API

        /// <summary>
        /// Returns the full text.
        /// </summary>
        /// <returns>string</returns>
        public abstract string GetFullText();

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        public abstract string GetRewrittenText();

        #endregion

        #region internal API

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        /// <param name="position">Position</param>
        internal abstract void Rewrite(ref int position);

        /// <summary>
        /// Generates a new text unit.
        /// </summary>
        internal abstract void GenerateTextUnit();

        #endregion

        #region protected API

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected PSharpSyntaxNode()
        {
            this.RewrittenTokens = new List<Token>();
        }

        #endregion
    }
}
