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
using Microsoft.CodeAnalysis.FindSymbols;

using Microsoft.PSharp.LanguageServices;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// A P# static analysis context.
    /// </summary>
    public sealed class AnalysisContext
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        internal Configuration Configuration;

        /// <summary>
        /// The solution of the P# program.
        /// </summary>
        internal Solution Solution;

        /// <summary>
        /// The project compilation for this analysis context.
        /// </summary>
        internal Compilation Compilation;

        /// <summary>
        /// List of machine class declerations in the project.
        /// </summary>
        internal List<ClassDeclarationSyntax> Machines;

        /// <summary>
        /// Dictionary containing machine inheritance information.
        /// </summary>
        internal Dictionary<ClassDeclarationSyntax, ClassDeclarationSyntax> MachineInheritance;

        /// <summary>
        /// List of machine actions per machine in the project.
        /// </summary>
        internal Dictionary<ClassDeclarationSyntax, List<string>> MachineActions;

        /// <summary>
        /// Dictionary of method summaries in the project.
        /// </summary>
        internal Dictionary<BaseMethodDeclarationSyntax, MethodSummary> Summaries;

        /// <summary>
        /// Dictionary of state transition graphs in the project.
        /// </summary>
        internal Dictionary<ClassDeclarationSyntax, StateTransitionGraphNode> StateTransitionGraphs;

        #endregion

        #region public API

        /// <summary>
        /// Create a new P# static analysis context from the given project.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="project">Project</param>
        /// <returns>AnalysisContext</returns>
        public static AnalysisContext Create(Configuration configuration, Project project)
        {
            return new AnalysisContext(configuration, project);
        }

        #endregion

        #region internal API

        /// <summary>
        /// Returns true if the given field symbol belongs to the machine
        /// that owns the given method summary. Returns false if not.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="summary">MethodSummary</param>
        /// <returns>Boolean value</returns>
        internal bool DoesFieldBelongToMachine(ISymbol symbol, MethodSummary summary)
        {
            if (symbol == null || summary.Machine == null ||
                !(symbol is IFieldSymbol))
            {
                return false;
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(symbol, this.Solution).Result;
            var fieldDecl = definition.DeclaringSyntaxReferences.First().GetSyntax().
                AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();

            if (summary.Machine.ChildNodes().OfType<FieldDeclarationSyntax>().Contains(fieldDecl))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the first non machine identifier.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Identifier</returns>
        internal IdentifierNameSyntax GetFirstNonMachineIdentifier(ExpressionSyntax expr,
            SemanticModel model)
        {
            IdentifierNameSyntax identifier = null;

            if (expr is IdentifierNameSyntax)
            {
                identifier = expr as IdentifierNameSyntax;
            }
            else if (expr is MemberAccessExpressionSyntax)
            {
                foreach (var id in (expr as MemberAccessExpressionSyntax).DescendantNodes().
                    OfType<IdentifierNameSyntax>())
                {
                    if (!this.IsMachineType(id, model))
                    {
                        identifier = id;
                        break;
                    }
                }
            }

            return identifier;
        }

        /// <summary>
        /// Returns true if the given expression is a non machine member access
        /// Returns false if it is.
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Boolean value</returns>
        internal bool IsExprNonMachineMemberAccess(ExpressionSyntax expr, SemanticModel model)
        {
            IdentifierNameSyntax identifier = null;
            bool isMemberAccess = false;

            if (expr is MemberAccessExpressionSyntax)
            {
                foreach (var id in (expr as MemberAccessExpressionSyntax).DescendantNodes().
                    OfType<IdentifierNameSyntax>())
                {
                    if (!this.IsMachineType(id, model))
                    {
                        identifier = id;
                        break;
                    }
                }

                if (identifier != null && identifier.Identifier.ValueText.Equals((expr
                    as MemberAccessExpressionSyntax).Name.Identifier.ValueText))
                {
                    isMemberAccess = true;
                }
            }

            return isMemberAccess;
        }

        /// <summary>
        /// Returns true if the type of the expression is an enum.
        /// Returns false if it is not
        /// </summary>
        /// <param name="model">SemanticModel</param>
        /// <returns>Boolean value</returns>
        internal bool IsExprEnum(ExpressionSyntax expr, SemanticModel model)
        {
            var type = model.GetTypeInfo(expr).Type;
            var typeDef = SymbolFinder.FindSourceDefinitionAsync(type, this.Solution).Result;
            if (typeDef != null && typeDef.DeclaringSyntaxReferences.First().
                GetSyntax().IsKind(SyntaxKind.EnumDeclaration))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given identifier is a machine.
        /// Returns false if it is not.
        /// </summary>
        /// <param name="identifier">Identifier</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Boolean value</returns>
        internal bool IsMachineType(IdentifierNameSyntax identifier, SemanticModel model)
        {
            TypeInfo typeInfo;
            try
            {
                typeInfo = model.GetTypeInfo(identifier);
            }
            catch
            {
                return false;
            }

            if (typeInfo.Type != null && typeInfo.Type.ToString().Equals("Microsoft.PSharp.Machine"))
            {
                return true;
            }
            else
            {
                var symbol = model.GetSymbolInfo(identifier).Symbol;
                var definition = SymbolFinder.FindSourceDefinitionAsync(symbol, this.Solution).Result;
                if (definition != null)
                {
                    var machineNode = definition.DeclaringSyntaxReferences.First().GetSyntax();
                    if (machineNode is ClassDeclarationSyntax)
                    {
                        NamespaceDeclarationSyntax machineNamespace = null;
                        this.TryGetNamespaceDeclarationOfSyntaxNode(
                            machineNode, out machineNamespace);
                        string machineName = machineNamespace.Name + "." + (machineNode
                            as ClassDeclarationSyntax).Identifier.ValueText;

                        foreach (var knownMachine in this.Machines)
                        {
                            NamespaceDeclarationSyntax knownMachineNamespace = null;
                            this.TryGetNamespaceDeclarationOfSyntaxNode(
                                knownMachine, out knownMachineNamespace);
                            string knownMachineName = knownMachineNamespace.Name + "." +
                                (knownMachine as ClassDeclarationSyntax).Identifier.ValueText;
                            if (machineName.Equals(knownMachineName))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                    else if (machineNode is VariableDeclaratorSyntax)
                    {
                        if (machineNode.FirstAncestorOrSelf<FieldDeclarationSyntax>() == null)
                        {
                            IdentifierNameSyntax machine = null;
                            if ((machineNode as VariableDeclaratorSyntax).Initializer == null)
                            {
                                machine = machineNode.Parent.DescendantNodesAndSelf().
                                    OfType<IdentifierNameSyntax>().First();
                            }
                            else
                            {
                                machine = (machineNode as VariableDeclaratorSyntax).Initializer.Value.
                                    DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().First();
                            }

                            var s = model.GetSymbolInfo(machine).Symbol;
                            return s.ToString().Equals("Microsoft.PSharp.MachineState.Machine");
                        }
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Returns true if the given type symbol is a machine.
        /// Returns false if it is not.
        /// </summary>
        /// <param name="identifier">Identifier</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Boolean value</returns>
        internal bool IsMachineType(ITypeSymbol typeSymbol, SemanticModel model)
        {
            if (typeSymbol != null && typeSymbol.ToString().Equals("Microsoft.PSharp.Machine"))
            {
                return true;
            }
            else
            {
                var definition = SymbolFinder.FindSourceDefinitionAsync(typeSymbol, this.Solution).Result;
                if (definition != null)
                {
                    var machineNode = definition.DeclaringSyntaxReferences.First().GetSyntax();
                    if (machineNode is ClassDeclarationSyntax)
                    {
                        NamespaceDeclarationSyntax machineNamespace = null;
                        this.TryGetNamespaceDeclarationOfSyntaxNode(
                            machineNode, out machineNamespace);
                        string machineName = machineNamespace.Name + "." + (machineNode
                            as ClassDeclarationSyntax).Identifier.ValueText;
                        foreach (var knownMachine in this.Machines)
                        {
                            NamespaceDeclarationSyntax knownMachineNamespace = null;
                            this.TryGetNamespaceDeclarationOfSyntaxNode(
                                knownMachine, out knownMachineNamespace);
                            string knownMachineName = knownMachineNamespace.Name + "." +
                                knownMachine.Identifier.ValueText;
                            if (machineName.Equals(knownMachineName))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                    else if (machineNode is VariableDeclaratorSyntax)
                    {
                        if (machineNode.FirstAncestorOrSelf<FieldDeclarationSyntax>() == null)
                        {
                            IdentifierNameSyntax machine = null;
                            if ((machineNode as VariableDeclaratorSyntax).Initializer == null)
                            {
                                machine = machineNode.Parent.DescendantNodesAndSelf().
                                    OfType<IdentifierNameSyntax>().First();
                            }
                            else
                            {
                                machine = (machineNode as VariableDeclaratorSyntax).Initializer.Value.
                                    DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().First();
                            }

                            var s = model.GetSymbolInfo(machine).Symbol;
                            return s.ToString().Equals("Microsoft.PSharp.MachineState.Machine");
                        }
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Checks if the given type is allowed to be send
        /// through an event to another machine.
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Boolean value</returns>
        internal bool IsTypeAllowedToBeSend(TypeSyntax type, SemanticModel model)
        {
            if (type is PredefinedTypeSyntax)
            {
                var predefinedType = (type as PredefinedTypeSyntax).Keyword;
                if (predefinedType.IsKind(SyntaxKind.BoolKeyword) ||
                    predefinedType.IsKind(SyntaxKind.ByteKeyword) ||
                    predefinedType.IsKind(SyntaxKind.SByteKeyword) ||
                    predefinedType.IsKind(SyntaxKind.UShortKeyword) ||
                    predefinedType.IsKind(SyntaxKind.ShortKeyword) ||
                    predefinedType.IsKind(SyntaxKind.UIntKeyword) ||
                    predefinedType.IsKind(SyntaxKind.IntKeyword) ||
                    predefinedType.IsKind(SyntaxKind.ULongKeyword) ||
                    predefinedType.IsKind(SyntaxKind.LongKeyword) ||
                    predefinedType.IsKind(SyntaxKind.FloatKeyword) ||
                    predefinedType.IsKind(SyntaxKind.DoubleKeyword) ||
                    predefinedType.IsKind(SyntaxKind.DecimalKeyword) ||
                    predefinedType.IsKind(SyntaxKind.CharKeyword) ||
                    predefinedType.IsKind(SyntaxKind.StringKeyword))
                {
                    return true;
                }
            }
            else if (type is IdentifierNameSyntax)
            {
                var identifierType = (type as IdentifierNameSyntax);
                if (this.IsMachineType(identifierType, model))
                {
                    return true;
                }
                else
                {
                    TypeInfo typeInfo;
                    try
                    {
                        typeInfo = model.GetTypeInfo(identifierType);
                    }
                    catch
                    {
                        return false;
                    }

                    return this.IsTypeAllowedToBeSend(typeInfo.Type);
                }
            }
            else if (type is GenericNameSyntax)
            {
                var typeSymbol = model.GetTypeInfo(type).Type;
                if (!typeSymbol.ToString().EndsWith("<Microsoft.PSharp.Machine>") &&
                    typeSymbol.ContainingNamespace.ToString().Equals("System.Collections.Generic"))
                {
                    return false;
                }

                var genericTypeArgList = (type as GenericNameSyntax).TypeArgumentList;
                foreach (var arg in genericTypeArgList.Arguments)
                {
                    if (!this.IsTypeAllowedToBeSend(arg, model))
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given type is allowed to be send
        /// through and event to another machine.
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Boolean value</returns>
        internal bool IsTypeAllowedToBeSend(ITypeSymbol type)
        {
            var typeName = type.ToString();
            if (typeName.Equals("bool") || typeName.Equals("byte") ||
                typeName.Equals("sbyte") || typeName.Equals("char") ||
                typeName.Equals("decimal") || typeName.Equals("double") ||
                typeName.Equals("float") || typeName.Equals("int") ||
                typeName.Equals("uint") || typeName.Equals("long") ||
                typeName.Equals("ulong") || typeName.Equals("short") ||
                typeName.Equals("ushort") || typeName.Equals("string") ||
                typeName.Equals("Microsoft.PSharp.Machine"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get the symbol from the given expression. Returns
        /// true if it succeeds. Returns false if not.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <param name="expr">Expression</param>
        /// <param name="model">SemanticModel</param>
        /// <returns>Boolean value</returns>
        internal bool TryGetSymbolFromExpression(out ISymbol symbol, ExpressionSyntax expr,
            SemanticModel model)
        {
            IdentifierNameSyntax identifier = null;
            bool result = false;
            symbol = null;

            if (expr is IdentifierNameSyntax)
            {
                identifier = expr as IdentifierNameSyntax;
            }
            else if (expr is MemberAccessExpressionSyntax)
            {
                foreach (var id in (expr as MemberAccessExpressionSyntax).DescendantNodes().
                    OfType<IdentifierNameSyntax>())
                {
                    if (!this.IsMachineType(id, model))
                    {
                        identifier = id;
                        break;
                    }
                }
            }

            if (identifier != null)
            {
                symbol = model.GetSymbolInfo(identifier).Symbol;
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Returns true if the given call is source of giving up ownership of data.
        /// Returns false if it is not.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="model">Semantic model</param>
        /// <param name="callee">Callee (optional)</param>
        /// <returns>Boolean value</returns>
        internal bool IsSourceOfGivingUpOwnership(InvocationExpressionSyntax call, SemanticModel model,
            string callee = null)
        {
            if (callee == null)
            {
                callee = this.GetCallee(call);
            }

            if (!(callee.Equals("Send") || callee.Equals("CreateMachine")))
            {
                return false;
            }

            var suffix = model.GetSymbolInfo(call).Symbol.ContainingSymbol.ToString();
            if (!(suffix.Equals("Microsoft.PSharp.Machine") ||
                suffix.Equals("Microsoft.PSharp.MachineState")))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the analysis should consider the given method.
        /// </summary>
        /// <param name="method">Method</param>
        /// <returns>Boolean value</returns>
        internal bool ShouldAnalyseMethod(MethodDeclarationSyntax method)
        {
            if (method.Modifiers.Any(SyntaxKind.AbstractKeyword) ||
                method.Identifier.ValueText.Equals("DefineIgnoredEvents") ||
                method.Identifier.ValueText.Equals("DefineDeferredEvents") ||
                method.Identifier.ValueText.Equals("DefineGotoStateTransitions") ||
                method.Identifier.ValueText.Equals("DefinePushStateTransitions") ||
                method.Identifier.ValueText.Equals("DefineActionBindings"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the given method is an entry point to the given machine.
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="machine">Machine</param>
        /// <returns>Boolean value</returns>
        internal bool IsEntryPointMethod(MethodDeclarationSyntax method, ClassDeclarationSyntax machine)
        {
            if (method.Modifiers.Any(SyntaxKind.OverrideKeyword) ||
                method.Identifier.ValueText.Equals("OnEntry") ||
                method.Identifier.ValueText.Equals("OnExit"))
            {
                return true;
            }

            var methodName = this.GetFullMethodName(method, machine, null);
            if (this.MachineActions[machine].Contains(methodName))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the full name of the given method (including namespace if it
        /// can detect it). If the method does not belong to a state then the
        /// state paremeter should be the null value.
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="machine">Machine</param>
        /// <param name="state">State</param>
        /// <returns>Full name of the method</returns>
        internal string GetFullMethodName(BaseMethodDeclarationSyntax method,
            ClassDeclarationSyntax machine, ClassDeclarationSyntax state)
        {
            string name = null;
            if (method is MethodDeclarationSyntax)
            {
                name = (method as MethodDeclarationSyntax).Identifier.ValueText;
            }
            else if (method is ConstructorDeclarationSyntax)
            {
                name = (method as ConstructorDeclarationSyntax).Identifier.ValueText;
            }

            if (state != null)
            {
                name = state.Identifier.ValueText + "." + name;
            }

            name = machine.Identifier.ValueText + "." + name;

            var syntaxNode = machine.Parent;
            if (syntaxNode == null)
            {
                return name;
            }

            NamespaceDeclarationSyntax namespaceDecl = null;
            this.TryGetNamespaceDeclarationOfSyntaxNode(machine, out namespaceDecl);
            return namespaceDecl.Name + "." + name;
        }

        /// <summary>
        /// Returns the callee of the given call expression.
        /// </summary>
        /// <param name="call">Call expression</param>
        /// <returns>Callee</returns>
        internal string GetCallee(InvocationExpressionSyntax call)
        {
            string callee = "";

            if (call.Expression is MemberAccessExpressionSyntax)
            {
                var memberAccessExpr = call.Expression as MemberAccessExpressionSyntax;
                if (memberAccessExpr.Name is IdentifierNameSyntax)
                {
                    callee = (memberAccessExpr.Name as IdentifierNameSyntax).Identifier.ValueText;
                }
                else if (memberAccessExpr.Name is GenericNameSyntax)
                {
                    callee = (memberAccessExpr.Name as GenericNameSyntax).Identifier.ValueText;
                }
            }
            else
            {
                callee = call.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().
                    First().Identifier.ValueText;
            }

            return callee;
        }

        /// <summary>
        /// Tries to get the namespace declaration for the given syntax
        /// node. Returns false if it cannot find a namespace.
        /// </summary>
        internal bool TryGetNamespaceDeclarationOfSyntaxNode(SyntaxNode syntaxNode,
            out NamespaceDeclarationSyntax result)
        {
            result = null;

            if (syntaxNode == null)
            {
                return false;
            }

            syntaxNode = syntaxNode.Parent;
            if (syntaxNode == null)
            {
                return false;
            }

            if (syntaxNode.GetType() == typeof(NamespaceDeclarationSyntax))
            {
                result = syntaxNode as NamespaceDeclarationSyntax;
                return true;
            }

            return this.TryGetNamespaceDeclarationOfSyntaxNode(syntaxNode, out result);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="project">Project</param>
        private AnalysisContext(Configuration configuration, Project project)
        {
            this.Configuration = configuration;
            this.Solution = project.Solution;
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
                    if (this.IsMachineType(typeSymbol, model))
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
                    var methodName = this.GetFullMethodName(method, machine, null);
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
