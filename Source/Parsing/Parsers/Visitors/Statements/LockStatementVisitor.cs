//-----------------------------------------------------------------------
// <copyright file="LockStatementVisitor.cs">
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
    /// The P# lock statement parsing visitor.
    /// </summary>
    internal sealed class LockStatementVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal LockStatementVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        internal void Visit(BlockSyntax parentNode)
        {
            var node = new LockStatement(base.TokenStream.Program, parentNode);
            node.LockKeyword = base.TokenStream.Peek();

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

            var expr = new ExpressionNode(base.TokenStream.Program, parentNode);

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

                expr.StmtTokens.Add(base.TokenStream.Peek());
                base.TokenStream.Index++;
                base.TokenStream.SkipCommentTokens();
            }

            node.Lock = expr;

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
                (base.TokenStream.Peek().Type != TokenType.New &&
                base.TokenStream.Peek().Type != TokenType.CreateMachine &&
                base.TokenStream.Peek().Type != TokenType.RaiseEvent &&
                base.TokenStream.Peek().Type != TokenType.SendEvent &&
                base.TokenStream.Peek().Type != TokenType.Monitor &&
                base.TokenStream.Peek().Type != TokenType.Assert &&
                base.TokenStream.Peek().Type != TokenType.IfCondition &&
                base.TokenStream.Peek().Type != TokenType.WhileLoop &&
                base.TokenStream.Peek().Type != TokenType.ForLoop &&
                base.TokenStream.Peek().Type != TokenType.ForeachLoop &&
                base.TokenStream.Peek().Type != TokenType.Lock &&
                base.TokenStream.Peek().Type != TokenType.Break &&
                base.TokenStream.Peek().Type != TokenType.Continue &&
                base.TokenStream.Peek().Type != TokenType.Return &&
                base.TokenStream.Peek().Type != TokenType.This &&
                base.TokenStream.Peek().Type != TokenType.Base &&
                base.TokenStream.Peek().Type != TokenType.Var &&
                base.TokenStream.Peek().Type != TokenType.Object &&
                base.TokenStream.Peek().Type != TokenType.String &&
                base.TokenStream.Peek().Type != TokenType.Sbyte &&
                base.TokenStream.Peek().Type != TokenType.Byte &&
                base.TokenStream.Peek().Type != TokenType.Short &&
                base.TokenStream.Peek().Type != TokenType.Ushort &&
                base.TokenStream.Peek().Type != TokenType.Int &&
                base.TokenStream.Peek().Type != TokenType.Uint &&
                base.TokenStream.Peek().Type != TokenType.Long &&
                base.TokenStream.Peek().Type != TokenType.Ulong &&
                base.TokenStream.Peek().Type != TokenType.Char &&
                base.TokenStream.Peek().Type != TokenType.Bool &&
                base.TokenStream.Peek().Type != TokenType.Decimal &&
                base.TokenStream.Peek().Type != TokenType.Float &&
                base.TokenStream.Peek().Type != TokenType.Double &&
                base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.Await &&
                base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket))
            {
                throw new ParsingException("Expected \"{\".",
                    new List<TokenType>
                {
                            TokenType.LeftCurlyBracket
                });
            }

            var blockNode = new BlockSyntax(base.TokenStream.Program,
                parentNode.Machine, parentNode.State, parentNode.IsModel);

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
            else if (base.TokenStream.Peek().Type == TokenType.Break)
            {
                new BreakStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Continue)
            {
                new ContinueStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.IfCondition)
            {
                new IfStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.WhileLoop)
            {
                new WhileStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.ForLoop)
            {
                new ForStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.ForeachLoop)
            {
                new ForeachStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Lock)
            {
                new LockStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Try)
            {
                new TryStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Break ||
                base.TokenStream.Peek().Type == TokenType.Continue ||
                base.TokenStream.Peek().Type == TokenType.Return ||
                base.TokenStream.Peek().Type == TokenType.This ||
                base.TokenStream.Peek().Type == TokenType.Base ||
                base.TokenStream.Peek().Type == TokenType.Var ||
                base.TokenStream.Peek().Type == TokenType.Object ||
                base.TokenStream.Peek().Type == TokenType.String ||
                base.TokenStream.Peek().Type == TokenType.Sbyte ||
                base.TokenStream.Peek().Type == TokenType.Byte ||
                base.TokenStream.Peek().Type == TokenType.Short ||
                base.TokenStream.Peek().Type == TokenType.Ushort ||
                base.TokenStream.Peek().Type == TokenType.Int ||
                base.TokenStream.Peek().Type == TokenType.Uint ||
                base.TokenStream.Peek().Type == TokenType.Long ||
                base.TokenStream.Peek().Type == TokenType.Ulong ||
                base.TokenStream.Peek().Type == TokenType.Char ||
                base.TokenStream.Peek().Type == TokenType.Bool ||
                base.TokenStream.Peek().Type == TokenType.Decimal ||
                base.TokenStream.Peek().Type == TokenType.Float ||
                base.TokenStream.Peek().Type == TokenType.Double ||
                base.TokenStream.Peek().Type == TokenType.Identifier ||
                base.TokenStream.Peek().Type == TokenType.Await)
            {
                new GenericStatementVisitor(base.TokenStream).Visit(blockNode);
            }
            else if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
            {
                new BlockSyntaxVisitor(base.TokenStream).Visit(blockNode);
            }

            node.StatementBlock = blockNode;
            
            parentNode.Statements.Add(node);
            base.TokenStream.Index++;
        }
    }
}
