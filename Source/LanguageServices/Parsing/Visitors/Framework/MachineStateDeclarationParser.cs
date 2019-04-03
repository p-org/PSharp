// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Framework
{
    /// <summary>
    /// The P# machine state declaration parsing visitor.
    /// </summary>
    internal sealed class MachineStateDeclarationParser : BaseStateVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MachineStateDeclarationParser"/> class.
        /// </summary>
        internal MachineStateDeclarationParser(PSharpProject project, List<Tuple<SyntaxToken, string>> errorLog,
            List<Tuple<SyntaxToken, string>> warningLog)
            : base(project, errorLog, warningLog)
        {
        }

        /// <summary>
        /// Returns true if the given class declaration is a state.
        /// </summary>
        protected override bool IsState(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl)
        {
            return Querying.IsMachineState(compilation, classDecl);
        }

        /// <summary>
        /// Returns the type of the state.
        /// </summary>
        protected override string GetTypeOfState()
        {
            return "MachineState";
        }

        /// <summary>
        /// Checks for special properties.
        /// </summary>
        protected override void CheckForSpecialProperties(ClassDeclarationSyntax state, CodeAnalysis.Compilation compilation)
        {
            this.CheckForLivenessAttribute(state, compilation);
        }

        /// <summary>
        /// Checks that a state does not have a liveness attribute.
        /// </summary>
        private void CheckForLivenessAttribute(ClassDeclarationSyntax state, CodeAnalysis.Compilation compilation)
        {
            var model = compilation.GetSemanticModel(state.SyntaxTree);

            var hotAttributes = state.AttributeLists.
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.Hot")).
                ToList();

            var coldAttributes = state.AttributeLists.
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.Cold")).
                ToList();

            if (hotAttributes.Count > 0 || coldAttributes.Count > 0)
            {
                this.ErrorLog.Add(Tuple.Create(state.Identifier, "A machine state cannot declare " +
                    "a liveness attribute."));
            }
        }
    }
}
