// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
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
                base.TokenStream.Peek().Type != TokenType.MulOp &&
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

            var nameVisitor = new NameVisitor(base.TokenStream);

            // Consumes multiple generic event names.
            var eventIdentifiers =
                nameVisitor.ConsumeMultipleNames(TokenType.EventIdentifier,
                tt => nameVisitor.ConsumeGenericEventName(tt));

            if (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type == TokenType.Identifier)
            {
                throw new ParsingException("Expected \",\".",
                    new List<TokenType>
                {
                    TokenType.Comma
                });
            }

            if (!base.TokenStream.Done &&
                (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket ||
                 base.TokenStream.Peek().Type == TokenType.RightAngleBracket))
            {
                throw new ParsingException("Invalid generic expression.",
                    new List<TokenType> { });
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
                if (eventIdentifier.Count == 1)
                {
                    // We don't want to collapse halt and default
                    // events to event identifiers.
                    resolvedEventIdentifiers.Add(eventIdentifier[0], eventIdentifier);
                }
                else
                {
                    var identifierBuilder = new StringBuilder();
                    foreach (var token in eventIdentifier)
                    {
                        identifierBuilder.Append(token.TextUnit.Text);
                    }

                    TextUnit textUnit = new TextUnit(identifierBuilder.ToString(),
                        eventIdentifier[0].TextUnit.Line);
                    resolvedEventIdentifiers.Add(new Token(textUnit, TokenType.EventIdentifier),
                        eventIdentifier);
                }
            }

            if (base.TokenStream.Peek().Type == TokenType.DoAction)
            {
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                bool isAsync = false;
                if (base.TokenStream.Peek().Type == TokenType.Async)
                {
                    isAsync = true;
                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                }

                if (base.TokenStream.Peek().Type == TokenType.Await)
                {
                    throw new ParsingException("'await' should not be used on actions.",
                        new List<TokenType>());
                }

                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
                {
                    var message = isAsync
                        ? "Expected action identifier or opening curly bracket."
                        : "Expected async keyword, action identifier, or opening curly bracket.";
                    var list = new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.LeftCurlyBracket
                    };

                    if (!isAsync)
                    {
                        list.Insert(0, TokenType.Async);
                    }

                    throw new ParsingException(message, list);
                }

                if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
                {
                    var blockNode = new BlockSyntax(base.TokenStream.Program,
                        parentNode.Machine, null);
                    new BlockSyntaxVisitor(base.TokenStream).Visit(blockNode);

                    foreach (var kvp in resolvedEventIdentifiers)
                    {
                        if (!parentNode.AddActionBinding(kvp.Key, kvp.Value, new AnonymousActionHandler(blockNode, isAsync)))
                        {
                            throw new ParsingException("Unexpected action handler.",
                                new List<TokenType>());
                        }
                    }
                }
                else
                {
                    if (isAsync)
                    {
                        throw new ParsingException("'async' should only be used on anonymous actions.",
                            new List<TokenType>());
                    }

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

                    bool isAsync = false;
                    if (base.TokenStream.Peek().Type == TokenType.Async)
                    {
                        isAsync = true;
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }

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
                        if (!parentNode.AddGotoStateTransition(kvp.Key, kvp.Value, stateIdentifiers,
                            new AnonymousActionHandler(blockNode, isAsync)))
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
