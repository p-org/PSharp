//-----------------------------------------------------------------------
// <copyright file="PSharpErrorParser.cs">
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
using System.Linq;
using System.Text;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# error parser.
    /// </summary>
    public class PSharpErrorParser : BaseParser
    {
        #region fields

        /// <summary>
        /// File path of syntax tree currently analysed.
        /// </summary>
        private string FilePath;

        /// <summary>
        /// True if the parser is running internally and not from
        /// visual studio or another external tool.
        /// Else false.
        /// </summary>
        private bool IsRunningInternally;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        public PSharpErrorParser()
            : base()
        {
            this.FilePath = "";
            this.IsRunningInternally = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filePath">File path</param>
        internal PSharpErrorParser(string filePath)
            : base()
        {
            this.FilePath = filePath;
            this.IsRunningInternally = true;
        }

        #endregion

        #region protected API

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
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.Using:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckUsingDirective();
                    base.Index++;
                    break;

                case TokenType.NamespaceDecl:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckNamespaceDeclaration();
                    base.Index++;
                    break;

                case TokenType.RightSquareBracket:
                case TokenType.LeftParenthesis:
                case TokenType.RightParenthesis:
                case TokenType.LeftCurlyBracket:
                case TokenType.CommentEnd:
                    this.ReportParsingError("Invalid use of \"" + token.Text + "\".");
                    break;

                default:
                    this.ReportParsingError("Must be declared inside a namespace.");
                    break;
            }

            this.ParseNextToken();
        }

        #endregion

        #region private API

        /// <summary>
        /// Checks a using directive for errors.
        /// </summary>
        private void CheckUsingDirective()
        {
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
        /// Checks a namespace declaration for errors.
        /// </summary>
        private void CheckNamespaceDeclaration()
        {
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

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.CheckNextIntraNamespaceDeclaration();
        }

        /// <summary>
        /// Checks the next intra-namespace declration for errors.
        /// </summary>
        private void CheckNextIntraNamespaceDeclaration()
        {
            if (base.Index == base.Tokens.Count)
            {
                this.ReportParsingError("Expected \"}\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Internal,
                    TokenType.Public,
                    TokenType.Abstract,
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
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.EventDecl:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckEventDeclaration();
                    base.Index++;
                    break;

                case TokenType.MachineDecl:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckMachineDeclaration();
                    base.Index++;
                    break;

                case TokenType.Internal:
                case TokenType.Public:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckTopLevelAccessModifier();
                    base.Index++;
                    break;

                case TokenType.Abstract:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckTopLevelAbstractModifier();
                    base.Index++;
                    break;

                case TokenType.LeftSquareBracket:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckAttributeList();
                    base.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    fixpoint = true;
                    base.Index++;
                    break;

                case TokenType.Private:
                case TokenType.Protected:
                    this.ReportParsingError("Event and machine declarations must be internal or public.");
                    break;

                case TokenType.RightSquareBracket:
                case TokenType.LeftParenthesis:
                case TokenType.RightParenthesis:
                case TokenType.LeftCurlyBracket:
                case TokenType.CommentEnd:
                    this.ReportParsingError("Invalid use of \"" + token.Text + "\".");
                    break;

                default:
                    this.ReportParsingError("Unexpected declaration.");
                    break;
            }

            if (!fixpoint)
            {
                this.CheckNextIntraNamespaceDeclaration();
            }
        }

        /// <summary>
        /// Checks a top level access modifier for errors.
        /// </summary>
        private void CheckTopLevelAccessModifier()
        {
            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Abstract &&
                base.Tokens[base.Index].Type != TokenType.EventDecl &&
                base.Tokens[base.Index].Type != TokenType.MachineDecl))
            {
                this.ReportParsingError("Expected event or machine declaration.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Abstract,
                    TokenType.EventDecl,
                    TokenType.MachineDecl
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.Abstract)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Index == base.Tokens.Count ||
                    (base.Tokens[base.Index].Type != TokenType.EventDecl &&
                    base.Tokens[base.Index].Type != TokenType.MachineDecl))
                {
                    this.ReportParsingError("Expected event or machine declaration.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.EventDecl,
                        TokenType.MachineDecl
                    });
                }
            }

            this.CheckTopLevelDeclaration();
        }

        /// <summary>
        /// Checks a top level abstract modifier for errors.
        /// </summary>
        private void CheckTopLevelAbstractModifier()
        {
            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Internal &&
                base.Tokens[base.Index].Type != TokenType.Public &&
                base.Tokens[base.Index].Type != TokenType.EventDecl &&
                base.Tokens[base.Index].Type != TokenType.MachineDecl))
            {
                this.ReportParsingError("Expected event or machine declaration.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Internal,
                    TokenType.Public,
                    TokenType.EventDecl,
                    TokenType.MachineDecl
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.Internal ||
                base.Tokens[base.Index].Type == TokenType.Public)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Index == base.Tokens.Count ||
                    (base.Tokens[base.Index].Type != TokenType.EventDecl &&
                    base.Tokens[base.Index].Type != TokenType.MachineDecl))
                {
                    this.ReportParsingError("Expected event or machine declaration.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.EventDecl,
                        TokenType.MachineDecl
                    });
                }
            }

            this.CheckTopLevelDeclaration();
        }

        /// <summary>
        /// Checks a top level declaration for errors.
        /// </summary>
        private void CheckTopLevelDeclaration()
        {
            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.EventDecl &&
                base.Tokens[base.Index].Type != TokenType.MachineDecl))
            {
                this.ReportParsingError("Expected event or machine declaration.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Abstract,
                    TokenType.EventDecl,
                    TokenType.MachineDecl
                });
            }
            
            if (base.Tokens[base.Index].Type == TokenType.EventDecl)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                this.CheckEventDeclaration();
            }
            else if (base.Tokens[base.Index].Type == TokenType.MachineDecl)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                this.CheckMachineDeclaration();
            }
        }

        /// <summary>
        /// Checks an event declaration for errors.
        /// </summary>
        private void CheckEventDeclaration()
        {
            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, base.Index, TokenType.EventIdentifier);

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
        /// Checks a machine declaration for errors.
        /// </summary>
        private void CheckMachineDeclaration()
        {
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
            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, base.Index, TokenType.MachineIdentifier);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Doublecolon &&
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket))
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Doublecolon,
                    TokenType.LeftCurlyBracket
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.Doublecolon)
            {
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

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, base.Index, TokenType.MachineLeftCurlyBracket);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.CheckNextIntraMachineDeclaration();
        }

        /// <summary>
        /// Checks the next intra-machine declration for errors.
        /// </summary>
        private void CheckNextIntraMachineDeclaration()
        {
            if (base.Index == base.Tokens.Count)
            {
                this.ReportParsingError("Expected \"}\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Private,
                    TokenType.Protected,
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
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.StateDecl:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckStateDeclaration();
                    base.Index++;
                    break;

                case TokenType.ActionDecl:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckActionDeclaration();
                    base.Index++;
                    break;

                case TokenType.Identifier:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckFieldOrMethodDeclaration();
                    base.Index++;
                    break;

                case TokenType.Private:
                case TokenType.Protected:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckMachineLevelAccessModifier();
                    base.Index++;
                    break;

                case TokenType.LeftSquareBracket:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckAttributeList();
                    base.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                        base.Tokens[base.Index].Line, base.Index, TokenType.MachineRightCurlyBracket);
                    base.CurrentMachine = "";
                    fixpoint = true;
                    base.Index++;
                    break;

                case TokenType.Internal:
                case TokenType.Public:
                    this.ReportParsingError("Machine fields, states or actions must be private or protected.");
                    break;

                case TokenType.Abstract:
                    this.ReportParsingError("Machine fields, states or actions cannot be abstract.");
                    break;

                case TokenType.RightSquareBracket:
                case TokenType.LeftParenthesis:
                case TokenType.RightParenthesis:
                case TokenType.LeftCurlyBracket:
                case TokenType.CommentEnd:
                    this.ReportParsingError("Invalid use of \"" + token.Text + "\".");
                    break;

                default:
                    this.ReportParsingError("Unexpected declaration.");
                    break;
            }

            if (!fixpoint)
            {
                this.CheckNextIntraMachineDeclaration();
            }
        }

        /// <summary>
        /// Checks a machine level access modifier for errors.
        /// </summary>
        private void CheckMachineLevelAccessModifier()
        {
            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Override &&
                base.Tokens[base.Index].Type != TokenType.StateDecl &&
                base.Tokens[base.Index].Type != TokenType.ActionDecl &&
                base.Tokens[base.Index].Type != TokenType.Identifier))
            {
                this.ReportParsingError("Expected state, action or method declaration.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Override,
                    TokenType.StateDecl,
                    TokenType.ActionDecl,
                    TokenType.Identifier
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.Override)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();

                if (base.Index == base.Tokens.Count ||
                    (base.Tokens[base.Index].Type != TokenType.StateDecl &&
                    base.Tokens[base.Index].Type != TokenType.ActionDecl &&
                    base.Tokens[base.Index].Type != TokenType.Identifier))
                {
                    this.ReportParsingError("Expected state, action or method declaration.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.StateDecl,
                        TokenType.ActionDecl,
                        TokenType.Identifier
                    });
                }
            }

            if (base.Tokens[base.Index].Type == TokenType.StateDecl)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                this.CheckStateDeclaration();
            }
            else if (base.Tokens[base.Index].Type == TokenType.ActionDecl)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                this.CheckActionDeclaration();
            }
            else if (base.Tokens[base.Index].Type == TokenType.Identifier)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                this.CheckFieldOrMethodDeclaration();
            }
        }

        /// <summary>
        /// Checks a state declaration for errors.
        /// </summary>
        private void CheckStateDeclaration()
        {
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
            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, base.Index, TokenType.StateIdentifier);

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

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, base.Index, TokenType.StateLeftCurlyBracket);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            this.CheckNextIntraStateDeclaration();
        }

        /// <summary>
        /// Checks the next intra-state declration for errors.
        /// </summary>
        private void CheckNextIntraStateDeclaration()
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
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.Entry:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckStateEntryDeclaration();
                    base.Index++;
                    break;

                case TokenType.Exit:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckStateExitDeclaration();
                    base.Index++;
                    break;

                case TokenType.OnAction:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckStateActionDeclaration();
                    base.Index++;
                    break;

                case TokenType.DeferEvent:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckDeferEventsDeclaration();
                    base.Index++;
                    break;

                case TokenType.IgnoreEvent:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckIgnoreEventsDeclaration();
                    base.Index++;
                    break;

                case TokenType.Private:
                case TokenType.Protected:
                case TokenType.Internal:
                case TokenType.Public:
                    this.ReportParsingError("State actions cannot have modifiers.");
                    break;

                case TokenType.Abstract:
                    this.ReportParsingError("State actions cannot be abstract.");
                    break;

                case TokenType.LeftSquareBracket:
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckAttributeList();
                    base.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                        base.Tokens[base.Index].Line, base.Index, TokenType.StateRightCurlyBracket);
                    base.CurrentState = "";
                    fixpoint = true;
                    base.Index++;
                    break;

                case TokenType.RightSquareBracket:
                case TokenType.LeftParenthesis:
                case TokenType.RightParenthesis:
                case TokenType.LeftCurlyBracket:
                case TokenType.CommentEnd:
                    this.ReportParsingError("Invalid use of \"" + token.Text + "\".");
                    break;

                default:
                    this.ReportParsingError("Unexpected declaration.");
                    break;
            }

            if (!fixpoint)
            {
                this.CheckNextIntraStateDeclaration();
            }
        }

        /// <summary>
        /// Checks a state entry declaration for errors.
        /// </summary>
        private void CheckStateEntryDeclaration()
        {
            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            this.CheckCodeRegion();
        }

        /// <summary>
        /// Checks a state exit declaration for errors.
        /// </summary>
        private void CheckStateExitDeclaration()
        {
            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            this.CheckCodeRegion();
        }

        /// <summary>
        /// Checks a state action declaration for errors.
        /// </summary>
        private void CheckStateActionDeclaration()
        {
            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, base.Index, TokenType.EventIdentifier);

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

                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                    base.Tokens[base.Index].Line, base.Index, TokenType.ActionIdentifier);

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

                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                    base.Tokens[base.Index].Line, base.Index, TokenType.StateIdentifier);

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
        /// Checks a defer events declaration for errors.
        /// </summary>
        private void CheckDeferEventsDeclaration()
        {
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
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                        base.Tokens[base.Index].Line, base.Index, TokenType.EventIdentifier);
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
        /// Checks an ignore events declaration for errors.
        /// </summary>
        private void CheckIgnoreEventsDeclaration()
        {
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
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                        base.Tokens[base.Index].Line, base.Index, TokenType.EventIdentifier);
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
        /// Checks an action declaration for errors.
        /// </summary>
        private void CheckActionDeclaration()
        {
            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected action identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            if (ParsingEngine.MachineFieldsAndMethods != null)
            {
                if (!ParsingEngine.MachineFieldsAndMethods.ContainsKey(base.CurrentMachine))
                {
                    ParsingEngine.MachineFieldsAndMethods.Add(base.CurrentMachine,
                        new HashSet<string>());
                }

                ParsingEngine.MachineFieldsAndMethods[base.CurrentMachine].Add(base.Tokens[base.Index].Text);
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, base.Index, TokenType.ActionIdentifier);

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

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();
            this.CheckCodeRegion();
        }

        /// <summary>
        /// Checks a field or method declaration for errors.
        /// </summary>
        private void CheckFieldOrMethodDeclaration()
        {
            if (base.Index == base.Tokens.Count ||
                    (base.Tokens[base.Index].Type != TokenType.LessThanOperator &&
                    base.Tokens[base.Index].Type != TokenType.Identifier))
            {
                this.ReportParsingError("Expected state, action or method declaration.");
                throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.LessThanOperator,
                        TokenType.Identifier
                    });
            }

            if (base.Tokens[base.Index].Type == TokenType.LessThanOperator)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                this.CheckLessThanOperator();

                if (base.Index == base.Tokens.Count ||
                    base.Tokens[base.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected method declaration.");
                    throw new EndOfTokensException(new List<TokenType>
                        {
                            TokenType.Identifier
                        });
                }
            }

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.Identifier))
            {
                this.ReportParsingError("Expected state, action or method declaration.");
                throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.LessThanOperator,
                        TokenType.Identifier
                    });
            }

            if (ParsingEngine.MachineFieldsAndMethods != null)
            {
                if (!ParsingEngine.MachineFieldsAndMethods.ContainsKey(base.CurrentMachine))
                {
                    ParsingEngine.MachineFieldsAndMethods.Add(base.CurrentMachine,
                        new HashSet<string>());
                }

                ParsingEngine.MachineFieldsAndMethods[base.CurrentMachine].Add(base.Tokens[base.Index].Text);
            }

            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.Semicolon &&
                base.Tokens[base.Index].Type != TokenType.LeftParenthesis)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Tokens[base.Index].Type == TokenType.LeftParenthesis)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                this.CheckMethodDeclaration();
            }
        }

        /// <summary>
        /// Checks a method declaration for errors.
        /// </summary>
        private void CheckMethodDeclaration()
        {
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.RightParenthesis)
            {
                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                    base.Tokens[base.Index].Line, base.Index);

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

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
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                this.CheckCodeRegion();
            }
        }

        /// <summary>
        /// Checks a code region for errors.
        /// </summary>
        private void CheckCodeRegion()
        {
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
                else if (base.Tokens[base.Index].Type == TokenType.DoAction)
                {
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                        base.Tokens[base.Index].Line, base.Index, TokenType.DoLoop);
                }
                else if (base.Tokens[base.Index].Type == TokenType.New)
                {
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckNewStatement();
                }
                else if (base.Tokens[base.Index].Type == TokenType.CreateMachine)
                {
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckCreateStatement();
                }
                else if (base.Tokens[base.Index].Type == TokenType.RaiseEvent)
                {
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckRaiseStatement();
                }
                else if (base.Tokens[base.Index].Type == TokenType.SendEvent)
                {
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckSendStatement();
                }
                else if (base.Tokens[base.Index].Type == TokenType.DeleteMachine)
                {
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckDeleteStatement();
                }

                if (bracketCounter > 0)
                {
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                }
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
        }

        /// <summary>
        /// Checks a new statement for errors.
        /// </summary>
        private void CheckNewStatement()
        {
            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected type identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.LeftParenthesis)
            {
                if (base.Tokens[base.Index].Type != TokenType.Identifier &&
                    base.Tokens[base.Index].Type != TokenType.Dot &&
                    base.Tokens[base.Index].Type != TokenType.LessThanOperator &&
                    base.Tokens[base.Index].Type != TokenType.NewLine)
                {
                    this.ReportParsingError("Expected type identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.Dot,
                        TokenType.LessThanOperator
                    });
                }

                if (base.Tokens[base.Index].Type == TokenType.Identifier)
                {
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                        base.Tokens[base.Index].Line, base.Index, TokenType.TypeIdentifier);
                }
                else if (base.Tokens[base.Index].Type == TokenType.LessThanOperator)
                {
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckLessThanOperator();
                }

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.LeftParenthesis)
            {
                this.ReportParsingError("Expected \"(\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis
                });
            }

            base.Index--;
        }

        /// <summary>
        /// Checks a create statement for errors.
        /// </summary>
        private void CheckCreateStatement()
        {
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
                base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket &&
                base.Tokens[base.Index].Type != TokenType.Semicolon)
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

                if (base.Tokens[base.Index].Type == TokenType.Identifier)
                {
                    base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                        base.Tokens[base.Index].Line, base.Index, TokenType.MachineIdentifier);
                }

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
            }

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
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                this.CheckArgumentsList();
            }
        }

        /// <summary>
        /// Checks a raise statement for errors.
        /// </summary>
        private void CheckRaiseStatement()
        {
            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, base.Index, TokenType.EventIdentifier);

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
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                this.CheckArgumentsList();
            }

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
        /// Checks a send statement for errors.
        /// </summary>
        private void CheckSendStatement()
        {
            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                base.Tokens[base.Index].Line, base.Index, TokenType.EventIdentifier);

            base.Index++;
            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                (base.Tokens[base.Index].Type != TokenType.LeftCurlyBracket &&
                base.Tokens[base.Index].Type != TokenType.ToMachine))
            {
                this.ReportParsingError("Expected \"{\" or \"to\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket,
                    TokenType.ToMachine
                });
            }

            if (base.Tokens[base.Index].Type == TokenType.LeftCurlyBracket)
            {
                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
                this.CheckArgumentsList();
                base.Index++;
            }

            base.SkipWhiteSpaceAndCommentTokens();

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.ToMachine)
            {
                this.ReportParsingError("Expected \"to\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.ToMachine
                });
            }

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
        /// Checks a delete statement for errors.
        /// </summary>
        private void CheckDeleteStatement()
        {
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
        /// Checks an attribute list for errors.
        /// </summary>
        private void CheckAttributeList()
        {
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.RightSquareBracket)
            {
                base.Tokens[base.Index] = new Token(base.Tokens[base.Index].Text,
                    base.Tokens[base.Index].Line, base.Index);

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
        /// Checks an argument list for errors.
        /// </summary>
        private void CheckArgumentsList()
        {
            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.RightCurlyBracket)
            {
                if (base.Tokens[base.Index].Type == TokenType.New)
                {
                    base.Index++;
                    base.SkipWhiteSpaceAndCommentTokens();
                    this.CheckNewStatement();
                }

                base.Index++;
                base.SkipWhiteSpaceAndCommentTokens();
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
        }

        /// <summary>
        /// Checks a less than operator region for errors.
        /// </summary>
        private void CheckLessThanOperator()
        {
            this.CheckGenericList();
        }

        /// <summary>
        /// Checks a generic list for errors.
        /// </summary>
        private void CheckGenericList()
        {
            var tokens = new List<Token>();
            var replaceIdx = base.Index;

            while (base.Index < base.Tokens.Count &&
                base.Tokens[base.Index].Type != TokenType.GreaterThanOperator)
            {
                if (base.Tokens[base.Index].Type == TokenType.Semicolon)
                {
                    return;
                }

                tokens.Add(new Token(base.Tokens[replaceIdx].Text, base.Tokens[replaceIdx].Line, replaceIdx));
                replaceIdx++;
            }

            if (base.Index == base.Tokens.Count ||
                base.Tokens[base.Index].Type != TokenType.GreaterThanOperator)
            {
                this.ReportParsingError("Expected \">\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.GreaterThanOperator
                });
            }

            foreach (var tok in tokens)
            {
                base.Tokens[base.Index] = tok;
                base.Index++;
            }
        }

        /// <summary>
        /// Reports a parsing error. Only works if the parser is
        /// running internally.
        /// </summary>
        /// <param name="error">Error</param>
        private void ReportParsingError(string error)
        {
            if (!this.IsRunningInternally)
            {
                return;
            }

            var errorIndex = base.Index;
            if (base.Index == base.Tokens.Count &&
                base.Index > 0)
            {
                errorIndex--;
            }

            var errorToken = base.Tokens[errorIndex];
            var errorLine = base.OriginalTokens.Where(val => val.Line == errorToken.Line);

            error += "\nIn " + this.FilePath + " (line " + errorToken.Line + "):\n";
            foreach (var token in errorLine)
            {
                error += token.Text;
            }

            foreach (var token in errorLine)
            {
                if (token.Equals(errorToken) && errorIndex == base.Index)
                {
                    error += new StringBuilder().Append('~', token.Text.Length);
                    break;
                }
                else
                {
                    error += new StringBuilder().Append(' ', token.Text.Length);
                }
            }

            if (errorIndex != base.Index)
            {
                error += "^";
            }

            ErrorReporter.ReportErrorAndExit(error);
        }

        #endregion
    }
}
