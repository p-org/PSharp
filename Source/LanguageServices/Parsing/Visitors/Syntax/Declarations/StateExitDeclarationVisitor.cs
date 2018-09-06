// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# state exit declaration parsing visitor.
    /// </summary>
    internal sealed class StateExitDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal StateExitDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="isAsync">True if the exit method is async</param>
        internal void Visit(StateDeclaration parentNode, bool isAsync = false)
        {
            if (parentNode.ExitDeclaration != null)
            {
                throw new ParsingException("Duplicate exit declaration.",
                    new List<TokenType>());
            }

            var node = new ExitDeclaration(base.TokenStream.Program, parentNode, isAsync);
            node.ExitKeyword = base.TokenStream.Peek();

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

            parentNode.ExitDeclaration = node;
        }
    }
}
