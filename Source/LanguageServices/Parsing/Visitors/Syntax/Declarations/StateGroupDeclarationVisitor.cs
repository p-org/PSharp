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
    /// The P# state group declaration parsing visitor.
    /// </summary>
    internal sealed class StateGroupDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal StateGroupDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }
        
        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Containing machine</param>
        /// <param name="groupNode">Containing group</param>
        /// <param name="modSet">Modifier set</param>
        internal void Visit(MachineDeclaration parentNode, StateGroupDeclaration groupNode, ModifierSet modSet)
        {
            this.CheckStateGroupModifierSet(modSet);

            var node = new StateGroupDeclaration(base.TokenStream.Program, parentNode, groupNode);
            node.AccessModifier = modSet.AccessModifier;
            node.StateGroupKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected state group identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.StateGroupIdentifier));

            node.Identifier = base.TokenStream.Peek();

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

            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.StateGroupLeftCurlyBracket));

            node.LeftCurlyBracketToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextPSharpIntraGroupDeclaration(node);

            if (groupNode == null)
            {
                parentNode.StateGroupDeclarations.Add(node);
            }
            else
            {
                groupNode.StateGroupDeclarations.Add(node);
            }

            var stateDeclarations = node.GetAllStateDeclarations();
            if (stateDeclarations.Count == 0)
            {
                throw new ParsingException("A state group must declare at least one state.",
                    new List<TokenType>());
            }
        }

        /// <summary>
        /// Visits the next intra-group declaration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextPSharpIntraGroupDeclaration(StateGroupDeclaration node)
        {
            bool fixpoint = false;
            while (!fixpoint)
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
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        break;

                    case TokenType.CommentStart:
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        break;

                    case TokenType.StartState:
                    case TokenType.HotState:
                    case TokenType.ColdState:
                    case TokenType.StateDecl:
                    case TokenType.StateGroupDecl:
                    case TokenType.Private:
                    case TokenType.Protected:
                    case TokenType.Internal:
                    case TokenType.Public:
                        this.VisitGroupLevelDeclaration(node);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.LeftSquareBracket:
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        new AttributeListVisitor(base.TokenStream).Visit();
                        base.TokenStream.Index++;
                        break;

                    case TokenType.RightCurlyBracket:
                        base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                            TokenType.MachineRightCurlyBracket));
                        node.RightCurlyBracketToken = base.TokenStream.Peek();
                        fixpoint = true;
                        break;

                    default:
                        throw new ParsingException("Unexpected token '" + base.TokenStream.Peek().TextUnit.Text + "'.",
                            new List<TokenType>());
                }

                if (base.TokenStream.Done)
                {
                    throw new ParsingException("Expected \"}\".",
                        new List<TokenType>
                    {
                            TokenType.Private,
                            TokenType.Protected,
                            TokenType.StartState,
                            TokenType.HotState,
                            TokenType.ColdState,
                            TokenType.StateDecl,
                            TokenType.StateGroupDecl,
                            TokenType.LeftSquareBracket,
                            TokenType.RightCurlyBracket
                    });
                }
            }
        }

        /// <summary>
        /// Visits a group level declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitGroupLevelDeclaration(StateGroupDeclaration parentNode)
        {
            ModifierSet modSet = ModifierSet.CreateDefault();

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.StateDecl &&
                base.TokenStream.Peek().Type != TokenType.StateGroupDecl &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl)
            {
                new ModifierVisitor(base.TokenStream).Visit(modSet);

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.StateDecl &&
                base.TokenStream.Peek().Type != TokenType.StateGroupDecl))
            {
                throw new ParsingException("Expected state or group declaration.",
                    new List<TokenType>
                {
                    TokenType.StateDecl,
                    TokenType.StateGroupDecl
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.StateDecl)
            {
                new StateDeclarationVisitor(base.TokenStream).Visit(parentNode.Machine, parentNode, modSet);
            }
            else if (base.TokenStream.Peek().Type == TokenType.StateGroupDecl)
            {
                new StateGroupDeclarationVisitor(base.TokenStream).Visit(parentNode.Machine, parentNode, modSet);
            }
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        /// <param name="modSet">ModifierSet</param>
        private void CheckStateGroupModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier == AccessModifier.Public)
            {
                throw new ParsingException("A machine state group cannot be public.",
                    new List<TokenType>());
            }
            else if (modSet.AccessModifier == AccessModifier.Internal)
            {
                throw new ParsingException("A machine state group cannot be internal.",
                    new List<TokenType>());
            }

            if (modSet.InheritanceModifier == InheritanceModifier.Abstract)
            {
                throw new ParsingException("A machine state group cannot be abstract.",
                    new List<TokenType>());
            }
            else if (modSet.InheritanceModifier == InheritanceModifier.Virtual)
            {
                throw new ParsingException("A machine state group cannot be virtual.",
                    new List<TokenType>());
            }
            else if (modSet.InheritanceModifier == InheritanceModifier.Override)
            {
                throw new ParsingException("A machine state group cannot be overriden.",
                    new List<TokenType>());
            }

            if (modSet.IsAsync)
            {
                throw new ParsingException("A machine state group cannot be async.",
                    new List<TokenType>());
            }

            if (modSet.IsPartial)
            {
                throw new ParsingException("A machine state group cannot be partial.",
                    new List<TokenType>());
            }

            if (modSet.IsStart)
            {
                throw new ParsingException("A machine state group cannot be marked start.",
                    new List<TokenType>());
            }
            else if (modSet.IsHot)
            {
                throw new ParsingException("A machine state group cannot be hot.",
                    new List<TokenType>());
            }
            else if (modSet.IsCold)
            {
                throw new ParsingException("A machine state group cannot be cold.",
                    new List<TokenType>());
            }
        }
    }
}
