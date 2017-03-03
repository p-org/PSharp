//-----------------------------------------------------------------------
// <copyright file="MachineMemberDeclarationVisitor.cs">
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
    /// The P# machine member declaration parsing visitor.
    /// </summary>
    internal sealed class MachineMemberDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal MachineMemberDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="modSet">Modifier set</param>
        internal void Visit(MachineDeclaration parentNode, ModifierSet modSet)
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
                new MachineMethodDeclarationVisitor(base.TokenStream).Visit(parentNode, typeIdentifier,
                    identifierToken, modSet);
            }
            else if (base.TokenStream.Peek().Type == TokenType.Semicolon)
            {
                this.CheckMachineFieldModifierSet(modSet);

                var node = new FieldDeclaration(base.TokenStream.Program, parentNode, modSet);
                node.TypeIdentifier = typeIdentifier;
                node.Identifier = identifierToken;
                node.SemicolonToken = base.TokenStream.Peek();

                parentNode.FieldDeclarations.Add(node);
            }
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        /// <param name="modSet">ModifierSet</param>
        private void CheckMachineFieldModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier == AccessModifier.Public)
            {
                throw new ParsingException("A machine field cannot be public.",
                    new List<TokenType>());
            }
            else if (modSet.AccessModifier == AccessModifier.Internal)
            {
                throw new ParsingException("A machine field cannot be internal.",
                    new List<TokenType>());
            }

            if (modSet.InheritanceModifier == InheritanceModifier.Abstract)
            {
                throw new ParsingException("A machine field cannot be abstract.",
                    new List<TokenType>());
            }
            else if (modSet.InheritanceModifier == InheritanceModifier.Virtual)
            {
                throw new ParsingException("A machine field cannot be virtual.",
                    new List<TokenType>());
            }
            else if (modSet.InheritanceModifier == InheritanceModifier.Override)
            {
                throw new ParsingException("A machine field cannot be overriden.",
                    new List<TokenType>());
            }

            if (modSet.IsAsync)
            {
                throw new ParsingException("A machine field cannot be async.",
                    new List<TokenType>());
            }

            if (modSet.IsPartial)
            {
                throw new ParsingException("A machine field cannot be partial.",
                    new List<TokenType>());
            }
        }
    }
}
