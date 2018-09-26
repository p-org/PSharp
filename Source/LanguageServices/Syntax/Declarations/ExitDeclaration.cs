// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.LanguageServices.Parsing;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Syntax
{
    /// <summary>
    /// Exit declaration syntax node.
    /// </summary>
    internal sealed class ExitDeclaration : PSharpSyntaxNode
    {
        #region fields

        /// <summary>
        /// The state parent node.
        /// </summary>
        internal readonly StateDeclaration State;

        /// <summary>
        /// The exit keyword.
        /// </summary>
        internal Token ExitKeyword;

        /// <summary>
        /// The statement block.
        /// </summary>
        internal BlockSyntax StatementBlock;

        /// <summary>
        /// True if the exit action is async.
        /// </summary>
        internal readonly bool IsAsync;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="program">Program</param>
        /// <param name="stateNode">StateDeclaration</param>
        /// <param name="isAsync">True if the exit action is async</param>
        internal ExitDeclaration(IPSharpProgram program, StateDeclaration stateNode, bool isAsync = false)
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
                $"_on_exit_action{suffix}";
            text += "\n" + StatementBlock.TextUnit.Text + "\n";

            base.TextUnit = new TextUnit(text, this.ExitKeyword.TextUnit.Line);
        }

        #endregion
    }
}
