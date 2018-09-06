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
    /// The P# state declaration parsing visitor.
    /// </summary>
    internal sealed class StateDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal StateDeclarationVisitor(TokenStream tokenStream)
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
            this.CheckMachineStateModifierSet(modSet);

            var node = new StateDeclaration(base.TokenStream.Program, parentNode, groupNode, modSet);
            node.StateKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected state identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.StateIdentifier));

            node.Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            // Check for inherited state.
            if (!base.TokenStream.Done && base.TokenStream.Peek().Type == TokenType.Colon)
            {
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                if (base.TokenStream.Done || base.TokenStream.Peek().Type != TokenType.Identifier)
                {
                    throw new ParsingException("Expected state identifier.",
                        new List<TokenType> { TokenType.Identifier });
                }

                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                    TokenType.StateIdentifier));
                node.BaseStateToken = base.TokenStream.Peek();
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

            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.StateLeftCurlyBracket));

            node.LeftCurlyBracketToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Program is PSharpProgram)
            {
                this.VisitNextPSharpIntraStateDeclaration(node);
            }
            else
            {
                this.VisitNextPIntraStateDeclaration(node);
            }

            // Insert into (immediately) containing group or machine declaration.
            if (groupNode != null)
            {
                groupNode.StateDeclarations.Add(node);
            }
            else
            {
                parentNode.StateDeclarations.Add(node);
            }
        }

        /// <summary>
        /// Visits the next intra-state declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextPSharpIntraStateDeclaration(StateDeclaration node)
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

                    case TokenType.Async:
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        token = base.TokenStream.Peek();
                        switch (token.Type)
                        {
                            case TokenType.Entry:
                                new StateEntryDeclarationVisitor(base.TokenStream).Visit(node, isAsync:true);
                                base.TokenStream.Index++;
                                break;

                            case TokenType.Exit:
                                new StateExitDeclarationVisitor(base.TokenStream).Visit(node, isAsync:true);
                                base.TokenStream.Index++;
                                break;
                            default:
                                throw new ParsingException("'async' was used in an incorrect context.",
                                    new List<TokenType>());
                        }
                        break;

                    case TokenType.CommentLine:
                    case TokenType.Region:
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        break;

                    case TokenType.CommentStart:
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        break;

                    case TokenType.Entry:
                        new StateEntryDeclarationVisitor(base.TokenStream).Visit(node);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.Exit:
                        new StateExitDeclarationVisitor(base.TokenStream).Visit(node);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.OnAction:
                        new StateActionDeclarationVisitor(base.TokenStream).Visit(node);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.DeferEvent:
                        new DeferEventsDeclarationVisitor(base.TokenStream).Visit(node);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.IgnoreEvent:
                        new IgnoreEventsDeclarationVisitor(base.TokenStream).Visit(node);
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
                            TokenType.StateRightCurlyBracket));
                        node.RightCurlyBracketToken = base.TokenStream.Peek();
                        fixpoint = true;
                        break;

                    case TokenType.Private:
                    case TokenType.Protected:
                    case TokenType.Internal:
                    case TokenType.Public:
                        throw new ParsingException("State actions cannot have modifiers.",
                            new List<TokenType>());

                    case TokenType.Abstract:
                    case TokenType.Virtual:
                        throw new ParsingException("State actions cannot be abstract or virtual.",
                            new List<TokenType>());

                    default:
                        throw new ParsingException("Unexpected token.",
                            new List<TokenType>());
                }

                if (base.TokenStream.Done)
                {
                    throw new ParsingException("Expected \"}\".",
                        new List<TokenType>
                    {
                            TokenType.Entry,
                            TokenType.Exit,
                            TokenType.OnAction,
                            TokenType.DeferEvent,
                            TokenType.IgnoreEvent,
                            TokenType.LeftSquareBracket,
                            TokenType.RightCurlyBracket
                    });
                }
            }
        }

        /// <summary>
        /// Visits the next intra-state declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextPIntraStateDeclaration(StateDeclaration node)
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

                    case TokenType.Entry:
                        new StateEntryDeclarationVisitor(base.TokenStream).Visit(node);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.Exit:
                        new StateExitDeclarationVisitor(base.TokenStream).Visit(node);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.OnAction:
                        new StateActionDeclarationVisitor(base.TokenStream).Visit(node);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.DeferEvent:
                        new DeferEventsDeclarationVisitor(base.TokenStream).Visit(node);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.IgnoreEvent:
                        new IgnoreEventsDeclarationVisitor(base.TokenStream).Visit(node);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.RightCurlyBracket:
                        base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                            TokenType.StateRightCurlyBracket));
                        node.RightCurlyBracketToken = base.TokenStream.Peek();
                        fixpoint = true;
                        break;

                    default:
                        throw new ParsingException("Unexpected token.",
                            new List<TokenType>());
                }

                if (base.TokenStream.Done)
                {
                    throw new ParsingException("Expected \"}\".",
                        new List<TokenType>
                    {
                        TokenType.Entry,
                        TokenType.Exit,
                        TokenType.OnAction,
                        TokenType.DeferEvent,
                        TokenType.IgnoreEvent
                    });
                }
            }
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        /// <param name="modSet">ModifierSet</param>
        private void CheckMachineStateModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier == AccessModifier.Public)
            {
                throw new ParsingException("A machine state cannot be public.",
                    new List<TokenType>());
            }
            else if (modSet.AccessModifier == AccessModifier.Internal)
            {
                throw new ParsingException("A machine state cannot be internal.",
                    new List<TokenType>());
            }

            if (modSet.InheritanceModifier == InheritanceModifier.Virtual)
            {
                throw new ParsingException("A machine state cannot be virtual.",
                    new List<TokenType>());
            }
            else if (modSet.InheritanceModifier == InheritanceModifier.Override)
            {
                throw new ParsingException("A machine state cannot be overriden.",
                    new List<TokenType>());
            }

            if (modSet.IsAsync)
            {
                throw new ParsingException("A machine state cannot be async.",
                    new List<TokenType>());
            }

            if (modSet.IsPartial)
            {
                throw new ParsingException("A machine state cannot be partial.",
                    new List<TokenType>());
            }
        }
    }
}
