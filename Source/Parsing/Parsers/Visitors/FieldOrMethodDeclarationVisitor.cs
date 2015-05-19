//-----------------------------------------------------------------------
// <copyright file="FieldOrMethodDeclarationVisitor.cs">
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
    /// The P# field or method declaration parsing visitor.
    /// </summary>
    public sealed class FieldOrMethodDeclarationVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        public FieldOrMethodDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modifier">Modifier</param>
        /// <param name="inheritanceModifier">Inheritance modifier</param>
        public void Visit(MachineDeclarationNode parentNode, Token modifier,
            Token inheritanceModifier)
        {
            TextUnit textUnit = null;
            new TypeIdentifierVisitor(base.TokenStream).Visit(ref textUnit);
            var typeIdentifier = new Token(textUnit, TokenType.TypeIdentifier);

            if (base.TokenStream.Done ||
                base.TokenStream.Peek().Type != TokenType.Identifier)
            {
                throw new ParsingException("Expected field or method identifier.",
                    new List<TokenType>
                {
                    TokenType.Identifier
                });
            }

            var identifierToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                    (base.TokenStream.Peek().Type != TokenType.LeftParenthesis &&
                    base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                throw new ParsingException("Expected \"(\" or \";\".",
                    new List<TokenType>
                {
                    TokenType.LeftParenthesis,
                    TokenType.Semicolon
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftParenthesis)
            {
                new MethodDeclarationVisitor(base.TokenStream).Visit(
                    parentNode, modifier, inheritanceModifier, typeIdentifier, identifierToken);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Semicolon)
            {
                if (inheritanceModifier != null)
                {
                    throw new ParsingException("A field declaration cannot " +
                        "have the abstract, virtual or override modifier.",
                        new List<TokenType>());
                }

                var node = new FieldDeclarationNode(parentNode);
                node.Modifier = modifier;
                node.TypeIdentifier = typeIdentifier;
                node.Identifier = identifierToken;
                node.SemicolonToken = base.TokenStream.Peek();

                parentNode.FieldDeclarations.Add(node);
            }
        }
    }
}
