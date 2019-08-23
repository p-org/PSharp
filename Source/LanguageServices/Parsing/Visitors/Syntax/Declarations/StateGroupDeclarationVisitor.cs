using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# state group declaration parsing visitor.
    /// </summary>
    internal sealed class StateGroupDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateGroupDeclarationVisitor"/> class.
        /// </summary>
        internal StateGroupDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {
        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        internal void Visit(MachineDeclaration parentNode, StateGroupDeclaration groupNode, ModifierSet modSet, TokenRange tokenRange)
        {
            this.CheckStateGroupModifierSet(modSet);

            var node = new StateGroupDeclaration(this.TokenStream.Program, parentNode, groupNode);
            node.AccessModifier = modSet.AccessModifier;
            node.StateGroupKeyword = this.TokenStream.Peek();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected state group identifier.", this.TokenStream.Peek(), TokenType.Identifier);
            }

            this.TokenStream.Swap(TokenType.StateGroupIdentifier);
            node.Identifier = this.TokenStream.Peek();
            node.HeaderTokenRange = tokenRange.FinishAndClone();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (this.TokenStream.Done ||
                this.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                throw new ParsingException("Expected \"{\".", this.TokenStream.Peek(), TokenType.LeftCurlyBracket);
            }

            this.TokenStream.Swap(TokenType.StateGroupLeftCurlyBracket);

            node.LeftCurlyBracketToken = this.TokenStream.Peek();

            this.TokenStream.Index++;
            this.TokenStream.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextPSharpIntraGroupDeclaration(node);

            if (groupNode is null)
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
                throw new ParsingException("A state group must declare at least one state.", this.TokenStream.Peek());
            }
        }

        /// <summary>
        /// Visits the next intra-group declaration.
        /// </summary>
        private void VisitNextPSharpIntraGroupDeclaration(StateGroupDeclaration node)
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

                        case TokenType.StartState:
                        case TokenType.HotState:
                        case TokenType.ColdState:
                        case TokenType.StateDecl:
                        case TokenType.StateGroupDecl:
                        case TokenType.Private:
                        case TokenType.Protected:
                        case TokenType.Internal:
                        case TokenType.Public:
                            this.VisitGroupLevelDeclaration(node, tokenRange.Start());
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
                            throw new ParsingException("Unexpected token '" + this.TokenStream.Peek().TextUnit.Text + "'.", this.TokenStream.Peek());
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
        /// Visits a group level declaration.
        /// </summary>
        private void VisitGroupLevelDeclaration(StateGroupDeclaration parentNode, TokenRange tokenRange)
        {
            ModifierSet modSet = ModifierSet.CreateDefault();

            while (!this.TokenStream.Done &&
                this.TokenStream.Peek().Type != TokenType.StateDecl &&
                this.TokenStream.Peek().Type != TokenType.StateGroupDecl &&
                this.TokenStream.Peek().Type != TokenType.MachineDecl)
            {
                new ModifierVisitor(this.TokenStream).Visit(modSet);

                this.TokenStream.Index++;
                this.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.TokenStream.Done ||
                (this.TokenStream.Peek().Type != TokenType.StateDecl &&
                this.TokenStream.Peek().Type != TokenType.StateGroupDecl))
            {
                throw new ParsingException("Expected state or group declaration.", this.TokenStream.Peek(), TokenType.StateDecl, TokenType.StateGroupDecl);
            }

            if (this.TokenStream.Peek().Type == TokenType.StateDecl)
            {
                new StateDeclarationVisitor(this.TokenStream).Visit(parentNode.Machine, parentNode, modSet, tokenRange.Start());
            }
            else if (this.TokenStream.Peek().Type == TokenType.StateGroupDecl)
            {
                new StateGroupDeclarationVisitor(this.TokenStream).Visit(parentNode.Machine, parentNode, modSet, tokenRange.Start());
            }
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        private void CheckStateGroupModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier == AccessModifier.Public)
            {
                throw new ParsingException("A machine state group cannot be public.", this.TokenStream.Peek());
            }
            else if (modSet.AccessModifier == AccessModifier.Internal)
            {
                throw new ParsingException("A machine state group cannot be internal.", this.TokenStream.Peek());
            }

            if (modSet.InheritanceModifier == InheritanceModifier.Abstract)
            {
                throw new ParsingException("A machine state group cannot be abstract.", this.TokenStream.Peek());
            }
            else if (modSet.InheritanceModifier == InheritanceModifier.Virtual)
            {
                throw new ParsingException("A machine state group cannot be virtual.", this.TokenStream.Peek());
            }
            else if (modSet.InheritanceModifier == InheritanceModifier.Override)
            {
                throw new ParsingException("A machine state group cannot be overriden.", this.TokenStream.Peek());
            }

            if (modSet.IsAsync)
            {
                throw new ParsingException("A machine state group cannot be async.", this.TokenStream.Peek());
            }

            if (modSet.IsPartial)
            {
                throw new ParsingException("A machine state group cannot be partial.", this.TokenStream.Peek());
            }

            if (modSet.IsStart)
            {
                throw new ParsingException("A machine state group cannot be marked start.", this.TokenStream.Peek());
            }
            else if (modSet.IsHot)
            {
                throw new ParsingException("A machine state group cannot be hot.", this.TokenStream.Peek());
            }
            else if (modSet.IsCold)
            {
                throw new ParsingException("A machine state group cannot be cold.", this.TokenStream.Peek());
            }
        }
    }
}
