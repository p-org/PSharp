//-----------------------------------------------------------------------
// <copyright file="MachineMethodDeclarationVisitor.cs">
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
    /// The P# machine method declaration parsing visitor.
    /// </summary>
    internal sealed class MachineMethodDeclarationVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal MachineMethodDeclarationVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="parentNode">Node</param>
        /// <param name="typeIdentifier">Type identifier</param>
        /// <param name="identifier">Identifier</param>
        /// <param name="modSet">Modifier set</param>
        internal void Visit(MachineDeclaration parentNode, Token typeIdentifier, Token identifier, ModifierSet modSet)
        {
            this.CheckMachineMethodModifierSet(modSet);

            var node = new MethodDeclaration(base.TokenStream.Program, parentNode);
            node.AccessModifier = modSet.AccessModifier;
            node.InheritanceModifier = modSet.InheritanceModifier;
            node.TypeIdentifier = typeIdentifier;
            node.Identifier = identifier;
            node.IsAsync = modSet.IsAsync;
            node.IsPartial = modSet.IsPartial;

            node.LeftParenthesisToken = base.TokenStream.Peek();

            base.TokenStream.Index++;
            base.TokenStream.SkipWhiteSpaceAndCommentTokens();

            while (!base.TokenStream.Done &&
                base.TokenStream.Peek().Type != TokenType.RightParenthesis)
            {
                // TODOswap Why?: base.TokenStream.Swap(base.TokenStream.Peek().TextUnit);

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
                throw new ParsingException("Expected \"{\" or \";\".", base.TokenStream.Peek(),
                    new []
                    {
                        TokenType.LeftCurlyBracket,
                        TokenType.Semicolon
                    });
            }

            if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
            {
                var blockNode = new BlockSyntax(base.TokenStream.Program, parentNode, null);
                new BlockSyntaxVisitor(base.TokenStream).Visit(blockNode);
                node.StatementBlock = blockNode;
            }
            else if (base.TokenStream.Peek().Type == TokenType.Semicolon)
            {
                node.SemicolonToken = base.TokenStream.Peek();
            }

            parentNode.MethodDeclarations.Add(node);
        }

        /// <summary>
        /// Checks the modifier set for errors.
        /// </summary>
        /// <param name="modSet">ModifierSet</param>
        private void CheckMachineMethodModifierSet(ModifierSet modSet)
        {
            if (modSet.AccessModifier == AccessModifier.Public)
            {
                throw new ParsingException("A machine method cannot be public.", base.TokenStream.Peek());
            }
            else if (modSet.AccessModifier == AccessModifier.Internal)
            {
                throw new ParsingException("A machine method cannot be internal.", base.TokenStream.Peek());
            }
        }
    }
}
