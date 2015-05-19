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

using Microsoft.PSharp.Parsing.Syntax;

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
            if (base.TokenStream.Done)
            {
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.MainMachine,
                    TokenType.EventDecl,
                    TokenType.MachineDecl,
                    TokenType.ModelDecl,
                    TokenType.Monitor
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
                    base.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    base.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.EventDecl:
                    this.VisitEventDeclaration();
                    base.TokenStream.Index++;
                    break;

                case TokenType.MainMachine:
                    this.VisitMainMachineModifier();
                    base.TokenStream.Index++;
                    break;

                case TokenType.MachineDecl:
                    this.VisitMachineDeclaration(false, false);
                    base.TokenStream.Index++;
                    break;

                case TokenType.Monitor:
                    this.VisitMachineDeclaration(false, true);
                    base.TokenStream.Index++;
                    break;

                case TokenType.ModelDecl:
                    this.VisitMachineDeclaration(false, false);
                    base.TokenStream.Index++;
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
            var node = new EventDeclarationNode();
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
                (base.TokenStream.Peek().Type != TokenType.Assert &&
                base.TokenStream.Peek().Type != TokenType.Assume &&
                base.TokenStream.Peek().Type != TokenType.Colon &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
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

            if (base.TokenStream.Peek().Type == TokenType.Assert ||
                base.TokenStream.Peek().Type == TokenType.Assume)
            {
                node.AssertAssumeKeyword = base.TokenStream.Peek();

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

                node.AssertIdentifier = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.Colon &&
                    base.TokenStream.Peek().Type != TokenType.Semicolon))
                {
                    this.ReportParsingError("Expected \":\" or \";\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Colon,
                        TokenType.Semicolon
                    });
                }
            }

            if (base.TokenStream.Peek().Type == TokenType.Colon)
            {
                node.ColonToken = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                var typeNode = new PTypeNode();
                this.VisitTypeIdentifier(typeNode);
                node.PayloadType = typeNode;
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

            (this.Program as PProgram).EventDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a main machine modifier.
        /// </summary>
        private void VisitMainMachineModifier()
        {
            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.ModelDecl))
            {
                this.ReportParsingError("Expected machine or model declaration.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.MachineDecl,
                    TokenType.ModelDecl
                });
            }

            this.VisitMachineDeclaration(true, false);
        }

        /// <summary>
        /// Visits a machine declaration.
        /// </summary>
        /// <param name="isMain">Is main machine</param>
        /// <param name="isMain">Is a monitor</param>
        private void VisitMachineDeclaration(bool isMain, bool isMonitor)
        {
            var node = new MachineDeclarationNode(isMain, isMonitor);
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

            (this.Program as PProgram).MachineDeclarations.Add(node);
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
                    base.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    base.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.StartState:
                    this.VisitStartStateModifier(node);
                    base.TokenStream.Index++;
                    break;

                case TokenType.StateDecl:
                    this.VisitStateDeclaration(node, false);
                    base.TokenStream.Index++;
                    break;

                case TokenType.ModelDecl:
                    this.VisitFunctionDeclaration(node, true);
                    base.TokenStream.Index++;
                    break;

                case TokenType.FunDecl:
                    this.VisitFunctionDeclaration(node, false);
                    base.TokenStream.Index++;
                    break;

                case TokenType.Var:
                    this.VisitFieldDeclaration(node);
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
                    base.CurrentMachine = "";
                    fixpoint = true;
                    base.TokenStream.Index++;
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
        private void VisitStartStateModifier(MachineDeclarationNode parentNode)
        {
            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.StateDecl &&
                base.TokenStream.Peek().Type != TokenType.ColdState &&
                base.TokenStream.Peek().Type != TokenType.HotState))
            {
                this.ReportParsingError("Expected state declaration.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.StateDecl
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.ColdState ||
                base.TokenStream.Peek().Type == TokenType.HotState)
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
            }

            this.VisitStateDeclaration(parentNode, true);
        }

        /// <summary>
        /// Visits a state declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="isInit">Is initial state</param>
        private void VisitStateDeclaration(MachineDeclarationNode parentNode, bool isInit)
        {
            var node = new StateDeclarationNode(parentNode, isInit);
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
                    TokenType.IgnoreEvent
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

                case TokenType.RightCurlyBracket:
                    base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                        TokenType.StateRightCurlyBracket));
                    node.RightCurlyBracketToken = base.TokenStream.Peek();
                    base.CurrentState = "";
                    fixpoint = true;
                    base.TokenStream.Index++;
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

            var eventIdentifiers = new List<Token>();

            bool expectsComma = false;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.DoAction &&
                base.TokenStream.Peek().Type != TokenType.GotoState &&
                base.TokenStream.Peek().Type != TokenType.PushState)
            {
                if ((!expectsComma &&
                    base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                    base.TokenStream.Peek().Type != TokenType.DefaultEvent) ||
                    (expectsComma && base.TokenStream.Peek().Type != TokenType.Comma))
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.Identifier ||
                    base.TokenStream.Peek().Type == TokenType.HaltEvent ||
                    base.TokenStream.Peek().Type == TokenType.DefaultEvent)
                {
                    if (base.TokenStream.Peek().Type == TokenType.Identifier)
                    {
                        base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                            TokenType.EventIdentifier));
                    }

                    eventIdentifiers.Add(base.TokenStream.Peek());

                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = false;
                }
            }

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.DoAction &&
                base.TokenStream.Peek().Type != TokenType.GotoState &&
                base.TokenStream.Peek().Type != TokenType.PushState))
            {
                this.ReportParsingError("Expected \"do\", \"goto\" or \"push\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.DoAction,
                    TokenType.GotoState,
                    TokenType.PushState
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
                foreach (var eventIdentifier in eventIdentifiers)
                {
                    if (!parentNode.AddActionBinding(eventIdentifier, actionIdentifier))
                    {
                        this.ReportParsingError("Unexpected action identifier.");
                    }
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

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.WithExit &&
                    base.TokenStream.Peek().Type != TokenType.Semicolon))
                {
                    this.ReportParsingError("Expected \";\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Semicolon
                    });
                }

                if (base.TokenStream.Peek().Type == TokenType.WithExit)
                {
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

                    var blockNode = new StatementBlockNode(parentNode.Machine, null);
                    this.VisitStatementBlock(blockNode);

                    foreach (var eventIdentifier in eventIdentifiers)
                    {
                        if (!parentNode.AddGotoStateTransition(eventIdentifier, stateIdentifier, blockNode))
                        {
                            this.ReportParsingError("Unexpected state identifier.");
                        }
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
                else
                {
                    foreach (var eventIdentifier in eventIdentifiers)
                    {
                        if (!parentNode.AddGotoStateTransition(eventIdentifier, stateIdentifier))
                        {
                            this.ReportParsingError("Unexpected state identifier.");
                        }
                    }
                }
            }
            else if (base.TokenStream.Peek().Type == TokenType.PushState)
            {
                if (parentNode.Machine.IsMonitor)
                {
                    this.ReportParsingError("Monitors cannot \"push\".");
                }

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

                foreach (var eventIdentifier in eventIdentifiers)
                {
                    if (!parentNode.AddPushStateTransition(eventIdentifier, stateIdentifier))
                    {
                        this.ReportParsingError("Unexpected state identifier.");
                    }
                }
            }
        }

        /// <summary>
        /// Visits a defer events declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitDeferEventsDeclaration(StateDeclarationNode parentNode)
        {
            if (parentNode.Machine.IsMonitor)
            {
                this.ReportParsingError("Monitors cannot \"defer\".");
            }

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
                    base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                    base.TokenStream.Peek().Type != TokenType.DefaultEvent)
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
                    if (parentNode.Machine.IsMonitor &&
                        base.TokenStream.Peek().Type == TokenType.DefaultEvent)
                    {
                        this.ReportParsingError("Monitors cannot use the \"default\" event.");
                    }

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
                    base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                    base.TokenStream.Peek().Type != TokenType.DefaultEvent)
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
        /// Visits a function declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="isModel">Is model</param>
        private void VisitFunctionDeclaration(MachineDeclarationNode parentNode, bool isModel)
        {
            var node = new PFunctionDeclarationNode();
            node.IsModel = isModel;

            if (isModel)
            {
                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.FunDecl)
                {
                    this.ReportParsingError("Expected function declaration.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.FunDecl
                    });
                }
            }

            node.FunctionKeyword = base.TokenStream.Peek();

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

            node.Identifier = base.TokenStream.Peek();

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

            bool expectsColon = false;
            bool expectsType = false;
            bool expectsComma = false;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                if ((!expectsColon && !expectsComma && !expectsType &&
                    base.TokenStream.Peek().Type != TokenType.Identifier) ||
                    (!expectsColon && !expectsComma && expectsType &&
                    base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Seq &&
                    base.TokenStream.Peek().Type != TokenType.Map &&
                    base.TokenStream.Peek().Type != TokenType.LeftParenthesis) ||
                    (expectsColon && base.TokenStream.Peek().Type != TokenType.Colon) ||
                    (expectsComma && base.TokenStream.Peek().Type != TokenType.Comma))
                {
                    break;
                }

                if (!expectsType &&
                    base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    node.Parameters.Add(base.TokenStream.Peek());

                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();

                    expectsColon = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Colon)
                {
                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();

                    expectsColon = false;
                    expectsType = true;
                }
                else if (expectsType &&
                    (base.TokenStream.Peek().Type == TokenType.MachineDecl ||
                    base.TokenStream.Peek().Type == TokenType.Int ||
                    base.TokenStream.Peek().Type == TokenType.Bool ||
                    base.TokenStream.Peek().Type == TokenType.Seq ||
                    base.TokenStream.Peek().Type == TokenType.Map ||
                    base.TokenStream.Peek().Type == TokenType.LeftParenthesis))
                {
                    var typeNode = new PTypeNode();
                    this.VisitTypeIdentifier(typeNode);
                    node.ParameterTypeNodes.Add(typeNode);

                    expectsType = false;
                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = false;
                }
            }

            node.RightParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Colon &&
                base.TokenStream.Peek().Type != TokenType.LeftSquareBracket &&
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

                var typeNode = new PTypeNode();
                this.VisitTypeIdentifier(typeNode);
                node.ReturnTypeNode = typeNode;
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftSquareBracket)
            {
                while (!base.TokenStream.Done &&
                    base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
                {
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

            var blockNode = new StatementBlockNode(parentNode, null);
            this.VisitStatementBlock(blockNode);
            node.StatementBlock = blockNode;

            parentNode.FunctionDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a field declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitFieldDeclaration(MachineDeclarationNode parentNode)
        {
            var nodes = new List<PFieldDeclarationNode>();
            var fieldKeyword = base.TokenStream.Peek();

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

            nodes.Add(new PFieldDeclarationNode(parentNode));
            nodes[nodes.Count - 1].FieldKeyword = fieldKeyword;
            nodes[nodes.Count - 1].Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            bool expectsComma = true;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.Colon)
            {
                if ((!expectsComma &&
                    base.TokenStream.Peek().Type != TokenType.Identifier) ||
                    (expectsComma && base.TokenStream.Peek().Type != TokenType.Comma))
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    nodes.Add(new PFieldDeclarationNode(parentNode));
                    nodes[nodes.Count - 1].FieldKeyword = fieldKeyword;
                    nodes[nodes.Count - 1].Identifier = base.TokenStream.Peek();

                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = false;
                }
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Colon)
            {
                this.ReportParsingError("Expected \":\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Colon
                });
            }

            foreach (var node in nodes)
            {
                node.ColonToken = base.TokenStream.Peek();
            }

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            var typeNode = new PTypeNode();
            this.VisitTypeIdentifier(typeNode);

            foreach (var node in nodes)
            {
                node.TypeNode = typeNode;
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

            foreach (var node in nodes)
            {
                node.SemicolonToken = base.TokenStream.Peek();
                parentNode.FieldDeclarations.Add(node);
            }
        }

        /// <summary>
        /// Visits a block of statements.
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

                case TokenType.RaiseEvent:
                    this.VisitRaiseStatement(node);
                    break;

                case TokenType.SendEvent:
                    this.VisitSendStatement(node);
                    break;

                case TokenType.Monitor:
                    this.VisitMonitorStatement(node);
                    break;

                case TokenType.PushState:
                    this.VisitPushStatement(node);
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

                case TokenType.Break:
                case TokenType.Continue:
                case TokenType.Return:
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
        /// Visits a create statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitNewStatement(StatementBlockNode parentNode)
        {
            var node = new PNewStatementNode(parentNode);
            node.NewKeyword = base.TokenStream.Peek();

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

            node.MachineIdentifier = base.TokenStream.Peek();

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

            var payload = new PPayloadSendExpressionNode(parentNode);
            this.VisitPayloadTuple(payload);
            node.Payload = payload;

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
            var node = new PRaiseStatementNode(parentNode);
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
                (base.TokenStream.Peek().Type != TokenType.Comma &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \",\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Comma,
                    TokenType.Semicolon
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Comma)
            {
                node.Comma = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                var payload = new PPayloadSendExpressionNode(parentNode);
                this.VisitPayload(payload);
                node.Payload = payload;
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
            if (parentNode.Machine.IsMonitor)
            {
                this.ReportParsingError("Monitors cannot \"send\".");
            }

            var node = new PSendStatementNode(parentNode);
            node.SendKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis))
            {
                this.ReportParsingError("Expected machine identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            var machineIdentifier = new PExpressionNode(parentNode);

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.Comma)
            {
                if (base.TokenStream.Peek().Type == TokenType.Payload)
                {
                    var payloadNode = new PPayloadReceiveNode();
                    this.VisitReceivedPayload(payloadNode);
                    machineIdentifier.StmtTokens.Add(null);
                    machineIdentifier.Payloads.Add(payloadNode);
                }
                else
                {
                    machineIdentifier.StmtTokens.Add(base.TokenStream.Peek());

                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                }
            }

            node.MachineIdentifier = machineIdentifier;

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Comma)
            {
                this.ReportParsingError("Expected \",\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Comma
                });
            }

            node.MachineComma = base.TokenStream.Peek();

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
                (base.TokenStream.Peek().Type != TokenType.Comma &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \",\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Comma,
                    TokenType.Semicolon
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Comma)
            {
                node.EventComma = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                var payload = new PPayloadSendExpressionNode(parentNode);
                this.VisitPayload(payload);
                node.Payload = payload;
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
        /// Visits a monitor statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitMonitorStatement(StatementBlockNode parentNode)
        {
            if (parentNode.Machine.IsMonitor)
            {
                this.ReportParsingError("Monitors cannot \"send\".");
            }

            var node = new PMonitorStatementNode(parentNode);
            node.MonitorKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected monitor identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            node.MonitorIdentifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Comma)
            {
                this.ReportParsingError("Expected \",\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Comma
                });
            }

            node.MonitorComma = base.TokenStream.Peek();

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
                (base.TokenStream.Peek().Type != TokenType.Comma &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \",\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Comma,
                    TokenType.Semicolon
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Comma)
            {
                node.EventComma = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                var payload = new PPayloadSendExpressionNode(parentNode);
                this.VisitPayload(payload);
                node.Payload = payload;
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
        /// Visits a push statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitPushStatement(StatementBlockNode parentNode)
        {
            if (parentNode.Machine.IsMonitor)
            {
                this.ReportParsingError("Monitors cannot \"push\".");
            }

            var node = new PPushStatementNode(parentNode);
            node.PushKeyword = base.TokenStream.Peek();

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

            node.StateToken = base.TokenStream.Peek();

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

            var predicate = new PExpressionNode(parentNode);

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
                else if (base.TokenStream.Peek().Type == TokenType.Payload)
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

            var guard = new PExpressionNode(parentNode);

            int counter = 1;
            while (!base.TokenStream.Done)
            {
                if (base.TokenStream.Peek().Type == TokenType.Payload)
                {
                    var payloadNode = new PPayloadReceiveNode();
                    this.VisitReceivedPayload(payloadNode);
                    guard.StmtTokens.Add(null);
                    guard.Payloads.Add(payloadNode);

                    if (payloadNode.RightParenthesisToken != null)
                    {
                        counter--;
                    }
                }

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
                base.TokenStream.Peek().Type != TokenType.RaiseEvent &&
                base.TokenStream.Peek().Type != TokenType.SendEvent &&
                base.TokenStream.Peek().Type != TokenType.Monitor &&
                base.TokenStream.Peek().Type != TokenType.Assert &&
                base.TokenStream.Peek().Type != TokenType.IfCondition &&
                base.TokenStream.Peek().Type != TokenType.WhileLoop &&
                base.TokenStream.Peek().Type != TokenType.Break &&
                base.TokenStream.Peek().Type != TokenType.Continue &&
                base.TokenStream.Peek().Type != TokenType.Return &&
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
            else if (base.TokenStream.Peek().Type == TokenType.RaiseEvent)
            {
                this.VisitRaiseStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.SendEvent)
            {
                this.VisitSendStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Monitor)
            {
                this.VisitMonitorStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Assert)
            {
                this.VisitAssertStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.IfCondition)
            {
                this.VisitIfStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.WhileLoop)
            {
                this.VisitWhileStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Break ||
                base.TokenStream.Peek().Type == TokenType.Continue ||
                base.TokenStream.Peek().Type == TokenType.Return ||
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
                    base.TokenStream.Peek().Type != TokenType.RaiseEvent &&
                    base.TokenStream.Peek().Type != TokenType.SendEvent &&
                    base.TokenStream.Peek().Type != TokenType.Monitor &&
                    base.TokenStream.Peek().Type != TokenType.Assert &&
                    base.TokenStream.Peek().Type != TokenType.IfCondition &&
                    base.TokenStream.Peek().Type != TokenType.WhileLoop &&
                    base.TokenStream.Peek().Type != TokenType.Break &&
                    base.TokenStream.Peek().Type != TokenType.Continue &&
                    base.TokenStream.Peek().Type != TokenType.Return &&
                    base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.IfCondition &&
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
                else if (base.TokenStream.Peek().Type == TokenType.RaiseEvent)
                {
                    this.VisitRaiseStatement(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.SendEvent)
                {
                    this.VisitSendStatement(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.Monitor)
                {
                    this.VisitMonitorStatement(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.Assert)
                {
                    this.VisitAssertStatement(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.IfCondition)
                {
                    this.VisitIfStatement(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.WhileLoop)
                {
                    this.VisitWhileStatement(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.Break ||
                    base.TokenStream.Peek().Type == TokenType.Continue ||
                    base.TokenStream.Peek().Type == TokenType.Return ||
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
        /// Visits an while statement.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitWhileStatement(StatementBlockNode parentNode)
        {
            var node = new WhileStatementNode(parentNode);
            node.WhileKeyword = base.TokenStream.Peek();

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

            var guard = new PExpressionNode(parentNode);

            int counter = 1;
            while (!base.TokenStream.Done)
            {
                if (base.TokenStream.Peek().Type == TokenType.Payload)
                {
                    var payloadNode = new PPayloadReceiveNode();
                    this.VisitReceivedPayload(payloadNode);
                    guard.StmtTokens.Add(null);
                    guard.Payloads.Add(payloadNode);

                    if (payloadNode.RightParenthesisToken != null)
                    {
                        counter--;
                    }
                }

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
                base.TokenStream.Peek().Type != TokenType.RaiseEvent &&
                base.TokenStream.Peek().Type != TokenType.SendEvent &&
                base.TokenStream.Peek().Type != TokenType.Assert &&
                base.TokenStream.Peek().Type != TokenType.IfCondition &&
                base.TokenStream.Peek().Type != TokenType.WhileLoop &&
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
            else if (base.TokenStream.Peek().Type == TokenType.RaiseEvent)
            {
                this.VisitRaiseStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.SendEvent)
            {
                this.VisitSendStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Monitor)
            {
                this.VisitMonitorStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Assert)
            {
                this.VisitAssertStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.IfCondition)
            {
                this.VisitIfStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.WhileLoop)
            {
                this.VisitWhileStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Identifier)
            {
                this.VisitGenericStatement(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
            {
                this.VisitStatementBlock(blockNode);
            }

            node.StatementBlock = blockNode;

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

            var expression = new PExpressionNode(parentNode);
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                if (base.TokenStream.Peek().Type == TokenType.New)
                {
                    node.Expression = expression;
                    parentNode.Statements.Add(node);
                    return;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Payload)
                {
                    var payloadNode = new PPayloadReceiveNode();
                    this.VisitReceivedPayload(payloadNode);
                    expression.StmtTokens.Add(null);
                    expression.Payloads.Add(payloadNode);
                    if (base.TokenStream.Peek().Type == TokenType.Semicolon)
                    {
                        break;
                    }
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
        /// Visits a received payload.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitReceivedPayload(PPayloadReceiveNode node)
        {
            node.PayloadKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.As)
            {
                this.ReportParsingError("Expected \"as\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.As
                });
            }

            node.AsKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            var typeNode = new PTypeNode();
            this.VisitTypeIdentifier(typeNode);
            node.Type = typeNode;

            if (base.TokenStream.Peek().Type == TokenType.RightParenthesis)
            {
                node.RightParenthesisToken = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Peek().Type == TokenType.Dot)
                {
                    node.DotToken = base.TokenStream.Peek();

                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();

                    if (base.TokenStream.Done ||
                        base.TokenStream.Peek().Type != TokenType.Identifier)
                    {
                        this.ReportParsingError("Expected index.");
                        throw new EndOfTokensException(new List<TokenType>
                        {
                            TokenType.Identifier
                        });
                    }

                    node.IndexToken = base.TokenStream.Peek();

                    base.TokenStream.Index++;
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
            if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Seq &&
                    base.TokenStream.Peek().Type != TokenType.Map &&
                    base.TokenStream.Peek().Type != TokenType.LeftParenthesis))
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

            if (base.TokenStream.Peek().Type == TokenType.MachineDecl ||
                base.TokenStream.Peek().Type == TokenType.Int ||
                base.TokenStream.Peek().Type == TokenType.Bool)
            {
                node.TypeTokens.Add(base.TokenStream.Peek());
            }
            else if (base.TokenStream.Peek().Type == TokenType.Seq)
            {
                this.VisitSeqTypeIdentifier(node);
            }
            else if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
            {
                this.VisitTupleTypeIdentifier(node);
            }

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
        }

        /// <summary>
        /// Visits a seq type identifier.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitSeqTypeIdentifier(PTypeNode node)
        {
            node.TypeTokens.Add(base.TokenStream.Peek());

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftSquareBracket)
            {
                this.ReportParsingError("Expected \"[\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftSquareBracket
                });
            }

            node.TypeTokens.Add(base.TokenStream.Peek());

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.VisitTypeIdentifier(node);

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.RightSquareBracket)
            {
                this.ReportParsingError("Expected \"]\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.RightSquareBracket
                });
            }

            node.TypeTokens.Add(base.TokenStream.Peek());
        }

        /// <summary>
        /// Visits a tuple type identifier.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitTupleTypeIdentifier(PTypeNode node)
        {
            node.TypeTokens.Add(base.TokenStream.Peek());

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.Int &&
                base.TokenStream.Peek().Type != TokenType.Bool &&
                base.TokenStream.Peek().Type != TokenType.Seq &&
                base.TokenStream.Peek().Type != TokenType.Map &&
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis))
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

            bool expectsComma = false;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                if ((!expectsComma &&
                    base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Seq &&
                    base.TokenStream.Peek().Type != TokenType.Map &&
                    base.TokenStream.Peek().Type != TokenType.LeftParenthesis) ||
                    (expectsComma && base.TokenStream.Peek().Type != TokenType.Comma))
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.MachineDecl ||
                    base.TokenStream.Peek().Type == TokenType.Int ||
                    base.TokenStream.Peek().Type == TokenType.Bool ||
                    base.TokenStream.Peek().Type == TokenType.Seq ||
                    base.TokenStream.Peek().Type == TokenType.Map ||
                    base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
                {
                    this.VisitTypeIdentifier(node);
                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    node.TypeTokens.Add(base.TokenStream.Peek());

                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = false;
                }
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

            node.TypeTokens.Add(base.TokenStream.Peek());
        }

        /// <summary>
        /// Visits an argument list.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitArgumentsList(PExpressionNode node)
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

        /// <summary>
        /// Visits a payload.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitPayload(PPayloadSendExpressionNode node)
        {
            if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
            {
                this.VisitPayloadTuple(node);
            }
            else
            {
                node.StmtTokens.Add(base.TokenStream.Peek());
            }

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
        }

        /// <summary>
        /// Visits a payload tuple.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitPayloadTuple(PPayloadSendExpressionNode node)
        {
            node.StmtTokens.Add(base.TokenStream.Peek());

            base.TokenStream.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            bool expectsComma = false;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                if ((!expectsComma &&
                    base.TokenStream.Peek().Type != TokenType.This &&
                    base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.True &&
                    base.TokenStream.Peek().Type != TokenType.False &&
                    base.TokenStream.Peek().Type != TokenType.Null &&
                    base.TokenStream.Peek().Type != TokenType.LeftParenthesis) ||
                    (expectsComma && base.TokenStream.Peek().Type != TokenType.Comma))
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.This ||
                    base.TokenStream.Peek().Type == TokenType.Identifier ||
                    base.TokenStream.Peek().Type == TokenType.True ||
                    base.TokenStream.Peek().Type == TokenType.False ||
                    base.TokenStream.Peek().Type == TokenType.Null ||
                    base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
                {
                    this.VisitPayload(node);
                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    node.StmtTokens.Add(base.TokenStream.Peek());

                    base.TokenStream.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    expectsComma = false;
                }
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
            
            node.StmtTokens.Add(base.TokenStream.Peek());
        }

        #endregion
    }
}
