// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Framework
{
    /// <summary>
    /// The P# monitor declaration parsing visitor.
    /// </summary>
    internal sealed class MonitorDeclarationParser : BaseMachineVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorDeclarationParser"/> class.
        /// </summary>
        internal MonitorDeclarationParser(PSharpProject project, List<Tuple<SyntaxToken, string>> errorLog,
            List<Tuple<SyntaxToken, string>> warningLog)
            : base(project, errorLog, warningLog)
        {
        }

        /// <summary>
        /// Returns true if the given class declaration is a machine.
        /// </summary>
        protected override bool IsMachine(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl) =>
            Querying.IsMonitor(compilation, classDecl);

        /// <summary>
        /// Returns true if the given class declaration is a state.
        /// </summary>
        protected override bool IsState(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl) =>
            Querying.IsMonitorState(compilation, classDecl);

        /// <summary>
        /// Returns true if the given class declaration is a stategroup.
        /// </summary>
        protected override bool IsStateGroup(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl) =>
            Querying.IsMachineStateGroup(compilation, classDecl);

        /// <summary>
        /// Returns the type of the machine.
        /// </summary>
        protected override string GetTypeOfMachine() => "Monitor";
    }
}
