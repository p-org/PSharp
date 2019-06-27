// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# machine declaration parsing visitor.
    /// </summary>
    internal sealed class MachineDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MachineDeclarationVisitor"/> class.
        /// </summary>
        internal MachineDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {
        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        internal void Visit(NamespaceDeclaration parentNode, bool isMonitor, ModifierSet modSet, TokenRange tokenRange)
        {
            if (isMonitor)
            {
                this.CheckMonitorModifierSet(modSet);
            }
            else
            {
                this.CheckMachineModifierSet(modSet);
            }

            var node = new MachineDeclaration(this.TokenStream.Program, parentNode, isMonitor, modSet);
            node.MachineKeyword = this.TokenStream.Peek();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected machine identifier.", this.TokenStream.Peek(), TokenType.Identifier);
            }

            this.TokenStream.Swap(TokenType.MachineIdentifier);

            node.Identifier = this.TokenStream.Peek();
            node.HeaderTokenRange = tokenRange.FinishAndClone();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            var nameVisitor = new NameVisitor(this.TokenStream);
            node.TemplateParameters = nameVisitor.ConsumeTemplateParams();

            if (this.TokenStream.Program is PSharpProgram)
            {
                if (this.TokenStream.Done ||
                    (this.TokenStream.Peek().Type != TokenType.Colon &&
                    this.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
                {
                    throw new ParsingException("Expected \":\" or \"{\".", this.TokenStream.Peek(), TokenType.Colon, TokenType.LeftCurlyBracket);
                }

                if (this.TokenStream.Peek().Type == TokenType.Colon)
                {
                    node.ColonToken = this.TokenStream.Peek();

                    this.TokenStream.Index++;
                    this.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    var baseNameTokensVisitor = new NameVisitor(this.TokenStream, node.HeaderTokenRange);

                    node.BaseNameTokens = baseNameTokensVisitor.ConsumeGenericName(TokenType.MachineIdentifier);
                }
            }

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                throw new ParsingException("Expected \"{\".", this.TokenStream.Peek(), TokenType.LeftCurlyBracket);
            }

            this.TokenStream.Swap(TokenType.MachineLeftCurlyBracket);

            node.LeftCurlyBracketToken = this.TokenStream.Peek();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextPSharpIntraMachineDeclaration(node);
            parentNode.MachineDeclarations.Add(node);

            var stateDeclarations = node.GetAllStateDeclarations();
            if (stateDeclarations.Count == 0 && node.BaseNameTokens.Count == 0)
            {
                throw new ParsingException("A machine must declare at least one state.", this.TokenStream.Peek());
            }

            var startStates = stateDeclarations.FindAll(s => s.IsStart);
            if (startStates.Count == 0 && node.BaseNameTokens.Count == 0)
            {
                throw new ParsingException("A machine must declare a start state.", this.TokenStream.Peek());
            }
            else if (startStates.Count > 1)
            {
                throw new ParsingException("A machine can declare only a single start state.", this.TokenStream.Peek());
            }
        }

        /// <summary>
        /// Visits the next intra-machine declaration.
        /// </summary>
        private void VisitNextPSharpIntraMachineDeclaration(MachineDeclaration node)
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

                        case TokenType.ExternDecl:
                            new EventDeclarationVisitor(this.TokenStream).VisitExternDeclaration(node.Namespace, node);
                            this.TokenStream.Index++;
                            break;

                        case TokenType.Abstract:
                        case TokenType.StartState:
                        case TokenType.HotState:
                        case TokenType.ColdState:
                        case TokenType.EventDecl:
                        case TokenType.StateDecl:
                        case TokenType.StateGroupDecl:
                        case TokenType.Void:
                        case TokenType.MachineDecl:
                        case TokenType.Object:
                        case TokenType.String:
                        case TokenType.Sbyte:
                        case TokenType.Byte:
                        case TokenType.Short:
                        case TokenType.Ushort:
                        case TokenType.Int:
                        case TokenType.Uint:
                        case TokenType.Long:
                        case TokenType.Ulong:
                        case TokenType.Char:
                        case TokenType.Bool:
                        case TokenType.Decimal:
                        case TokenType.Float:
                        case TokenType.Double:
                        case TokenType.Identifier:
                        case TokenType.Private:
                        case TokenType.Protected:
                        case TokenType.Internal:
                        case TokenType.Public:
                        case TokenType.Async:
                        case TokenType.Partial:
                            this.VisitMachineLevelDeclaration(node, tokenRange.Start());
                            this.TokenStream.Index++;
                            break;

                        case TokenType.LeftSquareBracket:
                            this.TokenStream.Index++;
                            this.TokenStream.SkipWhiteSpaceAndCommentTokens();
                            new AttributeListVisitor(this.TokenStream).Visit();
                            this.TokenStream.Index++;
                            break;

                        case TokenType.RightCurlyBracket:
                            this.TokenStream.Swap(TokenType.MachineRightCurlyBracket);
                            node.RightCurlyBracketToken = this.TokenStream.Peek();
                            fixpoint = true;
                            break;

                        default:
                            throw new ParsingException($"Unexpected token '{this.TokenStream.Peek().TextUnit.Text}'.", this.TokenStream.Peek());
                    }
                }

                if (this.TokenStream.Done)
                {
                    throw new ParsingException("Expected \"}\".", this.TokenStream.Peek(),
                        TokenType.Private,
                        TokenType.Protected,
                        TokenType.StartState,
                        TokenType.HotState,
                        TokenType.ColdState,
                        TokenType.StateDecl,
                        TokenType.StateGroupDecl,
                        TokenType.LeftSquareBracket,
                        TokenType.RightCurlyBracket);
                }
            }
        }

        /// <summary>
        /// Visits a machine level declaration.
        /// </summary>
        private void VisitMachineLevelDeclaration(MachineDeclaration parentNode, TokenRange tokenRange)
        {
            var modSet = ModifierSet.CreateDefault();

            while (!this.TokenStream.Done &&
                this.TokenStream.Peek().Type != TokenType.EventDecl &&
                this.TokenStream.Peek().Type != TokenType.StateDecl &&
                this.TokenStream.Peek().Type != TokenType.StateGroupDecl &&
                this.TokenStream.Peek().Type != TokenType.MachineDecl &&
                this.TokenStream.Peek().Type != TokenType.Void &&
                this.TokenStream.Peek().Type != TokenType.Object &&
                this.TokenStream.Peek().Type != TokenType.String &&
                this.TokenStream.Peek().Type != TokenType.Sbyte &&
                this.TokenStream.Peek().Type != TokenType.Byte &&
                this.TokenStream.Peek().Type != TokenType.Short &&
                this.TokenStream.Peek().Type != TokenType.Ushort &&
                this.TokenStream.Peek().Type != TokenType.Int &&
                this.TokenStream.Peek().Type != TokenType.Uint &&
                this.TokenStream.Peek().Type != TokenType.Long &&
                this.TokenStream.Peek().Type != TokenType.Ulong &&
                this.TokenStream.Peek().Type != TokenType.Char &&
                this.TokenStream.Peek().Type != TokenType.Bool &&
                this.TokenStream.Peek().Type != TokenType.Decimal &&
                this.TokenStream.Peek().Type != TokenType.Float &&
                this.TokenStream.Peek().Type != TokenType.Double &&
                this.TokenStream.Peek().Type != TokenType.Identifier)
            {
                new ModifierVisitor(this.TokenStream).Visit(modSet);

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.TokenStream.Done ||
                (this.TokenStream.Peek().Type != TokenType.EventDecl &&
                this.TokenStream.Peek().Type != TokenType.StateDecl &&
                this.TokenStream.Peek().Type != TokenType.StateGroupDecl &&
                this.TokenStream.Peek().Type != TokenType.MachineDecl &&
                this.TokenStream.Peek().Type != TokenType.Void &&
                this.TokenStream.Peek().Type != TokenType.Object &&
                this.TokenStream.Peek().Type != TokenType.String &&
                this.TokenStream.Peek().Type != TokenType.Sbyte &&
                this.TokenStream.Peek().Type != TokenType.Byte &&
                this.TokenStream.Peek().Type != TokenType.Short &&
                this.TokenStream.Peek().Type != TokenType.Ushort &&
                this.TokenStream.Peek().Type != TokenType.Int &&
                this.TokenStream.Peek().Type != TokenType.Uint &&
                this.TokenStream.Peek().Type != TokenType.Long &&
                this.TokenStream.Peek().Type != TokenType.Ulong &&
                this.TokenStream.Peek().Type != TokenType.Char &&
                this.TokenStream.Peek().Type != TokenType.Bool &&
                this.TokenStream.Peek().Type != TokenType.Decimal &&
                this.TokenStream.Peek().Type != TokenType.Float &&
                this.TokenStream.Peek().Type != TokenType.Double &&
                this.TokenStream.Peek().Type != TokenType.Identifier))
            {
                throw new ParsingException("Expected event, state, group or method declaration.", this.TokenStream.Peek(),
                    TokenType.EventDecl,
                    TokenType.StateDecl,
                    TokenType.StateGroupDecl,
                    TokenType.MachineDecl,
                    TokenType.Void,
                    TokenType.Object,
                    TokenType.String,
                    TokenType.Sbyte,
                    TokenType.Byte,
                    TokenType.Short,
                    TokenType.Ushort,
                    TokenType.Int,
                    TokenType.Uint,
                    TokenType.Long,
                    TokenType.Ulong,
                    TokenType.Char,
                    TokenType.Bool,
                    TokenType.Decimal,
                    TokenType.Float,
                    TokenType.Double,
                    TokenType.Identifier);
            }

            if (this.TokenStream.Peek().Type == TokenType.EventDecl)
            {
                new EventDeclarationVisitor(this.TokenStream).Visit(parentNode.Namespace, parentNode, modSet);
            }
            else if (this.TokenStream.Peek().Type == TokenType.StateDecl)
            {
                new StateDeclarationVisitor(this.TokenStream).Visit(parentNode, null, modSet, tokenRange.Start());
            }
            else if (this.TokenStream.Peek().Type == TokenType.StateGroupDecl)
            {
                new StateGroupDeclarationVisitor(this.TokenStream).Visit(parentNode, null, modSet, tokenRange.Start());
            }
            else
            {
                new MachineMemberDeclarationVisitor(this.TokenStream).Visit(parentNode, modSet);
            }
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        private void CheckMachineModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier == AccessModifier.Private)
            {
                throw new ParsingException("A machine cannot be declared as private.", this.TokenStream.Peek());
            }
            else if (modSet.AccessModifier == AccessModifier.Protected)
            {
                throw new ParsingException("A machine cannot be declared as protected.", this.TokenStream.Peek());
            }
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        private void CheckMonitorModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier == AccessModifier.Private)
            {
                throw new ParsingException("A monitor cannot be declared as private.", this.TokenStream.Peek());
            }
            else if (modSet.AccessModifier == AccessModifier.Protected)
            {
                throw new ParsingException("A monitor cannot be declared as protected.", this.TokenStream.Peek());
            }
        }
    }
}
