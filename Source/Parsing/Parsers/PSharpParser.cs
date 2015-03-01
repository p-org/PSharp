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
            if (base.Index == base.Tokens.Count)
            {
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Using,
                    TokenType.NamespaceDecl
                });
            }

            var token = base.Tokens[base.Index];
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.Comment:
                case TokenType.NewLine:
                    base.Index++;
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
                    base.Index++;
                    break;

                case TokenType.NamespaceDecl:
                    this.VisitNamespaceDeclaration();
                    base.Index++;
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
            node.UsingKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (base.Tokens[base.Index].Type != TokenType.Identifier &&
                    base.Tokens[base.Index].Type != TokenType.Dot &&
                    base.Tokens[base.Index].Type != TokenType.NewLine)
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
                    node.IdentifierTokens.Add(base.Tokens[base.Index]);
                }

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.Tokens[base.Index];

            (this.Program as PSharpProgram).UsingDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a namespace declaration.
        /// </summary>
        private void VisitNamespaceDeclaration()
        {
            var node = new NamespaceDeclarationNode();
            node.NamespaceKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected namespace identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                if (base.Tokens[base.Index].Type != TokenType.Identifier &&
                    base.Tokens[base.Index].Type != TokenType.Dot &&
                    base.Tokens[base.Index].Type != TokenType.NewLine)
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
                    node.IdentifierTokens.Add(base.Tokens[base.Index]);
                }

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            node.LeftCurlyBracketToken = base.Tokens[base.Index];

            base.Index++;
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
            if (base.Index == base.Tokens.Count)
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
            var token = base.Tokens[base.Index];
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.Comment:
                case TokenType.NewLine:
                    base.Index++;
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
                    base.Index++;
                    break;

                case TokenType.MainMachine:
                    this.VisitMainMachineModifier(node, null);
                    base.Index++;
                    break;

                case TokenType.MachineDecl:
                    this.VisitMachineDeclaration(node, false, null, null);
                    base.Index++;
                    break;

                case TokenType.Internal:
                case TokenType.Public:
                    this.VisitTopLevelAccessModifier(node);
                    base.Index++;
                    break;

                case TokenType.Abstract:
                case TokenType.Virtual:
                    this.VisitTopLevelAbstractModifier(node);
                    base.Index++;
                    break;

                case TokenType.LeftSquareBracket:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.VisitAttributeList();
                    base.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    node.RightCurlyBracketToken = base.Tokens[base.Index];
                    fixpoint = true;
                    base.Index++;
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
            var modifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Abstract &&
                base.Tokens[base.Index].Type != TokenType.Virtual &&
                base.Tokens[base.Index].Type != TokenType.MainMachine &&
                base.Tokens[base.Index].Type != TokenType.EventDecl &&
                base.Tokens[base.Index].Type != TokenType.MachineDecl))
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
            if (base.Tokens[base.Index].Type == TokenType.Abstract)
            {
                abstractModifier = base.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Index == base.Tokens.Count ||
                    base.Tokens[base.Index].Type != TokenType.MachineDecl)
                {
                    this.ReportParsingError("Expected machine declaration.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.MachineDecl
                    });
                }

                this.VisitMachineDeclaration(parentNode, false, modifier, abstractModifier);
            }
            else if (base.Tokens[base.Index].Type == TokenType.EventDecl)
            {
                this.VisitEventDeclaration(parentNode, modifier);
            }
            else if (base.Tokens[base.Index].Type == TokenType.MainMachine)
            {
                this.VisitMainMachineModifier(parentNode, modifier);
            }
            else if (base.Tokens[base.Index].Type == TokenType.MachineDecl)
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
            var abstractModifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Internal &&
                base.Tokens[base.Index].Type != TokenType.Public &&
                base.Tokens[base.Index].Type != TokenType.MachineDecl))
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
            if (base.Tokens[base.Index].Type == TokenType.Internal ||
                base.Tokens[base.Index].Type == TokenType.Public)
            {
                modifier = base.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Index == base.Tokens.Count ||
                    (base.Tokens[base.Index].Type != TokenType.MachineDecl))
                {
                    this.ReportParsingError("Expected machine declaration.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.MachineDecl
                    });
                }
            }

            if (base.Tokens[base.Index].Type == TokenType.EventDecl)
            {
                this.VisitEventDeclaration(parentNode, modifier);
            }
            else if (base.Tokens[base.Index].Type == TokenType.MachineDecl)
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
            node.EventKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                TokenType.EventIdentifier);

            node.Identifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.Tokens[base.Index];

            parentNode.EventDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a main machine modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modifier">Modifier</param>
        private void VisitMainMachineModifier(NamespaceDeclarationNode parentNode, Token modifier)
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.MachineDecl)
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
            var node = new MachineDeclarationNode(isMain);
            node.Modifier = modifier;
            node.AbstractModifier = abstractModifier;
            node.MachineKeyword = base.Tokens[base.Index];
            
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected machine identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.CurrentMachine = base.Tokens[base.Index].Text;
            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                TokenType.MachineIdentifier);

            node.Identifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Colon &&
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket))
            {
                this.ReportParsingError("Expected \":\" or \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Colon,
                    TokenType.LeftCurlyBracket
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.Colon)
            {
                node.ColonToken = base.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Index == base.Tokens.Count ||
                    base.Tokens[base.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected base machine identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                while (base.Index < base.Tokens.Count &&
                    base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
                {
                    if (base.Tokens[base.Index].Type != TokenType.Identifier &&
                        base.Tokens[base.Index].Type != TokenType.Dot &&
                        base.Tokens[base.Index].Type != TokenType.NewLine)
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
                        node.BaseNameTokens.Add(base.Tokens[base.Index]);
                    }

                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                }
            }

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                TokenType.MachineLeftCurlyBracket);

            node.LeftCurlyBracketToken = base.Tokens[base.Index];

            base.Index++;
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
            if (base.Index == base.Tokens.Count)
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
            var token = base.Tokens[base.Index];
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.Comment:
                case TokenType.NewLine:
                    base.Index++;
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
                    base.Index++;
                    break;

                case TokenType.StateDecl:
                    this.VisitStateDeclaration(node, false, null);
                    base.Index++;
                    break;

                case TokenType.ActionDecl:
                    this.VisitActionDeclaration(node, null, null);
                    base.Index++;
                    break;

                case TokenType.Identifier:
                    this.VisitFieldOrMethodDeclaration(node, null, null);
                    base.Index++;
                    break;

                case TokenType.Private:
                case TokenType.Protected:
                    this.VisitMachineLevelAccessModifier(node);
                    base.Index++;
                    break;

                case TokenType.Abstract:
                case TokenType.Virtual:
                    this.VisitMachineLevelAbstractModifier(node);
                    base.Index++;
                    break;

                case TokenType.LeftSquareBracket:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.VisitAttributeList();
                    base.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                        TokenType.MachineRightCurlyBracket);
                    node.RightCurlyBracketToken = base.Tokens[base.Index];
                    base.CurrentMachine = "";
                    fixpoint = true;
                    base.Index++;
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
            var modifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Abstract &&
                base.Tokens[base.Index].Type != TokenType.Virtual &&
                base.Tokens[base.Index].Type != TokenType.Override &&
                base.Tokens[base.Index].Type != TokenType.StartState &&
                base.Tokens[base.Index].Type != TokenType.StateDecl &&
                base.Tokens[base.Index].Type != TokenType.ActionDecl &&
                base.Tokens[base.Index].Type != TokenType.Int &&
                base.Tokens[base.Index].Type != TokenType.Bool &&
                base.Tokens[base.Index].Type != TokenType.Identifier))
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

            if (base.Tokens[base.Index].Type == TokenType.Abstract ||
                base.Tokens[base.Index].Type == TokenType.Virtual ||
                base.Tokens[base.Index].Type == TokenType.Override)
            {
                var inheritanceModifier = base.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Index == base.Tokens.Count ||
                    (base.Tokens[base.Index].Type != TokenType.ActionDecl &&
                    base.Tokens[base.Index].Type != TokenType.Int &&
                    base.Tokens[base.Index].Type != TokenType.Bool &&
                    base.Tokens[base.Index].Type != TokenType.Identifier))
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

                if (base.Tokens[base.Index].Type == TokenType.ActionDecl)
                {
                    this.VisitActionDeclaration(parentNode, modifier, inheritanceModifier);
                }
                else if (base.Tokens[base.Index].Type == TokenType.Int ||
                    base.Tokens[base.Index].Type == TokenType.Bool ||
                    base.Tokens[base.Index].Type == TokenType.Identifier)
                {
                    this.VisitFieldOrMethodDeclaration(parentNode, modifier, inheritanceModifier);
                }
            }
            else if (base.Tokens[base.Index].Type == TokenType.StartState)
            {
                this.VisitStartStateModifier(parentNode, modifier);
            }
            else if (base.Tokens[base.Index].Type == TokenType.StateDecl)
            {
                this.VisitStateDeclaration(parentNode, false, modifier);
            }
            else if (base.Tokens[base.Index].Type == TokenType.ActionDecl)
            {
                this.VisitActionDeclaration(parentNode, modifier, null);
            }
            else if (base.Tokens[base.Index].Type == TokenType.Int ||
                base.Tokens[base.Index].Type == TokenType.Bool ||
                base.Tokens[base.Index].Type == TokenType.Identifier)
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
            var abstractModifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
        }

        /// <summary>
        /// Visits a start state modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modifier">Modifier</param>
        private void VisitStartStateModifier(MachineDeclarationNode parentNode, Token modifier)
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.StateDecl)
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
            node.StateKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected state identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.CurrentState = base.Tokens[base.Index].Text;
            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                TokenType.StateIdentifier);

            node.Identifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                TokenType.StateLeftCurlyBracket);

            node.LeftCurlyBracketToken = base.Tokens[base.Index];

            base.Index++;
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
            if (base.Index == base.Tokens.Count)
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
            var token = base.Tokens[base.Index];
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.Comment:
                case TokenType.NewLine:
                    base.Index++;
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
                    base.Index++;
                    break;

                case TokenType.Exit:
                    this.VisitStateExitDeclaration(node);
                    base.Index++;
                    break;

                case TokenType.OnAction:
                    this.VisitStateActionDeclaration(node);
                    base.Index++;
                    break;

                case TokenType.DeferEvent:
                    this.VisitDeferEventsDeclaration(node);
                    base.Index++;
                    break;

                case TokenType.IgnoreEvent:
                    this.VisitIgnoreEventsDeclaration(node);
                    base.Index++;
                    break;

                case TokenType.LeftSquareBracket:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.VisitAttributeList();
                    base.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                        TokenType.StateRightCurlyBracket);
                    node.RightCurlyBracketToken = base.Tokens[base.Index];
                    base.CurrentState = "";
                    fixpoint = true;
                    base.Index++;
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
            node.EntryKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
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
            node.ExitKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
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
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                TokenType.EventIdentifier);

            var eventIdentifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.DoAction &&
                base.Tokens[base.Index].Type != TokenType.GotoState))
            {
                this.ReportParsingError("Expected \"do\" or \"goto\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.DoAction,
                    TokenType.GotoState
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.DoAction)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Index == base.Tokens.Count ||
                    base.Tokens[base.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected action identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                    TokenType.ActionIdentifier);

                var actionIdentifier = base.Tokens[base.Index];
                if (!parentNode.AddActionBinding(eventIdentifier, actionIdentifier))
                {
                    this.ReportParsingError("Unexpected action identifier.");
                }

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Index == base.Tokens.Count ||
                    base.Tokens[base.Index].Type != TokenType.Semicolon)
                {
                    this.ReportParsingError("Expected \";\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Semicolon
                    });
                }
            }
            else if (base.Tokens[base.Index].Type == TokenType.GotoState)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Index == base.Tokens.Count ||
                    base.Tokens[base.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected state identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                    TokenType.StateIdentifier);

                var stateIdentifier = base.Tokens[base.Index];
                if (!parentNode.AddStateTransition(eventIdentifier, stateIdentifier))
                {
                    this.ReportParsingError("Unexpected state identifier.");
                }

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Index == base.Tokens.Count ||
                    base.Tokens[base.Index].Type != TokenType.Semicolon)
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
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            bool expectsComma = false;
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (!expectsComma && base.Tokens[base.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected event identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                if (expectsComma && base.Tokens[base.Index].Type != TokenType.Comma)
                {
                    this.ReportParsingError("Expected \",\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Comma
                    });
                }

                if (base.Tokens[base.Index].Type == TokenType.Identifier)
                {
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                        TokenType.EventIdentifier);

                    if (!parentNode.AddDeferredEvent(base.Tokens[base.Index]))
                    {
                        this.ReportParsingError("Unexpected event identifier.");
                    }

                    expectsComma = true;
                }
                else if (base.Tokens[base.Index].Type == TokenType.Comma)
                {
                    expectsComma = false;
                }

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Semicolon)
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
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            bool expectsComma = false;
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (!expectsComma && base.Tokens[base.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected event identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                if (expectsComma && base.Tokens[base.Index].Type != TokenType.Comma)
                {
                    this.ReportParsingError("Expected \",\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Comma
                    });
                }

                if (base.Tokens[base.Index].Type == TokenType.Identifier)
                {
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                        TokenType.EventIdentifier);

                    if (!parentNode.AddIgnoredEvent(base.Tokens[base.Index]))
                    {
                        this.ReportParsingError("Unexpected event identifier.");
                    }

                    expectsComma = true;
                }
                else if (base.Tokens[base.Index].Type == TokenType.Comma)
                {
                    expectsComma = false;
                }

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Semicolon)
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
            node.ActionKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected action identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                TokenType.ActionIdentifier);

            node.Identifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket &&
                base.Tokens[base.Index].Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \"{\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftAngleBracket,
                    TokenType.Identifier
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                var blockNode = new StatementBlockNode(parentNode, null);
                this.VisitStatementBlock(blockNode);
                node.StatementBlock = blockNode;
            }
            else if (base.Tokens[base.Index].Type == TokenType.Semicolon)
            {
                node.SemicolonToken = base.Tokens[base.Index];
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

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected field or method identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            var identifierToken = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                    (base.Tokens[base.Index].Type != TokenType.LeftParenthesis &&
                    base.Tokens[base.Index].Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \"(\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis,
                    TokenType.Semicolon
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
            {
                this.VisitMethodDeclaration(parentNode, modifier, inheritanceModifier, typeIdentifier, identifierToken);
            }
            else if (base.Tokens[base.Index].Type == TokenType.Semicolon)
            {
                if (inheritanceModifier != null)
                {
                    this.ReportParsingError("A field declaration cannot have the abstract, virtual or override modifier.");
                }

                var node = new FieldDeclarationNode(parentNode);
                node.Modifier = modifier;
                node.TypeIdentifier = typeIdentifier;
                node.Identifier = identifierToken;
                node.SemicolonToken = base.Tokens[base.Index];

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

            node.LeftParenthesisToken = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.RightParenthesis)
            {
                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit);

                node.Parameters.Add(base.Tokens[base.Index]);

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            node.RightParenthesisToken = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket &&
                base.Tokens[base.Index].Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \"{\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket,
                    TokenType.Semicolon
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                var blockNode = new StatementBlockNode(parentNode, null);
                this.VisitStatementBlock(blockNode);
                node.StatementBlock = blockNode;
            }
            else if (base.Tokens[base.Index].Type == TokenType.Semicolon)
            {
                node.SemicolonToken = base.Tokens[base.Index];
            }

            parentNode.MethodDeclarations.Add(node);
        }

        /// <summary>
        /// Visits the statement block.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitStatementBlock(StatementBlockNode node)
        {
            node.LeftCurlyBracketToken = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextStatement(node);
        }

        /// <summary>
        /// Visits the next statement.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextStatement(StatementBlockNode node)
        {
            if (base.Index == base.Tokens.Count)
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
            var token = base.Tokens[base.Index];
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.Comment:
                case TokenType.NewLine:
                    base.Index++;
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

                case TokenType.This:
                case TokenType.Base:
                case TokenType.Var:
                case TokenType.Int:
                case TokenType.Bool:
                case TokenType.Identifier:
                    this.VisitGenericStatement(node);
                    break;

                case TokenType.RightCurlyBracket:
                    node.RightCurlyBracketToken = base.Tokens[base.Index];
                    fixpoint = true;
                    base.Index++;
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
            node.NewKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
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

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.LeftParenthesis)
            {
                this.ReportParsingError("Expected \"(\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
            {
                node.LeftParenthesisToken = base.Tokens[base.Index];

                var arguments = new ExpressionNode(parentNode);
                this.VisitArgumentsList(arguments);

                node.Arguments = arguments;
                node.RightParenthesisToken = base.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Tokens[base.Index].Type == TokenType.Semicolon)
            {
                node.SemicolonToken = base.Tokens[base.Index];
                parentNode.Statements.Add(node);
                base.Index++;
            }
        }

        /// <summary>
        /// Visits a create statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitCreateStatement(StatementBlockNode parentNode)
        {
            var node = new CreateStatementNode(parentNode);
            node.CreateKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected machine identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.LeftParenthesis &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (base.Tokens[base.Index].Type != TokenType.Identifier &&
                    base.Tokens[base.Index].Type != TokenType.Dot &&
                    base.Tokens[base.Index].Type != TokenType.NewLine)
                {
                    this.ReportParsingError("Expected machine identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.Dot
                    });
                }

                if (base.Tokens[base.Index].Type == TokenType.Identifier)
                {
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                        TokenType.MachineIdentifier);
                }

                node.MachineIdentifier.Add(base.Tokens[base.Index]);

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.LeftParenthesis &&
                base.Tokens[base.Index].Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \"(\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis,
                    TokenType.Semicolon
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
            {
                node.LeftParenthesisToken = base.Tokens[base.Index];

                var payload = new ExpressionNode(parentNode);
                this.VisitArgumentsList(payload);

                node.Payload = payload;
                node.RightParenthesisToken = base.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.Tokens[base.Index];
            parentNode.Statements.Add(node);
            base.Index++;
        }

        /// <summary>
        /// Visits a raise statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitRaiseStatement(StatementBlockNode parentNode)
        {
            var node = new RaiseStatementNode(parentNode);
            node.RaiseKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                TokenType.EventIdentifier);

            node.EventIdentifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.LeftParenthesis &&
                base.Tokens[base.Index].Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \"(\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis,
                    TokenType.Semicolon
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
            {
                node.LeftParenthesisToken = base.Tokens[base.Index];

                var payload = new ExpressionNode(parentNode);
                this.VisitArgumentsList(payload);

                node.Payload = payload;
                node.RightParenthesisToken = base.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.Tokens[base.Index];
            parentNode.Statements.Add(node);
            base.Index++;
        }

        /// <summary>
        /// Visits a send statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitSendStatement(StatementBlockNode parentNode)
        {
            var node = new SendStatementNode(parentNode);
            node.SendKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                TokenType.EventIdentifier);

            node.EventIdentifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.LeftParenthesis &&
                base.Tokens[base.Index].Type != TokenType.ToMachine))
            {
                this.ReportParsingError("Expected \"(\" or \"to\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis,
                    TokenType.ToMachine
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
            {
                node.LeftParenthesisToken = base.Tokens[base.Index];

                var payload = new ExpressionNode(parentNode);
                this.VisitArgumentsList(payload);

                node.Payload = payload;
                node.RightParenthesisToken = base.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.ToMachine)
            {
                this.ReportParsingError("Expected \"to\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.ToMachine
                });
            }

            node.ToKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Identifier &&
                base.Tokens[base.Index].Type != TokenType.This))
            {
                this.ReportParsingError("Expected machine identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.This
                });
            }

            var machineIdentifier = new ExpressionNode(parentNode);
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (base.Tokens[base.Index].Type != TokenType.Identifier &&
                    base.Tokens[base.Index].Type != TokenType.This &&
                    base.Tokens[base.Index].Type != TokenType.Dot &&
                    base.Tokens[base.Index].Type != TokenType.NewLine)
                {
                    this.ReportParsingError("Expected machine identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.This,
                        TokenType.Dot
                    });
                }

                machineIdentifier.StmtTokens.Add(base.Tokens[base.Index]);

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            node.MachineIdentifier = machineIdentifier;

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.Tokens[base.Index];
            parentNode.Statements.Add(node);
            base.Index++;
        }

        /// <summary>
        /// Visits an if statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitIfStatement(StatementBlockNode parentNode)
        {
            var node = new IfStatementNode(parentNode);
            node.IfKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.LeftParenthesis)
            {
                this.ReportParsingError("Expected \"(\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis
                });
            }

            node.LeftParenthesisToken = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            var guard = new ExpressionNode(parentNode);


            int counter = 1;
            while (base.Index < base.Tokens.Count)
            {
                if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
                {
                    counter++;
                }
                else if (base.Tokens[base.Index].Type == TokenType.RightParenthesis)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                guard.StmtTokens.Add(base.Tokens[base.Index]);
                base.Index++;
                base.SkipCommentTokens();
            }

            node.Guard = guard;

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.RightParenthesis)
            {
                this.ReportParsingError("Expected \")\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.RightParenthesis
                });
            }

            node.RightParenthesisToken = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            var blockNode = new StatementBlockNode(parentNode.Machine, parentNode.State);
            this.VisitStatementBlock(blockNode);
            node.StatementBlock = blockNode;

            //base.Index++;
            //base.SkipWhiteSpaceAndCommentTokens();

            //if (base.Tokens[base.Index].Type == TokenType.ElseCondition)
            //{

            //}

            parentNode.Statements.Add(node);
            base.Index++;
        }

        /// <summary>
        /// Visits a delete statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitDeleteStatement(StatementBlockNode parentNode)
        {
            var node = new DeleteStatementNode(parentNode);
            node.DeleteKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.Tokens[base.Index];
            parentNode.Statements.Add(node);
            base.Index++;
        }

        /// <summary>
        /// Visits an assert statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitAssertStatement(StatementBlockNode parentNode)
        {
            var node = new AssertStatementNode(parentNode);
            node.AssertKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.LeftParenthesis)
            {
                this.ReportParsingError("Expected \"(\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis
                });
            }

            node.LeftParenthesisToken = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            var predicate = new ExpressionNode(parentNode);
            int counter = 1;

            while (base.Index < base.Tokens.Count)
            {
                if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
                {
                    counter++;
                }
                else if (base.Tokens[base.Index].Type == TokenType.RightParenthesis)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                predicate.StmtTokens.Add(base.Tokens[base.Index]);
                base.Index++;
                base.SkipCommentTokens();
            }

            node.Predicate = predicate;

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.RightParenthesis)
            {
                this.ReportParsingError("Expected \")\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.RightParenthesis
                });
            }

            node.RightParenthesisToken = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = base.Tokens[base.Index];
            parentNode.Statements.Add(node);
            base.Index++;
        }

        /// <summary>
        /// Visits a generic statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitGenericStatement(StatementBlockNode parentNode)
        {
            var node = new GenericStatementNode(parentNode);

            var expression = new ExpressionNode(parentNode);
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (base.Tokens[base.Index].Type == TokenType.New ||
                    base.Tokens[base.Index].Type == TokenType.CreateMachine)
                {
                    node.Expression = expression;
                    parentNode.Statements.Add(node);
                    return;
                }

                expression.StmtTokens.Add(base.Tokens[base.Index]);
                base.Index++;
                base.SkipCommentTokens();
            }

            node.Expression = expression;
            node.SemicolonToken = base.Tokens[base.Index];

            parentNode.Statements.Add(node);
            base.Index++;
        }

        /// <summary>
        /// Visits a type identifier.
        /// </summary>
        /// <param name="textUnit">TextUnit</param>
        private void VisitTypeIdentifier(ref TextUnit textUnit)
        {
            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Int &&
                base.Tokens[base.Index].Type != TokenType.Bool &&
                base.Tokens[base.Index].Type != TokenType.Identifier))
            {
                this.ReportParsingError("Expected type identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            var position = base.Tokens[base.Index].TextUnit.Start;
            var line = base.Tokens[base.Index].TextUnit.Line;

            bool expectsDot = false;
            while (base.Index < base.Tokens.Count)
            {
                if (!expectsDot &&
                    (base.Tokens[base.Index].Type != TokenType.Int &&
                    base.Tokens[base.Index].Type != TokenType.Bool &&
                    base.Tokens[base.Index].Type != TokenType.Identifier) ||
                    (expectsDot && base.Tokens[base.Index].Type != TokenType.Dot))
                {
                    break;
                }

                if (base.Tokens[base.Index].Type == TokenType.Int ||
                    base.Tokens[base.Index].Type == TokenType.Bool ||
                    base.Tokens[base.Index].Type == TokenType.Identifier)
                {
                    expectsDot = true;
                }
                else if (base.Tokens[base.Index].Type == TokenType.Dot)
                {
                    expectsDot = false;
                }

                if (textUnit == null)
                {
                    textUnit = new TextUnit(base.Tokens[base.Index].TextUnit.Text,
                        line, position);
                }
                else
                {
                    textUnit = new TextUnit(textUnit.Text + base.Tokens[base.Index].TextUnit.Text,
                        line, position);
                }

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Tokens[base.Index].Type == TokenType.LeftAngleBracket)
            {
                textUnit = new TextUnit(textUnit.Text + base.Tokens[base.Index].TextUnit.Text,
                    line, position);

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                int counter = 1;
                while (base.Index < base.Tokens.Count)
                {
                    if (base.Tokens[base.Index].Type == TokenType.LeftAngleBracket)
                    {
                        counter++;
                    }
                    else if (base.Tokens[base.Index].Type == TokenType.RightAngleBracket)
                    {
                        counter--;
                    }

                    if (counter == 0 ||
                        (base.Tokens[base.Index].Type != TokenType.Int &&
                        base.Tokens[base.Index].Type != TokenType.Bool &&
                        base.Tokens[base.Index].Type != TokenType.Identifier &&
                        base.Tokens[base.Index].Type != TokenType.Dot &&
                        base.Tokens[base.Index].Type != TokenType.Comma &&
                        base.Tokens[base.Index].Type != TokenType.LeftSquareBracket &&
                        base.Tokens[base.Index].Type != TokenType.RightSquareBracket &&
                        base.Tokens[base.Index].Type != TokenType.LeftAngleBracket &&
                        base.Tokens[base.Index].Type != TokenType.RightAngleBracket))
                    {
                        break;
                    }

                    textUnit = new TextUnit(textUnit.Text + base.Tokens[base.Index].TextUnit.Text,
                        line, position);

                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                }

                if (base.Index == base.Tokens.Count ||
                    base.Tokens[base.Index].Type != TokenType.RightAngleBracket)
                {
                    this.ReportParsingError("Expected \">\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.RightAngleBracket
                    });
                }

                textUnit = new TextUnit(textUnit.Text + base.Tokens[base.Index].TextUnit.Text,
                    line, position);

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }
        }

        /// <summary>
        /// Visits an attribute list.
        /// </summary>
        private void VisitAttributeList()
        {
            int counter = 1;
            while (base.Index < base.Tokens.Count)
            {
                if (base.Tokens[base.Index].Type == TokenType.LeftSquareBracket)
                {
                    counter++;
                }
                else if (base.Tokens[base.Index].Type == TokenType.RightSquareBracket)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit);

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.RightSquareBracket)
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
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            int counter = 1;
            while (base.Index < base.Tokens.Count)
            {
                if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
                {
                    counter++;
                }
                else if (base.Tokens[base.Index].Type == TokenType.RightParenthesis)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                node.StmtTokens.Add(base.Tokens[base.Index]);

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.RightParenthesis)
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
