//-----------------------------------------------------------------------
// <copyright file="PSharpParser.cs">
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
    /// The P# parser.
    /// </summary>
    public sealed class PSharpParser : BaseParser
    {
        #region public API

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PSharpParser()
            : base()
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filePath">File path</param>
        internal PSharpParser(string filePath)
            : base(filePath)
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
            return new PSharpProgram(base.FilePath);
        }

        /// <summary>
        /// Parses the next available token.
        /// </summary>
        protected override void ParseNextToken()
        {
            if (base.TokenStream.Done)
            {
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Using,
                    TokenType.NamespaceDecl
                });
            }

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
                    base.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    base.SkipWhiteSpaceAndCommentTokens();
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
                case TokenType.Abstract:
                case TokenType.Virtual:
                case TokenType.EventDecl:
                case TokenType.MainMachine:
                case TokenType.MachineDecl:
                    this.ReportParsingError("Must be declared inside a namespace.");
                    break;

                default:
                    this.ReportParsingError("Unexpected token.");
                    break;
            }

            this.ParseNextToken();
        }

        #endregion

        #region private API

        //// <summary>
        /// Visits a using declaration.
        /// </summary>
        private void VisitUsingDeclaration()
        {
            var node = new UsingDeclarationNode();
            node.UsingKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected identifier.");
                throw new EndOfTokensException(new List<TokenType>
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
                    this.ReportParsingError("Expected identifier.");
                    throw new EndOfTokensException(new List<TokenType>
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
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
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
            var node = new NamespaceDeclarationNode();
            node.NamespaceKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected namespace identifier.");
                throw new EndOfTokensException(new List<TokenType>
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
                    this.ReportParsingError("Expected namespace identifier.");
                    throw new EndOfTokensException(new List<TokenType>
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
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            node.LeftCurlyBracketToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextIntraNamespaceDeclaration(node);

            (this.Program as PSharpProgram).NamespaceDeclarations.Add(node);
        }

        /// <summary>
        /// Visits the next intra-namespace declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextIntraNamespaceDeclaration(NamespaceDeclarationNode node)
        {
            if (base.TokenStream.Done)
            {
                this.ReportParsingError("Expected \"}\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Internal,
                    TokenType.Public,
                    TokenType.Abstract,
                    TokenType.Virtual,
                    TokenType.MainMachine,
                    TokenType.EventDecl,
                    TokenType.MachineDecl,
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
                    base.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    base.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.EventDecl:
                    this.VisitEventDeclaration(node, null);
                    base.TokenStream.Index++;
                    break;

                case TokenType.MainMachine:
                    this.VisitMainMachineModifier(node, null);
                    base.TokenStream.Index++;
                    break;

                case TokenType.MachineDecl:
                    this.VisitMachineDeclaration(node, false, null, null);
                    base.TokenStream.Index++;
                    break;

                case TokenType.Internal:
                case TokenType.Public:
                    this.VisitTopLevelAccessModifier(node);
                    base.TokenStream.Index++;
                    break;

                case TokenType.Abstract:
                case TokenType.Virtual:
                    this.VisitTopLevelAbstractModifier(node);
                    base.TokenStream.Index++;
                    break;

                case TokenType.LeftSquareBracket:
                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.VisitAttributeList();
                    base.TokenStream.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    node.RightCurlyBracketToken = base.TokenStream.Peek();
                    fixpoint = true;
                    base.TokenStream.Index++;
                    break;

                case TokenType.Private:
                case TokenType.Protected:
                    this.ReportParsingError("Event and machine declarations must be internal or public.");
                    break;

                default:
                    this.ReportParsingError("Unexpected token.");
                    break;
            }

            if (!fixpoint)
            {
                this.VisitNextIntraNamespaceDeclaration(node);
            }
        }

        /// <summary>
        /// Visits a top level access modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitTopLevelAccessModifier(NamespaceDeclarationNode parentNode)
        {
            var modifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Abstract &&
                base.TokenStream.Peek().Type != TokenType.Virtual &&
                base.TokenStream.Peek().Type != TokenType.MainMachine &&
                base.TokenStream.Peek().Type != TokenType.EventDecl &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl))
            {
                this.ReportParsingError("Expected event or machine declaration.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Abstract,
                    TokenType.Virtual,
                    TokenType.MainMachine,
                    TokenType.EventDecl,
                    TokenType.MachineDecl
                });
            }

            Token abstractModifier = null;
            if (base.TokenStream.Peek().Type == TokenType.Abstract)
            {
                abstractModifier = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.MachineDecl)
                {
                    this.ReportParsingError("Expected machine declaration.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.MachineDecl
                    });
                }

                this.VisitMachineDeclaration(parentNode, false, modifier, abstractModifier);
            }
            else if (base.TokenStream.Peek().Type == TokenType.EventDecl)
            {
                this.VisitEventDeclaration(parentNode, modifier);
            }
            else if (base.TokenStream.Peek().Type == TokenType.MainMachine)
            {
                this.VisitMainMachineModifier(parentNode, modifier);
            }
            else if (base.TokenStream.Peek().Type == TokenType.MachineDecl)
            {
                this.VisitMachineDeclaration(parentNode, false, modifier, abstractModifier);
            }
        }

        /// <summary>
        /// Visits a top level abstract modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitTopLevelAbstractModifier(NamespaceDeclarationNode parentNode)
        {
            var abstractModifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Internal &&
                base.TokenStream.Peek().Type != TokenType.Public &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl))
            {
                this.ReportParsingError("Expected machine declaration.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Internal,
                    TokenType.Public,
                    TokenType.MachineDecl
                });
            }

            Token modifier = null;
            if (base.TokenStream.Peek().Type == TokenType.Internal ||
                base.TokenStream.Peek().Type == TokenType.Public)
            {
                modifier = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.MachineDecl))
                {
                    this.ReportParsingError("Expected machine declaration.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.MachineDecl
                    });
                }
            }

            if (base.TokenStream.Peek().Type == TokenType.EventDecl)
            {
                this.VisitEventDeclaration(parentNode, modifier);
            }
            else if (base.TokenStream.Peek().Type == TokenType.MachineDecl)
            {
                this.VisitMachineDeclaration(parentNode, false, modifier, abstractModifier);
            }
        }

        /// <summary>
        /// Visits an event declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modifier">Modifier</param>
        private void VisitEventDeclaration(NamespaceDeclarationNode parentNode, Token modifier)
        {
            var node = new EventDeclarationNode();
            node.Modifier = modifier;
            node.EventKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                base.TokenStream.Peek().Type != TokenType.DefaultEvent))
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent,
                    TokenType.DefaultEvent
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Identifier)
            {
                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                    TokenType.EventIdentifier));
            }

            node.Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.TokenStream.Peek();

            parentNode.EventDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a main machine modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modifier">Modifier</param>
        private void VisitMainMachineModifier(NamespaceDeclarationNode parentNode, Token modifier)
        {
            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.MachineDecl)
            {
                this.ReportParsingError("Expected machine declaration.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.MachineDecl
                });
            }

            this.VisitMachineDeclaration(parentNode, true, modifier, null);
        }

        /// <summary>
        /// Visits a machine declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="isMain">Is main machine</param>
        /// <param name="modifier">Modifier</param>
        /// <param name="abstractModifier">Abstract modifier</param>
        private void VisitMachineDeclaration(NamespaceDeclarationNode parentNode, bool isMain,
            Token modifier, Token abstractModifier)
        {
            var node = new MachineDeclarationNode(isMain, false);
            node.Modifier = modifier;
            node.AbstractModifier = abstractModifier;
            node.MachineKeyword = base.TokenStream.Peek();
            
            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected machine identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.CurrentMachine = base.TokenStream.Peek().Text;
            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.MachineIdentifier));

            node.Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Colon &&
                base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
            {
                this.ReportParsingError("Expected \":\" or \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Colon,
                    TokenType.LeftCurlyBracket
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Colon)
            {
                node.ColonToken = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected base machine identifier.");
                    throw new EndOfTokensException(new List<TokenType>
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
                        this.ReportParsingError("Expected base machine identifier.");
                        throw new EndOfTokensException(new List<TokenType>
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
                    base.SkipWhiteSpaceAndCommentTokens();
                }
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.MachineLeftCurlyBracket));

            node.LeftCurlyBracketToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextIntraMachineDeclaration(node);

            parentNode.MachineDeclarations.Add(node);
        }

        /// <summary>
        /// Visits the next intra-machine declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextIntraMachineDeclaration(MachineDeclarationNode node)
        {
            if (base.TokenStream.Done)
            {
                this.ReportParsingError("Expected \"}\".");
                throw new EndOfTokensException(new List<TokenType>
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
                    base.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    base.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.StartState:
                    this.VisitStartStateModifier(node, null);
                    base.TokenStream.Index++;
                    break;

                case TokenType.StateDecl:
                    this.VisitStateDeclaration(node, false, null);
                    base.TokenStream.Index++;
                    break;

                case TokenType.ActionDecl:
                    this.VisitActionDeclaration(node, null, null);
                    base.TokenStream.Index++;
                    break;

                case TokenType.Identifier:
                    this.VisitFieldOrMethodDeclaration(node, null, null);
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
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.VisitAttributeList();
                    base.TokenStream.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                        TokenType.MachineRightCurlyBracket));
                    node.RightCurlyBracketToken = base.TokenStream.Peek();
                    base.CurrentMachine = "";
                    fixpoint = true;
                    base.TokenStream.Index++;
                    break;

                case TokenType.Internal:
                case TokenType.Public:
                    this.ReportParsingError("Machine fields, states or actions must be private or protected.");
                    break;

                default:
                    this.ReportParsingError("Unexpected token.");
                    break;
            }

            if (!fixpoint)
            {
                this.VisitNextIntraMachineDeclaration(node);
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
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Abstract &&
                base.TokenStream.Peek().Type != TokenType.Virtual &&
                base.TokenStream.Peek().Type != TokenType.Override &&
                base.TokenStream.Peek().Type != TokenType.StartState &&
                base.TokenStream.Peek().Type != TokenType.StateDecl &&
                base.TokenStream.Peek().Type != TokenType.ActionDecl &&
                base.TokenStream.Peek().Type != TokenType.Int &&
                base.TokenStream.Peek().Type != TokenType.Bool &&
                base.TokenStream.Peek().Type != TokenType.Identifier))
            {
                this.ReportParsingError("Expected state, action, field or method declaration.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Abstract,
                    TokenType.Virtual,
                    TokenType.Override,
                    TokenType.StartState,
                    TokenType.StateDecl,
                    TokenType.ActionDecl,
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
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.ActionDecl &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Identifier))
                {
                    this.ReportParsingError("Expected action or method declaration.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.ActionDecl,
                        TokenType.Int,
                        TokenType.Bool,
                        TokenType.Identifier
                    });
                }

                if (base.TokenStream.Peek().Type == TokenType.ActionDecl)
                {
                    this.VisitActionDeclaration(parentNode, modifier, inheritanceModifier);
                }
                else if (base.TokenStream.Peek().Type == TokenType.Int ||
                    base.TokenStream.Peek().Type == TokenType.Bool ||
                    base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    this.VisitFieldOrMethodDeclaration(parentNode, modifier, inheritanceModifier);
                }
            }
            else if (base.TokenStream.Peek().Type == TokenType.StartState)
            {
                this.VisitStartStateModifier(parentNode, modifier);
            }
            else if (base.TokenStream.Peek().Type == TokenType.StateDecl)
            {
                this.VisitStateDeclaration(parentNode, false, modifier);
            }
            else if (base.TokenStream.Peek().Type == TokenType.ActionDecl)
            {
                this.VisitActionDeclaration(parentNode, modifier, null);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Int ||
                base.TokenStream.Peek().Type == TokenType.Bool ||
                base.TokenStream.Peek().Type == TokenType.Identifier)
            {
                this.VisitFieldOrMethodDeclaration(parentNode, modifier, null);
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
            base.SkipWhiteSpaceAndCommentTokens();
        }

        /// <summary>
        /// Visits a start state modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modifier">Modifier</param>
        private void VisitStartStateModifier(MachineDeclarationNode parentNode, Token modifier)
        {
            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.StateDecl)
            {
                this.ReportParsingError("Expected state declaration.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.StateDecl
                });
            }

            this.VisitStateDeclaration(parentNode, true, modifier);
        }

        /// <summary>
        /// Visits a state declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="isInit">Is initial state</param>
        /// <param name="modifier">Modifier</param>
        private void VisitStateDeclaration(MachineDeclarationNode parentNode, bool isInit, Token modifier)
        {
            var node = new StateDeclarationNode(parentNode, isInit);
            node.Modifier = modifier;
            node.StateKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected state identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.CurrentState = base.TokenStream.Peek().Text;
            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.StateIdentifier));

            node.Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.StateLeftCurlyBracket));

            node.LeftCurlyBracketToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextIntraStateDeclaration(node);

            parentNode.StateDeclarations.Add(node);
        }

        /// <summary>
        /// Visits the next intra-state declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextIntraStateDeclaration(StateDeclarationNode node)
        {
            if (base.TokenStream.Done)
            {
                this.ReportParsingError("Expected \"}\".");
                throw new EndOfTokensException(new List<TokenType>
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
                    base.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    base.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.Entry:
                    this.VisitStateEntryDeclaration(node);
                    base.TokenStream.Index++;
                    break;

                case TokenType.Exit:
                    this.VisitStateExitDeclaration(node);
                    base.TokenStream.Index++;
                    break;

                case TokenType.OnAction:
                    this.VisitStateActionDeclaration(node);
                    base.TokenStream.Index++;
                    break;

                case TokenType.DeferEvent:
                    this.VisitDeferEventsDeclaration(node);
                    base.TokenStream.Index++;
                    break;

                case TokenType.IgnoreEvent:
                    this.VisitIgnoreEventsDeclaration(node);
                    base.TokenStream.Index++;
                    break;

                case TokenType.LeftSquareBracket:
                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.VisitAttributeList();
                    base.TokenStream.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                        TokenType.StateRightCurlyBracket));
                    node.RightCurlyBracketToken = base.TokenStream.Peek();
                    base.CurrentState = "";
                    fixpoint = true;
                    base.TokenStream.Index++;
                    break;

                case TokenType.Private:
                case TokenType.Protected:
                case TokenType.Internal:
                case TokenType.Public:
                    this.ReportParsingError("State actions cannot have modifiers.");
                    break;

                case TokenType.Abstract:
                case TokenType.Virtual:
                    this.ReportParsingError("State actions cannot be abstract or virtual.");
                    break;

                default:
                    this.ReportParsingError("Unexpected token.");
                    break;
            }

            if (!fixpoint)
            {
                this.VisitNextIntraStateDeclaration(node);
            }
        }

        /// <summary>
        /// Visits a state entry declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitStateEntryDeclaration(StateDeclarationNode parentNode)
        {
            var node = new EntryDeclarationNode();
            node.EntryKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            var blockNode = new StatementBlockNode(parentNode.Machine, parentNode);
            this.VisitStatementBlock(blockNode);
            node.StatementBlock = blockNode;

            parentNode.EntryDeclaration = node;
        }

        /// <summary>
        /// Visits a state exit declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitStateExitDeclaration(StateDeclarationNode parentNode)
        {
            var node = new ExitDeclarationNode();
            node.ExitKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            var blockNode = new StatementBlockNode(parentNode.Machine, parentNode);
            this.VisitStatementBlock(blockNode);
            node.StatementBlock = blockNode;

            parentNode.ExitDeclaration = node;
        }

        /// <summary>
        /// Visits a state action declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitStateActionDeclaration(StateDeclarationNode parentNode)
        {
            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                base.TokenStream.Peek().Type != TokenType.DefaultEvent))
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent,
                    TokenType.DefaultEvent
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Identifier)
            {
                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                    TokenType.EventIdentifier));
            }

            var eventIdentifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.DoAction &&
                base.TokenStream.Peek().Type != TokenType.GotoState))
            {
                this.ReportParsingError("Expected \"do\" or \"goto\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.DoAction,
                    TokenType.GotoState
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.DoAction)
            {
                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected action identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                    TokenType.ActionIdentifier));

                var actionIdentifier = base.TokenStream.Peek();
                if (!parentNode.AddActionBinding(eventIdentifier, actionIdentifier))
                {
                    this.ReportParsingError("Unexpected action identifier.");
                }

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.Semicolon)
                {
                    this.ReportParsingError("Expected \";\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Semicolon
                    });
                }
            }
            else if (base.TokenStream.Peek().Type == TokenType.GotoState)
            {
                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected state identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                    TokenType.StateIdentifier));

                var stateIdentifier = base.TokenStream.Peek();
                if (!parentNode.AddGotoStateTransition(eventIdentifier, stateIdentifier))
                {
                    this.ReportParsingError("Unexpected state identifier.");
                }

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.Semicolon)
                {
                    this.ReportParsingError("Expected \";\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Semicolon
                    });
                }
            }
        }

        /// <summary>
        /// Visits a defer events declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitDeferEventsDeclaration(StateDeclarationNode parentNode)
        {
            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                base.TokenStream.Peek().Type != TokenType.DefaultEvent))
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent,
                    TokenType.DefaultEvent
                });
            }

            bool expectsComma = false;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                if (!expectsComma &&
                    (base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                    base.TokenStream.Peek().Type != TokenType.DefaultEvent))
                {
                    this.ReportParsingError("Expected event identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.HaltEvent,
                        TokenType.DefaultEvent
                    });
                }

                if (expectsComma && base.TokenStream.Peek().Type != TokenType.Comma)
                {
                    this.ReportParsingError("Expected \",\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Comma
                    });
                }

                if (base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                        TokenType.EventIdentifier));

                    if (!parentNode.AddDeferredEvent(base.TokenStream.Peek()))
                    {
                        this.ReportParsingError("Unexpected event identifier.");
                    }

                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.HaltEvent ||
                    base.TokenStream.Peek().Type == TokenType.DefaultEvent)
                {
                    if (!parentNode.AddDeferredEvent(base.TokenStream.Peek()))
                    {
                        this.ReportParsingError("Unexpected event identifier.");
                    }

                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    expectsComma = false;
                }

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }
        }

        /// <summary>
        /// Visits an ignore events declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitIgnoreEventsDeclaration(StateDeclarationNode parentNode)
        {
            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                base.TokenStream.Peek().Type != TokenType.DefaultEvent))
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent,
                    TokenType.DefaultEvent
                });
            }

            bool expectsComma = false;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                if (!expectsComma &&
                    (base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                    base.TokenStream.Peek().Type != TokenType.DefaultEvent))
                {
                    this.ReportParsingError("Expected event identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.HaltEvent,
                        TokenType.DefaultEvent
                    });
                }

                if (expectsComma && base.TokenStream.Peek().Type != TokenType.Comma)
                {
                    this.ReportParsingError("Expected \",\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Comma
                    });
                }

                if (base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                        TokenType.EventIdentifier));

                    if (!parentNode.AddIgnoredEvent(base.TokenStream.Peek()))
                    {
                        this.ReportParsingError("Unexpected event identifier.");
                    }

                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.HaltEvent ||
                    base.TokenStream.Peek().Type == TokenType.DefaultEvent)
                {
                    if (!parentNode.AddDeferredEvent(base.TokenStream.Peek()))
                    {
                        this.ReportParsingError("Unexpected event identifier.");
                    }

                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    expectsComma = false;
                }

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }
        }

        /// <summary>
        /// Visits an action declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modifier">Modifier</param>
        /// <param name="inheritanceModifier">Inheritance modifier</param>
        private void VisitActionDeclaration(MachineDeclarationNode parentNode, Token modifier,
            Token inheritanceModifier)
        {
            var node = new ActionDeclarationNode();
            node.Modifier = modifier;
            node.InheritanceModifier = inheritanceModifier;
            node.ActionKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected action identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                TokenType.ActionIdentifier));

            node.Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \"{\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftAngleBracket,
                    TokenType.Identifier
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
            {
                var blockNode = new StatementBlockNode(parentNode, null);
                this.VisitStatementBlock(blockNode);
                node.StatementBlock = blockNode;
            }
            else if (base.TokenStream.Peek().Type == TokenType.Semicolon)
            {
                node.SemicolonToken = base.TokenStream.Peek();
            }

            parentNode.ActionDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a field or method declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modifier">Modifier</param>
        /// <param name="inheritanceModifier">Inheritance modifier</param>
        private void VisitFieldOrMethodDeclaration(MachineDeclarationNode parentNode, Token modifier,
            Token inheritanceModifier)
        {
            TextUnit textUnit = null;
            this.VisitTypeIdentifier(ref textUnit);
            var typeIdentifier = new Token(textUnit, TokenType.TypeIdentifier);

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected field or method identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            var identifierToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                    base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \"(\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis,
                    TokenType.Semicolon
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
            {
                this.VisitMethodDeclaration(parentNode, modifier, inheritanceModifier, typeIdentifier, identifierToken);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Semicolon)
            {
                if (inheritanceModifier != null)
                {
                    this.ReportParsingError("A field declaration cannot have the abstract, virtual or override modifier.");
                }

                var node = new FieldDeclarationNode(parentNode);
                node.Modifier = modifier;
                node.TypeIdentifier = typeIdentifier;
                node.Identifier = identifierToken;
                node.SemicolonToken = base.TokenStream.Peek();

                parentNode.FieldDeclarations.Add(node);
            }
        }

        /// <summary>
        /// Visits a method declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modifier">Modifier</param>
        /// <param name="inheritanceModifier">Inheritance modifier</param>
        /// <param name="typeIdentifier">TypeIdentifier</param>
        /// <param name="identifier">Identifier</param>
        private void VisitMethodDeclaration(MachineDeclarationNode parentNode, Token modifier,
            Token inheritanceModifier, Token typeIdentifier, Token identifier)
        {
            var node = new MethodDeclarationNode();
            node.Modifier = modifier;
            node.InheritanceModifier = inheritanceModifier;
            node.TypeIdentifier = typeIdentifier;
            node.Identifier = identifier;

            node.LeftParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit));

                node.Parameters.Add(base.TokenStream.Peek());

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            node.RightParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \"{\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket,
                    TokenType.Semicolon
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
            {
                var blockNode = new StatementBlockNode(parentNode, null);
                this.VisitStatementBlock(blockNode);
                node.StatementBlock = blockNode;
            }
            else if (base.TokenStream.Peek().Type == TokenType.Semicolon)
            {
                node.SemicolonToken = base.TokenStream.Peek();
            }

            parentNode.MethodDeclarations.Add(node);
        }

        /// <summary>
        /// Visits the statement block.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitStatementBlock(StatementBlockNode node)
        {
            node.LeftCurlyBracketToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextStatement(node);
        }

        /// <summary>
        /// Visits the next statement.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextStatement(StatementBlockNode node)
        {
            if (base.TokenStream.Done)
            {
                this.ReportParsingError("Expected \"}\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.IfCondition,
                    TokenType.DoLoop,
                    TokenType.ForLoop,
                    TokenType.ForeachLoop,
                    TokenType.WhileLoop,
                    TokenType.Break,
                    TokenType.Continue,
                    TokenType.Return,
                    TokenType.New,
                    TokenType.CreateMachine,
                    TokenType.RaiseEvent,
                    TokenType.SendEvent,
                    TokenType.DeleteMachine,
                    TokenType.Assert
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
                    base.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.New:
                    this.VisitNewStatement(node);
                    break;

                case TokenType.CreateMachine:
                    this.VisitCreateStatement(node);
                    break;

                case TokenType.RaiseEvent:
                    this.VisitRaiseStatement(node);
                    break;

                case TokenType.SendEvent:
                    this.VisitSendStatement(node);
                    break;

                case TokenType.DeleteMachine:
                    this.VisitDeleteStatement(node);
                    break;

                case TokenType.Assert:
                    this.VisitAssertStatement(node);
                    break;

                case TokenType.IfCondition:
                    this.VisitIfStatement(node);
                    break;
                    
                case TokenType.Break:
                case TokenType.Continue:
                case TokenType.Return:
                case TokenType.This:
                case TokenType.Base:
                case TokenType.Var:
                case TokenType.Int:
                case TokenType.Bool:
                case TokenType.Identifier:
                    this.VisitGenericStatement(node);
                    break;

                case TokenType.RightCurlyBracket:
                    node.RightCurlyBracketToken = base.TokenStream.Peek();
                    fixpoint = true;
                    base.TokenStream.Index++;
                    break;

                default:
                    this.ReportParsingError("Unexpected token.");
                    break;
            }

            if (!fixpoint)
            {
                this.VisitNextStatement(node);
            }
        }

        /// <summary>
        /// Visits a new statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitNewStatement(StatementBlockNode parentNode)
        {
            var node = new NewStatementNode(parentNode);
            node.NewKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected type identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            TextUnit textUnit = null;
            this.VisitTypeIdentifier(ref textUnit);
            var typeIdentifier = new Token(textUnit, TokenType.TypeIdentifier);
            node.TypeIdentifier = typeIdentifier;

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis)
            {
                this.ReportParsingError("Expected \"(\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
            {
                node.LeftParenthesisToken = base.TokenStream.Peek();

                var arguments = new ExpressionNode(parentNode);
                this.VisitArgumentsList(arguments);

                node.Arguments = arguments;
                node.RightParenthesisToken = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Peek().Type == TokenType.Semicolon)
            {
                node.SemicolonToken = base.TokenStream.Peek();
                parentNode.Statements.Add(node);
                base.TokenStream.Index++;
            }
        }

        /// <summary>
        /// Visits a create statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitCreateStatement(StatementBlockNode parentNode)
        {
            var node = new CreateStatementNode(parentNode);
            node.CreateKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected machine identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                if (base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.Dot &&
                    base.TokenStream.Peek().Type != TokenType.NewLine)
                {
                    this.ReportParsingError("Expected machine identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.Dot
                    });
                }

                if (base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                        TokenType.MachineIdentifier));
                }

                node.MachineIdentifier.Add(base.TokenStream.Peek());

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis)
            {
                this.ReportParsingError("Expected \"(\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis
                });
            }

            node.LeftParenthesisToken = base.TokenStream.Peek();

            var payload = new ExpressionNode(parentNode);
            this.VisitArgumentsList(payload);

            node.Payload = payload;
            node.RightParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.TokenStream.Peek();
            parentNode.Statements.Add(node);
            base.TokenStream.Index++;
        }

        /// <summary>
        /// Visits a raise statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitRaiseStatement(StatementBlockNode parentNode)
        {
            var node = new RaiseStatementNode(parentNode);
            node.RaiseKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.HaltEvent))
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Identifier)
            {
                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                    TokenType.EventIdentifier));
            }

            node.EventIdentifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \"(\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis,
                    TokenType.Semicolon
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
            {
                node.LeftParenthesisToken = base.TokenStream.Peek();

                var payload = new ExpressionNode(parentNode);
                this.VisitArgumentsList(payload);

                node.Payload = payload;
                node.RightParenthesisToken = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.TokenStream.Peek();
            parentNode.Statements.Add(node);
            base.TokenStream.Index++;
        }

        /// <summary>
        /// Visits a send statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitSendStatement(StatementBlockNode parentNode)
        {
            var node = new SendStatementNode(parentNode);
            node.SendKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.HaltEvent))
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Identifier)
            {
                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                    TokenType.EventIdentifier));
            }

            node.EventIdentifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                base.TokenStream.Peek().Type != TokenType.ToMachine))
            {
                this.ReportParsingError("Expected \"(\" or \"to\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis,
                    TokenType.ToMachine
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
            {
                node.LeftParenthesisToken = base.TokenStream.Peek();

                var payload = new ExpressionNode(parentNode);
                this.VisitArgumentsList(payload);

                node.Payload = payload;
                node.RightParenthesisToken = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.ToMachine)
            {
                this.ReportParsingError("Expected \"to\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.ToMachine
                });
            }

            node.ToKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.This))
            {
                this.ReportParsingError("Expected machine identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.This
                });
            }

            var machineIdentifier = new ExpressionNode(parentNode);
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                if (base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.This &&
                    base.TokenStream.Peek().Type != TokenType.Dot &&
                    base.TokenStream.Peek().Type != TokenType.NewLine)
                {
                    this.ReportParsingError("Expected machine identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.This,
                        TokenType.Dot
                    });
                }

                machineIdentifier.StmtTokens.Add(base.TokenStream.Peek());

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            node.MachineIdentifier = machineIdentifier;

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.TokenStream.Peek();
            parentNode.Statements.Add(node);
            base.TokenStream.Index++;
        }

        /// <summary>
        /// Visits an if statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitIfStatement(StatementBlockNode parentNode)
        {
            var node = new IfStatementNode(parentNode);
            node.IfKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis)
            {
                this.ReportParsingError("Expected \"(\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis
                });
            }

            node.LeftParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            var guard = new ExpressionNode(parentNode);
            
            int counter = 1;
            while (!base.TokenStream.Done)
            {
                if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
                {
                    counter++;
                }
                else if (base.TokenStream.Peek().Type == TokenType.RightParenthesis)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                guard.StmtTokens.Add(base.TokenStream.Peek());
                base.TokenStream.Index++;
                base.SkipCommentTokens();
            }

            node.Guard = guard;

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                this.ReportParsingError("Expected \")\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.RightParenthesis
                });
            }

            node.RightParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.New &&
                base.TokenStream.Peek().Type != TokenType.CreateMachine &&
                base.TokenStream.Peek().Type != TokenType.RaiseEvent &&
                base.TokenStream.Peek().Type != TokenType.SendEvent &&
                base.TokenStream.Peek().Type != TokenType.Assert &&
                base.TokenStream.Peek().Type != TokenType.IfCondition &&
                base.TokenStream.Peek().Type != TokenType.Break &&
                base.TokenStream.Peek().Type != TokenType.Continue &&
                base.TokenStream.Peek().Type != TokenType.Return &&
                base.TokenStream.Peek().Type != TokenType.This &&
                base.TokenStream.Peek().Type != TokenType.Base &&
                base.TokenStream.Peek().Type != TokenType.Var &&
                base.TokenStream.Peek().Type != TokenType.Int &&
                base.TokenStream.Peek().Type != TokenType.Bool &&
                base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            var blockNode = new StatementBlockNode(parentNode.Machine, parentNode.State);
            
            if (base.TokenStream.Peek().Type == TokenType.New)
            {
                this.VisitNewStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.CreateMachine)
            {
                this.VisitCreateStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.RaiseEvent)
            {
                this.VisitRaiseStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.SendEvent)
            {
                this.VisitSendStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Assert)
            {
                this.VisitAssertStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.IfCondition)
            {
                this.VisitIfStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Break ||
                base.TokenStream.Peek().Type == TokenType.Continue ||
                base.TokenStream.Peek().Type == TokenType.Return ||
                base.TokenStream.Peek().Type == TokenType.This ||
                base.TokenStream.Peek().Type == TokenType.Base ||
                base.TokenStream.Peek().Type == TokenType.Var ||
                base.TokenStream.Peek().Type == TokenType.Int ||
                base.TokenStream.Peek().Type == TokenType.Bool ||
                base.TokenStream.Peek().Type == TokenType.Identifier)
            {
                this.VisitGenericStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
            {
                this.VisitStatementBlock(blockNode);
            }
            
            node.StatementBlock = blockNode;

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Peek().Type == TokenType.ElseCondition)
            {
                node.ElseKeyword = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.New &&
                    base.TokenStream.Peek().Type != TokenType.CreateMachine &&
                    base.TokenStream.Peek().Type != TokenType.RaiseEvent &&
                    base.TokenStream.Peek().Type != TokenType.SendEvent &&
                    base.TokenStream.Peek().Type != TokenType.Assert &&
                    base.TokenStream.Peek().Type != TokenType.IfCondition &&
                    base.TokenStream.Peek().Type != TokenType.Break &&
                    base.TokenStream.Peek().Type != TokenType.Continue &&
                    base.TokenStream.Peek().Type != TokenType.Return &&
                    base.TokenStream.Peek().Type != TokenType.This &&
                    base.TokenStream.Peek().Type != TokenType.Base &&
                    base.TokenStream.Peek().Type != TokenType.Var &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
                {
                    this.ReportParsingError("Expected \"{\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.LeftCurlyBracket
                    });
                }

                var elseBlockNode = new StatementBlockNode(parentNode.Machine, parentNode.State);

                if (base.TokenStream.Peek().Type == TokenType.New)
                {
                    this.VisitNewStatement(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.CreateMachine)
                {
                    this.VisitCreateStatement(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.RaiseEvent)
                {
                    this.VisitRaiseStatement(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.SendEvent)
                {
                    this.VisitSendStatement(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.Assert)
                {
                    this.VisitAssertStatement(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.IfCondition)
                {
                    this.VisitIfStatement(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.Break ||
                    base.TokenStream.Peek().Type == TokenType.Continue ||
                    base.TokenStream.Peek().Type == TokenType.Return ||
                    base.TokenStream.Peek().Type == TokenType.This ||
                    base.TokenStream.Peek().Type == TokenType.Base ||
                    base.TokenStream.Peek().Type == TokenType.Var ||
                    base.TokenStream.Peek().Type == TokenType.Int ||
                    base.TokenStream.Peek().Type == TokenType.Bool ||
                    base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    this.VisitGenericStatement(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
                {
                    this.VisitStatementBlock(elseBlockNode);
                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                }

                node.ElseStatementBlock = elseBlockNode;
            }

            parentNode.Statements.Add(node);
        }

        /// <summary>
        /// Visits a delete statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitDeleteStatement(StatementBlockNode parentNode)
        {
            var node = new DeleteStatementNode(parentNode);
            node.DeleteKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.TokenStream.Peek();
            parentNode.Statements.Add(node);
            base.TokenStream.Index++;
        }

        /// <summary>
        /// Visits an assert statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitAssertStatement(StatementBlockNode parentNode)
        {
            var node = new AssertStatementNode(parentNode);
            node.AssertKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis)
            {
                this.ReportParsingError("Expected \"(\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis
                });
            }

            node.LeftParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            var predicate = new ExpressionNode(parentNode);
            int counter = 1;

            while (!base.TokenStream.Done)
            {
                if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
                {
                    counter++;
                }
                else if (base.TokenStream.Peek().Type == TokenType.RightParenthesis)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                predicate.StmtTokens.Add(base.TokenStream.Peek());
                base.TokenStream.Index++;
                base.SkipCommentTokens();
            }

            node.Predicate = predicate;

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                this.ReportParsingError("Expected \")\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.RightParenthesis
                });
            }

            node.RightParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.TokenStream.Peek();
            parentNode.Statements.Add(node);
            base.TokenStream.Index++;
        }

        /// <summary>
        /// Visits a generic statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitGenericStatement(StatementBlockNode parentNode)
        {
            var node = new GenericStatementNode(parentNode);

            var expression = new ExpressionNode(parentNode);
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                if (base.TokenStream.Peek().Type == TokenType.New ||
                    base.TokenStream.Peek().Type == TokenType.CreateMachine)
                {
                    node.Expression = expression;
                    parentNode.Statements.Add(node);
                    return;
                }

                expression.StmtTokens.Add(base.TokenStream.Peek());
                base.TokenStream.Index++;
                base.SkipCommentTokens();
            }

            node.Expression = expression;
            node.SemicolonToken = base.TokenStream.Peek();

            parentNode.Statements.Add(node);
            base.TokenStream.Index++;
        }

        /// <summary>
        /// Visits a type identifier.
        /// </summary>
        /// <param name="textUnit">TextUnit</param>
        private void VisitTypeIdentifier(ref TextUnit textUnit)
        {
            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Int &&
                base.TokenStream.Peek().Type != TokenType.Bool &&
                base.TokenStream.Peek().Type != TokenType.Identifier))
            {
                this.ReportParsingError("Expected type identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            var position = base.TokenStream.Peek().TextUnit.Start;
            var line = base.TokenStream.Peek().TextUnit.Line;

            bool expectsDot = false;
            while (!base.TokenStream.Done)
            {
                if (!expectsDot &&
                    (base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Identifier) ||
                    (expectsDot && base.TokenStream.Peek().Type != TokenType.Dot))
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.Int ||
                    base.TokenStream.Peek().Type == TokenType.Bool ||
                    base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    expectsDot = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Dot)
                {
                    expectsDot = false;
                }

                if (textUnit == null)
                {
                    textUnit = new TextUnit(base.TokenStream.Peek().TextUnit.Text,
                        line, position);
                }
                else
                {
                    textUnit = new TextUnit(textUnit.Text + base.TokenStream.Peek().TextUnit.Text,
                        line, position);
                }

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
            {
                textUnit = new TextUnit(textUnit.Text + base.TokenStream.Peek().TextUnit.Text,
                    line, position);

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                int counter = 1;
                while (!base.TokenStream.Done)
                {
                    if (base.TokenStream.Peek().Type == TokenType.LeftAngleBracket)
                    {
                        counter++;
                    }
                    else if (base.TokenStream.Peek().Type == TokenType.RightAngleBracket)
                    {
                        counter--;
                    }

                    if (counter == 0 ||
                        (base.TokenStream.Peek().Type != TokenType.Int &&
                        base.TokenStream.Peek().Type != TokenType.Bool &&
                        base.TokenStream.Peek().Type != TokenType.Identifier &&
                        base.TokenStream.Peek().Type != TokenType.Dot &&
                        base.TokenStream.Peek().Type != TokenType.Comma &&
                        base.TokenStream.Peek().Type != TokenType.LeftSquareBracket &&
                        base.TokenStream.Peek().Type != TokenType.RightSquareBracket &&
                        base.TokenStream.Peek().Type != TokenType.LeftAngleBracket &&
                        base.TokenStream.Peek().Type != TokenType.RightAngleBracket))
                    {
                        break;
                    }

                    textUnit = new TextUnit(textUnit.Text + base.TokenStream.Peek().TextUnit.Text,
                        line, position);

                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                }

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.RightAngleBracket)
                {
                    this.ReportParsingError("Expected \">\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.RightAngleBracket
                    });
                }

                textUnit = new TextUnit(textUnit.Text + base.TokenStream.Peek().TextUnit.Text,
                    line, position);

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }
        }

        /// <summary>
        /// Visits an attribute list.
        /// </summary>
        private void VisitAttributeList()
        {
            int counter = 1;
            while (!base.TokenStream.Done)
            {
                if (base.TokenStream.Peek().Type == TokenType.LeftSquareBracket)
                {
                    counter++;
                }
                else if (base.TokenStream.Peek().Type == TokenType.RightSquareBracket)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit));

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.RightSquareBracket)
            {
                this.ReportParsingError("Expected \"]\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.RightSquareBracket
                });
            }
        }

        /// <summary>
        /// Visits an argument list.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitArgumentsList(ExpressionNode node)
        {
            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            int counter = 1;
            while (!base.TokenStream.Done)
            {
                if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
                {
                    counter++;
                }
                else if (base.TokenStream.Peek().Type == TokenType.RightParenthesis)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                node.StmtTokens.Add(base.TokenStream.Peek());

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                this.ReportParsingError("Expected \")\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.RightParenthesis
                });
            }
        }

        #endregion
    }
}
