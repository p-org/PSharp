//-----------------------------------------------------------------------
// <copyright file="StateDeclarationVisitor.cs">
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
        /// <param name="parentNode">Node</param>
        /// <param name="groupNode">Parent state group</param>
        /// <param name="isStart">Is start state</param>
        /// <param name="isHot">Is start state</param>
        /// <param name="isCold">Is start state</param>
        /// <param name="accMod">Access modifier</param>
        internal void Visit(MachineDeclaration parentNode, StateGroupDeclaration groupNode,
            bool isStart, bool isHot, bool isCold, AccessModifier accMod)
        {
            var node = new StateDeclaration(base.TokenStream.Program, parentNode,
                groupNode, isStart, isHot, isCold);
            node.AccessModifier = accMod;
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
    }
}
