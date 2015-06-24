//-----------------------------------------------------------------------
// <copyright file="NewStatementVisitor.cs">
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
    /// The P# new statement parsing visitor.
    /// </summary>
    internal sealed class NewStatementVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal NewStatementVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        internal void Visit(StatementBlockNode parentNode)
        {
            var node = new NewStatementNode(base.TokenStream.Program, parentNode);
            node.NewKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected type identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            TextUnit textUnit = null;
            new TypeIdentifierVisitor(base.TokenStream).Visit(ref textUnit);
            var typeIdentifier = new Token(textUnit, TokenType.TypeIdentifier);
            node.TypeIdentifier = typeIdentifier;

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.LeftParenthesis)
            {
                throw new ParsingException("Expected \"(\".",
                    new List<TokenType>
                {
                    TokenType.LeftParenthesis
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
            {
                node.LeftParenthesisToken = base.TokenStream.Peek();

                var arguments = new ExpressionNode(base.TokenStream.Program, parentNode);
                new ArgumentsListVisitor(base.TokenStream).Visit(arguments);

                node.Arguments = arguments;
                node.RightParenthesisToken = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            if (base.TokenStream.Peek().Type == TokenType.Semicolon)
            {
                node.SemicolonToken = base.TokenStream.Peek();
                parentNode.Statements.Add(node);
                base.TokenStream.Index++;
            }
        }
    }
}
