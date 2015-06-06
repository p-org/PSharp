//-----------------------------------------------------------------------
// <copyright file="IfStatementVisitor.cs">
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

using Microsoft.PSharp.Parsing.Syntax;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# if statement parsing visitor.
    /// </summary>
    internal sealed class IfStatementVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal IfStatementVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        internal void Visit(StatementBlockNode parentNode)
        {
            var node = new IfStatementNode(parentNode);
            node.IfKeyword = base.TokenStream.Peek();

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

            if (base.TokenStream.IsPSharp)
            {
                var guard = new ExpressionNode(parentNode);

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

                    if (base.TokenStream.Peek().Type == TokenType.NonDeterministic &&
                        !parentNode.IsModel)
                    {
                        throw new ParsingException("Can only use the nondeterministic \"$\" " +
                            "in a model machine or method.",
                            new List<TokenType>());
                    }

                    guard.StmtTokens.Add(base.TokenStream.Peek());
                    base.TokenStream.Index++;
                    base.TokenStream.SkipCommentTokens();
                }

                if (guard.StmtTokens.Any(tok => tok.Type == TokenType.NonDeterministic) &&
                    guard.StmtTokens.Count > 1)
                {
                    throw new ParsingException("Unexpected guard expression.",
                        new List<TokenType>());
                }

                node.Guard = guard;
            }
            else
            {
                var guard = new PExpressionNode(parentNode);

                int counter = 1;
                while (!base.TokenStream.Done)
                {
                    if (base.TokenStream.Peek().Type == TokenType.Payload)
                    {
                        var payloadNode = new PPayloadReceiveNode(guard.IsModel);
                        new ReceivedPayloadVisitor(base.TokenStream).Visit(payloadNode);
                        guard.StmtTokens.Add(null);
                        guard.Payloads.Add(payloadNode);

                        if (payloadNode.RightParenthesisToken != null)
                        {
                            counter--;
                        }
                    }

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

                    guard.StmtTokens.Add(base.TokenStream.Peek());
                    base.TokenStream.Index++;
                    base.TokenStream.SkipCommentTokens();
                }

                node.Guard = guard;
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

            if (base.TokenStream.IsPSharp)
            {
                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.New &&
                    base.TokenStream.Peek().Type != TokenType.CreateMachine &&
                    base.TokenStream.Peek().Type != TokenType.RaiseEvent &&
                    base.TokenStream.Peek().Type != TokenType.SendEvent &&
                    base.TokenStream.Peek().Type != TokenType.Monitor &&
                    base.TokenStream.Peek().Type != TokenType.Assert &&
                    base.TokenStream.Peek().Type != TokenType.IfCondition &&
                    base.TokenStream.Peek().Type != TokenType.Break &&
                    base.TokenStream.Peek().Type != TokenType.Continue &&
                    base.TokenStream.Peek().Type != TokenType.Return &&
                    base.TokenStream.Peek().Type != TokenType.This &&
                    base.TokenStream.Peek().Type != TokenType.Base &&
                    base.TokenStream.Peek().Type != TokenType.Var &&
                    base.TokenStream.Peek().Type != TokenType.Object &&
                    base.TokenStream.Peek().Type != TokenType.Int &&
                    base.TokenStream.Peek().Type != TokenType.Float &&
                    base.TokenStream.Peek().Type != TokenType.Double &&
                    base.TokenStream.Peek().Type != TokenType.Bool &&
                    base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
                {
                    throw new ParsingException("Expected \"{\".",
                        new List<TokenType>
                    {
                            TokenType.LeftCurlyBracket
                    });
                }
            }
            else
            {
                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.New &&
                    base.TokenStream.Peek().Type != TokenType.RaiseEvent &&
                    base.TokenStream.Peek().Type != TokenType.SendEvent &&
                    base.TokenStream.Peek().Type != TokenType.Monitor &&
                    base.TokenStream.Peek().Type != TokenType.PushState &&
                    base.TokenStream.Peek().Type != TokenType.Assert &&
                    base.TokenStream.Peek().Type != TokenType.IfCondition &&
                    base.TokenStream.Peek().Type != TokenType.WhileLoop &&
                    base.TokenStream.Peek().Type != TokenType.Break &&
                    base.TokenStream.Peek().Type != TokenType.Continue &&
                    base.TokenStream.Peek().Type != TokenType.Return &&
                    base.TokenStream.Peek().Type != TokenType.Identifier &&
                    base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
                {
                    throw new ParsingException("Expected \"{\".",
                        new List<TokenType>
                    {
                            TokenType.LeftCurlyBracket
                    });
                }
            }
            
            var blockNode = new StatementBlockNode(parentNode.Machine,
                parentNode.State, parentNode.IsModel);

            if (base.TokenStream.Peek().Type == TokenType.New)
            {
                new NewStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.CreateMachine)
            {
                new CreateStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.RaiseEvent)
            {
                new RaiseStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.SendEvent)
            {
                new SendStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Monitor)
            {
                new MonitorStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.PushState)
            {
                new PushStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Pop)
            {
                new PopStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Assert)
            {
                new AssertStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.IfCondition)
            {
                new IfStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.WhileLoop)
            {
                new WhileStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.ForeachLoop)
            {
                new ForeachStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Break ||
                base.TokenStream.Peek().Type == TokenType.Continue ||
                base.TokenStream.Peek().Type == TokenType.Return ||
                base.TokenStream.Peek().Type == TokenType.This ||
                base.TokenStream.Peek().Type == TokenType.Base ||
                base.TokenStream.Peek().Type == TokenType.Var ||
                base.TokenStream.Peek().Type == TokenType.Object ||
                base.TokenStream.Peek().Type == TokenType.Int ||
                base.TokenStream.Peek().Type == TokenType.Float ||
                base.TokenStream.Peek().Type == TokenType.Double ||
                base.TokenStream.Peek().Type == TokenType.Bool ||
                base.TokenStream.Peek().Type == TokenType.Identifier)
            {
                new GenericStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
            {
                new StatementBlockVisitor(base.TokenStream).Visit(blockNode);
            }

            node.StatementBlock = blockNode;

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Peek().Type == TokenType.ElseCondition)
            {
                node.ElseKeyword = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.IsPSharp)
                {
                    if (base.TokenStream.Done ||
                        (base.TokenStream.Peek().Type != TokenType.New &&
                        base.TokenStream.Peek().Type != TokenType.CreateMachine &&
                        base.TokenStream.Peek().Type != TokenType.RaiseEvent &&
                        base.TokenStream.Peek().Type != TokenType.SendEvent &&
                        base.TokenStream.Peek().Type != TokenType.Monitor &&
                        base.TokenStream.Peek().Type != TokenType.Assert &&
                        base.TokenStream.Peek().Type != TokenType.IfCondition &&
                        base.TokenStream.Peek().Type != TokenType.Break &&
                        base.TokenStream.Peek().Type != TokenType.Continue &&
                        base.TokenStream.Peek().Type != TokenType.Return &&
                        base.TokenStream.Peek().Type != TokenType.This &&
                        base.TokenStream.Peek().Type != TokenType.Base &&
                        base.TokenStream.Peek().Type != TokenType.Var &&
                        base.TokenStream.Peek().Type != TokenType.Object &&
                        base.TokenStream.Peek().Type != TokenType.Int &&
                        base.TokenStream.Peek().Type != TokenType.Float &&
                        base.TokenStream.Peek().Type != TokenType.Double &&
                        base.TokenStream.Peek().Type != TokenType.Bool &&
                        base.TokenStream.Peek().Type != TokenType.Identifier &&
                        base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
                    {
                        throw new ParsingException("Expected \"{\".",
                            new List<TokenType>
                        {
                                TokenType.LeftCurlyBracket
                        });
                    }
                }
                else
                {
                    if (base.TokenStream.Done ||
                        (base.TokenStream.Peek().Type != TokenType.New &&
                        base.TokenStream.Peek().Type != TokenType.RaiseEvent &&
                        base.TokenStream.Peek().Type != TokenType.SendEvent &&
                        base.TokenStream.Peek().Type != TokenType.Monitor &&
                        base.TokenStream.Peek().Type != TokenType.PushState &&
                        base.TokenStream.Peek().Type != TokenType.Assert &&
                        base.TokenStream.Peek().Type != TokenType.IfCondition &&
                        base.TokenStream.Peek().Type != TokenType.WhileLoop &&
                        base.TokenStream.Peek().Type != TokenType.Break &&
                        base.TokenStream.Peek().Type != TokenType.Continue &&
                        base.TokenStream.Peek().Type != TokenType.Return &&
                        base.TokenStream.Peek().Type != TokenType.Identifier &&
                        base.TokenStream.Peek().Type != TokenType.IfCondition &&
                        base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
                    {
                        throw new ParsingException("Expected \"{\".",
                            new List<TokenType>
                        {
                                TokenType.LeftCurlyBracket
                        });
                    }
                }

                var elseBlockNode = new StatementBlockNode(parentNode.Machine,
                    parentNode.State, parentNode.IsModel);

                if (base.TokenStream.Peek().Type == TokenType.New)
                {
                    new NewStatementVisitor(base.TokenStream).Visit(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.CreateMachine)
                {
                    new CreateStatementVisitor(base.TokenStream).Visit(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.RaiseEvent)
                {
                    new RaiseStatementVisitor(base.TokenStream).Visit(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.SendEvent)
                {
                    new SendStatementVisitor(base.TokenStream).Visit(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.Monitor)
                {
                    new MonitorStatementVisitor(base.TokenStream).Visit(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.PushState)
                {
                    new PushStatementVisitor(base.TokenStream).Visit(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.Pop)
                {
                    new PopStatementVisitor(base.TokenStream).Visit(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.Assert)
                {
                    new AssertStatementVisitor(base.TokenStream).Visit(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.IfCondition)
                {
                    new IfStatementVisitor(base.TokenStream).Visit(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.WhileLoop)
                {
                    new WhileStatementVisitor(base.TokenStream).Visit(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.ForeachLoop)
                {
                    new ForeachStatementVisitor(base.TokenStream).Visit(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.Break ||
                    base.TokenStream.Peek().Type == TokenType.Continue ||
                    base.TokenStream.Peek().Type == TokenType.Return ||
                    base.TokenStream.Peek().Type == TokenType.This ||
                    base.TokenStream.Peek().Type == TokenType.Base ||
                    base.TokenStream.Peek().Type == TokenType.Var ||
                    base.TokenStream.Peek().Type == TokenType.Object ||
                    base.TokenStream.Peek().Type == TokenType.Int ||
                    base.TokenStream.Peek().Type == TokenType.Float ||
                    base.TokenStream.Peek().Type == TokenType.Double ||
                    base.TokenStream.Peek().Type == TokenType.Bool ||
                    base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    new GenericStatementVisitor(base.TokenStream).Visit(elseBlockNode);
                }
                else if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
                {
                    new StatementBlockVisitor(base.TokenStream).Visit(elseBlockNode);
                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();
                }

                node.ElseStatementBlock = elseBlockNode;
            }

            parentNode.Statements.Add(node);
        }
    }
}
