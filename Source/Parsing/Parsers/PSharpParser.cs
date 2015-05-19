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
            base.TokenStream.IsPSharp = true;
            return new PSharpProgram(base.FilePath);
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
            var node = new UsingDeclarationNode();
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
            var node = new NamespaceDeclarationNode();
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
                    new EventDeclarationVisitor(base.TokenStream).Visit(null, node, null);
                    base.TokenStream.Index++;
                    break;

                case TokenType.MainMachine:
                    this.VisitMainMachineModifier(node, null);
                    base.TokenStream.Index++;
                    break;

                case TokenType.MachineDecl:
                    new MachineDeclarationVisitor(base.TokenStream).Visit(null, node, false, false, null, null);
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
        /// Visits a top level access modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        private void VisitTopLevelAccessModifier(NamespaceDeclarationNode parentNode)
        {
            var modifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Abstract &&
                base.TokenStream.Peek().Type != TokenType.Virtual &&
                base.TokenStream.Peek().Type != TokenType.MainMachine &&
                base.TokenStream.Peek().Type != TokenType.EventDecl &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl))
            {
                throw new ParsingException("Expected event or machine declaration.",
                    new List<TokenType>
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
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.MachineDecl)
                {
                    throw new ParsingException("Expected machine declaration.",
                        new List<TokenType>
                    {
                        TokenType.MachineDecl
                    });
                }

                new MachineDeclarationVisitor(base.TokenStream).Visit(null, parentNode, false, false, modifier, abstractModifier);
            }
            else if (base.TokenStream.Peek().Type == TokenType.EventDecl)
            {
                new EventDeclarationVisitor(base.TokenStream).Visit(null, parentNode, modifier);
            }
            else if (base.TokenStream.Peek().Type == TokenType.MainMachine)
            {
                this.VisitMainMachineModifier(parentNode, modifier);
            }
            else if (base.TokenStream.Peek().Type == TokenType.MachineDecl)
            {
                new MachineDeclarationVisitor(base.TokenStream).Visit(null, parentNode, false, false, modifier, abstractModifier);
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
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Internal &&
                base.TokenStream.Peek().Type != TokenType.Public &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl))
            {
                throw new ParsingException("Expected machine declaration.",
                    new List<TokenType>
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
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.MachineDecl))
                {
                    throw new ParsingException("Expected machine declaration.",
                        new List<TokenType>
                    {
                        TokenType.MachineDecl
                    });
                }
            }

            if (base.TokenStream.Peek().Type == TokenType.EventDecl)
            {
                new EventDeclarationVisitor(base.TokenStream).Visit(null, parentNode, modifier);
            }
            else if (base.TokenStream.Peek().Type == TokenType.MachineDecl)
            {
                new MachineDeclarationVisitor(base.TokenStream).Visit(null, parentNode, false, false, modifier, abstractModifier);
            }
        }

        /// <summary>
        /// Visits a main machine modifier.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modifier">Modifier</param>
        private void VisitMainMachineModifier(NamespaceDeclarationNode parentNode, Token modifier)
        {
            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.MachineDecl)
            {
                throw new ParsingException("Expected machine declaration.",
                    new List<TokenType>
                {
                    TokenType.MachineDecl
                });
            }

            new MachineDeclarationVisitor(base.TokenStream).Visit(null, parentNode, true, false, modifier, null);
        }

        #endregion
    }
}
