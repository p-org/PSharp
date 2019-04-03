﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# defer events declaration parsing visitor.
    /// </summary>
    internal sealed class DeferEventsDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeferEventsDeclarationVisitor"/> class.
        /// </summary>
        internal DeferEventsDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {
        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        internal void Visit(StateDeclaration parentNode)
        {
            if (parentNode.Machine.IsMonitor)
            {
                throw new ParsingException("Monitors cannot \"defer\".");
            }

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            var nameVisitor = new NameVisitor(this.TokenStream);

            // Consumes multiple generic event names.
            var eventIdentifiers =
                nameVisitor.ConsumeMultipleNames(TokenType.EventIdentifier, tt => nameVisitor.ConsumeGenericEventName(tt));

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

            foreach (var kvp in resolvedEventIdentifiers)
            {
                if (!parentNode.AddDeferredEvent(kvp.Key, kvp.Value))
                {
                    throw new ParsingException("Unexpected defer declaration.");
                }
            }

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
                this.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                throw new ParsingException("Expected \";\".", TokenType.Semicolon);
            }
        }
    }
}
