using
namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// P# syntax token.
    /// </summary>
    public sealed class Token
    {
        /// <summary>
        /// The text unit that this token represents in the rewritten buffer.
        /// </summary>
        public readonly TextUnit TextUnit;

        /// <summary>
        /// The text that this token represents.
        /// </summary>
        public string Text => this.TextUnit.Text;

        /// <summary>
        /// The type of this token.
        /// </summary>
        public readonly TokenType Type;

        /// <summary>
        /// The default type of a token.
        /// </summary>
        public const TokenType DefaultTokenType = TokenType.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> class.
        /// </summary>
        public Token(TextUnit unit, TokenType type = DefaultTokenType)
        {
            this.TextUnit = unit;
            this.Type = type;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Token"/> class with an updated type.
        /// </summary>
        public Token WithType(TokenType updatedType) => new Token(this.TextUnit, updatedType);

        /// <summary>
        /// Returns a string representing the Token.
        /// </summary>
        public override string ToString() => $"{this.Text} {this.Type} ({this.TextUnit})";
    }
}
