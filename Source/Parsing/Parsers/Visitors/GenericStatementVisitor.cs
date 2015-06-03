//-----------------------------------------------------------------------
// <copyright file="GenericStatementVisitor.cs">
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
    /// The P# generic statement parsing visitor.
    /// </summary>
    public sealed class GenericStatementVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        public GenericStatementVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        public void Visit(StatementBlockNode parentNode)
        {
            var node = new GenericStatementNode(parentNode);

            if (base.TokenStream.IsPSharp)
            {
                var expression = new ExpressionNode(parentNode);
                while (!base.TokenStream.Done &&
                    base.TokenStream.Peek().Type != TokenType.Semicolon)
                {
                    if (base.TokenStream.Peek().Type == TokenType.New)
                    {
                        node.Expression = expression;
                        parentNode.Statements.Add(node);
                        new NewStatementVisitor(base.TokenStream).Visit(parentNode);
                        return;
                    }
                    else if (base.TokenStream.Peek().Type == TokenType.CreateMachine)
                    {
                        node.Expression = expression;
                        parentNode.Statements.Add(node);
                        new CreateStatementVisitor(base.TokenStream).Visit(parentNode);
                        return;
                    }

                    expression.StmtTokens.Add(base.TokenStream.Peek());
                    base.TokenStream.Index++;
                    base.TokenStream.SkipCommentTokens();
                }

                node.Expression = expression;
            }
            else
            {
                var expression = new PExpressionNode(parentNode);
                while (!base.TokenStream.Done &&
                    base.TokenStream.Peek().Type != TokenType.Semicolon)
                {
                    if (base.TokenStream.Peek().Type == TokenType.New)
                    {
                        node.Expression = expression;
                        parentNode.Statements.Add(node);
                        new CreateStatementVisitor(base.TokenStream).Visit(parentNode);
                        return;
                    }
                    else if (base.TokenStream.Peek().Type == TokenType.DefaultEvent)
                    {
                        node.Expression = expression;
                        parentNode.Statements.Add(node);
                        return;
                    }
                    else if (base.TokenStream.Peek().Type == TokenType.Payload)
                    {
                        var payloadNode = new PPayloadReceiveNode();
                        new ReceivedPayloadVisitor(base.TokenStream).Visit(payloadNode);
                        expression.StmtTokens.Add(null);
                        expression.Payloads.Add(payloadNode);
                        if (base.TokenStream.Peek().Type == TokenType.Semicolon)
                        {
                            break;
                        }
                    }

                    expression.StmtTokens.Add(base.TokenStream.Peek());
                    base.TokenStream.Index++;
                    base.TokenStream.SkipCommentTokens();
                }

                node.Expression = expression;
            }
            
            node.SemicolonToken = base.TokenStream.Peek();

            parentNode.Statements.Add(node);
            base.TokenStream.Index++;
        }
    }
}
