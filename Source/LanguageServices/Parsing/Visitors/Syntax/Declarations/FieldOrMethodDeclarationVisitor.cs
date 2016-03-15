//-----------------------------------------------------------------------
// <copyright file="FieldOrMethodDeclarationVisitor.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
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

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# field or method declaration parsing visitor.
    /// </summary>
    internal sealed class FieldOrMethodDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal FieldOrMethodDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="accMod">Access modifier</param>
        /// <param name="inhMod">Inheritance modifier</param>
        /// <param name="isAsync">Is async</param>
        /// <param name="isPartial">Is partial</param>
        internal void Visit(MachineDeclaration parentNode, AccessModifier accMod,
            InheritanceModifier inhMod, bool isAsync, bool isPartial)
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
                new MethodDeclarationVisitor(base.TokenStream).Visit(parentNode, typeIdentifier,
                    identifierToken, accMod, inhMod, isAsync, isPartial);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Semicolon)
            {
                if (inhMod == InheritanceModifier.Abstract)
                {
                    throw new ParsingException("A field cannot be abstract.",
                        new List<TokenType>());
                }
                else if (inhMod == InheritanceModifier.Virtual)
                {
                    throw new ParsingException("A field cannot be virtual.",
                        new List<TokenType>());
                }
                else if (inhMod == InheritanceModifier.Override)
                {
                    throw new ParsingException("A field cannot be overriden.",
                        new List<TokenType>());
                }

                if (isAsync)
                {
                    throw new ParsingException("A field cannot be async.",
                        new List<TokenType>());
                }

                if (isPartial)
                {
                    throw new ParsingException("A field cannot be partial.",
                        new List<TokenType>());
                }

                var node = new FieldDeclaration(base.TokenStream.Program, parentNode);
                node.AccessModifier = accMod;
                node.TypeIdentifier = typeIdentifier;
                node.Identifier = identifierToken;
                node.SemicolonToken = base.TokenStream.Peek();

                parentNode.FieldDeclarations.Add(node);
            }
        }
    }
}
