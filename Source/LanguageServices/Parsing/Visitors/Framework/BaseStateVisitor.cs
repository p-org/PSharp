//-----------------------------------------------------------------------
// <copyright file="BaseStateVisitor.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.LanguageServices.Parsing.Framework
{
    /// <summary>
    /// An abstract P# state visitor.
    /// </summary>
    internal abstract class BaseStateVisitor : BaseVisitor
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="errorLog">Error log</param>
        internal BaseStateVisitor(PSharpProject project, List<Tuple<SyntaxToken, string>> errorLog)
            : base(project, errorLog)
        {

        }

        /// <summary>
        /// Parses the syntax tree for errors.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        internal void Parse(SyntaxTree tree)
        {
            var project = ProgramInfo.GetProjectWithName(base.Project.Name);
            var compilation = project.GetCompilationAsync().Result;

            var states = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => this.IsState(compilation, val)).
                ToList();

            foreach (var state in states)
            {
                this.CheckForFields(state);
                this.CheckForMethods(state);
                this.CheckForClasses(state);
                this.CheckForStructs(state);

                this.CheckForDuplicateOnEntry(state, compilation);
                this.CheckForDuplicateOnExit(state, compilation);

                this.CheckForSpecialProperties(state, compilation);
            }
        }

        #endregion

        #region protected API

        /// <summary>
        /// Returns true if the given class declaration is a state.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean value</returns>
        protected abstract bool IsState(CodeAnalysis.Compilation compilation,
            ClassDeclarationSyntax classDecl);

        /// <summary>
        /// Returns the type of the state.
        /// </summary>
        /// <returns>Text</returns>
        protected abstract string GetTypeOfState();

        /// <summary>
        /// Checks for special properties.
        /// </summary>
        /// <param name="state">State</param>
        /// <param name="compilation">Compilation</param>
        protected abstract void CheckForSpecialProperties(ClassDeclarationSyntax state,
            CodeAnalysis.Compilation compilation);

        #endregion

        #region private API

        /// <summary>
        /// Checks that no fields are declared inside the state.
        /// </summary>
        /// <param name="state">State</param>
        private void CheckForFields(ClassDeclarationSyntax state)
        {
            var fields = state.DescendantNodes().OfType<FieldDeclarationSyntax>().
                ToList();

            if (fields.Count > 0)
            {
                base.ErrorLog.Add(Tuple.Create(state.Identifier,
                    "A state cannot declare fields."));
            }
        }

        /// <summary>
        /// Checks that no methods are declared inside the machine (beside the P# API ones).
        /// </summary>
        /// <param name="state">State</param>
        private void CheckForMethods(ClassDeclarationSyntax state)
        {
            var methods = state.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => !val.Modifiers.Any(SyntaxKind.OverrideKeyword)).
                ToList();

            if (methods.Count > 0)
            {
                base.ErrorLog.Add(Tuple.Create(state.Identifier,
                    "A state cannot declare methods (besides the P# API ones)."));
            }
        }

        /// <summary>
        /// Checks that no classes are declared inside the state.
        /// </summary>
        /// <param name="state">State</param>
        private void CheckForClasses(ClassDeclarationSyntax state)
        {
            var classes = state.DescendantNodes().OfType<ClassDeclarationSyntax>().
                ToList();

            if (classes.Count > 0)
            {
                base.ErrorLog.Add(Tuple.Create(state.Identifier,
                    "A state cannot declare classes."));
            }
        }

        /// <summary>
        /// Checks that no structs are declared inside the state.
        /// </summary>
        /// <param name="state">State</param>
        private void CheckForStructs(ClassDeclarationSyntax state)
        {
            var structs = state.DescendantNodes().OfType<StructDeclarationSyntax>().
                ToList();

            if (structs.Count > 0)
            {
                base.ErrorLog.Add(Tuple.Create(state.Identifier,
                    "A state cannot declare structs."));
            }
        }

        /// <summary>
        /// Checks that a state does not have a duplicate entry action.
        /// </summary>
        /// <param name="state">State</param>
        /// <param name="compilation">Compilation</param>
        private void CheckForDuplicateOnEntry(ClassDeclarationSyntax state, CodeAnalysis.Compilation compilation)
        {
            var model = compilation.GetSemanticModel(state.SyntaxTree);

            var onEntryAttribute = state.AttributeLists.
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnEntry")).
                FirstOrDefault();

            var onEntryMethod = state.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.OverrideKeyword)).
                Where(val => val.Identifier.ValueText.Equals("OnEntry")).
                FirstOrDefault();

            if (onEntryAttribute != null && onEntryMethod != null)
            {
                base.ErrorLog.Add(Tuple.Create(state.Identifier, "A state cannot have two entry actions."));
            }
        }

        /// <summary>
        /// Checks that a state does not have a duplicate exit action.
        /// </summary>
        /// <param name="state">State</param>
        /// <param name="compilation">Compilation</param>
        private void CheckForDuplicateOnExit(ClassDeclarationSyntax state, CodeAnalysis.Compilation compilation)
        {
            var model = compilation.GetSemanticModel(state.SyntaxTree);

            var onExitAttribute = state.AttributeLists.
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.OnExit")).
                FirstOrDefault();

            var onExitMethod = state.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.OverrideKeyword)).
                Where(val => val.Identifier.ValueText.Equals("OnExit")).
                FirstOrDefault();

            if (onExitAttribute != null && onExitMethod != null)
            {
                base.ErrorLog.Add(Tuple.Create(state.Identifier, "A state cannot have two exit actions."));
            }
        }

        #endregion
    }
}
