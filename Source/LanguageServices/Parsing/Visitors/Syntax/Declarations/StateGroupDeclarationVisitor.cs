//-----------------------------------------------------------------------
// <copyright file="StateGroupDeclarationVisitor.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

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
        /// <param name="tokenRange">The range of accumulated tokens</param>
        internal void Visit(MachineDeclaration parentNode, StateGroupDeclaration groupNode, ModifierSet modSet, TokenRange tokenRange)
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
                throw new ParsingException("Expected state group identifier.", base.TokenStream.Peek(),
                    TokenType.Identifier);
            }

            base.TokenStream.Swap(TokenType.StateGroupIdentifier);
            node.Identifier = base.TokenStream.Peek();
            node.HeaderTokenRange = tokenRange.FinishAndClone();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                throw new ParsingException("Expected \"{\".", base.TokenStream.Peek(),
                    TokenType.LeftCurlyBracket);
            }

            base.TokenStream.Swap(TokenType.StateGroupLeftCurlyBracket);

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
                throw new ParsingException("A state group must declare at least one state.", base.TokenStream.Peek());
            }
        }

        /// <summary>
        /// Visits the next intra-group declaration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextPSharpIntraGroupDeclaration(StateGroupDeclaration node)
        {
            bool fixpoint = false;
            var tokenRange = new TokenRange(base.TokenStream);
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
                        this.VisitGroupLevelDeclaration(node, tokenRange.Start());
                        base.TokenStream.Index++;
                        break;

                    case TokenType.LeftSquareBracket:
                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        new AttributeListVisitor(base.TokenStream).Visit();
                        base.TokenStream.Index++;
                        break;

                    case TokenType.RightCurlyBracket:
                        base.TokenStream.Swap(TokenType.MachineRightCurlyBracket);
                        node.RightCurlyBracketToken = base.TokenStream.Peek();
                        fixpoint = true;
                        break;

                    default:
                        throw new ParsingException("Unexpected token '" + base.TokenStream.Peek().TextUnit.Text + "'.", base.TokenStream.Peek());
                }

                if (base.TokenStream.Done)
                {
                    throw new ParsingException("Expected \"}\".", base.TokenStream.Peek(),
                        new []
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
        /// <param name="tokenRange">The range of accumulated tokens</param>
        private void VisitGroupLevelDeclaration(StateGroupDeclaration parentNode, TokenRange tokenRange)
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
                throw new ParsingException("Expected state or group declaration.", base.TokenStream.Peek(),
                    new []
                    {
                        TokenType.StateDecl,
                        TokenType.StateGroupDecl
                    });
            }

            if (base.TokenStream.Peek().Type == TokenType.StateDecl)
            {
                new StateDeclarationVisitor(base.TokenStream).Visit(parentNode.Machine, parentNode, modSet, tokenRange.Start());
            }
            else if (base.TokenStream.Peek().Type == TokenType.StateGroupDecl)
            {
                new StateGroupDeclarationVisitor(base.TokenStream).Visit(parentNode.Machine, parentNode, modSet, tokenRange.Start());
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
                throw new ParsingException("A machine state group cannot be public.", base.TokenStream.Peek());
            }
            else if (modSet.AccessModifier == AccessModifier.Internal)
            {
                throw new ParsingException("A machine state group cannot be internal.", base.TokenStream.Peek());
            }

            if (modSet.InheritanceModifier == InheritanceModifier.Abstract)
            {
                throw new ParsingException("A machine state group cannot be abstract.", base.TokenStream.Peek());
            }
            else if (modSet.InheritanceModifier == InheritanceModifier.Virtual)
            {
                throw new ParsingException("A machine state group cannot be virtual.", base.TokenStream.Peek());
            }
            else if (modSet.InheritanceModifier == InheritanceModifier.Override)
            {
                throw new ParsingException("A machine state group cannot be overriden.", base.TokenStream.Peek());
            }

            if (modSet.IsAsync)
            {
                throw new ParsingException("A machine state group cannot be async.", base.TokenStream.Peek());
            }

            if (modSet.IsPartial)
            {
                throw new ParsingException("A machine state group cannot be partial.", base.TokenStream.Peek());
            }

            if (modSet.IsStart)
            {
                throw new ParsingException("A machine state group cannot be marked start.", base.TokenStream.Peek());
            }
            else if (modSet.IsHot)
            {
                throw new ParsingException("A machine state group cannot be hot.", base.TokenStream.Peek());
            }
            else if (modSet.IsCold)
            {
                throw new ParsingException("A machine state group cannot be cold.", base.TokenStream.Peek());
            }
        }
    }
}
