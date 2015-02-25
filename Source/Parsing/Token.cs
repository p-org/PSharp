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
    public class Token
    {
        #region fields

        /// <summary>
        /// The text unit that this token represents.
        /// </summary>
        public readonly TextUnit TextUnit;

        /// <summary>
        /// The text that this token represents.
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// The type of this token.
        /// </summary>
        public readonly TokenType Type;

        /// <summary>
        /// The source code line of this token.
        /// </summary>
        public readonly int Line;

        #endregion

        #region public API

        /// <summary>
        /// Constructor. By default the type of the token is None.
        /// </summary>
        /// <param name="text">String</param>
        public Token(string text)
        {
            this.Text = text;
            this.Line = 0;
            this.Type = TokenType.None;
        }

        /// <summary>
        /// Constructor. By default the type of the token is None.
        /// </summary>
        /// <param name="text">String</param>
        /// <param name="line">Line</param>
        public Token(string text, int line)
        {
            this.Text = text;
            this.Line = line;
            this.Type = TokenType.None;
        }

        /// <summary>
        /// Constructor. By default the type of the token is None.
        /// </summary>
        /// <param name="unit">TextUnit</param>
        /// <param name="line">Line</param>
        public Token(TextUnit unit, int line)
        {
            this.TextUnit = unit;
            this.Text = unit.Text;
            this.Line = line;
            this.Type = TokenType.None;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="text">String</param>
        /// <param name="type">TokenType</param>
        public Token(string text, TokenType type)
        {
            this.Text = text;
            this.Line = 0;
            this.Type = type;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="text">String</param>
        /// <param name="line">Line</param>
        /// <param name="type">TokenType</param>
        public Token(string text, int line, TokenType type)
        {
            this.Text = text;
            this.Line = line;
            this.Type = type;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="unit">TextUnit</param>
        /// <param name="line">Line</param>
        /// <param name="type">TokenType</param>
        public Token(TextUnit unit, int line, TokenType type)
        {
            this.TextUnit = unit;
            this.Text = unit.Text;
            this.Line = line;
            this.Type = type;
        }

        #endregion
    }
}
