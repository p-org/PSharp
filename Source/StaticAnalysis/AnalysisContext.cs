//-----------------------------------------------------------------------
// <copyright file="AnalysisContext.cs">
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
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// The P# static analysis context.
    /// </summary>
    public sealed class AnalysisContext
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        internal LanguageServicesConfiguration Configuration;

        /// <summary>
        /// The project compilation for this analysis context.
        /// </summary>
        internal Compilation Compilation = null;

        /// <summary>
        /// List of machine class declerations in the project.
        /// </summary>
        internal List<ClassDeclarationSyntax> Machines = null;

        /// <summary>
        /// Dictionary containing machine inheritance information.
        /// </summary>
        internal Dictionary<ClassDeclarationSyntax, ClassDeclarationSyntax> MachineInheritance = null;

        /// <summary>
        /// List of machine actions per machine in the project.
        /// </summary>
        internal Dictionary<ClassDeclarationSyntax, List<string>> MachineActions = null;

        /// <summary>
        /// Dictionary of method summaries in the project.
        /// </summary>
        internal Dictionary<BaseMethodDeclarationSyntax, MethodSummary> Summaries = null;

        /// <summary>
        /// Dictionary of state transition graphs in the project.
        /// </summary>
        internal Dictionary<ClassDeclarationSyntax, StateTransitionGraphNode> StateTransitionGraphs = null;

        #endregion

        #region public API

        /// <summary>
        /// Create a new P# static analysis context from the given project.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="project">Project</param>
        /// <returns>AnalysisContext</returns>
        public static AnalysisContext Create(LanguageServicesConfiguration configuration, Project project)
        {
            return new AnalysisContext(configuration, project);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="project">Project</param>
        private AnalysisContext(LanguageServicesConfiguration configuration, Project project)
        {
            this.Configuration = configuration;
            this.Compilation = project.GetCompilationAsync().Result;

            this.Machines = new List<ClassDeclarationSyntax>();
            this.MachineInheritance = new Dictionary<ClassDeclarationSyntax, ClassDeclarationSyntax>();
            this.MachineActions = new Dictionary<ClassDeclarationSyntax, List<string>>();
            this.Summaries = new Dictionary<BaseMethodDeclarationSyntax, MethodSummary>();
            this.StateTransitionGraphs = new Dictionary<ClassDeclarationSyntax, StateTransitionGraphNode>();

            // Finds all the machines in the project.
            this.FindAllMachines();

            // Finds machine inheritance information.
            this.FindMachineInheritanceInformation();

            // Find all machine actions in the project.
            this.FindAllMachineActions();
        }

        /// <summary>
        /// Finds all P# machines in the project.
        /// </summary>
        private void FindAllMachines()
        {
            // Iterate the syntax trees for each project file.
            foreach (var tree in this.Compilation.SyntaxTrees)
            {
                if (!this.IsProgramSyntaxTree(tree))
                {
                    continue;
                }

                // Get the tree's semantic model.
                var model = this.Compilation.GetSemanticModel(tree);

                // Get the tree's root node compilation unit.
                var root = (CompilationUnitSyntax)tree.GetRoot();

                // Iterate the class declerations only if they are machines.
                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (Querying.IsMachine(this.Compilation, classDecl))
                    {
                        this.Machines.Add(classDecl);
                    }
                }
            }
        }

        /// <summary>
        /// Finds machine inheritance information for all P# machines
        /// in the project.
        /// </summary>
        private void FindMachineInheritanceInformation()
        {
            foreach (var machine in this.Machines)
            {
                var model = this.Compilation.GetSemanticModel(machine.SyntaxTree);
                var types = machine.BaseList.Types;
                foreach (var type in types)
                {
                    var typeSymbol = model.GetTypeInfo(type).Type;
                    if (Utilities.IsMachineType(typeSymbol, model, this))
                    {
                        if (!typeSymbol.Name.Equals("Machine"))
                        {
                            var inheritedMachine = this.Machines.Find(v
                                => v.Identifier.ValueText.Equals(typeSymbol.Name));
                            this.MachineInheritance.Add(machine, inheritedMachine);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds all machine actions for each P# machine in the project.
        /// </summary>
        private void FindAllMachineActions()
        {
            foreach (var machine in this.Machines)
            {
                var actionBindingFunc = machine.ChildNodes().OfType<MethodDeclarationSyntax>().
                    SingleOrDefault(m => m.Modifiers.Any(SyntaxKind.OverrideKeyword) &&
                    m.Identifier.ValueText.Equals("DefineActionBindings"));
                if (actionBindingFunc == null)
                {
                    this.MachineActions.Add(machine, new List<string>());
                    continue;
                }

                var model = this.Compilation.GetSemanticModel(machine.SyntaxTree);

                List<string> actionNames = new List<string>();
                foreach (var action in actionBindingFunc.DescendantNodesAndSelf().
                    OfType<ObjectCreationExpressionSyntax>())
                {
                    var type = model.GetTypeInfo(action).Type;
                    if (!type.ToString().Equals("System.Action"))
                    {
                        continue;
                    }

                    var actionFunc = action.ArgumentList.Arguments[0].Expression;
                    if (!(actionFunc is IdentifierNameSyntax))
                    {
                        continue;
                    }

                    var method = machine.ChildNodes().OfType<MethodDeclarationSyntax>().
                        SingleOrDefault(m => m.Identifier.ValueText.Equals(
                            (actionFunc as IdentifierNameSyntax).Identifier.ValueText) &&
                            m.ParameterList.Parameters.Count == 0);
                    var methodName = Utilities.GetFullMethodName(method, machine, null);
                    actionNames.Add(methodName);
                }

                this.MachineActions.Add(machine, actionNames);
            }
        }

        /// <summary>
        /// Returns true if the syntax tree belongs to the P# program.
        /// Else returns false.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>Boolean value</returns>
        private bool IsProgramSyntaxTree(SyntaxTree tree)
        {
            if (tree.FilePath.Contains("\\AssemblyInfo.cs") ||
                    tree.FilePath.Contains(".NETFramework,"))
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
