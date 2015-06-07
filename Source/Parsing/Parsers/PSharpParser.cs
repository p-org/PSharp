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
        /// <param name="project">PSharpProject</param>
        /// <param name="filePath">File path</param>
        internal PSharpParser(PSharpProject project, string filePath)
            : base(project, filePath)
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
            var program = new PSharpProgram(base.Project, base.FilePath);
            base.TokenStream.Program = program;
            return program;
        }

        /// <summary>
        /// Parses the next available token.
        /// </summary>
        protected override void ParseNextToken()
        {
            if (base.TokenStream.Done)
            {
                throw new ParsingException(new List<TokenType>
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
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    break;

                case TokenType.CommentStart:
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
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
            var node = new UsingDeclarationNode(base.TokenStream.Program);
            node.UsingKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected identifier.",
                    new List<TokenType>
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
                    throw new ParsingException("Expected identifier.",
                        new List<TokenType>
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
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Semicolon)
            {
                throw new ParsingException("Expected \";\".",
                    new List<TokenType>
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
            var node = new NamespaceDeclarationNode(base.TokenStream.Program);
            node.NamespaceKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected namespace identifier.",
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
                    throw new ParsingException("Expected namespace identifier.",
                        new List<TokenType>
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
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
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

            node.LeftCurlyBracketToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

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
                throw new ParsingException("Expected \"}\".",
                    new List<TokenType>
                {
                    TokenType.Internal,
                    TokenType.Public,
                    TokenType.Abstract,
                    TokenType.Virtual,
                    TokenType.MainMachine,
                    TokenType.EventDecl,
                    TokenType.MachineDecl,
                    TokenType.ModelDecl,
                    TokenType.Monitor,
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

                case TokenType.EventDecl:
                case TokenType.MachineDecl:
                case TokenType.ModelDecl:
                case TokenType.Monitor:
                case TokenType.MainMachine:
                case TokenType.Internal:
                case TokenType.Public:
                case TokenType.Abstract:
                case TokenType.Virtual:
                    this.VisitEventOrMachineDeclaration(node);
                    base.TokenStream.Index++;
                    break;

                case TokenType.LeftSquareBracket:
                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    new AttributeListVisitor(base.TokenStream).Visit();
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
        /// Visits an event or machine declaration.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitEventOrMachineDeclaration(NamespaceDeclarationNode parentNode)
        {
            AccessModifier am = AccessModifier.None;
            InheritanceModifier im = InheritanceModifier.None;
            bool isMain = false;

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.EventDecl &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.ModelDecl &&
                base.TokenStream.Peek().Type != TokenType.Monitor)
            {
                if (am != AccessModifier.None &&
                    (base.TokenStream.Peek().Type == TokenType.Public ||
                    base.TokenStream.Peek().Type == TokenType.Private ||
                    base.TokenStream.Peek().Type == TokenType.Protected ||
                    base.TokenStream.Peek().Type == TokenType.Internal))
                {
                    throw new ParsingException("More than one protection modifier.",
                        new List<TokenType>());
                }
                else if (im != InheritanceModifier.None &&
                    base.TokenStream.Peek().Type == TokenType.Abstract)
                {
                    throw new ParsingException("Duplicate abstract modifier.",
                        new List<TokenType>());
                }
                else if (isMain &&
                    base.TokenStream.Peek().Type == TokenType.MainMachine)
                {
                    throw new ParsingException("Duplicate main machine modifier.",
                        new List<TokenType>());
                }

                if (base.TokenStream.Peek().Type == TokenType.Public)
                {
                    am = AccessModifier.Public;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Private)
                {
                    am = AccessModifier.Private;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Protected)
                {
                    am = AccessModifier.Protected;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Internal)
                {
                    am = AccessModifier.Internal;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Abstract)
                {
                    im = InheritanceModifier.Abstract;
                }
                else if (base.TokenStream.Peek().Type == TokenType.MainMachine)
                {
                    isMain = true;
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.MainMachine &&
                base.TokenStream.Peek().Type != TokenType.EventDecl &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.ModelDecl &&
                base.TokenStream.Peek().Type != TokenType.Monitor))
            {
                throw new ParsingException("Expected event, machine, model or monitor declaration.",
                    new List<TokenType>
                {
                    TokenType.MainMachine,
                    TokenType.EventDecl,
                    TokenType.MachineDecl,
                    TokenType.ModelDecl,
                    TokenType.Monitor
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.EventDecl)
            {
                if (am == AccessModifier.Private)
                {
                    throw new ParsingException("An event cannot be private.",
                        new List<TokenType>());
                }
                else if (am == AccessModifier.Protected)
                {
                    throw new ParsingException("An event cannot be protected.",
                        new List<TokenType>());
                }

                if (im == InheritanceModifier.Abstract)
                {
                    throw new ParsingException("An event cannot be abstract.",
                        new List<TokenType>());
                }

                if (isMain)
                {
                    throw new ParsingException("An event cannot be main.",
                        new List<TokenType>());
                }

                new EventDeclarationVisitor(base.TokenStream).Visit(null, parentNode, am);
            }
            else if (base.TokenStream.Peek().Type == TokenType.MachineDecl ||
                base.TokenStream.Peek().Type == TokenType.ModelDecl)
            {
                if (am == AccessModifier.Private)
                {
                    throw new ParsingException("A machine cannot be private.",
                        new List<TokenType>());
                }
                else if (am == AccessModifier.Protected)
                {
                    throw new ParsingException("A machine cannot be protected.",
                        new List<TokenType>());
                }

                if (base.TokenStream.Peek().Type == TokenType.MachineDecl)
                {
                    new MachineDeclarationVisitor(base.TokenStream).Visit(null, parentNode,
                        isMain, false, false, am, im);
                }
                else if (base.TokenStream.Peek().Type == TokenType.ModelDecl)
                {
                    new MachineDeclarationVisitor(base.TokenStream).Visit(null, parentNode,
                        isMain, true, false, am, im);
                }
            }
            else if (base.TokenStream.Peek().Type == TokenType.Monitor)
            {
                if (am == AccessModifier.Private)
                {
                    throw new ParsingException("A monitor cannot be private.",
                        new List<TokenType>());
                }
                else if (am == AccessModifier.Protected)
                {
                    throw new ParsingException("A monitor cannot be protected.",
                        new List<TokenType>());
                }

                if (isMain)
                {
                    throw new ParsingException("A monitor cannot be main.",
                        new List<TokenType>());
                }

                new MachineDeclarationVisitor(base.TokenStream).Visit(null, parentNode,
                    false, false, true, am, im);
            }
        }

        #endregion
    }
}
