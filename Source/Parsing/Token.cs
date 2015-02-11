//-----------------------------------------------------------------------
// <copyright file="Token.cs">
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

namespace Microsoft.PSharp.Parsing
{
    internal class Token
    {
        #region fields

        /// <summary>
        /// The string that this token represents.
        /// </summary>
        public readonly string String;

        /// <summary>
        /// The type of this token.
        /// </summary>
        public readonly TokenType Type;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="text">String</param>
        public Token(string text)
        {
            this.String = text;
            this.Type = TokenType.None;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="text">String</param>
        /// <param name="type">TokenType</param>
        public Token(string text, TokenType type)
        {
            this.String = text;
            this.Type = type;
        }

        #endregion
    }
}
