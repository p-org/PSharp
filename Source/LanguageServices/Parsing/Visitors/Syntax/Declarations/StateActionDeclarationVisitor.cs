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
        /// Initializes a new instance of the <see cref="StateActionDeclarationVisitor"/> class.
        /// </summary>
        internal StateActionDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {
        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        internal void Visit(StateDeclaration parentNode)
        {
            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (this.TokenStream.Done ||
                (this.TokenStream.Peek().Type != TokenType.Identifier &&
                this.TokenStream.Peek().Type != TokenType.MulOp &&
                this.TokenStream.Peek().Type != TokenType.HaltEvent &&
                this.TokenStream.Peek().Type != TokenType.DefaultEvent))
            {
                throw new ParsingException(
                    "Expected event identifier.",
                    TokenType.Identifier,
                    TokenType.HaltEvent,
                    TokenType.DefaultEvent);
            }

            var nameVisitor = new NameVisitor(this.TokenStream);

            // Consumes multiple generic event names.
            var eventIdentifiers = nameVisitor.ConsumeMultipleNames(TokenType.EventIdentifier, tt => nameVisitor.ConsumeGenericEventName(tt));

            if (!this.TokenStream.Done &&
                this.TokenStream.Peek().Type == TokenType.Identifier)
            {
                throw new ParsingException("Expected \",\".", TokenType.Comma);
            }

            if (!this.TokenStream.Done &&
                (this.TokenStream.Peek().Type == TokenType.LeftAngleBracket ||
                 this.TokenStream.Peek().Type == TokenType.RightAngleBracket))
            {
                throw new ParsingException("Invalid generic expression.");
            }

            if (this.TokenStream.Done ||
                (this.TokenStream.Peek().Type != TokenType.DoAction &&
                this.TokenStream.Peek().Type != TokenType.GotoState &&
                this.TokenStream.Peek().Type != TokenType.PushState))
            {
                throw new ParsingException(
                    "Expected \"do\", \"goto\" or \"push\".",
                    TokenType.DoAction,
                    TokenType.GotoState,
                    TokenType.PushState);
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

                    TextUnit textUnit = new TextUnit(identifierBuilder.ToString(), eventIdentifier[0].TextUnit.Line);
                    resolvedEventIdentifiers.Add(new Token(textUnit, TokenType.EventIdentifier), eventIdentifier);
                }
            }

            if (this.TokenStream.Peek().Type == TokenType.DoAction)
            {
                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                bool isAsync = false;
                if (this.TokenStream.Peek().Type == TokenType.Async)
                {
                    isAsync = true;
                    this.TokenStream.Index++;
                    this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                }

                if (this.TokenStream.Peek().Type == TokenType.Await)
                {
                    throw new ParsingException("'await' should not be used on actions.");
                }

                if (this.TokenStream.Done ||
                    (this.TokenStream.Peek().Type != TokenType.Identifier &&
                    this.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
                {
                    var message = isAsync
                        ? "Expected action identifier or opening curly bracket."
                        : "Expected async keyword, action identifier, or opening curly bracket.";
                    if (!isAsync)
                    {
                        throw new ParsingException(message, TokenType.Async, TokenType.Identifier, TokenType.LeftCurlyBracket);
                    }
                    else
                    {
                        throw new ParsingException(message, TokenType.Identifier, TokenType.LeftCurlyBracket);
                    }
                }

                if (this.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
                {
                    var blockNode = new BlockSyntax(this.TokenStream.Program, parentNode.Machine, null);
                    new BlockSyntaxVisitor(this.TokenStream).Visit(blockNode);

                    foreach (var kvp in resolvedEventIdentifiers)
                    {
                        if (!parentNode.AddActionBinding(kvp.Key, kvp.Value, new AnonymousActionHandler(blockNode, isAsync)))
                        {
                            throw new ParsingException("Unexpected action handler.");
                        }
                    }
                }
                else
                {
                    if (isAsync)
                    {
                        throw new ParsingException("'async' should only be used on anonymous actions.");
                    }

                    this.TokenStream.Swap(new Token(this.TokenStream.Peek().TextUnit, TokenType.ActionIdentifier));

                    var actionIdentifier = this.TokenStream.Peek();
                    foreach (var kvp in resolvedEventIdentifiers)
                    {
                        if (!parentNode.AddActionBinding(kvp.Key, kvp.Value, actionIdentifier))
                        {
                            throw new ParsingException("Unexpected action handler.");
                        }
                    }

                    this.TokenStream.Index++;
                    this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    if (this.TokenStream.Done ||
                        this.TokenStream.Peek().Type != TokenType.Semicolon)
                    {
                        throw new ParsingException("Expected \";\".", TokenType.Semicolon);
                    }
                }
            }
            else if (this.TokenStream.Peek().Type == TokenType.GotoState)
            {
                var stateIdentifiers = this.ConsumeState();

                if (this.TokenStream.Done ||
                    (this.TokenStream.Peek().Type != TokenType.WithExit &&
                    this.TokenStream.Peek().Type != TokenType.Semicolon))
                {
                    throw new ParsingException("Expected \";\".", TokenType.Semicolon);
                }

                if (this.TokenStream.Peek().Type == TokenType.WithExit)
                {
                    this.TokenStream.Index++;
                    this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    bool isAsync = false;
                    if (this.TokenStream.Peek().Type == TokenType.Async)
                    {
                        isAsync = true;
                        this.TokenStream.Index++;
                        this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }

                    if (this.TokenStream.Done ||
                        this.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
                    {
                        throw new ParsingException("Expected \"{\".", TokenType.LeftCurlyBracket);
                    }

                    var blockNode = new BlockSyntax(this.TokenStream.Program, parentNode.Machine, null);
                    new BlockSyntaxVisitor(this.TokenStream).Visit(blockNode);

                    foreach (var kvp in resolvedEventIdentifiers)
                    {
                        if (!parentNode.AddGotoStateTransition(kvp.Key, kvp.Value, stateIdentifiers,
                            new AnonymousActionHandler(blockNode, isAsync)))
                        {
                            throw new ParsingException("Unexpected goto state transition.");
                        }
                    }
                }
                else
                {
                    foreach (var kvp in resolvedEventIdentifiers)
                    {
                        if (!parentNode.AddGotoStateTransition(kvp.Key, kvp.Value, stateIdentifiers))
                        {
                            throw new ParsingException("Unexpected goto state transition.");
                        }
                    }
                }
            }
            else if (this.TokenStream.Peek().Type == TokenType.PushState)
            {
                if (parentNode.Machine.IsMonitor)
                {
                    throw new ParsingException("Monitors cannot \"push\".");
                }

                var stateIdentifiers = this.ConsumeState();

                if (this.TokenStream.Done ||
                    this.TokenStream.Peek().Type != TokenType.Semicolon)
                {
                    throw new ParsingException("Expected \";\".", TokenType.Semicolon);
                }

                foreach (var kvp in resolvedEventIdentifiers)
                {
                    if (!parentNode.AddPushStateTransition(kvp.Key, kvp.Value, stateIdentifiers))
                    {
                        throw new ParsingException("Unexpected push state transition.");
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
            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected state identifier.", TokenType.Identifier);
            }

            var stateIdentifiers = new List<Token>();
            bool expectsDot = false;

            while (!this.TokenStream.Done &&
                this.TokenStream.Peek().Type != TokenType.WithExit &&
                this.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                if ((!expectsDot && this.TokenStream.Peek().Type != TokenType.Identifier) ||
                    (expectsDot && this.TokenStream.Peek().Type != TokenType.Dot))
                {
                    break;
                }

                if (this.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    this.TokenStream.Swap(new Token(this.TokenStream.Peek().TextUnit, TokenType.StateIdentifier));
                    stateIdentifiers.Add(this.TokenStream.Peek());

                    this.TokenStream.Index++;
                    this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    expectsDot = true;
                }
                else if (this.TokenStream.Peek().Type == TokenType.Dot)
                {
                    this.TokenStream.Index++;
                    this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    expectsDot = false;
                }
            }

            if (!expectsDot)
            {
                throw new ParsingException("Expected state identifier.", TokenType.Identifier);
            }

            return stateIdentifiers;
        }
    }
}
