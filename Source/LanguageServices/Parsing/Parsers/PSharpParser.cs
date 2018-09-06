﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

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
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">ParsingOptions</param>
        public PSharpParser(ParsingOptions options)
            : base(options)
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="options">ParsingOptions</param>
        internal PSharpParser(PSharpProject project, SyntaxTree tree, ParsingOptions options)
            : base(project, tree, options)
        {

        }

        #endregion

        #region protected API

        /// <summary>
        /// Returns a new P# program.
        /// </summary>
        /// <returns>P# program</returns>
        protected override IPSharpProgram CreateNewProgram()
        {
            var program = new PSharpProgram(base.Project, base.SyntaxTree);
            base.TokenStream.Program = program;
            return program;
        }

        /// <summary>
        /// Parses the tokens.
        /// </summary>
        protected override void ParseTokens()
        {
            while (!base.TokenStream.Done)
            {
                var token = base.TokenStream.Peek();
                switch (token.Type)
                {
                    case TokenType.WhiteSpace:
                    case TokenType.Comment:
                    case TokenType.NewLine:
                        base.TokenStream.Index++;
                        break;

                    case TokenType.CommentLine:
                    case TokenType.Region:
                    case TokenType.CommentStart:
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        break;

                    case TokenType.Using:
                        this.VisitUsingDeclaration();
                        base.TokenStream.Index++;
                        break;

                    case TokenType.NamespaceDecl:
                        this.VisitNamespaceDeclaration();
                        base.TokenStream.Index++;
                        break;

                    case TokenType.Internal:
                    case TokenType.Public:
                    case TokenType.Partial:
                    case TokenType.Abstract:
                    case TokenType.Virtual:
                    case TokenType.MachineDecl:
                        throw new ParsingException("Must be declared inside a namespace.",
                            new List<TokenType>());

                    case TokenType.ExternDecl:
                    case TokenType.EventDecl:
                        throw new ParsingException("Must be declared inside a namespace or machine.",
                            new List<TokenType>());

                    default:
                        throw new ParsingException("Unexpected token.",
                            new List<TokenType>());
                }
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Visits a using declaration.
        /// </summary>
        private void VisitUsingDeclaration()
        {
            var node = new UsingDeclaration(base.TokenStream.Program);
            node.UsingKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                if (base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.Dot &&
                    base.TokenStream.Peek().Type != TokenType.NewLine)
                {
                    throw new ParsingException("Expected identifier.",
                        new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.Dot
                    });
                }
                else
                {
                    node.IdentifierTokens.Add(base.TokenStream.Peek());
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                throw new ParsingException("Expected \";\".",
                    new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.TokenStream.Peek();

            (this.Program as PSharpProgram).UsingDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a namespace declaration.
        /// </summary>
        private void VisitNamespaceDeclaration()
        {
            var node = new NamespaceDeclaration(base.TokenStream.Program)
            {
                NamespaceKeyword = base.TokenStream.Peek()
            };

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected namespace identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                if (base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.Dot &&
                    base.TokenStream.Peek().Type != TokenType.NewLine)
                {
                    throw new ParsingException("Expected namespace identifier.",
                        new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.Dot
                    });
                }
                else
                {
                    node.IdentifierTokens.Add(base.TokenStream.Peek());
                }

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

            node.LeftCurlyBracketToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextIntraNamespaceDeclaration(node);

            (this.Program as PSharpProgram).NamespaceDeclarations.Add(node);
        }

        /// <summary>
        /// Visits the next intra-namespace declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextIntraNamespaceDeclaration(NamespaceDeclaration node)
        {
            if (base.TokenStream.Done)
            {
                throw new ParsingException("Expected \"}\".",
                    new List<TokenType>
                {
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
                    TokenType.RightCurlyBracket
                });
            }

            bool fixpoint = false;
            var token = base.TokenStream.Peek();
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.Comment:
                case TokenType.NewLine:
                    base.TokenStream.Index++;
                    break;

                case TokenType.CommentLine:
                case TokenType.Region:
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.ExternDecl:
                    new EventDeclarationVisitor(base.TokenStream).VisitExternDeclaration(node, null);
                    base.TokenStream.Index++;
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
                    base.TokenStream.Index++;
                    break;

                case TokenType.LeftSquareBracket:
                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    new AttributeListVisitor(base.TokenStream).Visit();
                    base.TokenStream.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    node.RightCurlyBracketToken = base.TokenStream.Peek();
                    fixpoint = true;
                    base.TokenStream.Index++;
                    break;

                default:
                    throw new ParsingException("Unexpected token.",
                        new List<TokenType>());
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

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.EventDecl &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.Monitor)
            {
                new ModifierVisitor(base.TokenStream).Visit(modSet);

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.EventDecl &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.Monitor))
            {
                throw new ParsingException("Expected event, machine or monitor declaration.",
                    new List<TokenType>
                {
                    TokenType.EventDecl,
                    TokenType.MachineDecl,
                    TokenType.Monitor
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.EventDecl)
            {
                new EventDeclarationVisitor(base.TokenStream).Visit(parentNode, null, modSet);
            }
            else if (base.TokenStream.Peek().Type == TokenType.MachineDecl)
            {
                new MachineDeclarationVisitor(base.TokenStream).Visit(null, parentNode, false, modSet);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Monitor)
            {
                new MachineDeclarationVisitor(base.TokenStream).Visit(null, parentNode, true, modSet);
            }
        }

        #endregion
    }
}
