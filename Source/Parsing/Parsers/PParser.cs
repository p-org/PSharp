//-----------------------------------------------------------------------
// <copyright file="PParser.cs">
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

using Microsoft.PSharp.Parsing.Syntax.P;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P parser.
    /// </summary>
    public sealed class PParser : BaseParser
    {
        #region public API

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PParser()
            : base()
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filePath">File path</param>
        internal PParser(string filePath)
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
            return new PProgram(base.FilePath);
        }

        /// <summary>
        /// Parses the next available token.
        /// </summary>
        protected override void ParseNextToken()
        {
            if (base.Index == this.Tokens.Count)
            {
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.MainMachine,
                    TokenType.EventDecl,
                    TokenType.MachineDecl,
                    TokenType.ModelDecl
                });
            }

            var token = this.Tokens[base.Index];
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.Comment:
                case TokenType.NewLine:
                    base.Index++;
                    break;

                case TokenType.CommentLine:
                    base.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    base.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.EventDecl:
                    this.VisitEventDeclaration();
                    base.Index++;
                    break;

                case TokenType.MainMachine:
                    this.VisitMainMachineModifier();
                    base.Index++;
                    break;

                case TokenType.MachineDecl:
                    this.VisitMachineDeclaration(false);
                    base.Index++;
                    break;

                case TokenType.ModelDecl:
                    this.VisitMachineDeclaration(false);
                    base.Index++;
                    break;

                default:
                    this.ReportParsingError("Unexpected token.");
                    break;
            }

            this.ParseNextToken();
        }

        #endregion

        #region private API

        /// <summary>
        /// Visits an event declaration.
        /// </summary>
        private void VisitEventDeclaration()
        {
            var node = new PEventDeclarationNode();
            node.EventKeyword = this.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == this.Tokens.Count ||
                this.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            this.Tokens[base.Index] = new Token(this.Tokens[base.Index].TextUnit,
                TokenType.EventIdentifier);

            node.Identifier = this.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == this.Tokens.Count ||
                (this.Tokens[base.Index].Type != TokenType.Colon &&
                this.Tokens[base.Index].Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \":\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Colon,
                    TokenType.Semicolon
                });
            }

            if (this.Tokens[base.Index].Type == TokenType.Colon)
            {
                node.ColonToken = this.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                var typeNode = new PTypeIdentifierNode();
                this.VisitTypeIdentifier(typeNode);
                node.PayloadType = typeNode;
            }

            if (base.Index == this.Tokens.Count ||
                this.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = this.Tokens[base.Index];

            (this.Program as PProgram).EventDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a main machine modifier.
        /// </summary>
        private void VisitMainMachineModifier()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == this.Tokens.Count ||
                (this.Tokens[base.Index].Type != TokenType.MachineDecl &&
                this.Tokens[base.Index].Type != TokenType.ModelDecl))
            {
                this.ReportParsingError("Expected machine or model declaration.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.MachineDecl,
                    TokenType.ModelDecl
                });
            }

            this.VisitMachineDeclaration(true);
        }

        /// <summary>
        /// Visits a machine declaration.
        /// </summary>
        /// <param name="isMain">Is main machine</param>
        private void VisitMachineDeclaration(bool isMain)
        {
            var node = new PMachineDeclarationNode(isMain);
            node.MachineKeyword = this.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == this.Tokens.Count ||
                this.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected machine identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.CurrentMachine = this.Tokens[base.Index].Text;
            this.Tokens[base.Index] = new Token(this.Tokens[base.Index].TextUnit,
                TokenType.MachineIdentifier);

            node.Identifier = this.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == this.Tokens.Count ||
                this.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            this.Tokens[base.Index] = new Token(this.Tokens[base.Index].TextUnit,
                TokenType.MachineLeftCurlyBracket);

            node.LeftCurlyBracketToken = this.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextIntraMachineDeclaration(node);

            (this.Program as PProgram).MachineDeclarations.Add(node);
        }

        /// <summary>
        /// Visits the next intra-machine declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextIntraMachineDeclaration(PMachineDeclarationNode node)
        {
            if (base.Index == this.Tokens.Count)
            {
                this.ReportParsingError("Expected \"}\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.StartState,
                    TokenType.StateDecl,
                    TokenType.FunDecl,
                    TokenType.Var
                });
            }

            bool fixpoint = false;
            var token = this.Tokens[base.Index];
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
                    this.VisitStartStateModifier(node);
                    base.Index++;
                    break;

                case TokenType.StateDecl:
                    this.VisitStateDeclaration(node, false);
                    base.Index++;
                    break;

                case TokenType.ModelDecl:
                    this.VisitFunctionDeclaration(node, true);
                    base.Index++;
                    break;

                case TokenType.FunDecl:
                    this.VisitFunctionDeclaration(node, false);
                    base.Index++;
                    break;

                case TokenType.Var:
                    this.VisitFieldDeclaration(node);
                    base.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    this.Tokens[base.Index] = new Token(this.Tokens[base.Index].TextUnit,
                        TokenType.MachineRightCurlyBracket);
                    node.RightCurlyBracketToken = this.Tokens[base.Index];
                    base.CurrentMachine = "";
                    fixpoint = true;
                    base.Index++;
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
        /// Visits a start state modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitStartStateModifier(PMachineDeclarationNode parentNode)
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

            this.VisitStateDeclaration(parentNode, true);
        }

        /// <summary>
        /// Visits a state declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="isInit">Is initial state</param>
        private void VisitStateDeclaration(PMachineDeclarationNode parentNode, bool isInit)
        {
            var node = new PStateDeclarationNode(parentNode, isInit);
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
        private void VisitNextIntraStateDeclaration(PStateDeclarationNode node)
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
                    TokenType.IgnoreEvent
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

                case TokenType.RightCurlyBracket:
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                        TokenType.StateRightCurlyBracket);
                    node.RightCurlyBracketToken = base.Tokens[base.Index];
                    base.CurrentState = "";
                    fixpoint = true;
                    base.Index++;
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
        private void VisitStateEntryDeclaration(PStateDeclarationNode parentNode)
        {
            var node = new PEntryDeclarationNode(parentNode.Machine, parentNode);
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

            node.LeftCurlyBracketToken = base.Tokens[base.Index];

            this.VisitStatementBlock(node);

            parentNode.EntryDeclaration = node;
        }

        /// <summary>
        /// Visits a state exit declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitStateExitDeclaration(PStateDeclarationNode parentNode)
        {
            var node = new PExitDeclarationNode(parentNode.Machine, parentNode);
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

            node.LeftCurlyBracketToken = base.Tokens[base.Index];

            this.VisitStatementBlock(node);

            parentNode.ExitDeclaration = node;
        }

        /// <summary>
        /// Visits a state action declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitStateActionDeclaration(PStateDeclarationNode parentNode)
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
        private void VisitDeferEventsDeclaration(PStateDeclarationNode parentNode)
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
        private void VisitIgnoreEventsDeclaration(PStateDeclarationNode parentNode)
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
        /// Visits a function declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="isModel">Is model</param>
        private void VisitFunctionDeclaration(PMachineDeclarationNode parentNode, bool isModel)
        {
            var node = new PFunctionDeclarationNode(parentNode);
            node.IsModel = isModel;

            if (isModel)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.FunDecl)
                {
                    this.ReportParsingError("Expected function declaration.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.FunDecl
                    });
                }
            }

            node.FunctionKeyword = base.Tokens[base.Index];

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

            node.Identifier = base.Tokens[base.Index];

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
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            node.LeftCurlyBracketToken = base.Tokens[base.Index];
            this.VisitStatementBlock(node);

            parentNode.FunctionDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a field declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitFieldDeclaration(PMachineDeclarationNode parentNode)
        {
            var node = new PFieldDeclarationNode(parentNode);
            node.FieldKeyword = base.Tokens[base.Index];

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

            node.Identifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Colon)
            {
                this.ReportParsingError("Expected \":\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Colon
                });
            }

            node.ColonToken = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            var typeNode = new PTypeIdentifierNode();
            this.VisitTypeIdentifier(typeNode);
            node.Type = typeNode;

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

            parentNode.FieldDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a block of statements.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitStatementBlock(PBaseActionDeclarationNode node)
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count)
            {
                this.ReportParsingError("Expected \"}\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.RightCurlyBracket,
                    TokenType.New,
                    TokenType.CreateMachine,
                    TokenType.RaiseEvent,
                    TokenType.SendEvent,
                    TokenType.DeleteMachine,
                    TokenType.Assert
                });
            }

            var startIdx = base.Index;

            int bracketCounter = 1;
            while (base.Index < base.Tokens.Count && bracketCounter > 0)
            {
                if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
                {
                    bracketCounter++;
                }
                else if (base.Tokens[base.Index].Type == TokenType.RightCurlyBracket)
                {
                    bracketCounter--;
                }
                else if (base.Tokens[base.Index].Type == TokenType.New)
                {
                    this.VisitNewStatement();
                }
                else if (base.Tokens[base.Index].Type == TokenType.RaiseEvent)
                {
                    this.VisitRaiseStatement();
                }
                else if (base.Tokens[base.Index].Type == TokenType.SendEvent)
                {
                    this.VisitSendStatement();
                }
                else if (base.Tokens[base.Index].Type == TokenType.DeleteMachine)
                {
                    this.VisitDeleteStatement();
                }

                if (bracketCounter > 0)
                {
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                }
            }

            if (node != null)
                for (int idx = startIdx; idx < base.Index; idx++)
                {
                    node.Statements.Add(base.Tokens[idx]);
                }

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.RightCurlyBracket)
            {
                this.ReportParsingError("Expected \"}\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.RightCurlyBracket
                });
            }

            if (node != null)
                node.RightCurlyBracketToken = base.Tokens[base.Index];
        }

        /// <summary>
        /// Visits a new statement.
        /// </summary>
        private void VisitNewStatement()
        {
            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                TokenType.CreateMachine);

            //base.Index++;
            //base.SkipWhiteSpaceAndCommentTokens();

            //if (base.Index == base.Tokens.Count ||
            //    base.Tokens[base.Index].Type != TokenType.Identifier)
            //{
            //    this.ReportParsingError("Expected base machine identifier.");
            //    throw new EndOfTokensException(new List<TokenType>
            //    {
            //        TokenType.Identifier
            //    });
            //}

            //while (base.Index < base.Tokens.Count &&
            //    base.Tokens[base.Index].Type != TokenType.LeftParenthesis &&
            //    base.Tokens[base.Index].Type != TokenType.Semicolon)
            //{
            //    if (base.Tokens[base.Index].Type != TokenType.Identifier &&
            //        base.Tokens[base.Index].Type != TokenType.Dot &&
            //        base.Tokens[base.Index].Type != TokenType.NewLine)
            //    {
            //        this.ReportParsingError("Expected identifier.");
            //        throw new EndOfTokensException(new List<TokenType>
            //        {
            //            TokenType.Identifier,
            //            TokenType.Dot
            //        });
            //    }

            //    if (base.Tokens[base.Index].Type == TokenType.Identifier)
            //    {
            //        base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
            //            base.Tokens[base.Index].Line, TokenType.MachineIdentifier);
            //    }

            //    base.Index++;
            //    base.SkipWhiteSpaceAndCommentTokens();
            //}

            //if (base.Index == base.Tokens.Count ||
            //    (base.Tokens[base.Index].Type != TokenType.LeftParenthesis &&
            //    base.Tokens[base.Index].Type != TokenType.Semicolon))
            //{
            //    this.ReportParsingError("Expected \"(\" or \";\".");
            //    throw new EndOfTokensException(new List<TokenType>
            //    {
            //        TokenType.LeftParenthesis,
            //        TokenType.Semicolon
            //    });
            //}

            //if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
            //{
            //    this.VisitArgumentsList();
            //}
        }

        /// <summary>
        /// Visits a raise statement.
        /// </summary>
        private void VisitRaiseStatement()
        {
            //base.Index++;
            //base.SkipWhiteSpaceAndCommentTokens();

            //if (base.Index == base.Tokens.Count ||
            //    base.Tokens[base.Index].Type != TokenType.Identifier)
            //{
            //    this.ReportParsingError("Expected event identifier.");
            //    throw new EndOfTokensException(new List<TokenType>
            //    {
            //        TokenType.Identifier
            //    });
            //}

            //base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
            //    base.Tokens[base.Index].Line, TokenType.EventIdentifier);

            //base.Index++;
            //base.SkipWhiteSpaceAndCommentTokens();

            //if (base.Index == base.Tokens.Count ||
            //    (base.Tokens[base.Index].Type != TokenType.LeftParenthesis &&
            //    base.Tokens[base.Index].Type != TokenType.Semicolon))
            //{
            //    this.ReportParsingError("Expected \"(\" or \";\".");
            //    throw new EndOfTokensException(new List<TokenType>
            //    {
            //        TokenType.LeftParenthesis,
            //        TokenType.Semicolon
            //    });
            //}

            //if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
            //{
            //    this.VisitArgumentsList();
            //}

            //base.SkipWhiteSpaceAndCommentTokens();

            //if (base.Index == base.Tokens.Count ||
            //    base.Tokens[base.Index].Type != TokenType.Semicolon)
            //{
            //    this.ReportParsingError("Expected \";\".");
            //    throw new EndOfTokensException(new List<TokenType>
            //    {
            //        TokenType.Semicolon
            //    });
            //}
        }

        /// <summary>
        /// Visits a send statement.
        /// </summary>
        private void VisitSendStatement()
        {
            //base.Index++;
            //base.SkipWhiteSpaceAndCommentTokens();

            //if (base.Index == base.Tokens.Count ||
            //    base.Tokens[base.Index].Type != TokenType.Identifier)
            //{
            //    this.ReportParsingError("Expected event identifier.");
            //    throw new EndOfTokensException(new List<TokenType>
            //    {
            //        TokenType.Identifier
            //    });
            //}

            //base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
            //    base.Tokens[base.Index].Line, TokenType.EventIdentifier);

            //base.Index++;
            //base.SkipWhiteSpaceAndCommentTokens();

            //if (base.Index == base.Tokens.Count ||
            //    (base.Tokens[base.Index].Type != TokenType.LeftParenthesis &&
            //    base.Tokens[base.Index].Type != TokenType.ToMachine))
            //{
            //    this.ReportParsingError("Expected \"(\" or \"to\".");
            //    throw new EndOfTokensException(new List<TokenType>
            //    {
            //        TokenType.LeftParenthesis,
            //        TokenType.ToMachine
            //    });
            //}

            //if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
            //{
            //    this.VisitArgumentsList();
            //    base.Index++;
            //}

            //base.SkipWhiteSpaceAndCommentTokens();

            //if (base.Index == base.Tokens.Count ||
            //    base.Tokens[base.Index].Type != TokenType.ToMachine)
            //{
            //    this.ReportParsingError("Expected \"to\".");
            //    throw new EndOfTokensException(new List<TokenType>
            //    {
            //        TokenType.ToMachine
            //    });
            //}

            //base.Index++;
            //base.SkipWhiteSpaceAndCommentTokens();

            //if (base.Index == base.Tokens.Count ||
            //    (base.Tokens[base.Index].Type != TokenType.Identifier &&
            //    base.Tokens[base.Index].Type != TokenType.This))
            //{
            //    this.ReportParsingError("Expected machine identifier.");
            //    throw new EndOfTokensException(new List<TokenType>
            //    {
            //        TokenType.Identifier,
            //        TokenType.This
            //    });
            //}

            //while (base.Index < base.Tokens.Count &&
            //    base.Tokens[base.Index].Type != TokenType.Semicolon)
            //{
            //    if (base.Tokens[base.Index].Type != TokenType.Identifier &&
            //        base.Tokens[base.Index].Type != TokenType.This &&
            //        base.Tokens[base.Index].Type != TokenType.Dot &&
            //        base.Tokens[base.Index].Type != TokenType.NewLine)
            //    {
            //        this.ReportParsingError("Expected machine identifier.");
            //        throw new EndOfTokensException(new List<TokenType>
            //        {
            //            TokenType.Identifier,
            //            TokenType.This,
            //            TokenType.Dot
            //        });
            //    }

            //    base.Index++;
            //    base.SkipWhiteSpaceAndCommentTokens();
            //}

            //if (base.Index == base.Tokens.Count ||
            //    base.Tokens[base.Index].Type != TokenType.Semicolon)
            //{
            //    this.ReportParsingError("Expected \";\".");
            //    throw new EndOfTokensException(new List<TokenType>
            //    {
            //        TokenType.Semicolon
            //    });
            //}
        }

        /// <summary>
        /// Visits a delete statement.
        /// </summary>
        private void VisitDeleteStatement()
        {
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

        /// <summary>
        /// Visits a type identifier.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitTypeIdentifier(PTypeIdentifierNode node)
        {
            if (base.Index == this.Tokens.Count ||
                    (this.Tokens[base.Index].Type != TokenType.MachineDecl &&
                    this.Tokens[base.Index].Type != TokenType.Int &&
                    this.Tokens[base.Index].Type != TokenType.Bool &&
                    this.Tokens[base.Index].Type != TokenType.LeftParenthesis))
            {
                this.ReportParsingError("Expected type.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.MachineDecl,
                    TokenType.Int,
                    TokenType.Bool
                });
            }

            if (this.Tokens[base.Index].Type == TokenType.MachineDecl ||
                this.Tokens[base.Index].Type == TokenType.Int ||
                this.Tokens[base.Index].Type == TokenType.Bool)
            {
                node.TypeTokens.Add(this.Tokens[base.Index]);
            }
            else if (this.Tokens[base.Index].Type == TokenType.LeftParenthesis)
            {
                node.TypeTokens.Add(this.Tokens[base.Index]);

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                this.VisitTupleTypeIdentifier(node);
            }

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
        }

        /// <summary>
        /// Visits a tuple type identifier.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitTupleTypeIdentifier(PTypeIdentifierNode node)
        {
            if (base.Index == this.Tokens.Count ||
                    (this.Tokens[base.Index].Type != TokenType.MachineDecl &&
                    this.Tokens[base.Index].Type != TokenType.Int &&
                    this.Tokens[base.Index].Type != TokenType.Bool &&
                    this.Tokens[base.Index].Type != TokenType.LeftParenthesis))
            {
                this.ReportParsingError("Expected type.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.MachineDecl,
                    TokenType.Int,
                    TokenType.Bool
                });
            }

            bool expectsComma = false;
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.RightParenthesis)
            {
                if (!expectsComma &&
                    (base.Tokens[base.Index].Type != TokenType.MachineDecl &&
                    base.Tokens[base.Index].Type != TokenType.Int &&
                    base.Tokens[base.Index].Type != TokenType.Bool &&
                    this.Tokens[base.Index].Type != TokenType.LeftParenthesis) ||
                    (expectsComma && base.Tokens[base.Index].Type != TokenType.Comma))
                {
                    break;
                }

                if (base.Tokens[base.Index].Type == TokenType.MachineDecl ||
                    base.Tokens[base.Index].Type == TokenType.Int ||
                    base.Tokens[base.Index].Type == TokenType.Bool ||
                    base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
                {
                    this.VisitTypeIdentifier(node);
                    expectsComma = true;
                }
                else if (base.Tokens[base.Index].Type == TokenType.Comma)
                {
                    node.TypeTokens.Add(this.Tokens[base.Index]);

                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = false;
                }
            }

            if (base.Index == this.Tokens.Count ||
                this.Tokens[base.Index].Type != TokenType.RightParenthesis)
            {
                this.ReportParsingError("Expected \")\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.RightParenthesis
                });
            }

            node.TypeTokens.Add(this.Tokens[base.Index]);
        }

        #endregion
    }
}
