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
    /// The P# machine method declaration parsing visitor.
    /// </summary>
    internal sealed class MachineMethodDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal MachineMethodDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="typeIdentifier">Type identifier</param>
        /// <param name="identifier">Identifier</param>
        /// <param name="modSet">Modifier set</param>
        internal void Visit(MachineDeclaration parentNode, Token typeIdentifier, Token identifier, ModifierSet modSet)
        {
            this.CheckMachineMethodModifierSet(modSet);

            var node = new MethodDeclaration(base.TokenStream.Program, parentNode);
            node.AccessModifier = modSet.AccessModifier;
            node.InheritanceModifier = modSet.InheritanceModifier;
            node.TypeIdentifier = typeIdentifier;
            node.Identifier = identifier;
            node.IsAsync = modSet.IsAsync;
            node.IsPartial = modSet.IsPartial;

            node.LeftParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit));

                node.Parameters.Add(base.TokenStream.Peek());

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            node.RightParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                throw new ParsingException("Expected \"{\" or \";\".",
                    new List<TokenType>
                {
                    TokenType.LeftCurlyBracket,
                    TokenType.Semicolon
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
            {
                var blockNode = new BlockSyntax(base.TokenStream.Program, parentNode, null);
                new BlockSyntaxVisitor(base.TokenStream).Visit(blockNode);
                node.StatementBlock = blockNode;
            }
            else if (base.TokenStream.Peek().Type == TokenType.Semicolon)
            {
                node.SemicolonToken = base.TokenStream.Peek();
            }

            parentNode.MethodDeclarations.Add(node);
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        /// <param name="modSet">ModifierSet</param>
        private void CheckMachineMethodModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier == AccessModifier.Public)
            {
                throw new ParsingException("A machine method cannot be public.",
                    new List<TokenType>());
            }
            else if (modSet.AccessModifier == AccessModifier.Internal)
            {
                throw new ParsingException("A machine method cannot be internal.",
                    new List<TokenType>());
            }
        }
    }
}
