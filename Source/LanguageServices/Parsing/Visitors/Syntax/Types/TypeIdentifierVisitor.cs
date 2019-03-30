// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# type identifier parsing visitor.
    /// </summary>
    internal sealed class TypeIdentifierVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeIdentifierVisitor"/> class.
        /// </summary>
        public TypeIdentifierVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {
        }

        /// <summary>
        /// Visits the text unit.
        /// </summary>
        public void Visit(ref TextUnit textUnit)
        {
            if (this.TokenStream.Done ||
                (this.TokenStream.Peek().Type != TokenType.MachineDecl &&
                this.TokenStream.Peek().Type != TokenType.Void &&
                this.TokenStream.Peek().Type != TokenType.Object &&
                this.TokenStream.Peek().Type != TokenType.String &&
                this.TokenStream.Peek().Type != TokenType.Sbyte &&
                this.TokenStream.Peek().Type != TokenType.Byte &&
                this.TokenStream.Peek().Type != TokenType.Short &&
                this.TokenStream.Peek().Type != TokenType.Ushort &&
                this.TokenStream.Peek().Type != TokenType.Int &&
                this.TokenStream.Peek().Type != TokenType.Uint &&
                this.TokenStream.Peek().Type != TokenType.Long &&
                this.TokenStream.Peek().Type != TokenType.Ulong &&
                this.TokenStream.Peek().Type != TokenType.Char &&
                this.TokenStream.Peek().Type != TokenType.Bool &&
                this.TokenStream.Peek().Type != TokenType.Decimal &&
                this.TokenStream.Peek().Type != TokenType.Float &&
                this.TokenStream.Peek().Type != TokenType.Double &&
                this.TokenStream.Peek().Type != TokenType.Identifier))
            {
                throw new ParsingException("Expected type identifier.", TokenType.Identifier);
            }

            var line = this.TokenStream.Peek().TextUnit.Line;

            bool expectsDot = false;
            while (!this.TokenStream.Done)
            {
                if ((!expectsDot &&
                    this.TokenStream.Peek().Type != TokenType.MachineDecl &&
                    this.TokenStream.Peek().Type != TokenType.Void &&
                    this.TokenStream.Peek().Type != TokenType.Object &&
                    this.TokenStream.Peek().Type != TokenType.String &&
                    this.TokenStream.Peek().Type != TokenType.Sbyte &&
                    this.TokenStream.Peek().Type != TokenType.Byte &&
                    this.TokenStream.Peek().Type != TokenType.Short &&
                    this.TokenStream.Peek().Type != TokenType.Ushort &&
                    this.TokenStream.Peek().Type != TokenType.Int &&
                    this.TokenStream.Peek().Type != TokenType.Uint &&
                    this.TokenStream.Peek().Type != TokenType.Long &&
                    this.TokenStream.Peek().Type != TokenType.Ulong &&
                    this.TokenStream.Peek().Type != TokenType.Char &&
                    this.TokenStream.Peek().Type != TokenType.Bool &&
                    this.TokenStream.Peek().Type != TokenType.Decimal &&
                    this.TokenStream.Peek().Type != TokenType.Float &&
                    this.TokenStream.Peek().Type != TokenType.Double &&
                    this.TokenStream.Peek().Type != TokenType.Identifier) ||
                    (expectsDot && this.TokenStream.Peek().Type != TokenType.Dot))
                {
                    break;
                }

                if (this.TokenStream.Peek().Type == TokenType.MachineDecl ||
                    this.TokenStream.Peek().Type == TokenType.Void ||
                    this.TokenStream.Peek().Type == TokenType.Object ||
                    this.TokenStream.Peek().Type == TokenType.String ||
                    this.TokenStream.Peek().Type == TokenType.Sbyte ||
                    this.TokenStream.Peek().Type == TokenType.Byte ||
                    this.TokenStream.Peek().Type == TokenType.Short ||
                    this.TokenStream.Peek().Type == TokenType.Ushort ||
                    this.TokenStream.Peek().Type == TokenType.Int ||
                    this.TokenStream.Peek().Type == TokenType.Uint ||
                    this.TokenStream.Peek().Type == TokenType.Long ||
                    this.TokenStream.Peek().Type == TokenType.Ulong ||
                    this.TokenStream.Peek().Type == TokenType.Char ||
                    this.TokenStream.Peek().Type == TokenType.Bool ||
                    this.TokenStream.Peek().Type == TokenType.Decimal ||
                    this.TokenStream.Peek().Type == TokenType.Float ||
                    this.TokenStream.Peek().Type == TokenType.Double ||
                    this.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    expectsDot = true;
                }
                else if (this.TokenStream.Peek().Type == TokenType.Dot)
                {
                    expectsDot = false;
                }

                if (this.TokenStream.Peek().Type == TokenType.MachineDecl)
                {
                    this.TokenStream.Swap(new Token(new TextUnit("MachineId", this.TokenStream.Peek().TextUnit.Line), TokenType.MachineDecl));
                }

                if (textUnit == null)
                {
                    textUnit = new TextUnit(this.TokenStream.Peek().TextUnit.Text, line);
                }
                else
                {
                    textUnit = new TextUnit(textUnit.Text + this.TokenStream.Peek().TextUnit.Text, line);
                }

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
            {
                textUnit = new TextUnit(textUnit.Text + this.TokenStream.Peek().TextUnit.Text, line);

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                int counter = 1;
                while (!this.TokenStream.Done)
                {
                    if (this.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
                    {
                        counter++;
                    }
                    else if (this.TokenStream.Peek().Type == TokenType.RightAngleBracket)
                    {
                        counter--;
                    }

                    if (counter == 0 ||
                        (this.TokenStream.Peek().Type != TokenType.MachineDecl &&
                        this.TokenStream.Peek().Type != TokenType.Void &&
                        this.TokenStream.Peek().Type != TokenType.Object &&
                        this.TokenStream.Peek().Type != TokenType.String &&
                        this.TokenStream.Peek().Type != TokenType.Sbyte &&
                        this.TokenStream.Peek().Type != TokenType.Byte &&
                        this.TokenStream.Peek().Type != TokenType.Short &&
                        this.TokenStream.Peek().Type != TokenType.Ushort &&
                        this.TokenStream.Peek().Type != TokenType.Int &&
                        this.TokenStream.Peek().Type != TokenType.Uint &&
                        this.TokenStream.Peek().Type != TokenType.Long &&
                        this.TokenStream.Peek().Type != TokenType.Ulong &&
                        this.TokenStream.Peek().Type != TokenType.Char &&
                        this.TokenStream.Peek().Type != TokenType.Bool &&
                        this.TokenStream.Peek().Type != TokenType.Decimal &&
                        this.TokenStream.Peek().Type != TokenType.Float &&
                        this.TokenStream.Peek().Type != TokenType.Double &&
                        this.TokenStream.Peek().Type != TokenType.Identifier &&
                        this.TokenStream.Peek().Type != TokenType.Dot &&
                        this.TokenStream.Peek().Type != TokenType.Comma &&
                        this.TokenStream.Peek().Type != TokenType.LeftSquareBracket &&
                        this.TokenStream.Peek().Type != TokenType.RightSquareBracket &&
                        this.TokenStream.Peek().Type != TokenType.LeftAngleBracket &&
                        this.TokenStream.Peek().Type != TokenType.RightAngleBracket))
                    {
                        break;
                    }

                    if (this.TokenStream.Peek().Type == TokenType.MachineDecl)
                    {
                        this.TokenStream.Swap(new Token(new TextUnit("MachineId", this.TokenStream.Peek().TextUnit.Line), TokenType.MachineDecl));
                    }

                    textUnit = new TextUnit(textUnit.Text + this.TokenStream.Peek().TextUnit.Text, line);

                    this.TokenStream.Index++;
                    this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                }

                if (this.TokenStream.Done ||
                    this.TokenStream.Peek().Type != TokenType.RightAngleBracket)
                {
                    throw new ParsingException("Expected \">\".", TokenType.RightAngleBracket);
                }

                textUnit = new TextUnit(textUnit.Text + this.TokenStream.Peek().TextUnit.Text, line);

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.TokenStream.Peek().Type == TokenType.LeftSquareBracket)
            {
                textUnit = new TextUnit(textUnit.Text + this.TokenStream.Peek().TextUnit.Text, line);

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (this.TokenStream.Done ||
                    this.TokenStream.Peek().Type != TokenType.RightSquareBracket)
                {
                    throw new ParsingException("Expected \"]\".", TokenType.RightSquareBracket);
                }

                textUnit = new TextUnit(textUnit.Text + this.TokenStream.Peek().TextUnit.Text, line);

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }
        }
    }
}
