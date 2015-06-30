//-----------------------------------------------------------------------
// <copyright file="MachineDeclarationParser.cs">
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

namespace Microsoft.PSharp.LanguageServices.Parsing.Framework
{
    /// <summary>
    /// The P# machine declaration parsing visitor.
    /// </summary>
    internal sealed class MachineDeclarationParser : BaseVisitor
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="errorLog">Error log</param>
        internal MachineDeclarationParser(PSharpProject project, List<Tuple<SyntaxToken, string>> errorLog)
            : base(project, errorLog)
        {

        }

        /// <summary>
        /// Parses the syntax tree for errors.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        internal void Parse(SyntaxTree tree)
        {
            var compilation = base.Project.Project.GetCompilationAsync().Result;

            var machines = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => Querying.IsMachine(compilation, val)).
                ToList();

            foreach (var machine in machines)
            {
                this.CheckForPublicFields(machine);
                this.CheckForInternalFields(machine);
                this.CheckForPublicMethods(machine);
                this.CheckForInternalMethods(machine);

                this.CheckForAtLeastOneState(machine, compilation);
                this.CheckForNonStateClasses(machine, compilation);
                this.CheckForStructs(machine);
                this.CheckForStartState(machine, compilation);
            }
        }

        #endregion

        #region private API

        /// <summary>
        /// Checks that machine fields are non-public.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void CheckForPublicFields(ClassDeclarationSyntax machine)
        {
            var modifiers = machine.DescendantNodes().OfType<FieldDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.PublicKeyword)).
                Select(val => val.Modifiers.First(tok => tok.Kind() == SyntaxKind.PublicKeyword)).
                ToList();

            foreach (var modifier in modifiers)
            {
                base.ErrorLog.Add(Tuple.Create(modifier, "A machine field cannot be public."));
            }
        }

        /// <summary>
        /// Checks that machine fields are non-internal.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void CheckForInternalFields(ClassDeclarationSyntax machine)
        {
            var modifiers = machine.DescendantNodes().OfType<FieldDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.InternalKeyword)).
                Select(val => val.Modifiers.First(tok => tok.Kind() == SyntaxKind.InternalKeyword)).
                ToList();

            foreach (var modifier in modifiers)
            {
                base.ErrorLog.Add(Tuple.Create(modifier, "A machine field cannot be internal."));
            }
        }

        /// <summary>
        /// Checks that machine methods are non-public.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void CheckForPublicMethods(ClassDeclarationSyntax machine)
        {
            var modifiers = machine.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.PublicKeyword)).
                Select(val => val.Modifiers.First(tok => tok.Kind() == SyntaxKind.PublicKeyword)).
                ToList();

            foreach (var modifier in modifiers)
            {
                base.ErrorLog.Add(Tuple.Create(modifier, "A machine method cannot be public."));
            }
        }

        /// <summary>
        /// Checks that machine methods are non-internal.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void CheckForInternalMethods(ClassDeclarationSyntax machine)
        {
            var modifiers = machine.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.InternalKeyword)).
                Select(val => val.Modifiers.First(tok => tok.Kind() == SyntaxKind.InternalKeyword)).
                ToList();

            foreach (var modifier in modifiers)
            {
                base.ErrorLog.Add(Tuple.Create(modifier, "A machine method cannot be internal."));
            }
        }

        /// <summary>
        /// Checks that at least one state is declared inside the machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="compilation">Compilation</param>
        private void CheckForAtLeastOneState(ClassDeclarationSyntax machine, CodeAnalysis.Compilation compilation)
        {
            var states = machine.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => Querying.IsMachineState(compilation, val)).
                ToList();

            if (states.Count == 0)
            {
                base.ErrorLog.Add(Tuple.Create(machine.Identifier, "A machine must declare at least one state."));
            }
        }

        /// <summary>
        /// Checks that no non-state classes are declared inside the machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="compilation">Compilation</param>
        private void CheckForNonStateClasses(ClassDeclarationSyntax machine, CodeAnalysis.Compilation compilation)
        {
            var classIdentifiers = machine.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => !Querying.IsMachineState(compilation, val)).
                Select(val => val.Identifier).
                ToList();

            foreach (var identifier in classIdentifiers)
            {
                base.ErrorLog.Add(Tuple.Create(identifier,
                    "A non-state class cannot be declared inside a machine."));
            }
        }

        /// <summary>
        /// Checks that no structs are declared inside the machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        private void CheckForStructs(ClassDeclarationSyntax machine)
        {
            var structIdentifiers = machine.DescendantNodes().OfType<StructDeclarationSyntax>().
                Select(val => val.Identifier).
                ToList();

            foreach (var identifier in structIdentifiers)
            {
                base.ErrorLog.Add(Tuple.Create(identifier,
                    "A struct cannot be declared inside a machine."));
            }
        }

        /// <summary>
        /// Checks that a machine has an start state.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <param name="compilation">Compilation</param>
        private void CheckForStartState(ClassDeclarationSyntax machine, CodeAnalysis.Compilation compilation)
        {
            var model = compilation.GetSemanticModel(machine.SyntaxTree);
            
            var stateAttributes = machine.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => Querying.IsMachineState(compilation, val)).
                SelectMany(val => val.AttributeLists).
                SelectMany(val => val.Attributes).
                Where(val => model.GetTypeInfo(val).Type.ToDisplayString().Equals("Microsoft.PSharp.Start")).
                ToList();

            if (stateAttributes.Count == 0)
            {
                base.ErrorLog.Add(Tuple.Create(machine.Identifier, "A machine must declare one start state."));
            }
            else if (stateAttributes.Count > 1)
            {
                base.ErrorLog.Add(Tuple.Create(machine.Identifier, "A machine must declare only one start state."));
            }
        }

        #endregion
    }
}
