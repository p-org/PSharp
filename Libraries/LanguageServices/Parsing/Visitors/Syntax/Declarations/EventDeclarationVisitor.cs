//-----------------------------------------------------------------------
// <copyright file="EventDeclarationVisitor.cs">
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

using System;
using System.Collections.Generic;

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# event declaration parsing visitor.
    /// </summary>
    internal sealed class EventDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal EventDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="parentNode">Node</param>
        /// <param name="accMod">Access modifier</param>
        internal void Visit(IPSharpProgram program, NamespaceDeclaration parentNode, AccessModifier accMod)
        {
            var node = new EventDeclaration(base.TokenStream.Program);
            node.AccessModifier = accMod;
            node.EventKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                base.TokenStream.Peek().Type != TokenType.DefaultEvent))
            {
                throw new ParsingException("Expected event identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent,
                    TokenType.DefaultEvent
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Identifier)
            {
                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                    TokenType.EventIdentifier));
            }

            node.Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Assert &&
                base.TokenStream.Peek().Type != TokenType.Assume &&
                base.TokenStream.Peek().Type != TokenType.LeftAngleBracket &&
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                throw new ParsingException("Expected \"(\" or \";\".",
                    new List<TokenType>
                {
                    TokenType.Assert,
                    TokenType.Assume,
                    TokenType.LeftParenthesis,
                    TokenType.Semicolon
                });
            }
            
            int genericCount = 0;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.Assert &&
                base.TokenStream.Peek().Type != TokenType.Assume &&
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                if (base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.Dot &&
                    base.TokenStream.Peek().Type != TokenType.Comma &&
                    base.TokenStream.Peek().Type != TokenType.LeftAngleBracket &&
                    base.TokenStream.Peek().Type != TokenType.RightAngleBracket &&
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
                    base.TokenStream.Peek().Type != TokenType.Double)
                {
                    break;
                }

                if (genericCount == 0 &&
                    base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    throw new ParsingException("Expected generic type.",
                        new List<TokenType>
                        {
                            TokenType.Identifier
                        });
                }
                else if (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
                {
                    node.GenericType.Add(base.TokenStream.Peek());
                    genericCount++;
                }
                else if (base.TokenStream.Peek().Type == TokenType.RightAngleBracket)
                {
                    if (genericCount == 0)
                    {
                        throw new ParsingException("Invalid generic expression.",
                            new List<TokenType>
                            {
                                TokenType.Identifier
                            });
                    }

                    node.GenericType.Add(base.TokenStream.Peek());
                    genericCount--;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                        TokenType.EventIdentifier));
                    node.GenericType.Add(base.TokenStream.Peek());
                }
                else if (base.TokenStream.Peek().Type == TokenType.Dot ||
                    base.TokenStream.Peek().Type == TokenType.Comma ||
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
                    base.TokenStream.Peek().Type == TokenType.Double)
                {
                    node.GenericType.Add(base.TokenStream.Peek());
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (genericCount > 0)
            {
                throw new ParsingException("Invalid generic expression.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }
            
            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Assert &&
                base.TokenStream.Peek().Type != TokenType.Assume &&
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                throw new ParsingException("Expected \"(\" or \";\".",
                    new List<TokenType>
                {
                    TokenType.Assert,
                    TokenType.Assume,
                    TokenType.LeftParenthesis,
                    TokenType.Semicolon
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Assert ||
                base.TokenStream.Peek().Type == TokenType.Assume)
            {
                bool isAssert = true;
                if (base.TokenStream.Peek().Type == TokenType.Assert)
                {
                    node.AssertKeyword = base.TokenStream.Peek();
                }
                else
                {
                    node.AssumeKeyword = base.TokenStream.Peek();
                    isAssert = false;
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.Identifier)
                {
                    throw new ParsingException("Expected integer.",
                        new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                int value;
                if (!int.TryParse(base.TokenStream.Peek().TextUnit.Text, out value))
                {
                    throw new ParsingException("Expected integer.",
                        new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                if (isAssert)
                {
                    node.AssertValue = value;
                }
                else
                {
                    node.AssumeValue = value;
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                    base.TokenStream.Peek().Type != TokenType.Semicolon))
                {
                    throw new ParsingException("Expected \"(\" or \";\".",
                        new List<TokenType>
                    {
                        TokenType.LeftParenthesis,
                        TokenType.Semicolon
                    });
                }
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
            {
                node.LeftParenthesis = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                bool isType = false;
                while (!base.TokenStream.Done &&
                    base.TokenStream.Peek().Type != TokenType.RightParenthesis)
                {
                    if (isType &&
                        base.TokenStream.Peek().Type != TokenType.Colon &&
                        base.TokenStream.Peek().Type != TokenType.Comma)
                    {
                        TextUnit textUnit = null;
                        new TypeIdentifierVisitor(base.TokenStream).Visit(ref textUnit);
                        var typeIdentifier = new Token(textUnit, TokenType.TypeIdentifier);
                        node.PayloadTypes.Add(typeIdentifier);
                    }
                    else if (base.TokenStream.Peek().Type != TokenType.Colon &&
                        base.TokenStream.Peek().Type != TokenType.Comma)
                    {
                        node.PayloadIdentifiers.Add(base.TokenStream.Peek());

                        isType = true;
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }

                    if (base.TokenStream.Peek().Type == TokenType.Comma)
                    {
                        isType = false;
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }
                    else if (base.TokenStream.Peek().Type == TokenType.Colon)
                    {
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }
                }

                if (node.PayloadIdentifiers.Count != node.PayloadTypes.Count)
                {
                    throw new ParsingException("The payload type of event '" + node.Identifier.TextUnit.Text +
                        "' was not declared correctly.\n" +
                        "  You must declare both a type and a name identifier, for example:\n\n" +
                        "    event e (a:int, b:bool)\n",
                        new List<TokenType>
                    {
                            TokenType.RightParenthesis
                    });
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

                node.RightParenthesis = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                throw new ParsingException("Expected \";\".",
                    new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.TokenStream.Peek();
            parentNode.EventDeclarations.Add(node);
        }
    }
}
