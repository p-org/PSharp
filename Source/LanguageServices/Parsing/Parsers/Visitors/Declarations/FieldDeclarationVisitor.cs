//-----------------------------------------------------------------------
// <copyright file="FieldDeclarationVisitor.cs">
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

using Microsoft.PSharp.LanguageServices.Parsing.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// The P# field declaration parsing visitor.
    /// </summary>
    internal sealed class FieldDeclarationVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal FieldDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        internal void Visit(MachineDeclaration parentNode)
        {
            var nodes = new List<PFieldDeclaration>();
            var fieldKeyword = base.TokenStream.Peek();

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

            nodes.Add(new PFieldDeclaration(base.TokenStream.Program, parentNode,
                parentNode.IsModel));
            nodes[nodes.Count - 1].FieldKeyword = fieldKeyword;
            nodes[nodes.Count - 1].Identifier = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            bool expectsComma = true;
            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.Colon)
            {
                if ((!expectsComma &&
                    base.TokenStream.Peek().Type != TokenType.Identifier) ||
                    (expectsComma && base.TokenStream.Peek().Type != TokenType.Comma))
                {
                    break;
                }

                if (base.TokenStream.Peek().Type == TokenType.Identifier)
                {
                    nodes.Add(new PFieldDeclaration(base.TokenStream.Program, parentNode,
                        parentNode.IsModel));
                    nodes[nodes.Count - 1].FieldKeyword = fieldKeyword;
                    nodes[nodes.Count - 1].Identifier = base.TokenStream.Peek();

                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = true;
                }
                else if (base.TokenStream.Peek().Type == TokenType.Comma)
                {
                    base.TokenStream.Index++;
                    base.TokenStream.SkipWhiteSpaceAndCommentTokens();

                    expectsComma = false;
                }
            }

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Colon)
            {
                throw new ParsingException("Expected \":\".",
                    new List<TokenType>
                {
                    TokenType.Colon
                });
            }

            foreach (var node in nodes)
            {
                node.ColonToken = base.TokenStream.Peek();
            }

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            PBaseType type = null;
            new TypeIdentifierVisitor(base.TokenStream).Visit(ref type);

            foreach (var node in nodes)
            {
                node.Type = type;
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

            foreach (var node in nodes)
            {
                node.SemicolonToken = base.TokenStream.Peek();
                parentNode.FieldDeclarations.Add(node);
            }
        }
    }
}
