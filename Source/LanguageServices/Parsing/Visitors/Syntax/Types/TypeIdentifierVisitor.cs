//-----------------------------------------------------------------------
// <copyright file="TypeIdentifierVisitor.cs">
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

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# type identifier parsing visitor.
    /// </summary>
    internal sealed class TypeIdentifierVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        public TypeIdentifierVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the type.
        /// </summary>
        /// <param name="type">Type</param>
        public void Visit(ref PBaseType type)
        {
            if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Seq &&
                    base.TokenStream.Peek().Type != TokenType.Map &&
                    base.TokenStream.Peek().Type != TokenType.Any &&
                    base.TokenStream.Peek().Type != TokenType.EventDecl &&
                    base.TokenStream.Peek().Type != TokenType.LeftParenthesis))
            {
                throw new ParsingException("Expected type.",
                    new List<TokenType>
                {
                    TokenType.MachineDecl,
                    TokenType.Int,
                    TokenType.Bool,
                    TokenType.Seq,
                    TokenType.Map,
                    TokenType.Any,
                    TokenType.EventDecl
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.MachineDecl)
            {
                base.TokenStream.Swap(new Token(new TextUnit("MachineId", base.TokenStream.
                    Peek().TextUnit.Line), TokenType.MachineDecl));
                type = new PBaseType(PType.Machine);
                type.TypeTokens.Add(base.TokenStream.Peek());
            }
            else if (base.TokenStream.Peek().Type == TokenType.Int)
            {
                type = new PBaseType(PType.Int);
                type.TypeTokens.Add(base.TokenStream.Peek());
            }
            else if (base.TokenStream.Peek().Type == TokenType.Bool)
            {
                type = new PBaseType(PType.Bool);
                type.TypeTokens.Add(base.TokenStream.Peek());
            }
            else if (base.TokenStream.Peek().Type == TokenType.Any)
            {
                type = new PBaseType(PType.Any);
                type.TypeTokens.Add(base.TokenStream.Peek());
            }
            else if (base.TokenStream.Peek().Type == TokenType.EventDecl)
            {
                type = new PBaseType(PType.Event);
                type.TypeTokens.Add(base.TokenStream.Peek());
            }
            else if (base.TokenStream.Peek().Type == TokenType.Seq)
            {
                var seqType = new PSeqType();
                new SeqTypeIdentifierVisitor(base.TokenStream).Visit(ref seqType);
                type = seqType;
            }
            else if (base.TokenStream.Peek().Type == TokenType.Map)
            {
                var mapType = new PMapType();
                new MapTypeIdentifierVisitor(base.TokenStream).Visit(ref mapType);
                type = mapType;
            }
            else if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
            {
                var tupleType = new PTupleType();
                new TupleTypeIdentifierVisitor(base.TokenStream).Visit(ref tupleType);
                type = tupleType;
            }

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();
        }

        /// <summary>
        /// Visits the text unit.
        /// </summary>
        /// <param name="textUnit">TextUnit</param>
        public void Visit(ref TextUnit textUnit)
        {
            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.Void &&
                base.TokenStream.Peek().Type != TokenType.Object &&
                base.TokenStream.Peek().Type != TokenType.String &&
                base.TokenStream.Peek().Type != TokenType.Sbyte &&
                base.TokenStream.Peek().Type != TokenType.Byte &&
                base.TokenStream.Peek().Type != TokenType.Short &&
                base.TokenStream.Peek().Type != TokenType.Ushort &&
                base.TokenStream.Peek().Type != TokenType.Int &&
                base.TokenStream.Peek().Type != TokenType.Uint &&
                base.TokenStream.Peek().Type != TokenType.Long &&
                base.TokenStream.Peek().Type != TokenType.Ulong &&
                base.TokenStream.Peek().Type != TokenType.Char &&
                base.TokenStream.Peek().Type != TokenType.Bool &&
                base.TokenStream.Peek().Type != TokenType.Decimal &&
                base.TokenStream.Peek().Type != TokenType.Float &&
                base.TokenStream.Peek().Type != TokenType.Double &&
                base.TokenStream.Peek().Type != TokenType.Identifier))
            {
                throw new ParsingException("Expected type identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }
            
            var line = base.TokenStream.Peek().TextUnit.Line;

            bool expectsDot = false;
            while (!base.TokenStream.Done)
            {
                if (!expectsDot &&
                    (base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                    base.TokenStream.Peek().Type != TokenType.Void &&
                    base.TokenStream.Peek().Type != TokenType.Object &&
                    base.TokenStream.Peek().Type != TokenType.String &&
                    base.TokenStream.Peek().Type != TokenType.Sbyte &&
                    base.TokenStream.Peek().Type != TokenType.Byte &&
                    base.TokenStream.Peek().Type != TokenType.Short &&
                    base.TokenStream.Peek().Type != TokenType.Ushort &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Uint &&
                    base.TokenStream.Peek().Type != TokenType.Long &&
                    base.TokenStream.Peek().Type != TokenType.Ulong &&
                    base.TokenStream.Peek().Type != TokenType.Char &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Decimal &&
                    base.TokenStream.Peek().Type != TokenType.Float &&
                    base.TokenStream.Peek().Type != TokenType.Double &&
                    base.TokenStream.Peek().Type != TokenType.Identifier) ||
                    (expectsDot && base.TokenStream.Peek().Type != TokenType.Dot))
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.MachineDecl ||
                    base.TokenStream.Peek().Type == TokenType.Void ||
                    base.TokenStream.Peek().Type == TokenType.Object ||
                    base.TokenStream.Peek().Type == TokenType.String ||
                    base.TokenStream.Peek().Type == TokenType.Sbyte ||
                    base.TokenStream.Peek().Type == TokenType.Byte ||
                    base.TokenStream.Peek().Type == TokenType.Short ||
                    base.TokenStream.Peek().Type == TokenType.Ushort ||
                    base.TokenStream.Peek().Type == TokenType.Int ||
                    base.TokenStream.Peek().Type == TokenType.Uint ||
                    base.TokenStream.Peek().Type == TokenType.Long ||
                    base.TokenStream.Peek().Type == TokenType.Ulong ||
                    base.TokenStream.Peek().Type == TokenType.Char ||
                    base.TokenStream.Peek().Type == TokenType.Bool ||
                    base.TokenStream.Peek().Type == TokenType.Decimal ||
                    base.TokenStream.Peek().Type == TokenType.Float ||
                    base.TokenStream.Peek().Type == TokenType.Double ||
                    base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    expectsDot = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Dot)
                {
                    expectsDot = false;
                }

                if (base.TokenStream.Peek().Type == TokenType.MachineDecl)
                {
                    base.TokenStream.Swap(new Token(new TextUnit("MachineId", base.TokenStream.
                        Peek().TextUnit.Line), TokenType.MachineDecl));
                }

                if (textUnit == null)
                {
                    textUnit = new TextUnit(base.TokenStream.Peek().TextUnit.Text,
                        line);
                }
                else
                {
                    textUnit = new TextUnit(textUnit.Text + base.TokenStream.Peek().TextUnit.Text,
                        line);
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
            {
                textUnit = new TextUnit(textUnit.Text + base.TokenStream.Peek().TextUnit.Text,
                    line);

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                int counter = 1;
                while (!base.TokenStream.Done)
                {
                    if (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
                    {
                        counter++;
                    }
                    else if (base.TokenStream.Peek().Type == TokenType.RightAngleBracket)
                    {
                        counter--;
                    }

                    if (counter == 0 ||
                        (base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                        base.TokenStream.Peek().Type != TokenType.Void &&
                        base.TokenStream.Peek().Type != TokenType.Object &&
                        base.TokenStream.Peek().Type != TokenType.String &&
                        base.TokenStream.Peek().Type != TokenType.Sbyte &&
                        base.TokenStream.Peek().Type != TokenType.Byte &&
                        base.TokenStream.Peek().Type != TokenType.Short &&
                        base.TokenStream.Peek().Type != TokenType.Ushort &&
                        base.TokenStream.Peek().Type != TokenType.Int &&
                        base.TokenStream.Peek().Type != TokenType.Uint &&
                        base.TokenStream.Peek().Type != TokenType.Long &&
                        base.TokenStream.Peek().Type != TokenType.Ulong &&
                        base.TokenStream.Peek().Type != TokenType.Char &&
                        base.TokenStream.Peek().Type != TokenType.Bool &&
                        base.TokenStream.Peek().Type != TokenType.Decimal &&
                        base.TokenStream.Peek().Type != TokenType.Float &&
                        base.TokenStream.Peek().Type != TokenType.Double &&
                        base.TokenStream.Peek().Type != TokenType.Identifier &&
                        base.TokenStream.Peek().Type != TokenType.Dot &&
                        base.TokenStream.Peek().Type != TokenType.Comma &&
                        base.TokenStream.Peek().Type != TokenType.LeftSquareBracket &&
                        base.TokenStream.Peek().Type != TokenType.RightSquareBracket &&
                        base.TokenStream.Peek().Type != TokenType.LeftAngleBracket &&
                        base.TokenStream.Peek().Type != TokenType.RightAngleBracket))
                    {
                        break;
                    }

                    if (base.TokenStream.Peek().Type == TokenType.MachineDecl)
                    {
                        base.TokenStream.Swap(new Token(new TextUnit("MachineId", base.TokenStream.
                            Peek().TextUnit.Line), TokenType.MachineDecl));
                    }

                    textUnit = new TextUnit(textUnit.Text + base.TokenStream.Peek().TextUnit.Text,
                        line);

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                }

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.RightAngleBracket)
                {
                    throw new ParsingException("Expected \">\".",
                        new List<TokenType>
                    {
                        TokenType.RightAngleBracket
                    });
                }

                textUnit = new TextUnit(textUnit.Text + base.TokenStream.Peek().TextUnit.Text,
                    line);

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }
        }
    }
}
