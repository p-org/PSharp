//-----------------------------------------------------------------------
// <copyright file="MachineDeclarationVisitor.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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

using Microsoft.PSharp.Parsing.Syntax;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# machine declaration parsing visitor.
    /// </summary>
    public sealed class MachineDeclarationVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        public MachineDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="parentNode">Node</param>
        /// <param name="isMain">Is main machine</param>
        /// <param name="isMonitor">Is a monitor</param>
        /// <param name="modifier">Modifier</param>
        /// <param name="abstractModifier">Abstract modifier</param>
        public void Visit(IPSharpProgram program, NamespaceDeclarationNode parentNode, bool isMain,
            bool isMonitor, Token modifier, Token abstractModifier)
        {
            var node = new MachineDeclarationNode(isMain, isMonitor);
            node.Modifier = modifier;
            node.AbstractModifier = abstractModifier;
            node.MachineKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected machine identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.TokenStream.CurrentMachine = base.TokenStream.Peek().Text;
            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.MachineIdentifier));

            node.Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.IsPSharp)
            {
                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.Colon &&
                    base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
                {
                    throw new ParsingException("Expected \":\" or \"{\".",
                        new List<TokenType>
                    {
                            TokenType.Colon,
                            TokenType.LeftCurlyBracket
                    });
                }

                if (base.TokenStream.Peek().Type == TokenType.Colon)
                {
                    node.ColonToken = base.TokenStream.Peek();

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    if (base.TokenStream.Done ||
                        base.TokenStream.Peek().Type != TokenType.Identifier)
                    {
                        throw new ParsingException("Expected base machine identifier.",
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
                            throw new ParsingException("Expected base machine identifier.",
                                new List<TokenType>
                            {
                                    TokenType.Identifier,
                                    TokenType.Dot
                            });
                        }
                        else
                        {
                            node.BaseNameTokens.Add(base.TokenStream.Peek());
                        }

                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }
                }
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
                TokenType.MachineLeftCurlyBracket));

            node.LeftCurlyBracketToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.IsPSharp)
            {
                this.VisitNextPSharpIntraMachineDeclaration(node);
                parentNode.MachineDeclarations.Add(node);
            }
            else
            {
                this.VisitNextPIntraMachineDeclaration(node);
                (program as PProgram).MachineDeclarations.Add(node);
            }
        }

        /// <summary>
        /// Visits the next intra-machine declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextPSharpIntraMachineDeclaration(MachineDeclarationNode node)
        {
            if (base.TokenStream.Done)
            {
                throw new ParsingException("Expected \"}\".",
                    new List<TokenType>
                {
                    TokenType.Private,
                    TokenType.Protected,
                    TokenType.StartState,
                    TokenType.StateDecl,
                    TokenType.ActionDecl,
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

                case TokenType.StartState:
                    this.VisitStartStateModifier(node, null);
                    base.TokenStream.Index++;
                    break;

                case TokenType.StateDecl:
                    new StateDeclarationVisitor(base.TokenStream).Visit(node, false, null);
                    base.TokenStream.Index++;
                    break;

                case TokenType.ActionDecl:
                    new ActionDeclarationVisitor(base.TokenStream).Visit(node, null, null);
                    base.TokenStream.Index++;
                    break;

                case TokenType.Identifier:
                    new FieldOrMethodDeclarationVisitor(base.TokenStream).Visit(node, null, null);
                    base.TokenStream.Index++;
                    break;

                case TokenType.Private:
                case TokenType.Protected:
                    this.VisitMachineLevelAccessModifier(node);
                    base.TokenStream.Index++;
                    break;

                case TokenType.Abstract:
                case TokenType.Virtual:
                    this.VisitMachineLevelAbstractModifier(node);
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
                    base.TokenStream.CurrentMachine = "";
                    fixpoint = true;
                    base.TokenStream.Index++;
                    break;

                case TokenType.Internal:
                case TokenType.Public:
                    throw new ParsingException("Machine fields, states or actions must be private or protected.",
                        new List<TokenType>());

                default:
                    throw new ParsingException("Unexpected token.",
                        new List<TokenType>());
            }

            if (!fixpoint)
            {
                this.VisitNextPSharpIntraMachineDeclaration(node);
            }
        }

        /// <summary>
        /// Visits a machine level access modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitMachineLevelAccessModifier(MachineDeclarationNode parentNode)
        {
            var modifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Abstract &&
                base.TokenStream.Peek().Type != TokenType.Virtual &&
                base.TokenStream.Peek().Type != TokenType.Override &&
                base.TokenStream.Peek().Type != TokenType.StartState &&
                base.TokenStream.Peek().Type != TokenType.StateDecl &&
                base.TokenStream.Peek().Type != TokenType.ActionDecl &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.Int &&
                base.TokenStream.Peek().Type != TokenType.Bool &&
                base.TokenStream.Peek().Type != TokenType.Identifier))
            {
                throw new ParsingException("Expected state, action, field or method declaration.",
                    new List<TokenType>
                {
                    TokenType.Abstract,
                    TokenType.Virtual,
                    TokenType.Override,
                    TokenType.StartState,
                    TokenType.StateDecl,
                    TokenType.ActionDecl,
                    TokenType.MachineDecl,
                    TokenType.Int,
                    TokenType.Bool,
                    TokenType.Identifier
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Abstract ||
                base.TokenStream.Peek().Type == TokenType.Virtual ||
                base.TokenStream.Peek().Type == TokenType.Override)
            {
                var inheritanceModifier = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.ActionDecl &&
                    base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Identifier))
                {
                    throw new ParsingException("Expected action or method declaration.",
                        new List<TokenType>
                    {
                        TokenType.ActionDecl,
                        TokenType.MachineDecl,
                        TokenType.Int,
                        TokenType.Bool,
                        TokenType.Identifier
                    });
                }

                if (base.TokenStream.Peek().Type == TokenType.ActionDecl)
                {
                    new ActionDeclarationVisitor(base.TokenStream).Visit(parentNode, modifier, inheritanceModifier);
                }
                else if (base.TokenStream.Peek().Type == TokenType.MachineDecl ||
                    base.TokenStream.Peek().Type == TokenType.Int ||
                    base.TokenStream.Peek().Type == TokenType.Bool ||
                    base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    new FieldOrMethodDeclarationVisitor(base.TokenStream).Visit(parentNode, modifier, inheritanceModifier);
                }
            }
            else if (base.TokenStream.Peek().Type == TokenType.StartState)
            {
                this.VisitStartStateModifier(parentNode, modifier);
            }
            else if (base.TokenStream.Peek().Type == TokenType.StateDecl)
            {
                new StateDeclarationVisitor(base.TokenStream).Visit(parentNode, false, modifier);
            }
            else if (base.TokenStream.Peek().Type == TokenType.ActionDecl)
            {
                new ActionDeclarationVisitor(base.TokenStream).Visit(parentNode, modifier, null);
            }
            else if (base.TokenStream.Peek().Type == TokenType.MachineDecl ||
                base.TokenStream.Peek().Type == TokenType.Int ||
                base.TokenStream.Peek().Type == TokenType.Bool ||
                base.TokenStream.Peek().Type == TokenType.Identifier)
            {
                new FieldOrMethodDeclarationVisitor(base.TokenStream).Visit(parentNode, modifier, null);
            }
        }

        /// <summary>
        /// Visits a machine level abstract modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitMachineLevelAbstractModifier(MachineDeclarationNode parentNode)
        {
            var abstractModifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();
        }

        /// <summary>
        /// Visits the next intra-machine declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextPIntraMachineDeclaration(MachineDeclarationNode node)
        {
            if (base.TokenStream.Done)
            {
                throw new ParsingException("Expected \"}\".",
                    new List<TokenType>
                {
                    TokenType.StartState,
                    TokenType.StateDecl,
                    TokenType.FunDecl,
                    TokenType.Var
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

                case TokenType.StartState:
                    this.VisitStartStateModifier(node, null);
                    base.TokenStream.Index++;
                    break;

                case TokenType.StateDecl:
                    new StateDeclarationVisitor(base.TokenStream).Visit(node, false, null);
                    base.TokenStream.Index++;
                    break;

                case TokenType.ModelDecl:
                    new FunctionDeclarationVisitor(base.TokenStream).Visit(node, true);
                    base.TokenStream.Index++;
                    break;

                case TokenType.FunDecl:
                    new FunctionDeclarationVisitor(base.TokenStream).Visit(node, false);
                    base.TokenStream.Index++;
                    break;

                case TokenType.Var:
                    new FieldDeclarationVisitor(base.TokenStream).Visit(node);
                    base.TokenStream.Index++;
                    break;

                case TokenType.ColdState:
                case TokenType.HotState:
                    base.TokenStream.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                        TokenType.MachineRightCurlyBracket));
                    node.RightCurlyBracketToken = base.TokenStream.Peek();
                    base.TokenStream.CurrentMachine = "";
                    fixpoint = true;
                    base.TokenStream.Index++;
                    break;

                default:
                    throw new ParsingException("Unexpected token.",
                        new List<TokenType>());
            }

            if (!fixpoint)
            {
                this.VisitNextPIntraMachineDeclaration(node);
            }
        }

        /// <summary>
        /// Visits a start state modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modifier">Modifier</param>
        private void VisitStartStateModifier(MachineDeclarationNode parentNode, Token modifier)
        {
            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.StateDecl &&
                base.TokenStream.Peek().Type != TokenType.ColdState &&
                base.TokenStream.Peek().Type != TokenType.HotState))
            {
                throw new ParsingException("Expected state declaration.",
                    new List<TokenType>
                {
                    TokenType.StateDecl
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.ColdState ||
                base.TokenStream.Peek().Type == TokenType.HotState)
            {
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.StateDecl)
                {
                    throw new ParsingException("Expected state declaration.",
                        new List<TokenType>
                    {
                        TokenType.StateDecl
                    });
                }
            }

            new StateDeclarationVisitor(base.TokenStream).Visit(parentNode, true, modifier);
        }
    }
}
