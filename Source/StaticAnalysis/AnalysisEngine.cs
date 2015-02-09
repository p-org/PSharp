//-----------------------------------------------------------------------
// <copyright file="AnalysisEngine.cs">
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

using Microsoft.PSharp.Tooling;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// The P# static analysis engine.
    /// </summary>
    public static class AnalysisEngine
    {
        #region fields

        internal static bool IsActive = false;

        /// <summary>
        /// List of machine class declerations in the project.
        /// </summary>
        internal static List<ClassDeclarationSyntax> Machines = null;

        /// <summary>
        /// Dictionary containing machine inheritance information.
        /// </summary>
        internal static Dictionary<ClassDeclarationSyntax, ClassDeclarationSyntax> MachineInheritance = null;

        /// <summary>
        /// List of machine actions per machine in the project.
        /// </summary>
        internal static Dictionary<ClassDeclarationSyntax, List<string>> MachineActions = null;

        /// <summary>
        /// Dictionary of method summaries in the project.
        /// </summary>
        internal static Dictionary<BaseMethodDeclarationSyntax, MethodSummary> Summaries = null;

        /// <summary>
        /// Dictionary of state transition graphs in the project.
        /// </summary>
        internal static Dictionary<ClassDeclarationSyntax, StateTransitionGraphNode> StateTransitionGraphs = null;

        #endregion

        #region public API

        /// <summary>
        /// Start the P# static analysis engine.
        /// </summary>
        public static void Start()
        {
            AnalysisEngine.Machines = new List<ClassDeclarationSyntax>();
            AnalysisEngine.MachineInheritance = new Dictionary<ClassDeclarationSyntax, ClassDeclarationSyntax>();
            AnalysisEngine.MachineActions = new Dictionary<ClassDeclarationSyntax, List<string>>();
            AnalysisEngine.Summaries = new Dictionary<BaseMethodDeclarationSyntax, MethodSummary>();
            AnalysisEngine.StateTransitionGraphs = new Dictionary<ClassDeclarationSyntax, StateTransitionGraphNode>();
            
            // Finds all the machines in the project.
            AnalysisEngine.FindAllMachines();

            // Finds machine inheritance information.
            AnalysisEngine.FindMachineInheritanceInformation();

            // Find all machine actions in the project.
            AnalysisEngine.FindAllMachineActions();

            AnalysisEngine.IsActive = true;
        }

        /// <summary>
        /// Stops the P# static analysis engine
        /// </summary>
        public static void Stop()
        {
            AnalysisEngine.Machines.Clear();
            AnalysisEngine.MachineInheritance.Clear();
            AnalysisEngine.MachineActions.Clear();
            AnalysisEngine.Summaries.Clear();
            AnalysisEngine.StateTransitionGraphs.Clear();

            AnalysisEngine.IsActive = false;
        }

        /// <summary>
        /// Prints program statistics.
        /// </summary>
        public static void PrintStatistics()
        {
            if (!Configuration.ShowProgramStatistics)
            {
                return;
            }

            Console.WriteLine("Number of machines in the program: {0}",
                StateTransitionAnalysis.NumOfMachines);
            Console.WriteLine("Number of state transitions in the program: {0}",
                StateTransitionAnalysis.NumOfTransitions);
            Console.WriteLine("Number of action bindings in the program: {0}",
                StateTransitionAnalysis.NumOfActionBindings);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Finds all P# machines in the project.
        /// </summary>
        private static void FindAllMachines()
        {
            // Iterate the syntax trees for each project file.
            foreach (var tree in ProgramContext.Compilation.SyntaxTrees)
            {
                if (!AnalysisEngine.IsProgramSyntaxTree(tree))
                {
                    continue;
                }

                // Get the tree's semantic model.
                var model = ProgramContext.Compilation.GetSemanticModel(tree);

                // Get the tree's root node compilation unit.
                var root = (CompilationUnitSyntax)tree.GetRoot();

                // Iterate the class declerations only if they are machines.
                foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (AnalysisEngine.IsMachine(classDecl))
                    {
                        AnalysisEngine.Machines.Add(classDecl);
                    }
                }
            }
        }

        /// <summary>
        /// Finds machine inheritance information for all P# machines
        /// in the project.
        /// </summary>
        private static void FindMachineInheritanceInformation()
        {
            foreach (var machine in AnalysisEngine.Machines)
            {
                var model = ProgramContext.Compilation.GetSemanticModel(machine.SyntaxTree);
                var types = machine.BaseList.Types;
                foreach (var type in types)
                {
                    var typeSymbol = model.GetTypeInfo(type).Type;
                    if (Utilities.IsMachineType(typeSymbol, model))
                    {
                        if (!typeSymbol.Name.Equals("Machine"))
                        {
                            var inheritedMachine = AnalysisEngine.Machines.Find(v
                                => v.Identifier.ValueText.Equals(typeSymbol.Name));
                            AnalysisEngine.MachineInheritance.Add(machine, inheritedMachine);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds all machine actions for each P# machine in the project.
        /// </summary>
        private static void FindAllMachineActions()
        {
            foreach (var machine in AnalysisEngine.Machines)
            {
                var actionBindingFunc = machine.ChildNodes().OfType<MethodDeclarationSyntax>().
                    SingleOrDefault(m => m.Modifiers.Any(SyntaxKind.OverrideKeyword) &&
                    m.Identifier.ValueText.Equals("DefineActionBindings"));
                if (actionBindingFunc == null)
                {
                    AnalysisEngine.MachineActions.Add(machine, new List<string>());
                    continue;
                }

                var model = ProgramContext.Compilation.GetSemanticModel(machine.SyntaxTree);

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

                AnalysisEngine.MachineActions.Add(machine, actionNames);
            }
        }

        /// <summary>
        /// Returns true if the given class declaration is a P# machine.
        /// </summary>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean value</returns>
        private static bool IsMachine(ClassDeclarationSyntax classDecl)
        {
            if (classDecl.BaseList == null ||
                classDecl.BaseList.Types.Any(t => t.ToString().Equals("Event")) ||
                classDecl.BaseList.Types.Any(t => t.ToString().Equals("State")))
            {
                return false;
            }
            else if (!classDecl.BaseList.Types.Any(t => t.ToString().Equals("Machine")))
            {
                foreach (var tree in ProgramContext.Compilation.SyntaxTrees)
                {
                    var model = ProgramContext.Compilation.GetSemanticModel(tree);
                    foreach (var type in classDecl.BaseList.Types)
                    {
                        ITypeSymbol typeSymbol = null;
                        try
                        {
                            typeSymbol = model.GetTypeInfo(type).Type;
                        }
                        catch
                        {
                            continue;
                        }

                        if (typeSymbol.DeclaringSyntaxReferences.Count() == 0)
                        {
                            break;
                        }

                        var parentClass = typeSymbol.DeclaringSyntaxReferences.First()
                            .GetSyntax() as ClassDeclarationSyntax;
                        if (parentClass != null && parentClass is ClassDeclarationSyntax &&
                            AnalysisEngine.IsMachine(parentClass))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the syntax tree belongs to the P# program.
        /// Else returns false.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>Boolean value</returns>
        private static bool IsProgramSyntaxTree(SyntaxTree tree)
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
