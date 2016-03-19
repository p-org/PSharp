//-----------------------------------------------------------------------
// <copyright file="PSharpAnalysisContext.cs">
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
using Microsoft.CodeAnalysis.FindSymbols;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// The P# static analysis context.
    /// </summary>
    public sealed class PSharpAnalysisContext : AnalysisContext
    {
        #region fields

        /// <summary>
        /// List of state-machines in the project.
        /// </summary>
        internal List<StateMachine> Machines;

        /// <summary>
        /// Dictionary of state transition graphs in the project.
        /// </summary>
        internal Dictionary<StateMachine, StateTransitionGraphNode> StateTransitionGraphs;

        /// <summary>
        /// Dictionary containing machine inheritance information.
        /// </summary>
        internal Dictionary<StateMachine, StateMachine> MachineInheritanceMap;

        #endregion

        #region public API

        /// <summary>
        /// Create a new state-machine static analysis context.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="project">Project</param>
        /// <returns>StateMachineAnalysisContext</returns>
        public new static PSharpAnalysisContext Create(Configuration configuration, Project project)
        {
            return new PSharpAnalysisContext(configuration, project);
        }

        /// <summary>
        /// Tries to get the method summary of the given object creation. Returns
        /// null if such summary cannot be found.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>MethodSummary</returns>
        public override MethodSummary TryGetSummary(ObjectCreationExpressionSyntax call, SemanticModel model)
        {
            return PSharpMethodSummary.TryGet(call, model, this);
        }

        /// <summary>
        /// Tries to get the method summary of the given invocation. Returns
        /// null if such summary cannot be found.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>MethodSummary</returns>
        public override MethodSummary TryGetSummary(InvocationExpressionSyntax call, SemanticModel model)
        {
            return PSharpMethodSummary.TryGet(call, model, this);
        }

        /// <summary>
        /// Returns true if the given type is passed by value or is immutable.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Boolean</returns>
        public override bool IsTypePassedByValueOrImmutable(ITypeSymbol type)
        {
            var typeName = type.ContainingNamespace.ToString() + "." + type.Name;
            if (base.IsTypePassedByValueOrImmutable(type) ||
                typeName.Equals(typeof(Microsoft.PSharp.MachineId).FullName))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="project">Project</param>
        private PSharpAnalysisContext(Configuration configuration, Project project)
            : base(configuration, project)
        {
            this.Machines = new List<StateMachine>();
            this.StateTransitionGraphs = new Dictionary<StateMachine, StateTransitionGraphNode>();
            this.MachineInheritanceMap = new Dictionary<StateMachine, StateMachine>();

            this.FindAllStateMachines();
            this.FindStateMachineInheritanceInformation();
        }

        #endregion

        #region internal API

        /// <summary>
        /// Returns true if the given field symbol belongs to the machine
        /// that owns the given method summary. Returns false if not.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean</returns>
        internal bool DoesFieldBelongToMachine(ISymbol symbol, PSharpMethodSummary summary)
        {
            if (symbol == null || summary.Machine == null ||
                !(symbol is IFieldSymbol))
            {
                return false;
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(symbol, this.Solution).Result;
            var fieldDecl = definition.DeclaringSyntaxReferences.First().GetSyntax().
                AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();

            if (summary.Machine.Declaration.ChildNodes().OfType<FieldDeclarationSyntax>().Contains(fieldDecl))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Finds all state-machines in the project.
        /// </summary>
        private void FindAllStateMachines()
        {
            // Iterate the syntax trees for each project file.
            foreach (var tree in base.Compilation.SyntaxTrees)
            {
                if (!base.IsProgramSyntaxTree(tree))
                {
                    continue;
                }
                
                // Get the tree's semantic model.
                var model = base.Compilation.GetSemanticModel(tree);

                // Get the tree's root node compilation unit.
                var root = (CompilationUnitSyntax)tree.GetRoot();

                // Iterate the class declerations only if they are machines.
                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (Querying.IsMachine(base.Compilation, classDecl))
                    {
                        this.Machines.Add(new StateMachine(classDecl, this));
                    }
                }
            }
        }

        /// <summary>
        /// Finds state-machine inheritance information for all
        /// state-machines in the project.
        /// </summary>
        private void FindStateMachineInheritanceInformation()
        {
            //foreach (var machine in this.Machines)
            //{
            //    IList<INamedTypeSymbol> baseTypes = base.GetBaseTypes(machine);
            //    foreach (var type in baseTypes)
            //    {
            //        if (type.ToString().Equals(typeof(Microsoft.PSharp.Machine).FullName))
            //        {
            //            break;
            //        }

            //        var inheritedMachine = this.Machines.Find(m
            //            => base.GetFullClassName(m).Equals(type.ToString()));
            //        this.MachineInheritance.Add(machine, inheritedMachine);
            //    }
            //}
        }

        #endregion
    }
}
