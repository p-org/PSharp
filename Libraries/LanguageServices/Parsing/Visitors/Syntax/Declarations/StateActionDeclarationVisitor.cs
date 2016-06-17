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
using System.Linq;
using System.Text;

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

            var eventIdentifiers = new List<List<Token>>();
            eventIdentifiers.Add(new List<Token>());

            int genericCount = 0;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.DoAction &&
                base.TokenStream.Peek().Type != TokenType.GotoState &&
                base.TokenStream.Peek().Type != TokenType.PushState)
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
                    base.TokenStream.Peek().Type != TokenType.Double &&
                    base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                    base.TokenStream.Peek().Type != TokenType.DefaultEvent)
                {
                    break;
                }

                if (genericCount == 0 &&
                    base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    if (eventIdentifiers.Last().Count == 0)
                    {
                        throw new ParsingException("Expected event identifier.",
                            new List<TokenType>
                            {
                                TokenType.Identifier,
                                TokenType.HaltEvent,
                                TokenType.DefaultEvent
                            });
                    }

                    eventIdentifiers.Add(new List<Token>());
                }
                else if (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
                {
                    if (eventIdentifiers.Last().Count == 0)
                    {
                        throw new ParsingException("Expected event identifier.",
                            new List<TokenType>
                            {
                                TokenType.Identifier,
                                TokenType.HaltEvent,
                                TokenType.DefaultEvent
                            });
                    }

                    eventIdentifiers.Last().Add(base.TokenStream.Peek());
                    genericCount++;
                }
                else if (base.TokenStream.Peek().Type == TokenType.RightAngleBracket)
                {
                    if (eventIdentifiers.Last().Count == 0)
                    {
                        throw new ParsingException("Expected event identifier.",
                            new List<TokenType>
                            {
                                TokenType.Identifier,
                                TokenType.HaltEvent,
                                TokenType.DefaultEvent
                            });
                    }

                    if (genericCount == 0)
                    {
                        throw new ParsingException("Invalid generic expression.",
                            new List<TokenType>
                            {
                                TokenType.Identifier
                            });
                    }

                    eventIdentifiers.Last().Add(base.TokenStream.Peek());
                    genericCount--;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Dot ||
                    base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    if (eventIdentifiers.Last().Count == 0)
                    {
                        throw new ParsingException("Expected event identifier.",
                            new List<TokenType>
                            {
                                TokenType.Identifier,
                                TokenType.HaltEvent,
                                TokenType.DefaultEvent
                            });
                    }

                    eventIdentifiers.Last().Add(base.TokenStream.Peek());
                }
                else if (base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                        TokenType.EventIdentifier));
                    eventIdentifiers.Last().Add(base.TokenStream.Peek());
                }
                else if (base.TokenStream.Peek().Type == TokenType.Object ||
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
                    base.TokenStream.Peek().Type == TokenType.HaltEvent ||
                    base.TokenStream.Peek().Type == TokenType.DefaultEvent)
                {
                    eventIdentifiers.Last().Add(base.TokenStream.Peek());
                }

                if (eventIdentifiers.Last().Count > 0 &&
                    eventIdentifiers.Last().Any(token => token.Type == TokenType.HaltEvent ||
                    token.Type == TokenType.DefaultEvent))
                {
                    throw new ParsingException("Special events 'halt' and 'default' cannot be generic.",
                        new List<TokenType>
                        {
                            TokenType.Identifier
                        });
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (genericCount > 0)
            {
                throw new ParsingException("Invalid generic expression.",
                    new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent,
                    TokenType.DefaultEvent
                });
            }

            if (eventIdentifiers.Last().Count == 0)
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

            var resolvedEventIdentifiers = new Dictionary<Token, List<Token>>();
            foreach (var eventIdentifier in eventIdentifiers)
            {
                StringBuilder identifierBuilder = new StringBuilder();
                foreach (var token in eventIdentifier)
                {
                    identifierBuilder.Append(token.TextUnit.Text);
                }

                TextUnit textUnit = new TextUnit(identifierBuilder.ToString(),
                    eventIdentifier[0].TextUnit.Line);
                resolvedEventIdentifiers.Add(new Token(textUnit, TokenType.EventIdentifier),
                    eventIdentifier);
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

                    foreach (var kvp in resolvedEventIdentifiers)
                    {
                        if (!parentNode.AddActionBinding(kvp.Key, kvp.Value, blockNode))
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
                    foreach (var kvp in resolvedEventIdentifiers)
                    {
                        if (!parentNode.AddActionBinding(kvp.Key, kvp.Value, actionIdentifier))
                        {
                            throw new ParsingException("Unexpected action handler.",
                                new List<TokenType>());
                        }
                    }

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

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
            }
            else if (base.TokenStream.Peek().Type == TokenType.GotoState)
            {
                var stateIdentifiers = this.ConsumeState();

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

                    foreach (var kvp in resolvedEventIdentifiers)
                    {
                        if (!parentNode.AddGotoStateTransition(kvp.Key, kvp.Value, stateIdentifiers, blockNode))
                        {
                            throw new ParsingException("Unexpected goto state transition.",
                                new List<TokenType>());
                        }
                    }
                }
                else
                {
                    foreach (var kvp in resolvedEventIdentifiers)
                    {
                        if (!parentNode.AddGotoStateTransition(kvp.Key, kvp.Value, stateIdentifiers))
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

                var stateIdentifiers = this.ConsumeState();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.Semicolon)
                {
                    throw new ParsingException("Expected \";\".",
                        new List<TokenType>
                    {
                        TokenType.Semicolon
                    });
                }

                foreach (var kvp in resolvedEventIdentifiers)
                {
                    if (!parentNode.AddPushStateTransition(kvp.Key, kvp.Value, stateIdentifiers))
                    {
                        throw new ParsingException("Unexpected push state transition.",
                            new List<TokenType>());
                    }
                }
            }
        }

        /// <summary>
        /// Consumes state-identifier(.state-identifier)*.
        /// Stops at [With|SemiColon].
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
