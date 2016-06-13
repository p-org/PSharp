//-----------------------------------------------------------------------
// <copyright file="StateGroupDeclarationParser.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
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
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices.Parsing.Framework
{
    /// <summary>
    /// The P# machine state group declaration parsing visitor.
    /// </summary>
    internal sealed class StateGroupDeclarationParser : BaseVisitor
    {
        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="errorLog">Error log</param>
        /// <param name="warningLog">Warning log</param>
        internal StateGroupDeclarationParser(PSharpProject project, List<Tuple<SyntaxToken, string>> errorLog,
            List<Tuple<SyntaxToken, string>> warningLog)
            : base(project, errorLog, warningLog)
        {

        }

        /// <summary>
        /// Parses the syntax tree for errors.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        internal void Parse(SyntaxTree tree)
        {
            var project = base.Project.CompilationContext.GetProjectWithName(base.Project.Name);
            var compilation = project.GetCompilationAsync().Result;

            var stateGroups = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => Querying.IsMachineStateGroup(compilation, val)).
                ToList();

            foreach (var stateGroup in stateGroups)
            {
                this.CheckForNonStateClasses(stateGroup, compilation);
                this.CheckForAtLeastOneStateOrGroup(stateGroup, compilation);
                this.CheckForStructs(stateGroup);
            }
        }


        #endregion

        #region private API

        /// <summary>
        /// Checks that no non-state or non-state-group classes are declared inside the group.
        /// </summary>
        /// <param name="stateGroup">State group</param>
        /// <param name="compilation">Compilation</param>
        private void CheckForNonStateClasses(ClassDeclarationSyntax stateGroup,
            CodeAnalysis.Compilation compilation)
        {
            var classIdentifiers = stateGroup.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => !Querying.IsMachineState(compilation, val) &&
                    !Querying.IsMachineStateGroup(compilation, val)).
                Select(val => val.Identifier).
                ToList();

            foreach (var identifier in classIdentifiers)
            {
                base.ErrorLog.Add(Tuple.Create(identifier, "Not allowed to declare non-state, " +
                    $"non-group class '{identifier.ValueText}' inside state group " +
                    $"'{stateGroup.Identifier.ValueText}'."));
            }
        }

        /// <summary>
        /// Checks that at least one state or group is declared inside the group.
        /// </summary>
        /// <param name="stateGroup">State group</param>
        /// <param name="compilation">Compilation</param>
        private void CheckForAtLeastOneStateOrGroup(ClassDeclarationSyntax stateGroup,
            CodeAnalysis.Compilation compilation)
        {
            var states = stateGroup.DescendantNodes().OfType<ClassDeclarationSyntax>().
                Where(val => Querying.IsMachineState(compilation, val) ||
                    Querying.IsMachineStateGroup(compilation, val)).
                ToList();

            if (states.Count == 0)
            {
                base.WarningLog.Add(Tuple.Create(stateGroup.Identifier, "State group " +
                    $"'{stateGroup.Identifier.ValueText}' must declare at least one state or group."));
            }
        }

        /// <summary>
        /// Checks that no structs are declared inside the group.
        /// </summary>
        /// <param name="stateGroup">State group</param>
        private void CheckForStructs(ClassDeclarationSyntax stateGroup)
        {
            var structIdentifiers = stateGroup.DescendantNodes().OfType<StructDeclarationSyntax>().
                Select(val => val.Identifier).
                ToList();

            foreach (var identifier in structIdentifiers)
            {
                base.ErrorLog.Add(Tuple.Create(identifier, "Not allowed to declare struct " +
                    $"'{identifier.ValueText}' inside state group '{stateGroup.Identifier.ValueText}'."));
            }
        }

        #endregion
    }
}
