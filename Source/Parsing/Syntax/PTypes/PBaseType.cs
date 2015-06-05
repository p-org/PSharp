//-----------------------------------------------------------------------
// <copyright file="PBaseType.cs">
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
using System.Linq;

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Base type.
    /// </summary>
    internal class PBaseType
    {
        #region fields

        /// <summary>
        /// The type.
        /// </summary>
        internal PType Type;

        /// <summary>
        /// The type tokens.
        /// </summary>
        internal List<Token> TypeTokens;

        /// <summary>
        /// The rewritten text unit.
        /// </summary>
        internal TextUnit RewrittenTextUnit;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">Type</param>
        internal PBaseType(PType type)
        {
            this.Type = type;
            this.TypeTokens = new List<Token>();
        }

        /// <summary>
        /// Returns the rewritten text.
        /// </summary>
        /// <returns>string</returns>
        internal string GetRewrittenText()
        {
            if (this.TypeTokens.Count == 0)
            {
                return "";
            }

            return this.RewrittenTextUnit.Text;
        }

        /// <summary>
        /// Rewrites the type to the intermediate C# representation.
        /// </summary>
        internal void Rewrite()
        {
            if (this.TypeTokens.Count == 0)
            {
                return;
            }

            var text = this.RewriteTypeTokens();

            this.RewrittenTextUnit = new TextUnit(text, this.TypeTokens.First().TextUnit.Line);

            return;
        }

        #endregion

        #region protected API

        /// <summary>
        /// Rewrites a type.
        /// </summary>
        /// <returns>Text</returns>
        protected virtual string RewriteTypeTokens()
        {
            var text = "";

            if (this.Type == PType.Machine)
            {
                text += "Machine";
            }
            else if (this.Type == PType.Any)
            {
                text += "object";
            }
            else if (this.Type == PType.Event)
            {
                text += "Type";
            }
            else
            {
                foreach (var token in this.TypeTokens)
                {
                    text += token.TextUnit.Text;
                }
            }

            return text;
        }

        #endregion
    }
}
