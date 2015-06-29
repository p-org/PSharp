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
        internal MachineDeclarationParser(PSharpProject project, Dictionary<SyntaxToken, string> errorLog)
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
                this.CheckNonPublicFields(machine);
                this.CheckNonInternalFields(machine);
                this.CheckNonPublicMethods(machine);
                this.CheckNonInternalMethods(machine);
            }
        }

        #endregion

        #region private API

        /// <summary>
        /// Checks that machine fields are non-public.
        /// </summary>
        /// <param name="machine"></param>
        private void CheckNonPublicFields(ClassDeclarationSyntax machine)
        {
            var modifiers = machine.DescendantNodes().OfType<FieldDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.PublicKeyword)).
                Select(val => val.Modifiers.First(tok => tok.Kind() == SyntaxKind.PublicKeyword)).
                ToList();

            foreach (var modifier in modifiers)
            {
                base.ErrorLog.Add(modifier, "A machine field cannot be public.");
            }
        }

        /// <summary>
        /// Checks that machine fields are non-public.
        /// </summary>
        /// <param name="machine"></param>
        private void CheckNonInternalFields(ClassDeclarationSyntax machine)
        {
            var modifiers = machine.DescendantNodes().OfType<FieldDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.InternalKeyword)).
                Select(val => val.Modifiers.First(tok => tok.Kind() == SyntaxKind.InternalKeyword)).
                ToList();

            foreach (var modifier in modifiers)
            {
                base.ErrorLog.Add(modifier, "A machine field cannot be internal.");
            }
        }

        /// <summary>
        /// Checks that machine methods are non-public.
        /// </summary>
        /// <param name="machine"></param>
        private void CheckNonPublicMethods(ClassDeclarationSyntax machine)
        {
            var modifiers = machine.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.PublicKeyword)).
                Select(val => val.Modifiers.First(tok => tok.Kind() == SyntaxKind.PublicKeyword)).
                ToList();

            foreach (var modifier in modifiers)
            {
                base.ErrorLog.Add(modifier, "A machine method cannot be public.");
            }
        }

        /// <summary>
        /// Checks that machine methods are non-public.
        /// </summary>
        /// <param name="machine"></param>
        private void CheckNonInternalMethods(ClassDeclarationSyntax machine)
        {
            var modifiers = machine.DescendantNodes().OfType<MethodDeclarationSyntax>().
                Where(val => val.Modifiers.Any(SyntaxKind.InternalKeyword)).
                Select(val => val.Modifiers.First(tok => tok.Kind() == SyntaxKind.InternalKeyword)).
                ToList();

            foreach (var modifier in modifiers)
            {
                base.ErrorLog.Add(modifier, "A machine method cannot be internal.");
            }
        }

        #endregion
    }
}
