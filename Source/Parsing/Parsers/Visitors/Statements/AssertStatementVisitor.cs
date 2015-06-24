//-----------------------------------------------------------------------
// <copyright file="AssertStatementVisitor.cs">
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
    /// The P# assert statement parsing visitor.
    /// </summary>
    internal sealed class AssertStatementVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal AssertStatementVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        internal void Visit(BlockSyntax parentNode)
        {
            var node = new AssertStatementNode(base.TokenStream.Program, parentNode);
            node.AssertKeyword = base.TokenStream.Peek();

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

            if (base.TokenStream.Program is PSharpProgram)
            {
                var predicate = new ExpressionNode(base.TokenStream.Program, parentNode);

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

                    if (base.TokenStream.Peek().Type == TokenType.NonDeterministic)
                    {
                        throw new ParsingException("Can only use the nondeterministic \"$\" " +
                            "keyword as the guard of an if statement.", new List<TokenType>());
                    }

                    predicate.StmtTokens.Add(base.TokenStream.Peek());
                    base.TokenStream.Index++;
                    base.TokenStream.SkipCommentTokens();
                }

                node.Predicate = predicate;
            }
            else
            {
                var predicate = new PExpressionNode(base.TokenStream.Program, parentNode);

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
                        var payloadNode = new PPayloadReceiveNode(base.TokenStream.Program, predicate.IsModel);
                        new ReceivedPayloadVisitor(base.TokenStream).Visit(payloadNode);
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
                    base.TokenStream.SkipCommentTokens();
                }

                node.Predicate = predicate;
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                throw new ParsingException("Expected \")\".",
                    new List<TokenType>
                {
                    TokenType.RightParenthesis
                });
            }

            node.RightParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

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
