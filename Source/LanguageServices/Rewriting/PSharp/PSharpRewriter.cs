﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.LanguageServices.Rewriting.PSharp
{
    /// <summary>
    /// An abstract P# program rewriter.
    /// </summary>
    internal abstract class PSharpRewriter
    {
        /// <summary>
        /// The P# program.
        /// </summary>
        protected IPSharpProgram Program;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSharpRewriter"/> class.
        /// </summary>
        protected PSharpRewriter(IPSharpProgram program) => this.Program = program;

        /// <summary>
        /// Updates the syntax tree.
        /// </summary>
        protected void UpdateSyntaxTree(string text) => this.Program.UpdateSyntaxTree(text);
    }
}
