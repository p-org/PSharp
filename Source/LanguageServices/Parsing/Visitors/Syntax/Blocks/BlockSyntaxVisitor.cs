// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
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
        /// Constructor.
        /// </summary>
        /// <param name="tokenStream">TokenStream</param>
        internal BlockSyntaxVisitor(TokenStream tokenStream)
            : base(tokenStream)
        {

        }

        /// <summary>
        /// Visits the syntax node.
        /// </summary>
        /// <param name="node">Node</param>
        internal void Visit(BlockSyntax node)
        {
            node.OpenBraceToken = base.TokenStream.Peek();

            var text = string.Empty;

            int counter = 0;
            while (!base.TokenStream.Done)
            {
                text += base.TokenStream.Peek().TextUnit.Text;

                if (base.TokenStream.Peek().Type == TokenType.LeftCurlyBracket)
                {
                    counter++;
                }
                else if (base.TokenStream.Peek().Type == TokenType.RightCurlyBracket)
                {
                    counter--;
                }

                if (counter == 0)
                {
                    break;
                }

                base.TokenStream.Index++;
            }

            node.Block = CSharpSyntaxTree.ParseText(text);
        }
    }
}
