// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# state declaration parsing visitor.
    /// </summary>
    internal sealed class StateDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateDeclarationVisitor"/> class.
        /// </summary>
        internal StateDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {
        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        internal void Visit(MachineDeclaration parentNode, StateGroupDeclaration groupNode, ModifierSet modSet, TokenRange tokenRange)
        {
            this.CheckMachineStateModifierSet(modSet);

            var node = new StateDeclaration(this.TokenStream.Program, parentNode, groupNode, modSet);
            node.StateKeyword = this.TokenStream.Peek();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected state identifier.", this.TokenStream.Peek(), TokenType.Identifier);
            }

            this.TokenStream.Swap(TokenType.StateIdentifier);
            node.Identifier = this.TokenStream.Peek();
            node.HeaderTokenRange = tokenRange.FinishAndClone();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            // Check for inherited state.
            if (!this.TokenStream.Done && this.TokenStream.Peek().Type == TokenType.Colon)
            {
                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                if (this.TokenStream.Done || this.TokenStream.Peek().Type != TokenType.Identifier)
                {
                    throw new ParsingException("Expected state identifier.", this.TokenStream.Peek(), TokenType.Identifier);
                }

                this.TokenStream.Swap(TokenType.StateIdentifier);
                node.BaseStateToken = this.TokenStream.Peek();
                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                throw new ParsingException("Expected \"{\".", this.TokenStream.Peek(), TokenType.LeftCurlyBracket);
            }

            this.TokenStream.Swap(TokenType.StateLeftCurlyBracket);

            node.LeftCurlyBracketToken = this.TokenStream.Peek();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (this.TokenStream.Program is PSharpProgram)
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
        private void VisitNextPSharpIntraStateDeclaration(StateDeclaration node)
        {
            bool fixpoint = false;
            var tokenRange = new TokenRange(this.TokenStream);
            while (!fixpoint)
            {
                Token badToken = null;
                if (!this.TokenStream.Done)
                {
                    var token = this.TokenStream.Peek();
                    switch (token.Type)
                    {
                        case TokenType.WhiteSpace:
                        case TokenType.QuotedString:
                        case TokenType.Comment:
                        case TokenType.NewLine:
                            this.TokenStream.Index++;
                            break;

                        case TokenType.Async:
                            tokenRange.Start();
                            this.TokenStream.Index++;
                            this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                            token = this.TokenStream.Peek();
                            switch (token.Type)
                            {
                                case TokenType.Entry:
                                    new StateEntryDeclarationVisitor(this.TokenStream).Visit(node, tokenRange, isAsync: true);
                                    this.TokenStream.Index++;
                                    break;

                                case TokenType.Exit:
                                    new StateExitDeclarationVisitor(this.TokenStream).Visit(node, tokenRange, isAsync: true);
                                    this.TokenStream.Index++;
                                    break;
                                default:
                                    throw new ParsingException("'async' was used in an incorrect context.", this.TokenStream.Peek());
                            }

                            break;

                        case TokenType.CommentLine:
                        case TokenType.Region:
                            this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                            break;

                        case TokenType.CommentStart:
                            this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                            break;

                        case TokenType.Entry:
                            new StateEntryDeclarationVisitor(this.TokenStream).Visit(node, tokenRange.Start());
                            this.TokenStream.Index++;
                            break;

                        case TokenType.Exit:
                            new StateExitDeclarationVisitor(this.TokenStream).Visit(node, tokenRange.Start());
                            this.TokenStream.Index++;
                            break;

                        case TokenType.OnAction:
                            new StateActionDeclarationVisitor(this.TokenStream).Visit(node);
                            this.TokenStream.Index++;
                            break;

                        case TokenType.DeferEvent:
                            new DeferEventsDeclarationVisitor(this.TokenStream).Visit(node);
                            this.TokenStream.Index++;
                            break;

                        case TokenType.IgnoreEvent:
                            new IgnoreEventsDeclarationVisitor(this.TokenStream).Visit(node);
                            this.TokenStream.Index++;
                            break;

                        case TokenType.LeftSquareBracket:
                            this.TokenStream.Index++;
                            this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                            new AttributeListVisitor(this.TokenStream).Visit();
                            this.TokenStream.Index++;
                            break;

                        case TokenType.RightCurlyBracket:
                            this.TokenStream.Swap(TokenType.StateRightCurlyBracket);
                            node.RightCurlyBracketToken = this.TokenStream.Peek();
                            fixpoint = true;
                            break;

                        case TokenType.Private:
                        case TokenType.Protected:
                        case TokenType.Internal:
                        case TokenType.Public:
                            throw new ParsingException("State actions cannot have modifiers.", this.TokenStream.Peek());

                        case TokenType.Abstract:
                        case TokenType.Virtual:
                            throw new ParsingException("State actions cannot be abstract or virtual.", this.TokenStream.Peek());

                        default:
                            badToken = token;
                            break;
                    }
                }

                if (this.TokenStream.Done || badToken != null)
                {
                    throw new ParsingException($"Unexpected {(this.TokenStream.Done ? "end of file" : $"token: {badToken.Text}")}.", this.TokenStream.Peek(),
                        TokenType.Entry,
                        TokenType.Exit,
                        TokenType.OnAction,
                        TokenType.DeferEvent,
                        TokenType.IgnoreEvent,
                        TokenType.LeftSquareBracket,
                        TokenType.RightCurlyBracket);
                }
            }
        }

        /// <summary>
        /// Visits the next intra-state declration.
        /// </summary>
        private void VisitNextPIntraStateDeclaration(StateDeclaration node)
        {
            bool fixpoint = false;
            var tokenRange = new TokenRange(this.TokenStream);
            while (!fixpoint)
            {
                if (!this.TokenStream.Done)
                {
                    var token = this.TokenStream.Peek();
                    switch (token.Type)
                    {
                        case TokenType.WhiteSpace:
                        case TokenType.QuotedString:
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

                        case TokenType.Entry:
                            new StateEntryDeclarationVisitor(this.TokenStream).Visit(node, tokenRange.Start());
                            this.TokenStream.Index++;
                            break;

                        case TokenType.Exit:
                            new StateExitDeclarationVisitor(this.TokenStream).Visit(node, tokenRange.Start());
                            this.TokenStream.Index++;
                            break;

                        case TokenType.OnAction:
                            new StateActionDeclarationVisitor(this.TokenStream).Visit(node);
                            this.TokenStream.Index++;
                            break;

                        case TokenType.DeferEvent:
                            new DeferEventsDeclarationVisitor(this.TokenStream).Visit(node);
                            this.TokenStream.Index++;
                            break;

                        case TokenType.IgnoreEvent:
                            new IgnoreEventsDeclarationVisitor(this.TokenStream).Visit(node);
                            this.TokenStream.Index++;
                            break;

                        case TokenType.RightCurlyBracket:
                            this.TokenStream.Swap(TokenType.StateRightCurlyBracket);
                            node.RightCurlyBracketToken = this.TokenStream.Peek();
                            fixpoint = true;
                            break;

                        default:
                            throw new ParsingException("Unexpected token.", this.TokenStream.Peek());
                    }
                }

                if (this.TokenStream.Done)
                {
                    throw new ParsingException("Expected \"}\".", this.TokenStream.Peek(),
                        TokenType.Entry,
                        TokenType.Exit,
                        TokenType.OnAction,
                        TokenType.DeferEvent,
                        TokenType.IgnoreEvent);
                }
            }
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        private void CheckMachineStateModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier == AccessModifier.Public)
            {
                throw new ParsingException("A machine state cannot be public.", this.TokenStream.Peek());
            }
            else if (modSet.AccessModifier == AccessModifier.Internal)
            {
                throw new ParsingException("A machine state cannot be internal.", this.TokenStream.Peek());
            }

            if (modSet.InheritanceModifier == InheritanceModifier.Virtual)
            {
                throw new ParsingException("A machine state cannot be virtual.", this.TokenStream.Peek());
            }
            else if (modSet.InheritanceModifier == InheritanceModifier.Override)
            {
                throw new ParsingException("A machine state cannot be overriden.", this.TokenStream.Peek());
            }

            if (modSet.IsAsync)
            {
                throw new ParsingException("A machine state cannot be async.", this.TokenStream.Peek());
            }

            if (modSet.IsPartial)
            {
                throw new ParsingException("A machine state cannot be partial.", this.TokenStream.Peek());
            }
        }
    }
}
