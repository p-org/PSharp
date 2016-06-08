//-----------------------------------------------------------------------
// <copyright file="PSharpParser.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
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

using Microsoft.CodeAnalysis;

using Microsoft.PSharp.LanguageServices.Parsing.Syntax;
using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// The P# parser.
    /// </summary>
    public sealed class PSharpParser : TokenParser
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">ParsingOptions</param>
        public PSharpParser(ParsingOptions options)
            : base(options)
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="options">ParsingOptions</param>
        internal PSharpParser(PSharpProject project, SyntaxTree tree, ParsingOptions options)
            : base(project, tree, options)
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
            var program = new PSharpProgram(base.Project, base.SyntaxTree);
            base.TokenStream.Program = program;
            return program;
        }

        /// <summary>
        /// Parses the tokens.
        /// </summary>
        protected override void ParseTokens()
        {
            while (!base.TokenStream.Done)
            {
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
                    case TokenType.Partial:
                    case TokenType.Abstract:
                    case TokenType.Virtual:
                    case TokenType.EventDecl:
                    case TokenType.MachineDecl:
                        throw new ParsingException("Must be declared inside a namespace.",
                            new List<TokenType>());

                    default:
                        throw new ParsingException("Unexpected token.",
                            new List<TokenType>());
                }
            }

            throw new ParsingException(new List<TokenType>
            {
                TokenType.Using,
                TokenType.NamespaceDecl
            });
        }

        #endregion

        #region private methods

        /// <summary>
        /// Visits a using declaration.
        /// </summary>
        private void VisitUsingDeclaration()
        {
            var node = new UsingDeclaration(base.TokenStream.Program);
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
            var node = new NamespaceDeclaration(base.TokenStream.Program);
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
        private void VisitNextIntraNamespaceDeclaration(NamespaceDeclaration node)
        {
            if (base.TokenStream.Done)
            {
                throw new ParsingException("Expected \"}\".",
                    new List<TokenType>
                {
                    TokenType.Internal,
                    TokenType.Public,
                    TokenType.Partial,
                    TokenType.Abstract,
                    TokenType.Virtual,
                    TokenType.EventDecl,
                    TokenType.MachineDecl,
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
                case TokenType.Monitor:
                case TokenType.Internal:
                case TokenType.Public:
                case TokenType.Partial:
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
                    throw new ParsingException("Event and machine declarations must be internal or public.",
                        new List<TokenType>());

                default:
                    throw new ParsingException("Unexpected token.",
                        new List<TokenType>());
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
        private void VisitEventOrMachineDeclaration(NamespaceDeclaration parentNode)
        {
            AccessModifier am = AccessModifier.None;
            InheritanceModifier im = InheritanceModifier.None;
            bool isPartial = false;

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.EventDecl &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl &&
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
                else if (isPartial &&
                    base.TokenStream.Peek().Type == TokenType.Partial)
                {
                    throw new ParsingException("Duplicate partial modifier.",
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
                else if (base.TokenStream.Peek().Type == TokenType.Partial)
                {
                    isPartial = true;
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.EventDecl &&
                base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.Monitor))
            {
                throw new ParsingException("Expected event, machine or monitor declaration.",
                    new List<TokenType>
                {
                    TokenType.EventDecl,
                    TokenType.MachineDecl,
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

                if (isPartial)
                {
                    throw new ParsingException("An event cannot be declared as partial.",
                        new List<TokenType>());
                }

                new EventDeclarationVisitor(base.TokenStream).Visit(null, parentNode, am);
            }
            else if (base.TokenStream.Peek().Type == TokenType.MachineDecl)
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

                new MachineDeclarationVisitor(base.TokenStream).Visit(null, parentNode,
                    false, isPartial, am, im);
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

                new MachineDeclarationVisitor(base.TokenStream).Visit(null, parentNode,
                    true, isPartial, am, im);
            }
        }

        #endregion
    }
}
