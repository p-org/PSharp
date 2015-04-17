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

using Microsoft.PSharp.Parsing.PSyntax;

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
            if (base.Index == base.Tokens.Count)
            {
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.MainMachine,
                    TokenType.EventDecl,
                    TokenType.MachineDecl,
                    TokenType.ModelDecl
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
            node.EventKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Identifier &&
                base.Tokens[base.Index].Type != TokenType.HaltEvent &&
                base.Tokens[base.Index].Type != TokenType.DefaultEvent))
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent,
                    TokenType.DefaultEvent
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.Identifier)
            {
                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                    TokenType.EventIdentifier);
            }

            node.Identifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Assert &&
                base.Tokens[base.Index].Type != TokenType.Assume &&
                base.Tokens[base.Index].Type != TokenType.Colon &&
                base.Tokens[base.Index].Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \":\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Assert,
                    TokenType.Assume,
                    TokenType.Colon,
                    TokenType.Semicolon
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.Assert ||
                base.Tokens[base.Index].Type == TokenType.Assume)
            {
                node.AssertAssumeKeyword = base.Tokens[base.Index];

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

                node.AssertIdentifier = base.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Index == base.Tokens.Count ||
                    (base.Tokens[base.Index].Type != TokenType.Colon &&
                    base.Tokens[base.Index].Type != TokenType.Semicolon))
                {
                    this.ReportParsingError("Expected \":\" or \";\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Colon,
                        TokenType.Semicolon
                    });
                }
            }

            if (base.Tokens[base.Index].Type == TokenType.Colon)
            {
                node.ColonToken = base.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                var typeNode = new PTypeNode();
                this.VisitTypeIdentifier(typeNode);
                node.PayloadType = typeNode;
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

            (this.Program as PProgram).EventDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a main machine modifier.
        /// </summary>
        private void VisitMainMachineModifier()
        {
            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.MachineDecl &&
                base.Tokens[base.Index].Type != TokenType.ModelDecl))
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

            (this.Program as PProgram).MachineDeclarations.Add(node);
        }

        /// <summary>
        /// Visits the next intra-machine declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextIntraMachineDeclaration(PMachineDeclarationNode node)
        {
            if (base.Index == base.Tokens.Count)
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
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                        TokenType.MachineRightCurlyBracket);
                    node.RightCurlyBracketToken = base.Tokens[base.Index];
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
            var node = new PEntryDeclarationNode();
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

            var blockNode = new PStatementBlockNode(parentNode.Machine, parentNode);
            this.VisitStatementBlock(blockNode);
            node.StatementBlock = blockNode;

            parentNode.EntryDeclaration = node;
        }

        /// <summary>
        /// Visits a state exit declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitStateExitDeclaration(PStateDeclarationNode parentNode)
        {
            var node = new PExitDeclarationNode();
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

            var blockNode = new PStatementBlockNode(parentNode.Machine, parentNode);
            this.VisitStatementBlock(blockNode);
            node.StatementBlock = blockNode;

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
                (base.Tokens[base.Index].Type != TokenType.Identifier &&
                base.Tokens[base.Index].Type != TokenType.HaltEvent &&
                base.Tokens[base.Index].Type != TokenType.DefaultEvent))
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent,
                    TokenType.DefaultEvent
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.Identifier)
            {
                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                    TokenType.EventIdentifier);
            }

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
                (base.Tokens[base.Index].Type != TokenType.Identifier &&
                base.Tokens[base.Index].Type != TokenType.HaltEvent &&
                base.Tokens[base.Index].Type != TokenType.DefaultEvent))
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
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (!expectsComma &&
                    base.Tokens[base.Index].Type != TokenType.Identifier &&
                    base.Tokens[base.Index].Type != TokenType.HaltEvent &&
                    base.Tokens[base.Index].Type != TokenType.DefaultEvent)
                {
                    this.ReportParsingError("Expected event identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.HaltEvent,
                        TokenType.DefaultEvent
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
                else if (base.Tokens[base.Index].Type == TokenType.HaltEvent ||
                    base.Tokens[base.Index].Type == TokenType.DefaultEvent)
                {
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
                (base.Tokens[base.Index].Type != TokenType.Identifier &&
                base.Tokens[base.Index].Type != TokenType.HaltEvent &&
                base.Tokens[base.Index].Type != TokenType.DefaultEvent))
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
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (!expectsComma &&
                    base.Tokens[base.Index].Type != TokenType.Identifier &&
                    base.Tokens[base.Index].Type != TokenType.HaltEvent &&
                    base.Tokens[base.Index].Type != TokenType.DefaultEvent)
                {
                    this.ReportParsingError("Expected event identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.HaltEvent,
                        TokenType.DefaultEvent
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
                else if (base.Tokens[base.Index].Type == TokenType.HaltEvent ||
                    base.Tokens[base.Index].Type == TokenType.DefaultEvent)
                {
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
        /// Visits a function declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="isModel">Is model</param>
        private void VisitFunctionDeclaration(PMachineDeclarationNode parentNode, bool isModel)
        {
            var node = new PFunctionDeclarationNode();
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

            var blockNode = new PStatementBlockNode(parentNode, null);
            this.VisitStatementBlock(blockNode);
            node.StatementBlock = blockNode;

            parentNode.FunctionDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a field declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitFieldDeclaration(PMachineDeclarationNode parentNode)
        {
            var nodes = new List<PFieldDeclarationNode>();
            var fieldKeyword = base.Tokens[base.Index];

            //var node = new PFieldDeclarationNode(parentNode);
            //node.FieldKeyword = base.Tokens[base.Index];

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

            nodes.Add(new PFieldDeclarationNode(parentNode));
            nodes[nodes.Count - 1].FieldKeyword = fieldKeyword;
            nodes[nodes.Count - 1].Identifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            bool expectsComma = true;
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Colon)
            {
                if ((!expectsComma &&
                    base.Tokens[base.Index].Type != TokenType.Identifier) ||
                    (expectsComma && base.Tokens[base.Index].Type != TokenType.Comma))
                {
                    break;
                }

                if (base.Tokens[base.Index].Type == TokenType.Identifier)
                {
                    nodes.Add(new PFieldDeclarationNode(parentNode));
                    nodes[nodes.Count - 1].FieldKeyword = fieldKeyword;
                    nodes[nodes.Count - 1].Identifier = base.Tokens[base.Index];

                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = true;
                }
                else if (base.Tokens[base.Index].Type == TokenType.Comma)
                {
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = false;
                }
            }

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Colon)
            {
                this.ReportParsingError("Expected \":\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Colon
                });
            }

            foreach (var node in nodes)
            {
                node.ColonToken = base.Tokens[base.Index];
            }

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            var typeNode = new PTypeNode();
            this.VisitTypeIdentifier(typeNode);

            foreach (var node in nodes)
            {
                node.Type = typeNode;
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

            foreach (var node in nodes)
            {
                node.SemicolonToken = base.Tokens[base.Index];
                parentNode.FieldDeclarations.Add(node);
            }
        }

        /// <summary>
        /// Visits a block of statements.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitStatementBlock(PStatementBlockNode node)
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
        private void VisitNextStatement(PStatementBlockNode node)
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

                case TokenType.RaiseEvent:
                    this.VisitRaiseStatement(node);
                    break;

                case TokenType.SendEvent:
                    this.VisitSendStatement(node);
                    break;

                case TokenType.Assert:
                    this.VisitAssertStatement(node);
                    break;

                case TokenType.IfCondition:
                    this.VisitIfStatement(node);
                    break;

                case TokenType.WhileLoop:
                    this.VisitWhileStatement(node);
                    break;

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
        /// Visits a create statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitNewStatement(PStatementBlockNode parentNode)
        {
            var node = new PNewStatementNode(parentNode);
            node.NewKeyword = base.Tokens[base.Index];

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

            node.MachineIdentifier = base.Tokens[base.Index];

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
                var payload = new PPayloadSendExpressionNode(parentNode);
                this.VisitPayloadTuple(payload);
                node.Payload = payload;

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
        private void VisitRaiseStatement(PStatementBlockNode parentNode)
        {
            var node = new PRaiseStatementNode(parentNode);
            node.RaiseKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Identifier &&
                base.Tokens[base.Index].Type != TokenType.HaltEvent))
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.Identifier)
            {
                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                    TokenType.EventIdentifier);
            }

            node.EventIdentifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Comma &&
                base.Tokens[base.Index].Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \",\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Comma,
                    TokenType.Semicolon
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.Comma)
            {
                node.Comma = base.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                var payload = new PPayloadSendExpressionNode(parentNode);
                this.VisitPayload(payload);
                node.Payload = payload;
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
        private void VisitSendStatement(PStatementBlockNode parentNode)
        {
            var node = new PSendStatementNode(parentNode);
            node.SendKeyword = base.Tokens[base.Index];

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

            var machineIdentifier = new PExpressionNode(parentNode);
            machineIdentifier.StmtTokens.Add(base.Tokens[base.Index]);
            node.MachineIdentifier = machineIdentifier;

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Comma)
            {
                this.ReportParsingError("Expected \",\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Comma
                });
            }

            node.MachineComma = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Identifier &&
                base.Tokens[base.Index].Type != TokenType.HaltEvent))
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.Identifier)
            {
                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].TextUnit,
                    TokenType.EventIdentifier);
            }

            node.EventIdentifier = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Comma &&
                base.Tokens[base.Index].Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \",\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Comma,
                    TokenType.Semicolon
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.Comma)
            {
                node.EventComma = base.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                var payload = new PPayloadSendExpressionNode(parentNode);
                this.VisitPayload(payload);
                node.Payload = payload;
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
        /// Visits an assert statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitAssertStatement(PStatementBlockNode parentNode)
        {
            var node = new PAssertStatementNode(parentNode);
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

            var predicate = new PExpressionNode(parentNode);
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
                else if (base.Tokens[base.Index].Type == TokenType.Payload)
                {
                    var payloadNode = new PPayloadReceiveNode();
                    this.VisitReceivedPayload(payloadNode);
                    predicate.StmtTokens.Add(null);
                    predicate.Payloads.Add(payloadNode);
                    if (payloadNode.RightParenthesisToken != null)
                    {
                        counter--;
                    }
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
        /// Visits an if statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitIfStatement(PStatementBlockNode parentNode)
        {
            var node = new PIfStatementNode(parentNode);
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

            var guard = new PExpressionNode(parentNode);

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
                (base.Tokens[base.Index].Type != TokenType.New &&
                base.Tokens[base.Index].Type != TokenType.RaiseEvent &&
                base.Tokens[base.Index].Type != TokenType.SendEvent &&
                base.Tokens[base.Index].Type != TokenType.Assert &&
                base.Tokens[base.Index].Type != TokenType.IfCondition &&
                base.Tokens[base.Index].Type != TokenType.WhileLoop &&
                base.Tokens[base.Index].Type != TokenType.Identifier &&
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket))
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            var blockNode = new PStatementBlockNode(parentNode.Machine, parentNode.State);

            if (base.Tokens[base.Index].Type == TokenType.New)
            {
                this.VisitNewStatement(blockNode);
            }
            else if (base.Tokens[base.Index].Type == TokenType.RaiseEvent)
            {
                this.VisitRaiseStatement(blockNode);
            }
            else if (base.Tokens[base.Index].Type == TokenType.SendEvent)
            {
                this.VisitSendStatement(blockNode);
            }
            else if (base.Tokens[base.Index].Type == TokenType.Assert)
            {
                this.VisitAssertStatement(blockNode);
            }
            else if (base.Tokens[base.Index].Type == TokenType.IfCondition)
            {
                this.VisitIfStatement(blockNode);
            }
            else if (base.Tokens[base.Index].Type == TokenType.WhileLoop)
            {
                this.VisitWhileStatement(blockNode);
            }
            else if (base.Tokens[base.Index].Type == TokenType.Identifier)
            {
                this.VisitGenericStatement(blockNode);
            }
            else if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                this.VisitStatementBlock(blockNode);
                node.StatementBlock = blockNode;
            }

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Tokens[base.Index].Type == TokenType.ElseCondition)
            {
                node.ElseKeyword = base.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Index == base.Tokens.Count ||
                    (base.Tokens[base.Index].Type != TokenType.New &&
                    base.Tokens[base.Index].Type != TokenType.RaiseEvent &&
                    base.Tokens[base.Index].Type != TokenType.SendEvent &&
                    base.Tokens[base.Index].Type != TokenType.Assert &&
                    base.Tokens[base.Index].Type != TokenType.IfCondition &&
                    base.Tokens[base.Index].Type != TokenType.WhileLoop &&
                    base.Tokens[base.Index].Type != TokenType.Identifier &&
                    base.Tokens[base.Index].Type != TokenType.IfCondition &&
                    base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket))
                {
                    this.ReportParsingError("Expected \"{\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.LeftCurlyBracket
                    });
                }

                if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
                {
                    var elseBlockNode = new PStatementBlockNode(parentNode.Machine, parentNode.State);
                    this.VisitStatementBlock(elseBlockNode);
                    node.ElseStatementBlock = elseBlockNode;

                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                }
            }

            parentNode.Statements.Add(node);
        }

        /// <summary>
        /// Visits an while statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitWhileStatement(PStatementBlockNode parentNode)
        {
            var node = new PWhileStatementNode(parentNode);
            node.WhileKeyword = base.Tokens[base.Index];

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

            var guard = new PExpressionNode(parentNode);

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
                (base.Tokens[base.Index].Type != TokenType.New &&
                base.Tokens[base.Index].Type != TokenType.RaiseEvent &&
                base.Tokens[base.Index].Type != TokenType.SendEvent &&
                base.Tokens[base.Index].Type != TokenType.Assert &&
                base.Tokens[base.Index].Type != TokenType.IfCondition &&
                base.Tokens[base.Index].Type != TokenType.WhileLoop &&
                base.Tokens[base.Index].Type != TokenType.Identifier &&
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket))
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            var blockNode = new PStatementBlockNode(parentNode.Machine, parentNode.State);

            if (base.Tokens[base.Index].Type == TokenType.New)
            {
                this.VisitNewStatement(blockNode);
            }
            else if (base.Tokens[base.Index].Type == TokenType.RaiseEvent)
            {
                this.VisitRaiseStatement(blockNode);
            }
            else if (base.Tokens[base.Index].Type == TokenType.SendEvent)
            {
                this.VisitSendStatement(blockNode);
            }
            else if (base.Tokens[base.Index].Type == TokenType.Assert)
            {
                this.VisitAssertStatement(blockNode);
            }
            else if (base.Tokens[base.Index].Type == TokenType.IfCondition)
            {
                this.VisitIfStatement(blockNode);
            }
            else if (base.Tokens[base.Index].Type == TokenType.WhileLoop)
            {
                this.VisitWhileStatement(blockNode);
            }
            else if (base.Tokens[base.Index].Type == TokenType.Identifier)
            {
                this.VisitGenericStatement(blockNode);
            }
            else if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                this.VisitStatementBlock(blockNode);
                node.StatementBlock = blockNode;
            }

            parentNode.Statements.Add(node);
            base.Index++;
        }

        /// <summary>
        /// Visits a generic statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitGenericStatement(PStatementBlockNode parentNode)
        {
            var node = new PGenericStatementNode(parentNode);

            var expression = new PExpressionNode(parentNode);
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
            {
                if (base.Tokens[base.Index].Type == TokenType.New)
                {
                    node.Expression = expression;
                    parentNode.Statements.Add(node);
                    return;
                }
                else if (base.Tokens[base.Index].Type == TokenType.Payload)
                {
                    var payloadNode = new PPayloadReceiveNode();
                    this.VisitReceivedPayload(payloadNode);
                    expression.StmtTokens.Add(null);
                    expression.Payloads.Add(payloadNode);
                    if (base.Tokens[base.Index].Type == TokenType.Semicolon)
                    {
                        break;
                    }
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
        /// Visits a received payload.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitReceivedPayload(PPayloadReceiveNode node)
        {
            node.PayloadKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.As)
            {
                this.ReportParsingError("Expected \"as\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.As
                });
            }

            node.AsKeyword = base.Tokens[base.Index];

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            var typeNode = new PTypeNode();
            this.VisitTypeIdentifier(typeNode);
            node.Type = typeNode;

            if (base.Tokens[base.Index].Type == TokenType.RightParenthesis)
            {
                node.RightParenthesisToken = base.Tokens[base.Index];

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Tokens[base.Index].Type == TokenType.Dot)
                {
                    node.DotToken = base.Tokens[base.Index];

                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();

                    if (base.Index == base.Tokens.Count ||
                        base.Tokens[base.Index].Type != TokenType.Identifier)
                    {
                        this.ReportParsingError("Expected index.");
                        throw new EndOfTokensException(new List<TokenType>
                        {
                            TokenType.Identifier
                        });
                    }

                    node.IndexToken = base.Tokens[base.Index];

                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                }
            }
        }

        /// <summary>
        /// Visits a type identifier.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitTypeIdentifier(PTypeNode node)
        {
            if (base.Index == base.Tokens.Count ||
                    (base.Tokens[base.Index].Type != TokenType.MachineDecl &&
                    base.Tokens[base.Index].Type != TokenType.Int &&
                    base.Tokens[base.Index].Type != TokenType.Bool &&
                    base.Tokens[base.Index].Type != TokenType.Seq &&
                    base.Tokens[base.Index].Type != TokenType.Map &&
                    base.Tokens[base.Index].Type != TokenType.LeftParenthesis))
            {
                this.ReportParsingError("Expected type.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.MachineDecl,
                    TokenType.Int,
                    TokenType.Bool,
                    TokenType.Seq,
                    TokenType.Map
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.MachineDecl ||
                base.Tokens[base.Index].Type == TokenType.Int ||
                base.Tokens[base.Index].Type == TokenType.Bool)
            {
                node.TypeTokens.Add(base.Tokens[base.Index]);
            }
            else if (base.Tokens[base.Index].Type == TokenType.Seq)
            {
                this.VisitSeqTypeIdentifier(node);
            }
            else if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
            {
                this.VisitTupleTypeIdentifier(node);
            }

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
        }

        /// <summary>
        /// Visits a seq type identifier.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitSeqTypeIdentifier(PTypeNode node)
        {
            node.TypeTokens.Add(base.Tokens[base.Index]);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.LeftSquareBracket)
            {
                this.ReportParsingError("Expected \"[\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftSquareBracket
                });
            }

            node.TypeTokens.Add(base.Tokens[base.Index]);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.VisitTypeIdentifier(node);

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.RightSquareBracket)
            {
                this.ReportParsingError("Expected \"]\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.RightSquareBracket
                });
            }

            node.TypeTokens.Add(base.Tokens[base.Index]);
        }

        /// <summary>
        /// Visits a tuple type identifier.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitTupleTypeIdentifier(PTypeNode node)
        {
            node.TypeTokens.Add(base.Tokens[base.Index]);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                    (base.Tokens[base.Index].Type != TokenType.MachineDecl &&
                    base.Tokens[base.Index].Type != TokenType.Int &&
                    base.Tokens[base.Index].Type != TokenType.Bool &&
                    base.Tokens[base.Index].Type != TokenType.LeftParenthesis))
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
                if ((!expectsComma &&
                    base.Tokens[base.Index].Type != TokenType.MachineDecl &&
                    base.Tokens[base.Index].Type != TokenType.Int &&
                    base.Tokens[base.Index].Type != TokenType.Bool &&
                    base.Tokens[base.Index].Type != TokenType.LeftParenthesis) ||
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
                    node.TypeTokens.Add(base.Tokens[base.Index]);

                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = false;
                }
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

            node.TypeTokens.Add(base.Tokens[base.Index]);
        }

        /// <summary>
        /// Visits an argument list.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitArgumentsList(PExpressionNode node)
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

        /// <summary>
        /// Visits a payload.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitPayload(PPayloadSendExpressionNode node)
        {
            if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
            {
                this.VisitPayloadTuple(node);
            }
            else
            {
                node.StmtTokens.Add(base.Tokens[base.Index]);
            }

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
        }

        /// <summary>
        /// Visits a payload tuple.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitPayloadTuple(PPayloadSendExpressionNode node)
        {
            node.StmtTokens.Add(base.Tokens[base.Index]);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            bool expectsComma = false;
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.RightParenthesis)
            {
                if ((!expectsComma &&
                    base.Tokens[base.Index].Type != TokenType.This &&
                    base.Tokens[base.Index].Type != TokenType.Identifier &&
                    base.Tokens[base.Index].Type != TokenType.True &&
                    base.Tokens[base.Index].Type != TokenType.False &&
                    base.Tokens[base.Index].Type != TokenType.LeftParenthesis) ||
                    (expectsComma && base.Tokens[base.Index].Type != TokenType.Comma))
                {
                    break;
                }

                if (base.Tokens[base.Index].Type == TokenType.This ||
                    base.Tokens[base.Index].Type == TokenType.Identifier ||
                    base.Tokens[base.Index].Type == TokenType.True ||
                    base.Tokens[base.Index].Type == TokenType.False ||
                    base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
                {
                    this.VisitPayload(node);
                    expectsComma = true;
                }
                else if (base.Tokens[base.Index].Type == TokenType.Comma)
                {
                    node.StmtTokens.Add(base.Tokens[base.Index]);

                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    expectsComma = false;
                }
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
            
            node.StmtTokens.Add(base.Tokens[base.Index]);
        }

        #endregion
    }
}
