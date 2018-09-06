// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Framework
{
    /// <summary>
    /// The P# machine declaration parsing visitor.
    /// </summary>
    internal sealed class MachineDeclarationParser : BaseMachineVisitor
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="errorLog">Error log</param>
        /// <param name="warningLog">Warning log</param>
        internal MachineDeclarationParser(PSharpProject project, List<Tuple<SyntaxToken, string>> errorLog,
            List<Tuple<SyntaxToken, string>> warningLog)
            : base(project, errorLog, warningLog)
        {

        }

        #endregion

        #region protected API

        /// <summary>
        /// Returns true if the given class declaration is a machine.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean</returns>
        protected override bool IsMachine(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl)
        {
            return Querying.IsMachine(compilation, classDecl);
        }

        /// <summary>
        /// Returns true if the given class declaration is a state.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean</returns>
        protected override bool IsState(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl)
        {
            return Querying.IsMachineState(compilation, classDecl);
        }

        /// <summary>
        /// Returns true if the given class declaration is a stategroup.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean</returns>
        protected override bool IsStateGroup(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl)
        {
            return Querying.IsMachineStateGroup(compilation, classDecl);
        }

        /// <summary>
        /// Returns the type of the machine.
        /// </summary>
        /// <returns>Text</returns>
        protected override string GetTypeOfMachine()
        {
            return "Machine";
        }

        #endregion
    }
}
