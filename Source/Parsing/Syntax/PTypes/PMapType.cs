//-----------------------------------------------------------------------
// <copyright file="PMapType.cs">
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

namespace Microsoft.PSharp.Parsing.Syntax
{
    /// <summary>
    /// Map type.
    /// </summary>
    internal sealed class PMapType : PBaseType
    {
        #region fields

        /// <summary>
        /// The key type.
        /// </summary>
        internal PBaseType KeyType;

        /// <summary>
        /// The value type.
        /// </summary>
        internal PBaseType ValueType;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        internal PMapType()
            : base(PType.Map)
        {

        }

        #endregion

        #region protected API

        /// <summary>
        /// Rewrites a map type.
        /// </summary>
        /// <returns>Text</returns>
        protected override string RewriteTypeTokens()
        {
            var text = "Map<";

            this.KeyType.Rewrite();
            text += this.KeyType.GetRewrittenText();

            text += ", ";

            this.ValueType.Rewrite();
            text += this.ValueType.GetRewrittenText();

            text += ">";

            return text;
        }

        #endregion
    }
}
