﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# state entry declaration parsing visitor.
    /// </summary>
    internal sealed class StateEntryDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal StateEntryDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="isAsync">True if the entry method is async</param>
        internal void Visit(StateDeclaration parentNode, bool isAsync = false)
        {
            if (parentNode.EntryDeclaration != null)
            {
                throw new ParsingException("Duplicate entry declaration.",
                    new List<TokenType>());
            }

            var node = new EntryDeclaration(base.TokenStream.Program, parentNode, isAsync);
            node.EntryKeyword = base.TokenStream.Peek();

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

            var blockNode = new BlockSyntax(base.TokenStream.Program, parentNode.Machine, parentNode);
            new BlockSyntaxVisitor(base.TokenStream).Visit(blockNode);
            node.StatementBlock = blockNode;

            parentNode.EntryDeclaration = node;
        }
    }
}
