//-----------------------------------------------------------------------
// <copyright file="PTupleType.cs">
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

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Tuple type.
    /// </summary>
    internal sealed class PTupleType : PBaseType
    {
        #region fields

        /// <summary>
        /// The tuple types.
        /// </summary>
        internal List<PBaseType> TupleTypes;

        /// <summary>
        /// The name tokens. Only used for named tuples.
        /// </summary>
        internal List<Token> NameTokens;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        internal PTupleType()
            : base(PType.Tuple)
        {
            this.TupleTypes = new List<PBaseType>();
            this.NameTokens = new List<Token>();
        }

        #endregion

        #region protected API

        /// <summary>
        /// Rewrites a tuple type.
        /// </summary>
        /// <returns>Text</returns>
        protected override string RewriteTypeTokens()
        {
            var text = "Container<";

            for (int idx = 0; idx < this.TupleTypes.Count; idx++)
            {
                this.TupleTypes[idx].Rewrite();
                text += this.TupleTypes[idx].GetRewrittenText();

                if (idx < this.TupleTypes.Count - 1)
                {
                    text += ",";
                }
            }

            text += ">";

            return text;
        }

        #endregion
    }
}
