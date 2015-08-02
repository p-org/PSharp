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

using Microsoft.CodeAnalysis;

using Microsoft.PSharp.LanguageServices.Parsing.Syntax;
using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// The P parser.
    /// </summary>
    public sealed class PParser : TokenParser
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
        /// <param name="project">PSharpProject</param>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="exitAtError">Exits at error</param>
        internal PParser(PSharpProject project, SyntaxTree tree, bool exitAtError = true)
            : base(project, tree, exitAtError)
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
            var program = new PProgram(base.Project, base.SyntaxTree);
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
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        break;

                    case TokenType.CommentStart:
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                        break;

                    case TokenType.EventDecl:
                        new EventDeclarationVisitor(base.TokenStream).Visit(this.Program, null,
                            AccessModifier.None);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.MainMachine:
                        this.VisitMainMachineModifier();
                        base.TokenStream.Index++;
                        break;

                    case TokenType.MachineDecl:
                        new MachineDeclarationVisitor(base.TokenStream).Visit(this.Program, null, false,
                            false, false, AccessModifier.None, InheritanceModifier.None);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.ModelDecl:
                        new MachineDeclarationVisitor(base.TokenStream).Visit(this.Program, null, false,
                            true, false, AccessModifier.None, InheritanceModifier.None);
                        base.TokenStream.Index++;
                        break;

                    case TokenType.Monitor:
                        new MachineDeclarationVisitor(base.TokenStream).Visit(this.Program, null, false,
                            false, true, AccessModifier.None, InheritanceModifier.None);
                        base.TokenStream.Index++;
                        break;

                    default:
                        throw new ParsingException("Unexpected token.",
                            new List<TokenType>());
                }
            }

            throw new ParsingException(new List<TokenType>
            {
                TokenType.MainMachine,
                TokenType.EventDecl,
                TokenType.MachineDecl,
                TokenType.ModelDecl,
                TokenType.Monitor
            });
        }

        #endregion

        #region private API

        /// <summary>
        /// Visits a main machine modifier.
        /// </summary>
        private void VisitMainMachineModifier()
        {
            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.MachineDecl &&
                base.TokenStream.Peek().Type != TokenType.ModelDecl))
            {
                throw new ParsingException("Expected machine or model declaration.",
                    new List<TokenType>
                {
                    TokenType.MachineDecl,
                    TokenType.ModelDecl
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.MachineDecl)
            {
                new MachineDeclarationVisitor(base.TokenStream).Visit(this.Program, null,
                    true, false, false, AccessModifier.None, InheritanceModifier.None);
            }
            else if (base.TokenStream.Peek().Type == TokenType.ModelDecl)
            {
                new MachineDeclarationVisitor(base.TokenStream).Visit(this.Program, null,
                    true, true, false, AccessModifier.None, InheritanceModifier.None);
            }
        }

        #endregion
    }
}
