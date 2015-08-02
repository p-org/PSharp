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

namespace Microsoft.PSharp.Parsing
{
    public sealed class Token
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

        #endregion

        #region public API

        /// <summary>
        /// Constructor. By default the type of the token is None.
        /// </summary>
        /// <param name="text">String</param>
        public Token(string text)
        {
            this.Text = text;
            this.Type = TokenType.None;
        }

        /// <summary>
        /// Constructor. By default the type of the token is None.
        /// </summary>
        /// <param name="unit">TextUnit</param>
        public Token(TextUnit unit)
        {
            this.TextUnit = unit;
            this.Text = unit.Text;
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
            this.Type = type;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="unit">TextUnit</param>
        /// <param name="type">TokenType</param>
        public Token(TextUnit unit, TokenType type)
        {
            this.TextUnit = unit;
            this.Text = unit.Text;
            this.Type = type;
        }

        #endregion
    }
}
