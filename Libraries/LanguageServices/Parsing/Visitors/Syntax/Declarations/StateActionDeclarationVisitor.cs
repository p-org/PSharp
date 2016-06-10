//-----------------------------------------------------------------------
// <copyright file="StateActionDeclarationVisitor.cs">
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
    /// The P# action declaration parsing visitor.
    /// </summary>
    internal sealed class StateActionDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal StateActionDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        internal void Visit(StateDeclaration parentNode)
        {
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

            var eventIdentifiers = new List<Token>();

            bool expectsComma = false;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.DoAction &&
                base.TokenStream.Peek().Type != TokenType.GotoState &&
                base.TokenStream.Peek().Type != TokenType.PushState)
            {
                if ((!expectsComma &&
                    base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                    base.TokenStream.Peek().Type != TokenType.DefaultEvent) ||
                    (expectsComma && base.TokenStream.Peek().Type != TokenType.Comma))
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.Identifier ||
                    base.TokenStream.Peek().Type == TokenType.HaltEvent ||
                    base.TokenStream.Peek().Type == TokenType.DefaultEvent)
                {
                    if (base.TokenStream.Peek().Type == TokenType.Identifier)
                    {
                        base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                            TokenType.EventIdentifier));
                    }

                    eventIdentifiers.Add(base.TokenStream.Peek());

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = false;
                }
            }

            if (!expectsComma)
            {
                throw new ParsingException("Expected event identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent,
                    TokenType.DefaultEvent
                });
            }

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.DoAction &&
                base.TokenStream.Peek().Type != TokenType.GotoState &&
                base.TokenStream.Peek().Type != TokenType.PushState))
            {
                throw new ParsingException("Expected \"do\", \"goto\" or \"push\".",
                    new List<TokenType>
                {
                    TokenType.DoAction,
                    TokenType.GotoState,
                    TokenType.PushState
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.DoAction)
            {
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
                {
                    throw new ParsingException("Expected action identifier.",
                        new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
                {
                    var blockNode = new BlockSyntax(base.TokenStream.Program,
                        parentNode.Machine, null);
                    new BlockSyntaxVisitor(base.TokenStream).Visit(blockNode);

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    foreach (var eventIdentifier in eventIdentifiers)
                    {
                        if (!parentNode.AddActionBinding(eventIdentifier, blockNode))
                        {
                            throw new ParsingException("Unexpected action handler.",
                                new List<TokenType>());
                        }
                    }
                }
                else
                {
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                    TokenType.ActionIdentifier));

                    var actionIdentifier = base.TokenStream.Peek();
                    foreach (var eventIdentifier in eventIdentifiers)
                    {
                        if (!parentNode.AddActionBinding(eventIdentifier, actionIdentifier))
                        {
                            throw new ParsingException("Unexpected action handler.",
                                new List<TokenType>());
                        }
                    }

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
            }
            else if (base.TokenStream.Peek().Type == TokenType.GotoState)
            {
                var stateIdentifiers = ConsumeState();

                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.WithExit &&
                    base.TokenStream.Peek().Type != TokenType.Semicolon))
                {
                    throw new ParsingException("Expected \";\".",
                        new List<TokenType>
                    {
                        TokenType.Semicolon
                    });
                }

                if (base.TokenStream.Peek().Type == TokenType.WithExit)
                {
                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    if (base.TokenStream.Done ||
                        base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
                    {
                        throw new ParsingException("Expected \"{\".",
                            new List<TokenType>
                        {
                            TokenType.LeftCurlyBracket
                        });
                    }

                    var blockNode = new BlockSyntax(base.TokenStream.Program, parentNode.Machine, null);
                    new BlockSyntaxVisitor(base.TokenStream).Visit(blockNode);

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    foreach (var eventIdentifier in eventIdentifiers)
                    {
                        if (!parentNode.AddGotoStateTransition(eventIdentifier, stateIdentifiers, blockNode))
                        {
                            throw new ParsingException("Unexpected goto state transition.",
                                new List<TokenType>());
                        }
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
                }
                else
                {
                    foreach (var eventIdentifier in eventIdentifiers)
                    {
                        if (!parentNode.AddGotoStateTransition(eventIdentifier, stateIdentifiers))
                        {
                            throw new ParsingException("Unexpected goto state transition.",
                                new List<TokenType>());
                        }
                    }
                }
            }
            else if (base.TokenStream.Peek().Type == TokenType.PushState)
            {
                if (parentNode.Machine.IsMonitor)
                {
                    throw new ParsingException("Monitors cannot \"push\".",
                        new List<TokenType>());
                }

                var stateIdentifiers = ConsumeState();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.Semicolon)
                {
                    throw new ParsingException("Expected \";\".",
                        new List<TokenType>
                    {
                        TokenType.Semicolon
                    });
                }

                foreach (var eventIdentifier in eventIdentifiers)
                {
                    if (!parentNode.AddPushStateTransition(eventIdentifier, stateIdentifiers))
                    {
                        throw new ParsingException("Unexpected push state transition.",
                            new List<TokenType>());
                    }
                }
            }
        }

        /// <summary>
        /// Consume state-identifier(.state-identifier)*
        /// Stops at [With|SemiColon]
        /// </summary>
        private List<Token> ConsumeState()
        {
            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected state identifier.",
                    new List<TokenType>
                {
                        TokenType.Identifier
                });
            }

            var stateIdentifiers = new List<Token>();
            bool expectsDot = false;

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.WithExit &&
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                if ((!expectsDot && base.TokenStream.Peek().Type != TokenType.Identifier) ||
                    (expectsDot && base.TokenStream.Peek().Type != TokenType.Dot))
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                        TokenType.StateIdentifier));
                    stateIdentifiers.Add(base.TokenStream.Peek());

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    expectsDot = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Dot)
                {
                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    expectsDot = false;
                }
            }

            if (!expectsDot)
            {
                throw new ParsingException("Expected state identifier.",
                    new List<TokenType>
                {
                        TokenType.Identifier
                });
            }

            return stateIdentifiers;
        }
    }
}
