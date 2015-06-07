//-----------------------------------------------------------------------
// <copyright file="EventDeclarationVisitor.cs">
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
    /// The P# event declaration parsing visitor.
    /// </summary>
    internal sealed class EventDeclarationVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal EventDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="parentNode">Node</param>
        /// <param name="accMod">Access modifier</param>
        internal void Visit(IPSharpProgram program, NamespaceDeclarationNode parentNode, AccessModifier accMod)
        {
            var node = new EventDeclarationNode(base.TokenStream.Program);
            node.AccessModifier = accMod;
            node.EventKeyword = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Identifier &&
                base.TokenStream.Peek().Type != TokenType.HaltEvent &&
                base.TokenStream.Peek().Type != TokenType.DefaultEvent))
            {
                throw new ParsingException("Expected event identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier,
                    TokenType.HaltEvent,
                    TokenType.DefaultEvent
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Identifier)
            {
                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit,
                    TokenType.EventIdentifier));
            }

            node.Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.Assert &&
                base.TokenStream.Peek().Type != TokenType.Assume &&
                base.TokenStream.Peek().Type != TokenType.Colon &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                throw new ParsingException("Expected \":\" or \";\".",
                    new List<TokenType>
                {
                    TokenType.Assert,
                    TokenType.Assume,
                    TokenType.Colon,
                    TokenType.Semicolon
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.Assert ||
                base.TokenStream.Peek().Type == TokenType.Assume)
            {
                bool isAssert = true;
                if (base.TokenStream.Peek().Type == TokenType.Assert)
                {
                    node.AssertKeyword = base.TokenStream.Peek();
                }
                else
                {
                    node.AssumeKeyword = base.TokenStream.Peek();
                    isAssert = false;
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    base.TokenStream.Peek().Type != TokenType.Identifier)
                {
                    throw new ParsingException("Expected integer.",
                        new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                int value;
                if (!int.TryParse(base.TokenStream.Peek().TextUnit.Text, out value))
                {
                    throw new ParsingException("Expected integer.",
                        new List<TokenType>
                    {
                        TokenType.Identifier
                    });
                }

                if (isAssert)
                {
                    node.AssertValue = value;
                }
                else
                {
                    node.AssumeValue = value;
                }

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.Colon &&
                    base.TokenStream.Peek().Type != TokenType.Semicolon))
                {
                    throw new ParsingException("Expected \":\" or \";\".",
                        new List<TokenType>
                    {
                        TokenType.Colon,
                        TokenType.Semicolon
                    });
                }
            }

            if (base.TokenStream.Peek().Type == TokenType.Colon)
            {
                node.ColonToken = base.TokenStream.Peek();

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                PBaseType payloadType = null;
                new TypeIdentifierVisitor(base.TokenStream).Visit(ref payloadType);
                node.PayloadType = payloadType;
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

            if (base.TokenStream.Program is PSharpProgram)
            {
                parentNode.EventDeclarations.Add(node);
            }
            else
            {
                (program as PProgram).EventDeclarations.Add(node);
            }
        }
    }
}
