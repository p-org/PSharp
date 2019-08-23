// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# machine member declaration parsing visitor.
    /// </summary>
    internal sealed class MachineMemberDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MachineMemberDeclarationVisitor"/> class.
        /// </summary>
        internal MachineMemberDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {
        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        internal void Visit(MachineDeclaration parentNode, ModifierSet modSet)
        {
            TextUnit textUnit = null;
            new TypeIdentifierVisitor(this.TokenStream).Visit(ref textUnit);
            var typeIdentifier = new Token(textUnit, TokenType.TypeIdentifier);

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected field or method identifier.", this.TokenStream.Peek(), TokenType.Identifier);
            }

            var identifierToken = this.TokenStream.Peek();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (this.TokenStream.Done ||
                    (this.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                    this.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                throw new ParsingException("Expected \"(\" or \";\".", this.TokenStream.Peek(), TokenType.LeftParenthesis, TokenType.Semicolon);
            }

            if (this.TokenStream.Peek().Type == TokenType.LeftParenthesis)
            {
                new MachineMethodDeclarationVisitor(this.TokenStream).Visit(parentNode, typeIdentifier, identifierToken, modSet);
            }
            else if (this.TokenStream.Peek().Type == TokenType.Semicolon)
            {
                this.CheckMachineFieldModifierSet(modSet);

                var node = new FieldDeclaration(this.TokenStream.Program, parentNode, modSet)
                {
                    TypeIdentifier = typeIdentifier,
                    Identifier = identifierToken,
                    SemicolonToken = this.TokenStream.Peek()
                };

                parentNode.FieldDeclarations.Add(node);
            }
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        private void CheckMachineFieldModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier == AccessModifier.Public)
            {
                throw new ParsingException("A machine field cannot be public.", this.TokenStream.Peek());
            }
            else if (modSet.AccessModifier == AccessModifier.Internal)
            {
                throw new ParsingException("A machine field cannot be internal.", this.TokenStream.Peek());
            }

            if (modSet.InheritanceModifier == InheritanceModifier.Abstract)
            {
                throw new ParsingException("A machine field cannot be abstract.", this.TokenStream.Peek());
            }
            else if (modSet.InheritanceModifier == InheritanceModifier.Virtual)
            {
                throw new ParsingException("A machine field cannot be virtual.", this.TokenStream.Peek());
            }
            else if (modSet.InheritanceModifier == InheritanceModifier.Override)
            {
                throw new ParsingException("A machine field cannot be overriden.", this.TokenStream.Peek());
            }

            if (modSet.IsAsync)
            {
                throw new ParsingException("A machine field cannot be async.", this.TokenStream.Peek());
            }

            if (modSet.IsPartial)
            {
                throw new ParsingException("A machine field cannot be partial.", this.TokenStream.Peek());
            }
        }
    }
}
