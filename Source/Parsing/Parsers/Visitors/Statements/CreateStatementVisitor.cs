//-----------------------------------------------------------------------
// <copyright file="CreateStatementVisitor.cs">
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
    /// The P# create statement parsing visitor.
    /// </summary>
    internal sealed class CreateStatementVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal CreateStatementVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        internal void Visit(BlockSyntax parentNode)
        {
            var node = new CreateStatement(base.TokenStream.Program, parentNode);
            node.CreateKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected machine identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            if (base.TokenStream.Program is PSharpProgram)
            {
                while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                base.TokenStream.Peek().Type != TokenType.Semicolon)
                {
                    if (base.TokenStream.Peek().Type != TokenType.Identifier &&
                        base.TokenStream.Peek().Type != TokenType.Dot &&
                        base.TokenStream.Peek().Type != TokenType.NewLine)
                    {
                        throw new ParsingException("Expected machine identifier.",
                            new List<TokenType>
                        {
                                TokenType.Identifier,
                                TokenType.Dot
                        });
                    }

                    if (base.TokenStream.Peek().Type == TokenType.Identifier)
                    {
                        base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                            TokenType.MachineIdentifier));
                    }

                    node.MachineIdentifier.Add(base.TokenStream.Peek());

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                }
            }
            else
            {
                node.MachineIdentifier.Add(base.TokenStream.Peek());

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }
            
            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis)
            {
                throw new ParsingException("Expected \"(\".",
                    new List<TokenType>
                {
                    TokenType.LeftParenthesis
                });
            }

            if (base.TokenStream.Program is PSharpProgram)
            {
                node.LeftParenthesisToken = base.TokenStream.Peek();

                var payload = new ExpressionNode(base.TokenStream.Program, parentNode);
                new ArgumentsListVisitor(base.TokenStream).Visit(payload);

                node.Payload = payload;
                node.RightParenthesisToken = base.TokenStream.Peek();
            }
            else
            {
                var payload = new PPayloadSendExpressionNode(base.TokenStream.Program, parentNode);
                new PayloadTupleVisitor(base.TokenStream).Visit(payload);
                node.Payload = payload;
            }
            
            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Models &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                throw new ParsingException("Expected \"models\" or \";\".",
                    new List<TokenType>
                {
                    TokenType.Models,
                    TokenType.Semicolon
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Models)
            {
                node.ModelsKeyword = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.Identifier)
                {
                    throw new ParsingException("Expected machine identifier.",
                        new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                if (base.TokenStream.Program is PSharpProgram)
                {
                    while (!base.TokenStream.Done &&
                        base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                        base.TokenStream.Peek().Type != TokenType.Semicolon)
                    {
                        if (base.TokenStream.Peek().Type != TokenType.Identifier &&
                            base.TokenStream.Peek().Type != TokenType.Dot &&
                            base.TokenStream.Peek().Type != TokenType.NewLine)
                        {
                            throw new ParsingException("Expected machine identifier.",
                                new List<TokenType>
                            {
                                TokenType.Identifier,
                                TokenType.Dot
                            });
                        }

                        if (base.TokenStream.Peek().Type == TokenType.Identifier)
                        {
                            base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                                TokenType.MachineIdentifier));
                        }

                        node.RealMachineIdentifier.Add(base.TokenStream.Peek());

                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }
                }
                else
                {
                    node.RealMachineIdentifier.Add(base.TokenStream.Peek());

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                }
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
            parentNode.Statements.Add(node);
            base.TokenStream.Index++;
        }
    }
}
