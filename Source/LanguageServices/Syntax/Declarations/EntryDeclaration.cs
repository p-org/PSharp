﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp.LanguageServices.Parsing;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Entry declaration syntax node.
    /// </summary>
    internal sealed class EntryDeclaration : PSharpSyntaxNode
    {
        /// <summary>
        /// The state parent node.
        /// </summary>
        internal readonly StateDeclaration State;

        /// <summary>
        /// The entry keyword.
        /// </summary>
        internal Token EntryKeyword;

        /// <summary>
        /// The statement block.
        /// </summary>
        internal BlockSyntax StatementBlock;

        /// <summary>
        /// True if the entry action is async.
        /// </summary>
        internal readonly bool IsAsync;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntryDeclaration"/> class.
        /// </summary>
        internal EntryDeclaration(IPSharpProgram program, StateDeclaration stateNode, bool isAsync = false)
            : base(program)
        {
            this.State = stateNode;
            this.IsAsync = isAsync;
        }

        /// <summary>
        /// Rewrites the syntax node declaration to the intermediate C#
        /// representation.
        /// </summary>
        internal override void Rewrite(int indentLevel)
        {
            this.StatementBlock.Rewrite(indentLevel);

            var typeStr = this.IsAsync ? "async System.Threading.Tasks.Task" : "void";
            var suffix = this.IsAsync ? "_async()" : "()";
            string text = GetIndent(indentLevel) + $"protected {typeStr} psharp_" + this.State.GetFullyQualifiedName() +
                $"_on_entry_action{suffix}";
            text += "\n" + this.StatementBlock.TextUnit.Text + "\n";

            this.TextUnit = new TextUnit(text, this.EntryKeyword.TextUnit.Line);
        }
    }
}
