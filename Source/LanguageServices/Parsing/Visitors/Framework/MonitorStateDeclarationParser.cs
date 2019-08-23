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
    /// The P# monitor state declaration parsing visitor.
    /// </summary>
    internal sealed class MonitorStateDeclarationParser : BaseStateVisitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorStateDeclarationParser"/> class.
        /// </summary>
        internal MonitorStateDeclarationParser(PSharpProject project, List<Tuple<SyntaxToken, string>> errorLog,
            List<Tuple<SyntaxToken, string>> warningLog)
            : base(project, errorLog, warningLog)
        {
        }

        /// <summary>
        /// Returns true if the given class declaration is a state.
        /// </summary>
        protected override bool IsState(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl)
        {
            return Querying.IsMonitorState(compilation, classDecl);
        }

        /// <summary>
        /// Returns the type of the state.
        /// </summary>
        protected override string GetTypeOfState()
        {
            return "MonitorState";
        }

        /// <summary>
        /// Checks for special properties.
        /// </summary>
        protected override void CheckForSpecialProperties(ClassDeclarationSyntax state, CodeAnalysis.Compilation compilation)
        {
            this.CheckForDuplicateLivenessAttributes(state, compilation);
        }

        /// <summary>
        /// Checks that a state does not have a duplicate liveness attribute.
        /// </summary>
        private void CheckForDuplicateLivenessAttributes(ClassDeclarationSyntax state, CodeAnalysis.Compilation compilation)
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

            if (hotAttributes.Count > 0 && coldAttributes.Count > 0)
            {
                this.ErrorLog.Add(Tuple.Create(state.Identifier, "A monitor state cannot declare both " +
                    "hot and cold liveness attributes."));
            }
        }
    }
}
