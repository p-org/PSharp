//-----------------------------------------------------------------------
// <copyright file="StatementBlockVisitor.cs">
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
    /// The P# statement block parsing visitor.
    /// </summary>
    internal sealed class StatementBlockVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal StatementBlockVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        internal void Visit(StatementBlockNode node)
        {
            node.LeftCurlyBracketToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Program is PSharpProgram)
            {
                this.VisitNextPSharpStatement(node);
            }
            else
            {
                this.VisitNextPStatement(node);
            }
        }

        /// <summary>
        /// Visits the next statement.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextPSharpStatement(StatementBlockNode node)
        {
            if (base.TokenStream.Done)
            {
                throw new ParsingException("Expected \"}\".",
                    new List<TokenType>
                {
                    TokenType.IfCondition,
                    TokenType.DoLoop,
                    TokenType.ForLoop,
                    TokenType.WhileLoop,
                    TokenType.ForLoop,
                    TokenType.ForeachLoop,
                    TokenType.Break,
                    TokenType.Continue,
                    TokenType.Return,
                    TokenType.New,
                    TokenType.CreateMachine,
                    TokenType.RaiseEvent,
                    TokenType.SendEvent,
                    TokenType.Monitor,
                    TokenType.Assert,
                    TokenType.Lock
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

                case TokenType.New:
                    new NewStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.CreateMachine:
                    new CreateStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.RaiseEvent:
                    new RaiseStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.SendEvent:
                    new SendStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.Monitor:
                    new MonitorStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.Pop:
                    new PopStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.Assert:
                    new AssertStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.IfCondition:
                    new IfStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.WhileLoop:
                    new WhileStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.ForLoop:
                    new ForStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.ForeachLoop:
                    new ForeachStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.Lock:
                    new LockStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.Try:
                    new TryStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.Break:
                case TokenType.Continue:
                case TokenType.Return:
                case TokenType.This:
                case TokenType.Base:
                case TokenType.Var:
                case TokenType.MachineDecl:
                case TokenType.Object:
                case TokenType.String:
                case TokenType.Sbyte:
                case TokenType.Byte:
                case TokenType.Short:
                case TokenType.Ushort:
                case TokenType.Int:
                case TokenType.Uint:
                case TokenType.Long:
                case TokenType.Ulong:
                case TokenType.Char:
                case TokenType.Bool:
                case TokenType.Decimal:
                case TokenType.Float:
                case TokenType.Double:
                case TokenType.Identifier:
                case TokenType.Await:
                    new GenericStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.RightCurlyBracket:
                    node.RightCurlyBracketToken = base.TokenStream.Peek();
                    fixpoint = true;
                    base.TokenStream.Index++;
                    break;

                default:
                    throw new ParsingException("Unexpected token.",
                        new List<TokenType>());
            }

            if (!fixpoint)
            {
                this.VisitNextPSharpStatement(node);
            }
        }

        /// <summary>
        /// Visits the next statement.
        /// </summary>
        /// <param name="node">Node</param>
        private void VisitNextPStatement(StatementBlockNode node)
        {
            if (base.TokenStream.Done)
            {
                throw new ParsingException("Expected \"}\".",
                    new List<TokenType>
                {
                    TokenType.IfCondition,
                    TokenType.WhileLoop,
                    TokenType.Break,
                    TokenType.Continue,
                    TokenType.Return,
                    TokenType.New,
                    TokenType.DefaultEvent,
                    TokenType.CreateMachine,
                    TokenType.RaiseEvent,
                    TokenType.SendEvent,
                    TokenType.Monitor,
                    TokenType.PushState,
                    TokenType.Assert
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

                case TokenType.New:
                    new CreateStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.RaiseEvent:
                    new RaiseStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.SendEvent:
                    new SendStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.Monitor:
                    new MonitorStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.PushState:
                    new PushStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.Pop:
                    new PopStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.Assert:
                    new AssertStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.IfCondition:
                    new IfStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.WhileLoop:
                    new WhileStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.DefaultEvent:
                    new DefaultStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.Break:
                case TokenType.Continue:
                case TokenType.Return:
                case TokenType.Identifier:
                    new GenericStatementVisitor(base.TokenStream).Visit(node);
                    break;

                case TokenType.RightCurlyBracket:
                    node.RightCurlyBracketToken = base.TokenStream.Peek();
                    fixpoint = true;
                    base.TokenStream.Index++;
                    break;

                default:
                    throw new ParsingException("Unexpected token.",
                        new List<TokenType>());
            }

            if (!fixpoint)
            {
                this.VisitNextPStatement(node);
            }
        }
    }
}
