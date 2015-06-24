//-----------------------------------------------------------------------
// <copyright file="MonitorStatementVisitor.cs">
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
    /// The P# monitor statement parsing visitor.
    /// </summary>
    internal sealed class MonitorStatementVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal MonitorStatementVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        internal void Visit(BlockSyntax parentNode)
        {
            if (parentNode.Machine.IsMonitor)
            {
                throw new ParsingException("Monitors cannot \"send\".",
                    new List<TokenType>());
            }

            var node = new MonitorStatement(base.TokenStream.Program, parentNode);
            node.MonitorKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected monitor identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            if (base.TokenStream.Program is PSharpProgram)
            {
                var monitorIdentifier = new ExpressionNode(base.TokenStream.Program, parentNode);
                while (!base.TokenStream.Done &&
                    base.TokenStream.Peek().Type != TokenType.Comma)
                {
                    if (base.TokenStream.Peek().Type != TokenType.Identifier &&
                        base.TokenStream.Peek().Type != TokenType.Dot &&
                        base.TokenStream.Peek().Type != TokenType.NewLine)
                    {
                        throw new ParsingException("Expected monitor identifier.",
                            new List<TokenType>
                        {
                                TokenType.Identifier
                        });
                    }

                    monitorIdentifier.StmtTokens.Add(base.TokenStream.Peek());

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                }

                node.MonitorIdentifier = monitorIdentifier;
            }
            else
            {
                var monitorIdentifier = new PExpressionNode(base.TokenStream.Program, parentNode);

                while (!base.TokenStream.Done &&
                    base.TokenStream.Peek().Type != TokenType.Comma)
                {
                    if (base.TokenStream.Peek().Type == TokenType.Payload)
                    {
                        var payloadNode = new PPayloadReceiveNode(base.TokenStream.Program,
                            monitorIdentifier.IsModel);
                        new ReceivedPayloadVisitor(base.TokenStream).Visit(payloadNode);
                        monitorIdentifier.StmtTokens.Add(null);
                        monitorIdentifier.Payloads.Add(payloadNode);
                    }
                    else
                    {
                        monitorIdentifier.StmtTokens.Add(base.TokenStream.Peek());

                        base.TokenStream.Index++;
                        base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                    }
                }

                node.MonitorIdentifier = monitorIdentifier;
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Comma)
            {
                throw new ParsingException("Expected \",\".",
                    new List<TokenType>
                {
                    TokenType.Comma
                });
            }

            node.MonitorSeparator = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.HaltEvent))
            {
                throw new ParsingException("Expected event identifier.",
                    new List<TokenType>
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
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Program is PSharpProgram)
            {
                if (!base.TokenStream.Done &&
                    base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
                {
                    node.EventSeparator = base.TokenStream.Peek();

                    var payload = new ExpressionNode(base.TokenStream.Program, parentNode);
                    new ArgumentsListVisitor(base.TokenStream).Visit(payload);

                    node.Payload = payload;

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                }
            }
            else
            {
                if (!base.TokenStream.Done &&
                    base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    node.EventSeparator = base.TokenStream.Peek();

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    var payload = new PPayloadSendExpressionNode(base.TokenStream.Program, parentNode);
                    new PayloadVisitor(base.TokenStream).Visit(payload);
                    node.Payload = payload;
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
