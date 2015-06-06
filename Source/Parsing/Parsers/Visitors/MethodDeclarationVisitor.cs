//-----------------------------------------------------------------------
// <copyright file="MethodDeclarationVisitor.cs">
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
    /// The P# method declaration parsing visitor.
    /// </summary>
    internal sealed class MethodDeclarationVisitor : BaseParseVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal MethodDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modifier">Modifier</param>
        /// <param name="inheritanceModifier">Inheritance modifier</param>
        /// <param name="typeIdentifier">TypeIdentifier</param>
        /// <param name="identifier">Identifier</param>
        /// <param name="isModel">Is model</param>
        internal void Visit(MachineDeclarationNode parentNode, Token modifier, Token inheritanceModifier,
            Token typeIdentifier, Token identifier, bool isModel)
        {
            if (parentNode.IsModel)
            {
                isModel = true;
            }

            var node = new MethodDeclarationNode(isModel);
            node.Modifier = modifier;
            node.InheritanceModifier = inheritanceModifier;
            node.TypeIdentifier = typeIdentifier;
            node.Identifier = identifier;

            node.LeftParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                base.TokenStream.Swap(new Token(base.TokenStream.Peek().TextUnit));

                node.Parameters.Add(base.TokenStream.Peek());

                base.TokenStream.Index++;
                base.TokenStream.SkipWhiteSpaceAndCommentTokens();
            }

            node.RightParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            if (base.TokenStream.Done ||
                (base.TokenStream.Peek().Type != TokenType.LeftCurlyBracket &&
                base.TokenStream.Peek().Type != TokenType.Semicolon))
            {
                throw new ParsingException("Expected \"{\" or \";\".",
                    new List<TokenType>
                {
                    TokenType.LeftCurlyBracket,
                    TokenType.Semicolon
                });
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
            {
                var blockNode = new StatementBlockNode(parentNode, null, parentNode.IsModel);
                new StatementBlockVisitor(base.TokenStream).Visit(blockNode);
                node.StatementBlock = blockNode;
            }
            else if (base.TokenStream.Peek().Type == TokenType.Semicolon)
            {
                node.SemicolonToken = base.TokenStream.Peek();
            }

            parentNode.MethodDeclarations.Add(node);
        }
    }
}
