// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// P# syntax token stream.
    /// </summary>
    public sealed class TokenStream
    {
        /// <summary>
        /// List of tokens in the stream.
        /// </summary>
        private readonly List<Token> Tokens;

        /// <summary>
        /// The current index of the stream.
        /// </summary>
        public int Index;

        /// <summary>
        /// The length of the stream.
        /// </summary>
        public int Length => this.Tokens.Count;

        /// <summary>
        /// True if no tokens remaining in the stream.
        /// </summary>
        /// <remarks>
        /// Use >= because in some cases of early end of string (e.g. VS Lang Service parsing) we may increment this twice.
        /// </remarks>
        public bool Done => this.Index >= this.Length;

        /// <summary>
        /// The program this token stream belongs to.
        /// </summary>
        internal IPSharpProgram Program;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenStream"/> class.
        /// </summary>
        public TokenStream(List<Token> tokens)
        {
            this.Tokens = tokens.ToList();
            this.Index = 0;
        }

        /// <summary>
        /// Returns the next token in the stream and progresses by one token,
        /// or null if the stream is empty.
        /// </summary>
        public Token Next() => this.Done ? null : this.Tokens[this.Index++];

        /// <summary>
        /// Returns the next token in the stream without progressing to the next token,
        /// or null if the stream is empty.
        /// </summary>
        public Token Peek() => this.Done ? null : this.Tokens[this.Index];

        /// <summary>
        /// Returns the type of the most recent non-whitespace token, to help refine the
        /// list of expected tokens in the event of error.
        /// </summary>
        public TokenType PrevNonWhitespaceType()
        {
            for (var ii = this.Index - 1; ii >= 0; --ii)
            {
                var tokType = this.Tokens[ii].Type;
                switch (tokType)
                {
                    case TokenType.WhiteSpace:
                    case TokenType.NewLine:
                        continue;
                    default:
                        return tokType;
                }
            }

            return TokenType.None;
        }

        /// <summary>
        /// Swaps the current token with the new token, or does nothing if the stream is empty.
        /// </summary>
        public void Swap(TextUnit updatedText, TokenType updatedType = Token.DefaultTokenType)
        {
            if (!this.Done)
            {
                this.Tokens[this.Index] = new Token(updatedText, updatedType);
            }
        }

        /// <summary>
        /// Swaps the current token with a new token containing updated type.
        /// Does nothing if the stream is empty or the current index is past the end of the stream.
        /// </summary>
        public void Swap(TokenType updatedType)
        {
            if (!this.Done)
            {
                this.Tokens[this.Index] = this.Peek().WithType(updatedType);
            }
        }

        /// <summary>
        /// Returns the token in the given index of the stream, or null if the index is out of bounds.
        /// </summary>
        public Token GetAt(int index) => (index >= this.Tokens.Count || index < 0) ? null : this.Tokens[index];

        /// <summary>
        /// Returns the last token in the stream, or null if there are no tokens.
        /// </summary>
        public Token Last() => this.Tokens.Count == 0 ? null : this.Tokens[this.Tokens.Count - 1];

        /// <summary>
        /// Skips whitespace and comment tokens.
        /// </summary>
        public List<Token> SkipWhiteSpaceAndCommentTokens()
        {
            var skipped = new List<Token>();
            while (!this.Done)
            {
                var repeat = this.CommentOutLineComment();
                repeat = repeat || this.CommentOutMultiLineComment();
                repeat = repeat || this.SkipWhiteSpaceTokens(skipped);

                if (!repeat)
                {
                    break;
                }
            }

            return skipped;
        }

        /// <summary>
        /// Skips comment tokens.
        /// </summary>
        public void SkipCommentTokens()
        {
            while (!this.Done)
            {
                var repeat = this.CommentOutLineComment();
                repeat = repeat || this.CommentOutMultiLineComment();

                if (!repeat)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Consumes the next token in the stream, or does nothing if the stream is empty.
        /// </summary>
        private void Consume()
        {
            if (!this.Done)
            {
                this.Tokens.RemoveAt(this.Index);
            }
        }

        /// <summary>
        /// Skips whitespace tokens.
        /// </summary>
        private bool SkipWhiteSpaceTokens(List<Token> skipped)
        {
            if ((this.Peek().Type != TokenType.WhiteSpace) &&
                (this.Peek().Type != TokenType.NewLine))
            {
                return false;
            }

            while (!this.Done &&
                (this.Peek().Type == TokenType.WhiteSpace ||
                this.Peek().Type == TokenType.NewLine))
            {
                skipped.Add(this.Next());
            }

            return true;
        }

        /// <summary>
        /// Comments out a line-wide comment, if any.
        /// </summary>
        private bool CommentOutLineComment()
        {
            if ((this.Peek().Type != TokenType.CommentLine) &&
                (this.Peek().Type != TokenType.Region))
            {
                return false;
            }

            while (!this.Done &&
                this.Peek().Type != TokenType.NewLine)
            {
                this.Consume();
            }

            return true;
        }

        /// <summary>
        /// Comments out a multi-line comment, if any.
        /// </summary>
        private bool CommentOutMultiLineComment()
        {
            if (this.Peek().Type != TokenType.CommentStart)
            {
                return false;
            }

            while (!this.Done &&
                this.Peek().Type != TokenType.CommentEnd)
            {
                this.Consume();
            }

            this.Consume();
            return true;
        }

        /// <summary>
        /// Returns a string representation of the TokenStream's token count, current index, and current token.
        /// </summary>
        public override string ToString()
        {
            var token = this.Done ? "<done>" : this.Peek().ToString();
            return $"{this.Length}[{this.Index}] {token}";
        }
    }
}
