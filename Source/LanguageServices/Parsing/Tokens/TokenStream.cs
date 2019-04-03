﻿// ------------------------------------------------------------------------------------------------
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
        public int Length
        {
            get => this.Tokens.Count;
        }

        /// <summary>
        /// True if no tokens remaining in the stream.
        /// </summary>
        public bool Done
        {
            get => this.Index == this.Length;
        }

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
        public Token Next()
        {
            if (this.Index == this.Tokens.Count)
            {
                return null;
            }

            var token = this.Tokens[this.Index];
            this.Index++;

            return token;
        }

        /// <summary>
        /// Returns the next token in the stream without progressing to the next token,
        /// or null if the stream is empty.
        /// </summary>
        public Token Peek()
        {
            if (this.Index == this.Tokens.Count)
            {
                return null;
            }

            return this.Tokens[this.Index];
        }

        /// <summary>
        /// Swaps the current token with the new token, or does nothing if the stream is empty.
        /// </summary>
        public void Swap(Token token)
        {
            if (this.Index == this.Tokens.Count)
            {
                return;
            }

            this.Tokens[this.Index] = token;
        }

        /// <summary>
        /// Returns the token in the given index of the stream, or null if the index is out of bounds.
        /// </summary>
        public Token GetAt(int index)
        {
            if (index >= this.Tokens.Count || index < 0)
            {
                return null;
            }

            return this.Tokens[index];
        }

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
            if (this.Index == this.Tokens.Count)
            {
                return;
            }

            this.Tokens.RemoveAt(this.Index);
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
    }
}
