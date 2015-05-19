//-----------------------------------------------------------------------
// <copyright file="FunctionDeclarationVisitor.cs">
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
    /// The P# function declaration parsing visitor.
    /// </summary>
    public sealed class FunctionDeclarationVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        public FunctionDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="isModel">Is model</param>
        public void Visit(MachineDeclarationNode parentNode, bool isModel)
        {
            var node = new PFunctionDeclarationNode();
            node.IsModel = isModel;

            if (isModel)
            {
                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.FunDecl)
                {
                    throw new ParsingException("Expected function declaration.",
                        new List<TokenType>
                    {
                        TokenType.FunDecl
                    });
                }
            }

            node.FunctionKeyword = base.TokenStream.Peek();

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

            node.Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis)
            {
                throw new ParsingException("Expected \"(\".",
                    new List<TokenType>
                {
                    TokenType.LeftParenthesis
                });
            }

            node.LeftParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

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
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    expectsColon = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Colon)
                {
                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

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
                    new TypeIdentifierVisitor(base.TokenStream).Visit(typeNode);
                    node.ParameterTypeNodes.Add(typeNode);

                    expectsType = false;
                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = false;
                }
            }

            node.RightParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Colon &&
                base.TokenStream.Peek().Type != TokenType.LeftSquareBracket &&
                base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
            {
                throw new ParsingException("Expected \":\" or \"{\".",
                    new List<TokenType>
                {
                    TokenType.Colon,
                    TokenType.LeftCurlyBracket
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Colon)
            {
                node.ColonToken = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                var typeNode = new PTypeNode();
                new TypeIdentifierVisitor(base.TokenStream).Visit(typeNode);
                node.ReturnTypeNode = typeNode;
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftSquareBracket)
            {
                while (!base.TokenStream.Done &&
                    base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket)
                {
                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                }
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

            var blockNode = new StatementBlockNode(parentNode, null);
            new StatementBlockVisitor(base.TokenStream).Visit(blockNode);
            node.StatementBlock = blockNode;

            parentNode.FunctionDeclarations.Add(node);
        }
    }
}
