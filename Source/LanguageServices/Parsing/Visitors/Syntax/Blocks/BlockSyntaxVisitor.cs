// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis.CSharp;

using Microsoft.PSharp.LanguageServices.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Syntax
{
    /// <summary>
    /// The P# block syntax parsing visitor.
    /// </summary>
    internal sealed class BlockSyntaxVisitor : BaseTokenVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockSyntaxVisitor"/> class.
        /// </summary>
        internal BlockSyntaxVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {
        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        internal void Visit(BlockSyntax node)
        {
            node.OpenBraceToken = this.TokenStream.Peek();

            var text = string.Empty;

            int counter = 0;
            while (!this.TokenStream.Done)
            {
                text += this.TokenStream.Peek().TextUnit.Text;

                if (this.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
                {
                    counter++;
                }
                else if (this.TokenStream.Peek().Type == TokenType.RightCurlyBracket)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                this.TokenStream.Index++;
            }

            node.Block = CSharpSyntaxTree.ParseText(text);
        }
    }
}
