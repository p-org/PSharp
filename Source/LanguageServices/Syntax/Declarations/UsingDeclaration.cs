// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Using declaration syntax node.
    /// </summary>
    internal sealed class UsingDeclaration : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The using keyword.
        /// </summary>
        internal Token UsingKeyword;

        /// <summary>
        /// The identifier tokens.
        /// </summary>
        internal List<Token> IdentifierTokens;

        /// <summary>
        /// The semicolon token.
        /// </summary>
        internal Token SemicolonToken;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        internal UsingDeclaration(IPSharpProgram program)
            : base(program)
        {
            this.IdentifierTokens = new List<Token>();
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite(int indentLevel)
        {
            var text = GetIndent(indentLevel) + this.GetRewrittenUsingDeclaration();
            base.TextUnit = new TextUnit(text, this.UsingKeyword.TextUnit.Line);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the rewritten using declaration.
        /// </summary>
        /// <returns>Text</returns>
        private string GetRewrittenUsingDeclaration()
        {
            var text = this.UsingKeyword.TextUnit.Text;
            text += " ";

            foreach (var token in this.IdentifierTokens)
            {
                text += token.TextUnit.Text;
            }

            text += this.SemicolonToken.TextUnit.Text + "\n";

            return text;
        }

        #endregion
    }
}
