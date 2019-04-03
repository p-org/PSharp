// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;

using Microsoft.PSharp.LanguageServices.Parsing.Syntax;
using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// The P# parser.
    /// </summary>
    public sealed class PSharpParser : TokenParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PSharpParser"/> class.
        /// </summary>
        public PSharpParser(ParsingOptions options)
            : base(options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PSharpParser"/> class.
        /// </summary>
        internal PSharpParser(PSharpProject project, SyntaxTree tree, ParsingOptions options)
            : base(project, tree, options)
        {
        }

        /// <summary>
        /// Returns a new P# program.
        /// </summary>
        protected override IPSharpProgram CreateNewProgram()
        {
            var program = new PSharpProgram(this.Project, this.SyntaxTree);
            this.TokenStream.Program = program;
            return program;
        }

        /// <summary>
        /// Parses the tokens.
        /// </summary>
        protected override void ParseTokens()
        {
            while (!this.TokenStream.Done)
            {
                var token = this.TokenStream.Peek();
                switch (token.Type)
                {
                    case TokenType.WhiteSpace:
                    case TokenType.Comment:
                    case TokenType.NewLine:
                        this.TokenStream.Index++;
                        break;

                    case TokenType.CommentLine:
                    case TokenType.Region:
                    case TokenType.CommentStart:
                        this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        break;

                    case TokenType.Using:
                        this.VisitUsingDeclaration();
                        this.TokenStream.Index++;
                        break;

                    case TokenType.NamespaceDecl:
                        this.VisitNamespaceDeclaration();
                        this.TokenStream.Index++;
                        break;

                    case TokenType.Internal:
                    case TokenType.Public:
                    case TokenType.Partial:
                    case TokenType.Abstract:
                    case TokenType.Virtual:
                    case TokenType.MachineDecl:
                        throw new ParsingException("Must be declared inside a namespace.");

                    case TokenType.ExternDecl:
                    case TokenType.EventDecl:
                        throw new ParsingException("Must be declared inside a namespace or machine.");

                    default:
                        throw new ParsingException("Unexpected token.");
                }
            }
        }

        /// <summary>
        /// Visits a using declaration.
        /// </summary>
        private void VisitUsingDeclaration()
        {
            var node = new UsingDeclaration(this.TokenStream.Program);
            node.UsingKeyword = this.TokenStream.Peek();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected identifier.", TokenType.Identifier);
            }

            while (!this.TokenStream.Done &&
                this.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                if (this.TokenStream.Peek().Type != TokenType.Identifier &&
                    this.TokenStream.Peek().Type != TokenType.Dot &&
                    this.TokenStream.Peek().Type != TokenType.NewLine)
                {
                    throw new ParsingException("Expected identifier.", TokenType.Identifier, TokenType.Dot);
                }
                else
                {
                    node.IdentifierTokens.Add(this.TokenStream.Peek());
                }

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                throw new ParsingException("Expected \";\".", TokenType.Semicolon);
            }

            node.SemicolonToken = this.TokenStream.Peek();

            (this.Program as PSharpProgram).UsingDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a namespace declaration.
        /// </summary>
        private void VisitNamespaceDeclaration()
        {
            var node = new NamespaceDeclaration(this.TokenStream.Program)
            {
                NamespaceKeyword = this.TokenStream.Peek()
            };

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected namespace identifier.", TokenType.Identifier);
            }

            while (!this.TokenStream.Done &&
                this.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                if (this.TokenStream.Peek().Type != TokenType.Identifier &&
                    this.TokenStream.Peek().Type != TokenType.Dot &&
                    this.TokenStream.Peek().Type != TokenType.NewLine)
                {
                    throw new ParsingException("Expected namespace identifier.", TokenType.Identifier, TokenType.Dot);
                }
                else
                {
                    node.IdentifierTokens.Add(this.TokenStream.Peek());
                }

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                throw new ParsingException("Expected \"{\".", TokenType.LeftCurlyBracket);
            }

            node.LeftCurlyBracketToken = this.TokenStream.Peek();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextIntraNamespaceDeclaration(node);

            (this.Program as PSharpProgram).NamespaceDeclarations.Add(node);
        }

        /// <summary>
        /// Visits the next intra-namespace declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextIntraNamespaceDeclaration(NamespaceDeclaration node)
        {
            if (this.TokenStream.Done)
            {
                throw new ParsingException(
                    "Expected \"}\".",
                    TokenType.Internal,
                    TokenType.Public,
                    TokenType.Partial,
                    TokenType.Abstract,
                    TokenType.Virtual,
                    TokenType.ExternDecl,
                    TokenType.EventDecl,
                    TokenType.MachineDecl,
                    TokenType.Monitor,
                    TokenType.LeftSquareBracket,
                    TokenType.RightCurlyBracket);
            }

            bool fixpoint = false;
            var token = this.TokenStream.Peek();
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.Comment:
                case TokenType.NewLine:
                    this.TokenStream.Index++;
                    break;

                case TokenType.CommentLine:
                case TokenType.Region:
                    this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.ExternDecl:
                    new EventDeclarationVisitor(this.TokenStream).VisitExternDeclaration(node, null);
                    this.TokenStream.Index++;
                    break;

                case TokenType.EventDecl:
                case TokenType.MachineDecl:
                case TokenType.Monitor:
                case TokenType.Internal:
                case TokenType.Public:
                case TokenType.Private:
                case TokenType.Protected:
                case TokenType.Partial:
                case TokenType.Abstract:
                case TokenType.Virtual:
                    this.VisitEventOrMachineDeclaration(node);
                    this.TokenStream.Index++;
                    break;

                case TokenType.LeftSquareBracket:
                    this.TokenStream.Index++;
                    this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    new AttributeListVisitor(this.TokenStream).Visit();
                    this.TokenStream.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    node.RightCurlyBracketToken = this.TokenStream.Peek();
                    fixpoint = true;
                    this.TokenStream.Index++;
                    break;

                default:
                    throw new ParsingException("Unexpected token.");
            }

            if (!fixpoint)
            {
                this.VisitNextIntraNamespaceDeclaration(node);
            }
        }

        /// <summary>
        /// Visits an event or machine declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitEventOrMachineDeclaration(NamespaceDeclaration parentNode)
        {
            ModifierSet modSet = ModifierSet.CreateDefault();

            while (!this.TokenStream.Done &&
                this.TokenStream.Peek().Type != TokenType.EventDecl &&
                this.TokenStream.Peek().Type != TokenType.MachineDecl &&
                this.TokenStream.Peek().Type != TokenType.Monitor)
            {
                new ModifierVisitor(this.TokenStream).Visit(modSet);

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.TokenStream.Done ||
                (this.TokenStream.Peek().Type != TokenType.EventDecl &&
                this.TokenStream.Peek().Type != TokenType.MachineDecl &&
                this.TokenStream.Peek().Type != TokenType.Monitor))
            {
                throw new ParsingException(
                    "Expected event, machine or monitor declaration.",
                    TokenType.EventDecl,
                    TokenType.MachineDecl,
                    TokenType.Monitor);
            }

            if (this.TokenStream.Peek().Type == TokenType.EventDecl)
            {
                new EventDeclarationVisitor(this.TokenStream).Visit(parentNode, null, modSet);
            }
            else if (this.TokenStream.Peek().Type == TokenType.MachineDecl)
            {
                new MachineDeclarationVisitor(this.TokenStream).Visit(parentNode, false, modSet);
            }
            else if (this.TokenStream.Peek().Type == TokenType.Monitor)
            {
                new MachineDeclarationVisitor(this.TokenStream).Visit(parentNode, true, modSet);
            }
        }
    }
}
