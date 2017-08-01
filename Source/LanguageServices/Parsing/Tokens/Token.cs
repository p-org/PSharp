//-----------------------------------------------------------------------
// <copyright file="Token.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
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

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// P# syntax token.
    /// </summary>
    public sealed class Token
    {
        #region fields

        /// <summary>
        /// The text unit that this token represents in the rewritten buffer.
        /// </summary>
        public readonly TextUnit TextUnit;

        /// <summary>
        /// The text that this token represents.
        /// </summary>
        public string Text { get { return this.TextUnit.Text; } }

        /// <summary>
        /// The type of this token.
        /// </summary>
        public readonly TokenType Type;

        /// <summary>
        /// The default type of a token.
        /// </summary>
        public const TokenType DefaultTokenType = TokenType.None;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="unit">TextUnit</param>
        /// <param name="type">TokenType</param>
        public Token(TextUnit unit, TokenType type = DefaultTokenType)
        {
            this.TextUnit = unit;
            this.Type = type;
        }

        /// <summary>
        /// Copy and type-update constructor.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="updatedType"></param>
        public Token(Token original, TokenType updatedType) : this(original, original.TextUnit, updatedType)
        {
        }

        /// <summary>
        /// Copy and update constructor. Ensures that the original text is preserved across the copy.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="updatedText"></param>
        /// <param name="updatedType"></param>
        public Token(Token original, TextUnit updatedText, TokenType updatedType = DefaultTokenType)
        {
            this.TextUnit = updatedText;
            this.Type = updatedType;
        }

        /// <summary>
        /// Returns a string representing the Token.
        /// </summary>
        /// <returns>A string representing the TextUnit.</returns>
        public override string ToString()
        {
            return $"{this.Text} {this.Type} ({this.TextUnit})";
        }
        #endregion
    }
}
