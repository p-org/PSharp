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
using System.Linq;
using System.Text;

using Microsoft.PSharp.Parsing.Syntax;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# parser.
    /// </summary>
    public class PSharpParser : IParser
    {
        #region fields

        /// <summary>
        /// File path of syntax tree currently parsed.
        /// </summary>
        private string FilePath;

        /// <summary>
        /// List of original tokens.
        /// </summary>
        private List<Token> OriginalTokens;

        /// <summary>
        /// Root of the P# program.
        /// </summary>
        private ProgramRoot Root;

        /// <summary>
        /// List of tokens.
        /// </summary>
        private List<Token> Tokens;

        /// <summary>
        /// The current index.
        /// </summary>
        private int Index;

        /// <summary>
        /// The name of the currently parsed machine.
        /// </summary>
        private string CurrentMachine;

        /// <summary>
        /// The name of the currently parsed state.
        /// </summary>
        private string CurrentState;

        /// <summary>
        /// List of expected token types at end of parsing.
        /// </summary>
        private List<TokenType> ExpectedTokenTypes;

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
        public PSharpParser()
        {
            this.IsRunningInternally = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filePath">File path</param>
        internal PSharpParser(string filePath)
        {
            this.FilePath = filePath;
            this.IsRunningInternally = true;
        }

        /// <summary>
        /// Returns the parsed tokens.
        /// </summary>
        /// <param name="tokens">List of tokens</param>
        /// <returns>The P# program root</returns>
        public ProgramRoot ParseTokens(List<Token> tokens)
        {
            this.OriginalTokens = tokens.ToList();
            this.Root = new ProgramRoot(this.FilePath);
            this.Tokens = tokens;
            this.Index = 0;
            this.CurrentMachine = "";
            this.CurrentState = "";
            this.ExpectedTokenTypes = new List<TokenType>();

            try
            {
                this.ParseNextToken();
            }
            catch (EndOfTokensException ex)
            {
                this.ExpectedTokenTypes = ex.ExpectedTokenTypes;
            }
            catch (ParsingException ex)
            {
                ErrorReporter.ReportErrorAndExit(ex.Message);
            }

            this.Root.GenerateTextUnits();
            return this.Root;
        }

        /// <summary>
        /// Returns the expected token types at the end of parsing.
        /// </summary>
        /// <returns>Expected token types</returns>
        public List<TokenType> GetExpectedTokenTypes()
        {
            return this.ExpectedTokenTypes;
        }

        #endregion

        #region protected API

        /// <summary>
        /// Parses the next available token.
        /// </summary>
        private void ParseNextToken()
        {
            if (this.Index == this.Tokens.Count)
            {
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Using,
                    TokenType.NamespaceDecl
                });
            }

            var token = this.Tokens[this.Index];
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.Comment:
                case TokenType.NewLine:
                    this.Index++;
                    break;

                case TokenType.CommentLine:
                case TokenType.Region:
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.Using:
                    this.VisitUsingDeclaration();
                    this.Index++;
                    break;

                case TokenType.NamespaceDecl:
                    this.VisitNamespaceDeclaration();
                    this.Index++;
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

        //// <summary>
        /// Visits a using declaration.
        /// </summary>
        private void VisitUsingDeclaration()
        {
            var node = new UsingDeclarationNode();
            node.UsingKeyword = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.Semicolon)
            {
                if (this.Tokens[this.Index].Type != TokenType.Identifier &&
                    this.Tokens[this.Index].Type != TokenType.Dot &&
                    this.Tokens[this.Index].Type != TokenType.NewLine)
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
                    node.IdentifierTokens.Add(this.Tokens[this.Index]);
                }

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = this.Tokens[this.Index];

            this.Root.UsingDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a namespace declaration.
        /// </summary>
        private void VisitNamespaceDeclaration()
        {
            var node = new NamespaceDeclarationNode();
            node.NamespaceKeyword = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected namespace identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.LeftCurlyBracket)
            {
                if (this.Tokens[this.Index].Type != TokenType.Identifier &&
                    this.Tokens[this.Index].Type != TokenType.Dot &&
                    this.Tokens[this.Index].Type != TokenType.NewLine)
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
                    node.IdentifierTokens.Add(this.Tokens[this.Index]);
                }

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            node.LeftCurlyBracketToken = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextIntraNamespaceDeclaration(node);

            this.Root.NamespaceDeclarations.Add(node);
        }

        /// <summary>
        /// Visits the next intra-namespace declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextIntraNamespaceDeclaration(NamespaceDeclarationNode node)
        {
            if (this.Index == this.Tokens.Count)
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
            var token = this.Tokens[this.Index];
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.Comment:
                case TokenType.NewLine:
                    this.Index++;
                    break;

                case TokenType.CommentLine:
                case TokenType.Region:
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.EventDecl:
                    this.VisitEventDeclaration(node, null);
                    this.Index++;
                    break;

                case TokenType.MainMachine:
                    this.VisitMainMachineModifier(node, null);
                    this.Index++;
                    break;

                case TokenType.MachineDecl:
                    this.VisitMachineDeclaration(node, false, null, null);
                    this.Index++;
                    break;

                case TokenType.Internal:
                case TokenType.Public:
                    this.VisitTopLevelAccessModifier(node);
                    this.Index++;
                    break;

                case TokenType.Abstract:
                case TokenType.Virtual:
                    this.VisitTopLevelAbstractModifier(node);
                    this.Index++;
                    break;

                case TokenType.LeftSquareBracket:
                    this.Index++;
                    this.SkipWhiteSpaceAndCommentTokens();
                    this.VisitAttributeList();
                    this.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    node.RightCurlyBracketToken = this.Tokens[this.Index];
                    fixpoint = true;
                    this.Index++;
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
                this.VisitNextIntraNamespaceDeclaration(node);
            }
        }

        /// <summary>
        /// Visits a top level access modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitTopLevelAccessModifier(NamespaceDeclarationNode parentNode)
        {
            var modifier = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                (this.Tokens[this.Index].Type != TokenType.Abstract &&
                this.Tokens[this.Index].Type != TokenType.Virtual &&
                this.Tokens[this.Index].Type != TokenType.MainMachine &&
                this.Tokens[this.Index].Type != TokenType.EventDecl &&
                this.Tokens[this.Index].Type != TokenType.MachineDecl))
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
            if (this.Tokens[this.Index].Type == TokenType.Abstract)
            {
                abstractModifier = this.Tokens[this.Index];

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();

                if (this.Index == this.Tokens.Count ||
                    this.Tokens[this.Index].Type != TokenType.MachineDecl)
                {
                    this.ReportParsingError("Expected machine declaration.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.MachineDecl
                    });
                }

                this.VisitMachineDeclaration(parentNode, false, modifier, abstractModifier);
            }
            else if (this.Tokens[this.Index].Type == TokenType.EventDecl)
            {
                this.VisitEventDeclaration(parentNode, modifier);
            }
            else if (this.Tokens[this.Index].Type == TokenType.MainMachine)
            {
                this.VisitMainMachineModifier(parentNode, modifier);
            }
            else if (this.Tokens[this.Index].Type == TokenType.MachineDecl)
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
            var abstractModifier = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                (this.Tokens[this.Index].Type != TokenType.Internal &&
                this.Tokens[this.Index].Type != TokenType.Public &&
                this.Tokens[this.Index].Type != TokenType.MachineDecl))
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
            if (this.Tokens[this.Index].Type == TokenType.Internal ||
                this.Tokens[this.Index].Type == TokenType.Public)
            {
                modifier = this.Tokens[this.Index];

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();

                if (this.Index == this.Tokens.Count ||
                    (this.Tokens[this.Index].Type != TokenType.MachineDecl))
                {
                    this.ReportParsingError("Expected machine declaration.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.MachineDecl
                    });
                }
            }

            if (this.Tokens[this.Index].Type == TokenType.EventDecl)
            {
                this.VisitEventDeclaration(parentNode, modifier);
            }
            else if (this.Tokens[this.Index].Type == TokenType.MachineDecl)
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
            node.EventKeyword = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                this.Tokens[this.Index].Line, TokenType.EventIdentifier);

            node.Identifier = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }

            node.SemicolonToken = this.Tokens[this.Index];

            parentNode.EventDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a main machine modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitMainMachineModifier(NamespaceDeclarationNode parentNode, Token modifier)
        {
            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.MachineDecl)
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
            node.MachineKeyword = this.Tokens[this.Index];
            
            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected machine identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            this.CurrentMachine = this.Tokens[this.Index].Text;
            this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                this.Tokens[this.Index].Line, TokenType.MachineIdentifier);

            node.Identifier = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                (this.Tokens[this.Index].Type != TokenType.Doublecolon &&
                this.Tokens[this.Index].Type != TokenType.LeftCurlyBracket))
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Doublecolon,
                    TokenType.LeftCurlyBracket
                });
            }

            if (this.Tokens[this.Index].Type == TokenType.Doublecolon)
            {
                node.DoubleColonToken = this.Tokens[this.Index];

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();

                if (this.Index == this.Tokens.Count ||
                    this.Tokens[this.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected base machine identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                while (this.Index < this.Tokens.Count &&
                    this.Tokens[this.Index].Type != TokenType.LeftCurlyBracket)
                {
                    if (this.Tokens[this.Index].Type != TokenType.Identifier &&
                        this.Tokens[this.Index].Type != TokenType.Dot &&
                        this.Tokens[this.Index].Type != TokenType.NewLine)
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
                        node.BaseNameTokens.Add(this.Tokens[this.Index]);
                    }

                    this.Index++;
                    this.SkipWhiteSpaceAndCommentTokens();
                }
            }

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                this.Tokens[this.Index].Line, TokenType.MachineLeftCurlyBracket);

            node.LeftCurlyBracketToken = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextIntraMachineDeclaration(node);

            parentNode.MachineDeclarations.Add(node);
        }

        /// <summary>
        /// Visits the next intra-machine declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextIntraMachineDeclaration(MachineDeclarationNode node)
        {
            if (this.Index == this.Tokens.Count)
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
            var token = this.Tokens[this.Index];
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.Comment:
                case TokenType.NewLine:
                    this.Index++;
                    break;

                case TokenType.CommentLine:
                case TokenType.Region:
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.StartState:
                    this.VisitStartStateModifier(node, null);
                    this.Index++;
                    break;

                case TokenType.StateDecl:
                    this.VisitStateDeclaration(node, false, null);
                    this.Index++;
                    break;

                case TokenType.ActionDecl:
                    this.VisitActionDeclaration(node, null, null);
                    this.Index++;
                    break;

                case TokenType.Identifier:
                    this.VisitFieldOrMethodDeclaration(node, null, null);
                    this.Index++;
                    break;

                case TokenType.Private:
                case TokenType.Protected:
                    this.VisitMachineLevelAccessModifier(node);
                    this.Index++;
                    break;

                case TokenType.Abstract:
                case TokenType.Virtual:
                    this.VisitMachineLevelAbstractModifier(node);
                    this.Index++;
                    break;

                case TokenType.LeftSquareBracket:
                    this.Index++;
                    this.SkipWhiteSpaceAndCommentTokens();
                    this.VisitAttributeList();
                    this.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                        this.Tokens[this.Index].Line, TokenType.MachineRightCurlyBracket);
                    node.RightCurlyBracketToken = this.Tokens[this.Index];
                    this.CurrentMachine = "";
                    fixpoint = true;
                    this.Index++;
                    break;

                case TokenType.Internal:
                case TokenType.Public:
                    this.ReportParsingError("Machine fields, states or actions must be private or protected.");
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
                this.VisitNextIntraMachineDeclaration(node);
            }
        }

        /// <summary>
        /// Visits a machine level access modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitMachineLevelAccessModifier(MachineDeclarationNode parentNode)
        {
            var modifier = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                (this.Tokens[this.Index].Type != TokenType.Abstract &&
                this.Tokens[this.Index].Type != TokenType.Virtual &&
                this.Tokens[this.Index].Type != TokenType.Override &&
                this.Tokens[this.Index].Type != TokenType.StartState &&
                this.Tokens[this.Index].Type != TokenType.StateDecl &&
                this.Tokens[this.Index].Type != TokenType.ActionDecl &&
                this.Tokens[this.Index].Type != TokenType.Int &&
                this.Tokens[this.Index].Type != TokenType.Bool &&
                this.Tokens[this.Index].Type != TokenType.Identifier))
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

            if (this.Tokens[this.Index].Type == TokenType.Abstract ||
                this.Tokens[this.Index].Type == TokenType.Virtual ||
                this.Tokens[this.Index].Type == TokenType.Override)
            {
                var inheritanceModifier = this.Tokens[this.Index];

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();

                if (this.Index == this.Tokens.Count ||
                    (this.Tokens[this.Index].Type != TokenType.ActionDecl &&
                    this.Tokens[this.Index].Type != TokenType.Int &&
                    this.Tokens[this.Index].Type != TokenType.Bool &&
                    this.Tokens[this.Index].Type != TokenType.Identifier))
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

                if (this.Tokens[this.Index].Type == TokenType.ActionDecl)
                {
                    this.VisitActionDeclaration(parentNode, modifier, inheritanceModifier);
                }
                else if (this.Tokens[this.Index].Type == TokenType.Int ||
                    this.Tokens[this.Index].Type == TokenType.Bool ||
                    this.Tokens[this.Index].Type == TokenType.Identifier)
                {
                    this.VisitFieldOrMethodDeclaration(parentNode, modifier, inheritanceModifier);
                }
            }
            else if (this.Tokens[this.Index].Type == TokenType.StartState)
            {
                this.VisitStartStateModifier(parentNode, modifier);
            }
            else if (this.Tokens[this.Index].Type == TokenType.StateDecl)
            {
                this.VisitStateDeclaration(parentNode, false, modifier);
            }
            else if (this.Tokens[this.Index].Type == TokenType.ActionDecl)
            {
                this.VisitActionDeclaration(parentNode, modifier, null);
            }
            else if (this.Tokens[this.Index].Type == TokenType.Int ||
                this.Tokens[this.Index].Type == TokenType.Bool ||
                this.Tokens[this.Index].Type == TokenType.Identifier)
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
            var abstractModifier = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();
        }

        /// <summary>
        /// Visits a start state modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modifier">Modifier</param>
        private void VisitStartStateModifier(MachineDeclarationNode parentNode, Token modifier)
        {
            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.StateDecl)
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
            node.StateKeyword = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected state identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            this.CurrentState = this.Tokens[this.Index].Text;
            this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                this.Tokens[this.Index].Line, TokenType.StateIdentifier);

            node.Identifier = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                this.Tokens[this.Index].Line, TokenType.StateLeftCurlyBracket);

            node.LeftCurlyBracketToken = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            this.VisitNextIntraStateDeclaration(node);

            parentNode.StateDeclarations.Add(node);
        }

        /// <summary>
        /// Visits the next intra-state declration.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextIntraStateDeclaration(StateDeclarationNode node)
        {
            if (this.Index == this.Tokens.Count)
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
            var token = this.Tokens[this.Index];
            switch (token.Type)
            {
                case TokenType.WhiteSpace:
                case TokenType.Comment:
                case TokenType.NewLine:
                    this.Index++;
                    break;

                case TokenType.CommentLine:
                case TokenType.Region:
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    this.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.Entry:
                    this.VisitStateEntryDeclaration(node);
                    this.Index++;
                    break;

                case TokenType.Exit:
                    this.VisitStateExitDeclaration(node);
                    this.Index++;
                    break;

                case TokenType.OnAction:
                    this.VisitStateActionDeclaration(node);
                    this.Index++;
                    break;

                case TokenType.DeferEvent:
                    this.VisitDeferEventsDeclaration(node);
                    this.Index++;
                    break;

                case TokenType.IgnoreEvent:
                    this.VisitIgnoreEventsDeclaration(node);
                    this.Index++;
                    break;

                case TokenType.LeftSquareBracket:
                    this.Index++;
                    this.SkipWhiteSpaceAndCommentTokens();
                    this.VisitAttributeList();
                    this.Index++;
                    break;

                case TokenType.RightCurlyBracket:
                    this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                        this.Tokens[this.Index].Line, TokenType.StateRightCurlyBracket);
                    node.RightCurlyBracketToken = this.Tokens[this.Index];
                    this.CurrentState = "";
                    fixpoint = true;
                    this.Index++;
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
                this.VisitNextIntraStateDeclaration(node);
            }
        }

        /// <summary>
        /// Visits a state entry declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitStateEntryDeclaration(StateDeclarationNode parentNode)
        {
            var node = new EntryDeclarationNode(parentNode.Machine, parentNode);
            node.EntryKeyword = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            node.LeftCurlyBracketToken = this.Tokens[this.Index];

            this.VisitCodeRegion(node);

            parentNode.EntryDeclaration = node;
        }

        /// <summary>
        /// Visits a state exit declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitStateExitDeclaration(StateDeclarationNode parentNode)
        {
            var node = new ExitDeclarationNode(parentNode.Machine, parentNode);
            node.ExitKeyword = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.LeftCurlyBracket)
            {
                this.ReportParsingError("Expected \"{\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket
                });
            }

            node.LeftCurlyBracketToken = this.Tokens[this.Index];

            this.VisitCodeRegion(node);

            parentNode.ExitDeclaration = node;
        }

        /// <summary>
        /// Visits a state action declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitStateActionDeclaration(StateDeclarationNode parentNode)
        {
            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                this.Tokens[this.Index].Line, TokenType.EventIdentifier);

            var eventIdentifier = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                (this.Tokens[this.Index].Type != TokenType.DoAction &&
                this.Tokens[this.Index].Type != TokenType.GotoState))
            {
                this.ReportParsingError("Expected \"do\" or \"goto\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.DoAction,
                    TokenType.GotoState
                });
            }

            if (this.Tokens[this.Index].Type == TokenType.DoAction)
            {
                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();

                if (this.Index == this.Tokens.Count ||
                    this.Tokens[this.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected action identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                    this.Tokens[this.Index].Line, TokenType.ActionIdentifier);

                var actionIdentifier = this.Tokens[this.Index];
                if (!parentNode.AddActionBinding(eventIdentifier, actionIdentifier))
                {
                    this.ReportParsingError("Unexpected action identifier.");
                }

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();

                if (this.Index == this.Tokens.Count ||
                    this.Tokens[this.Index].Type != TokenType.Semicolon)
                {
                    this.ReportParsingError("Expected \";\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Semicolon
                    });
                }
            }
            else if (this.Tokens[this.Index].Type == TokenType.GotoState)
            {
                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();

                if (this.Index == this.Tokens.Count ||
                    this.Tokens[this.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected state identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                    this.Tokens[this.Index].Line, TokenType.StateIdentifier);

                var stateIdentifier = this.Tokens[this.Index];
                if (!parentNode.AddStateTransition(eventIdentifier, stateIdentifier))
                {
                    this.ReportParsingError("Unexpected state identifier.");
                }

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();

                if (this.Index == this.Tokens.Count ||
                    this.Tokens[this.Index].Type != TokenType.Semicolon)
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
            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            bool expectsComma = false;
            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.Semicolon)
            {
                if (!expectsComma && this.Tokens[this.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected event identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                if (expectsComma && this.Tokens[this.Index].Type != TokenType.Comma)
                {
                    this.ReportParsingError("Expected \",\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Comma
                    });
                }

                if (this.Tokens[this.Index].Type == TokenType.Identifier)
                {
                    this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                        this.Tokens[this.Index].Line, TokenType.EventIdentifier);

                    if (!parentNode.AddDeferredEvent(this.Tokens[this.Index]))
                    {
                        this.ReportParsingError("Unexpected event identifier.");
                    }

                    expectsComma = true;
                }
                else if (this.Tokens[this.Index].Type == TokenType.Comma)
                {
                    expectsComma = false;
                }

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Semicolon)
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
            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            bool expectsComma = false;
            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.Semicolon)
            {
                if (!expectsComma && this.Tokens[this.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected event identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                if (expectsComma && this.Tokens[this.Index].Type != TokenType.Comma)
                {
                    this.ReportParsingError("Expected \",\".");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Comma
                    });
                }

                if (this.Tokens[this.Index].Type == TokenType.Identifier)
                {
                    this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                        this.Tokens[this.Index].Line, TokenType.EventIdentifier);

                    if (!parentNode.AddIgnoredEvent(this.Tokens[this.Index]))
                    {
                        this.ReportParsingError("Unexpected event identifier.");
                    }

                    expectsComma = true;
                }
                else if (this.Tokens[this.Index].Type == TokenType.Comma)
                {
                    expectsComma = false;
                }

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Semicolon)
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
            var node = new ActionDeclarationNode(parentNode);
            node.Modifier = modifier;
            node.InheritanceModifier = inheritanceModifier;
            node.ActionKeyword = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected action identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                this.Tokens[this.Index].Line, TokenType.ActionIdentifier);

            node.Identifier = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                (this.Tokens[this.Index].Type != TokenType.LeftCurlyBracket &&
                this.Tokens[this.Index].Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \"{\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftAngleBracket,
                    TokenType.Identifier
                });
            }

            if (this.Tokens[this.Index].Type == TokenType.LeftCurlyBracket)
            {
                node.LeftCurlyBracketToken = this.Tokens[this.Index];
                this.VisitCodeRegion(node);
            }
            else if (this.Tokens[this.Index].Type == TokenType.Semicolon)
            {
                node.SemicolonToken = this.Tokens[this.Index];
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
            var typeNode = new TypeIdentifierNode();
            typeNode.Identifier = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                (this.Tokens[this.Index].Type != TokenType.LeftAngleBracket &&
                this.Tokens[this.Index].Type != TokenType.Identifier))
            {
                this.ReportParsingError("Expected field or method declaration.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftAngleBracket,
                    TokenType.Identifier
                });
            }

            if (this.Tokens[this.Index].Type == TokenType.LeftAngleBracket)
            {
                this.VisitGenericTypesList(typeNode);

                if (this.Index == this.Tokens.Count ||
                    this.Tokens[this.Index].Type != TokenType.Identifier)
                {
                    this.ReportParsingError("Expected field or method declaration.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }
            }

            var identifierToken = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                    (this.Tokens[this.Index].Type != TokenType.LeftParenthesis &&
                    this.Tokens[this.Index].Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \"(\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis,
                    TokenType.Semicolon
                });
            }

            if (this.Tokens[this.Index].Type == TokenType.LeftParenthesis)
            {
                this.VisitMethodDeclaration(parentNode, modifier, inheritanceModifier, typeNode, identifierToken);
            }
            else if (this.Tokens[this.Index].Type == TokenType.Semicolon)
            {
                if (inheritanceModifier != null)
                {
                    this.ReportParsingError("A field declaration cannot have the abstract, virtual or override modifier.");
                }

                var node = new FieldDeclarationNode(parentNode);
                node.Modifier = modifier;
                node.TypeIdentifier = typeNode;
                node.Identifier = identifierToken;
                node.SemicolonToken = this.Tokens[this.Index];

                parentNode.FieldDeclarations.Add(node);
            }
        }

        /// <summary>
        /// Visits a method declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modifier">Modifier</param>
        /// <param name="inheritanceModifier">Inheritance modifier</param>
        /// <param name="typeNode">TypeNode</param>
        /// <param name="identifier">Identifier</param>
        private void VisitMethodDeclaration(MachineDeclarationNode parentNode, Token modifier,
            Token inheritanceModifier, TypeIdentifierNode typeNode, Token identifier)
        {
            var node = new MethodDeclarationNode(parentNode);
            node.Modifier = modifier;
            node.InheritanceModifier = inheritanceModifier;
            node.TypeIdentifier = typeNode;
            node.Identifier = identifier;

            node.LeftParenthesisToken = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.RightParenthesis)
            {
                this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                    this.Tokens[this.Index].Line);

                node.Parameters.Add(this.Tokens[this.Index]);

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();
            }

            node.RightParenthesisToken = this.Tokens[this.Index];

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                (this.Tokens[this.Index].Type != TokenType.LeftCurlyBracket &&
                this.Tokens[this.Index].Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \"{\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftCurlyBracket,
                    TokenType.Semicolon
                });
            }

            if (this.Tokens[this.Index].Type == TokenType.LeftCurlyBracket)
            {
                node.LeftCurlyBracketToken = this.Tokens[this.Index];
                this.VisitCodeRegion(node);
            }
            else if (this.Tokens[this.Index].Type == TokenType.Semicolon)
            {
                node.SemicolonToken = this.Tokens[this.Index];
            }

            parentNode.MethodDeclarations.Add(node);
        }

        /// <summary>
        /// Visits a code region.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitCodeRegion(BaseActionDeclarationNode node)
        {
            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count)
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

            var startIdx = this.Index;

            int bracketCounter = 1;
            while (this.Index < this.Tokens.Count && bracketCounter > 0)
            {
                if (this.Tokens[this.Index].Type == TokenType.LeftCurlyBracket)
                {
                    bracketCounter++;
                }
                else if (this.Tokens[this.Index].Type == TokenType.RightCurlyBracket)
                {
                    bracketCounter--;
                }
                else if (this.Tokens[this.Index].Type == TokenType.DoAction)
                {
                    this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                        this.Tokens[this.Index].Line, TokenType.DoLoop);
                }
                else if (this.Tokens[this.Index].Type == TokenType.New)
                {
                    this.VisitNewStatement();
                }
                else if (this.Tokens[this.Index].Type == TokenType.CreateMachine)
                {
                    this.VisitCreateStatement();
                }
                else if (this.Tokens[this.Index].Type == TokenType.RaiseEvent)
                {
                    this.VisitRaiseStatement();
                }
                else if (this.Tokens[this.Index].Type == TokenType.SendEvent)
                {
                    this.VisitSendStatement();
                }
                else if (this.Tokens[this.Index].Type == TokenType.DeleteMachine)
                {
                    this.VisitDeleteStatement();
                }

                if (bracketCounter > 0)
                {
                    this.Index++;
                    this.SkipWhiteSpaceAndCommentTokens();
                }
            }

            if (node != null)
                for (int idx = startIdx; idx < this.Index; idx++)
                {
                    node.Statements.Add(this.Tokens[idx]);
                }

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.RightCurlyBracket)
            {
                this.ReportParsingError("Expected \"}\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.RightCurlyBracket
                });
            }

            if (node != null)
            node.RightCurlyBracketToken = this.Tokens[this.Index];
        }

        /// <summary>
        /// Visits a new statement.
        /// </summary>
        private void VisitNewStatement()
        {
            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected type identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.LeftParenthesis)
            {
                if (this.Tokens[this.Index].Type != TokenType.Identifier &&
                    this.Tokens[this.Index].Type != TokenType.Dot &&
                    this.Tokens[this.Index].Type != TokenType.LeftAngleBracket &&
                    this.Tokens[this.Index].Type != TokenType.NewLine)
                {
                    this.ReportParsingError("Expected type identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.Dot,
                        TokenType.LeftAngleBracket
                    });
                }

                if (this.Tokens[this.Index].Type == TokenType.Identifier)
                {
                    this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                        this.Tokens[this.Index].Line, TokenType.TypeIdentifier);
                }
                else if (this.Tokens[this.Index].Type == TokenType.LeftAngleBracket)
                {
                    this.VisitGenericTypesList();
                }

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.LeftParenthesis)
            {
                this.ReportParsingError("Expected \"(\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis
                });
            }

            this.Index--;
        }

        /// <summary>
        /// Visits a create statement.
        /// </summary>
        private void VisitCreateStatement()
        {
            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected base machine identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.LeftParenthesis &&
                this.Tokens[this.Index].Type != TokenType.Semicolon)
            {
                if (this.Tokens[this.Index].Type != TokenType.Identifier &&
                    this.Tokens[this.Index].Type != TokenType.Dot &&
                    this.Tokens[this.Index].Type != TokenType.NewLine)
                {
                    this.ReportParsingError("Expected identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.Dot
                    });
                }

                if (this.Tokens[this.Index].Type == TokenType.Identifier)
                {
                    this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                        this.Tokens[this.Index].Line, TokenType.MachineIdentifier);
                }

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.Index == this.Tokens.Count ||
                (this.Tokens[this.Index].Type != TokenType.LeftParenthesis &&
                this.Tokens[this.Index].Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \"(\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis,
                    TokenType.Semicolon
                });
            }

            if (this.Tokens[this.Index].Type == TokenType.LeftParenthesis)
            {
                this.VisitArgumentsList();
            }
        }

        /// <summary>
        /// Visits a raise statement.
        /// </summary>
        private void VisitRaiseStatement()
        {
            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                this.Tokens[this.Index].Line, TokenType.EventIdentifier);

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                (this.Tokens[this.Index].Type != TokenType.LeftParenthesis &&
                this.Tokens[this.Index].Type != TokenType.Semicolon))
            {
                this.ReportParsingError("Expected \"(\" or \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis,
                    TokenType.Semicolon
                });
            }

            if (this.Tokens[this.Index].Type == TokenType.LeftParenthesis)
            {
                this.VisitArgumentsList();
            }

            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }
        }

        /// <summary>
        /// Visits a send statement.
        /// </summary>
        private void VisitSendStatement()
        {
            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Identifier)
            {
                this.ReportParsingError("Expected event identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                this.Tokens[this.Index].Line, TokenType.EventIdentifier);

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                (this.Tokens[this.Index].Type != TokenType.LeftParenthesis &&
                this.Tokens[this.Index].Type != TokenType.ToMachine))
            {
                this.ReportParsingError("Expected \"(\" or \"to\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.LeftParenthesis,
                    TokenType.ToMachine
                });
            }

            if (this.Tokens[this.Index].Type == TokenType.LeftParenthesis)
            {
                this.VisitArgumentsList();
                this.Index++;
            }

            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.ToMachine)
            {
                this.ReportParsingError("Expected \"to\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.ToMachine
                });
            }

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                (this.Tokens[this.Index].Type != TokenType.Identifier &&
                this.Tokens[this.Index].Type != TokenType.This))
            {
                this.ReportParsingError("Expected machine identifier.");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.This
                });
            }

            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.Semicolon)
            {
                if (this.Tokens[this.Index].Type != TokenType.Identifier &&
                    this.Tokens[this.Index].Type != TokenType.This &&
                    this.Tokens[this.Index].Type != TokenType.Dot &&
                    this.Tokens[this.Index].Type != TokenType.NewLine)
                {
                    this.ReportParsingError("Expected machine identifier.");
                    throw new EndOfTokensException(new List<TokenType>
                    {
                        TokenType.Identifier,
                        TokenType.This,
                        TokenType.Dot
                    });
                }

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }
        }

        /// <summary>
        /// Visits a delete statement.
        /// </summary>
        private void VisitDeleteStatement()
        {
            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.Semicolon)
            {
                this.ReportParsingError("Expected \";\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.Semicolon
                });
            }
        }

        /// <summary>
        /// Visits an attribute list.
        /// </summary>
        private void VisitAttributeList()
        {
            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.RightSquareBracket)
            {
                this.Tokens[this.Index] = new Token(this.Tokens[this.Index].TextUnit,
                    this.Tokens[this.Index].Line);

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.RightSquareBracket)
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
        private void VisitArgumentsList()
        {
            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.RightParenthesis)
            {
                if (this.Tokens[this.Index].Type == TokenType.New)
                {
                    this.VisitNewStatement();
                }

                this.Index++;
                this.SkipWhiteSpaceAndCommentTokens();
            }

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.RightParenthesis)
            {
                this.ReportParsingError("Expected \")\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.RightParenthesis
                });
            }
        }

        /// <summary>
        /// Visits a generic types list.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitGenericTypesList(TypeIdentifierNode node = null)
        {
            if (node != null)
            {
                node.LeftAngleBracket = this.Tokens[this.Index];
            }

            this.Index++;
            this.SkipWhiteSpaceAndCommentTokens();

            var tokens = new List<Token>();
            var replaceIdx = this.Index;

            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.RightAngleBracket)
            {
                if (this.Tokens[this.Index].Type == TokenType.Semicolon)
                {
                    return;
                }

                tokens.Add(new Token(this.Tokens[replaceIdx].Text, this.Tokens[replaceIdx].Line));
                replaceIdx++;
            }

            if (this.Index == this.Tokens.Count ||
                this.Tokens[this.Index].Type != TokenType.RightAngleBracket)
            {
                this.ReportParsingError("Expected \">\".");
                throw new EndOfTokensException(new List<TokenType>
                {
                    TokenType.RightAngleBracket
                });
            }

            if (node != null)
            {
                node.RightAngleBracket = this.Tokens[this.Index];
            }

            foreach (var tok in tokens)
            {
                this.Tokens[this.Index] = tok;
                if (node != null)
                {
                    node.TypeTokens.Add(this.Tokens[this.Index]);
                }

                this.Index++;
            }
        }

        /// <summary>
        /// Skips whitespace and comment tokens.
        /// </summary>
        /// <returns>Skipped tokens</returns>
        private List<Token> SkipWhiteSpaceAndCommentTokens()
        {
            var skipped = new List<Token>();
            while (this.Index < this.Tokens.Count)
            {
                var repeat = this.CommentOutLineComment();
                repeat = repeat || this.CommentOutMultiLineComment();
                repeat = repeat || this.SkipWhiteSpaceTokens(skipped);

                if (!repeat)
                {
                    break;
                }
            }

            return skipped;
        }

        /// <summary>
        /// Skips whitespace tokens.
        /// </summary>
        /// <param name="skipped">Skipped tokens</param>
        /// <returns>Boolean value</returns>
        private bool SkipWhiteSpaceTokens(List<Token> skipped)
        {
            if ((this.Tokens[this.Index].Type != TokenType.WhiteSpace) &&
                (this.Tokens[this.Index].Type != TokenType.NewLine))
            {
                return false;
            }

            while (this.Index < this.Tokens.Count &&
                (this.Tokens[this.Index].Type == TokenType.WhiteSpace ||
                this.Tokens[this.Index].Type == TokenType.NewLine))
            {
                skipped.Add(this.Tokens[this.Index]);
                this.Index++;
            }

            return true;
        }

        /// <summary>
        /// Comments out a line-wide comment, if any.
        /// </summary>
        /// <returns>Boolean value</returns>
        private bool CommentOutLineComment()
        {
            if ((this.Tokens[this.Index].Type != TokenType.CommentLine) &&
                (this.Tokens[this.Index].Type != TokenType.Region))
            {
                return false;
            }

            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.NewLine)
            {
                this.Tokens.RemoveAt(this.Index);
            }

            return true;
        }

        /// <summary>
        /// Comments out a multi-line comment, if any.
        /// </summary>
        /// <returns>Boolean value</returns>
        private bool CommentOutMultiLineComment()
        {
            if (this.Tokens[this.Index].Type != TokenType.CommentStart)
            {
                return false;
            }

            while (this.Index < this.Tokens.Count &&
                this.Tokens[this.Index].Type != TokenType.CommentEnd)
            {
                this.Tokens.RemoveAt(this.Index);
            }

            this.Tokens.RemoveAt(this.Index);

            return true;
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

            var errorIndex = this.Index;
            if (this.Index == this.Tokens.Count &&
                this.Index > 0)
            {
                errorIndex--;
            }

            var errorToken = this.Tokens[errorIndex];
            var errorLine = this.OriginalTokens.Where(val => val.Line == errorToken.Line).ToList();

            error += "\nIn " + this.Root.FilePath + " (line " + errorToken.Line + "):\n";

            int nonWhiteIndex = 0;
            for (int idx = 0; idx < errorLine.Count; idx++)
            {
                if (errorLine[idx].Type != TokenType.WhiteSpace)
                {
                    nonWhiteIndex = idx;
                    break;
                }
            }

            for (int idx = nonWhiteIndex; idx < errorLine.Count; idx++)
            {
                error += errorLine[idx].TextUnit.Text;
            }

            for (int idx = nonWhiteIndex; idx < errorLine.Count; idx++)
            {
                if (errorLine[idx].Equals(errorToken) && errorIndex == this.Index)
                {
                    error += new StringBuilder().Append('~', errorLine[idx].TextUnit.Text.Length);
                    break;
                }
                else
                {
                    error += new StringBuilder().Append(' ', errorLine[idx].TextUnit.Text.Length);
                }
            }

            if (errorIndex != this.Index)
            {
                error += "^";
            }

            ErrorReporter.ReportErrorAndExit(error);
        }

        #endregion
    }
}
