//-----------------------------------------------------------------------
// <copyright file="TupleTypeIdentifierVisitor.cs">
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

using Microsoft.PSharp.LanguageServices.Parsing.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// The P# tuple type identifier parsing visitor.
    /// </summary>
    internal sealed class TupleTypeIdentifierVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        public TupleTypeIdentifierVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node
        /// </summary>
        /// <param name="type">Type</param>
        public void Visit(ref PTupleType type)
        {
            type.TypeTokens.Add(base.TokenStream.Peek());

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.Int &&
                base.TokenStream.Peek().Type != TokenType.Bool &&
                base.TokenStream.Peek().Type != TokenType.Seq &&
                base.TokenStream.Peek().Type != TokenType.Map &&
                base.TokenStream.Peek().Type != TokenType.Any &&
                base.TokenStream.Peek().Type != TokenType.EventDecl &&
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                base.TokenStream.Peek().Type != TokenType.Identifier))
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

            bool isNamed = false;
            bool expectsName = false;
            if (base.TokenStream.Peek().Type == TokenType.Identifier)
            {
                isNamed = true;
                expectsName = true;
            }

            bool expectsComma = false;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                if ((!expectsComma && !expectsName &&
                    base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Seq &&
                    base.TokenStream.Peek().Type != TokenType.Map &&
                    base.TokenStream.Peek().Type != TokenType.Any &&
                    base.TokenStream.Peek().Type != TokenType.EventDecl &&
                    base.TokenStream.Peek().Type != TokenType.LeftParenthesis) ||
                    (expectsName && base.TokenStream.Peek().Type != TokenType.Identifier) ||
                    (expectsComma && base.TokenStream.Peek().Type != TokenType.Comma))
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.MachineDecl ||
                    base.TokenStream.Peek().Type == TokenType.Int ||
                    base.TokenStream.Peek().Type == TokenType.Bool ||
                    base.TokenStream.Peek().Type == TokenType.Seq ||
                    base.TokenStream.Peek().Type == TokenType.Map ||
                    base.TokenStream.Peek().Type == TokenType.Any ||
                    base.TokenStream.Peek().Type == TokenType.EventDecl ||
                    base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
                {
                    PBaseType tupleType = null;
                    new TypeIdentifierVisitor(base.TokenStream).Visit(ref tupleType);
                    type.TupleTypes.Add(tupleType);

                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    type.NameTokens.Add(base.TokenStream.Peek());

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    if (base.TokenStream.Done ||
                        (base.TokenStream.Peek().Type != TokenType.Colon))
                    {
                        throw new ParsingException("Expected \":\".",
                            new List<TokenType>
                        {
                                TokenType.Colon
                        });
                    }

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    expectsName = false;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    type.TypeTokens.Add(base.TokenStream.Peek());

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = false;

                    if (isNamed)
                    {
                        expectsName = true;
                    }
                }
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                throw new ParsingException("Expected \")\".",
                    new List<TokenType>
                {
                    TokenType.RightParenthesis
                });
            }

            type.TypeTokens.Add(base.TokenStream.Peek());
        }
    }
}
