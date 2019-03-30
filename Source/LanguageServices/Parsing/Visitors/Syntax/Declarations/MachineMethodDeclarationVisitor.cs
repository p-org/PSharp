// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# machine method declaration parsing visitor.
    /// </summary>
    internal sealed class MachineMethodDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MachineMethodDeclarationVisitor"/> class.
        /// </summary>
        internal MachineMethodDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {
        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        internal void Visit(MachineDeclaration parentNode, Token typeIdentifier, Token identifier, ModifierSet modSet)
        {
            CheckMachineMethodModifierSet(modSet);

            var node = new MethodDeclaration(this.TokenStream.Program, parentNode);
            node.AccessModifier = modSet.AccessModifier;
            node.InheritanceModifier = modSet.InheritanceModifier;
            node.TypeIdentifier = typeIdentifier;
            node.Identifier = identifier;
            node.IsAsync = modSet.IsAsync;
            node.IsPartial = modSet.IsPartial;

            node.LeftParenthesisToken = this.TokenStream.Peek();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            while (!this.TokenStream.Done &&
                this.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                this.TokenStream.Swap(new Token(this.TokenStream.Peek().TextUnit));

                node.Parameters.Add(this.TokenStream.Peek());

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            node.RightParenthesisToken = this.TokenStream.Peek();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (this.TokenStream.Done ||
                (this.TokenStream.Peek().Type != TokenType.LeftCurlyBracket &&
                this.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                throw new ParsingException("Expected \"{\" or \";\".", TokenType.LeftCurlyBracket, TokenType.Semicolon);
            }

            if (this.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
            {
                var blockNode = new BlockSyntax(this.TokenStream.Program, parentNode, null);
                new BlockSyntaxVisitor(this.TokenStream).Visit(blockNode);
                node.StatementBlock = blockNode;
            }
            else if (this.TokenStream.Peek().Type == TokenType.Semicolon)
            {
                node.SemicolonToken = this.TokenStream.Peek();
            }

            parentNode.MethodDeclarations.Add(node);
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        private static void CheckMachineMethodModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier == AccessModifier.Public)
            {
                throw new ParsingException("A machine method cannot be public.");
            }
            else if (modSet.AccessModifier == AccessModifier.Internal)
            {
                throw new ParsingException("A machine method cannot be internal.");
            }
        }
    }
}
