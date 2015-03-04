//-----------------------------------------------------------------------
// <copyright file="Utilities.cs">
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

using System.Linq;

using Microsoft.PSharp.Tooling;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Class implementing commonly used utility methods.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>
        /// Returns true if the given field symbol belongs to the machine
        /// that owns the given method summary. Returns false if not.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="summary"></param>
        /// <returns>Boolean value</returns>
        internal static bool DoesFieldBelongToMachine(ISymbol symbol, MethodSummary summary)
        {
            if (symbol == null || summary.Machine == null ||
                !(symbol is IFieldSymbol))
            {
                return false;
            }

            var definition = SymbolFinder.FindSourceDefinitionAsync(symbol,
                ProgramInfo.Solution).Result;
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
        internal static IdentifierNameSyntax GetFirstNonMachineIdentifier(ExpressionSyntax expr, SemanticModel model)
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
                    if (!Utilities.IsMachineType(id, model))
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
        internal static bool IsExprNonMachineMemberAccess(ExpressionSyntax expr, SemanticModel model)
        {
            IdentifierNameSyntax identifier = null;
            bool isMemberAccess = false;

            if (expr is MemberAccessExpressionSyntax)
            {
                foreach (var id in (expr as MemberAccessExpressionSyntax).DescendantNodes().
                    OfType<IdentifierNameSyntax>())
                {
                    if (!Utilities.IsMachineType(id, model))
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
        internal static bool IsExprEnum(ExpressionSyntax expr, SemanticModel model)
        {
            var type = model.GetTypeInfo(expr).Type;
            var typeDef = SymbolFinder.FindSourceDefinitionAsync(type,
                ProgramInfo.Solution).Result;
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
        internal static bool IsMachineType(IdentifierNameSyntax identifier, SemanticModel model)
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
                var definition = SymbolFinder.FindSourceDefinitionAsync(symbol,
                    ProgramInfo.Solution).Result;
                if (definition != null)
                {
                    var machineNode = definition.DeclaringSyntaxReferences.First().GetSyntax();
                    if (machineNode is ClassDeclarationSyntax)
                    {
                        NamespaceDeclarationSyntax machineNamespace = null;
                        Utilities.TryGetNamespaceDeclarationOfSyntaxNode(
                            machineNode, out machineNamespace);
                        string machineName = machineNamespace.Name + "." + (machineNode
                            as ClassDeclarationSyntax).Identifier.ValueText;

                        foreach (var knownMachine in AnalysisContext.Machines)
                        {
                            NamespaceDeclarationSyntax knownMachineNamespace = null;
                            Utilities.TryGetNamespaceDeclarationOfSyntaxNode(
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
                            return s.ToString().Equals("Microsoft.PSharp.State.Machine");
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
        internal static bool IsMachineType(ITypeSymbol typeSymbol, SemanticModel model)
        {
            if (typeSymbol != null && typeSymbol.ToString().Equals("Microsoft.PSharp.Machine"))
            {
                return true;
            }
            else
            {
                var definition = SymbolFinder.FindSourceDefinitionAsync(typeSymbol,
                    ProgramInfo.Solution).Result;
                if (definition != null)
                {
                    var machineNode = definition.DeclaringSyntaxReferences.First().GetSyntax();
                    if (machineNode is ClassDeclarationSyntax)
                    {
                        NamespaceDeclarationSyntax machineNamespace = null;
                        Utilities.TryGetNamespaceDeclarationOfSyntaxNode(
                            machineNode, out machineNamespace);
                        string machineName = machineNamespace.Name + "." + (machineNode
                            as ClassDeclarationSyntax).Identifier.ValueText;
                        foreach (var knownMachine in AnalysisContext.Machines)
                        {
                            NamespaceDeclarationSyntax knownMachineNamespace = null;
                            Utilities.TryGetNamespaceDeclarationOfSyntaxNode(
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
                            return s.ToString().Equals("Microsoft.PSharp.State.Machine");
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
        internal static bool IsTypeAllowedToBeSend(TypeSyntax type, SemanticModel model)
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
                if (Utilities.IsMachineType(identifierType, model))
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

                    return Utilities.IsTypeAllowedToBeSend(typeInfo.Type);
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
                    if (!Utilities.IsTypeAllowedToBeSend(arg, model))
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
        internal static bool IsTypeAllowedToBeSend(ITypeSymbol type)
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
        internal static bool TryGetSymbolFromExpression(out ISymbol symbol, ExpressionSyntax expr, SemanticModel model)
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
                    if (!Utilities.IsMachineType(id, model))
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
        internal static bool IsSourceOfGivingUpOwnership(InvocationExpressionSyntax call, SemanticModel model,
            string callee = null)
        {
            if (callee == null)
            {
                callee = Utilities.GetCallee(call);
            }

            if (!(callee.Equals("Send") || callee.Equals("Invoke") ||
                callee.Equals("Create") || callee.Equals("CreateMonitor")))
            {
                return false;
            }

            var suffix = model.GetSymbolInfo(call).Symbol.ContainingSymbol.ToString();
            if (!(suffix.Equals("Microsoft.PSharp.Machine") ||
                suffix.Equals("Microsoft.PSharp.State") ||
                suffix.Equals("Microsoft.PSharp.Machine.Factory")))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the given call is invoking a machine factory method.
        /// Returns false if it is not.
        /// </summary>
        /// <param name="call">Call</param>
        /// <param name="model">Semantic model</param>
        /// <param name="callee">Callee (optional)</param>
        /// <returns>Boolean value</returns>
        internal static bool IsMachineFactoryMethod(InvocationExpressionSyntax call, SemanticModel model,
            string callee = null)
        {
            if (callee == null)
            {
                callee = Utilities.GetCallee(call);
            }

            if (!(callee.Equals("Create") || callee.Equals("CreateMonitor")))
            {
                return false;
            }

            var suffix = model.GetSymbolInfo(call).Symbol.ContainingSymbol.ToString();
            if (!(suffix.Equals("Microsoft.PSharp.Machine.Factory")))
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
        internal static bool ShouldAnalyseMethod(MethodDeclarationSyntax method)
        {
            if (method.Modifiers.Any(SyntaxKind.AbstractKeyword) ||
                method.Identifier.ValueText.Equals("DefineIgnoredEvents") ||
                method.Identifier.ValueText.Equals("DefineDeferredEvents") ||
                method.Identifier.ValueText.Equals("DefineStepStateTransitions") ||
                method.Identifier.ValueText.Equals("DefineCallStateTransitions") ||
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
        internal static bool IsEntryPointMethod(MethodDeclarationSyntax method, ClassDeclarationSyntax machine)
        {
            if (method.Modifiers.Any(SyntaxKind.OverrideKeyword) ||
                method.Identifier.ValueText.Equals("OnEntry") ||
                method.Identifier.ValueText.Equals("OnExit"))
            {
                return true;
            }

            var methodName = Utilities.GetFullMethodName(method, machine, null);
            if (AnalysisContext.MachineActions[machine].Contains(methodName))
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
        internal static string GetFullMethodName(BaseMethodDeclarationSyntax method,
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
            Utilities.TryGetNamespaceDeclarationOfSyntaxNode(machine, out namespaceDecl);
            return namespaceDecl.Name + "." + name;
        }

        /// <summary>
        /// Returns the callee of the given call expression.
        /// </summary>
        /// <param name="call">Call expression</param>
        /// <returns>Callee</returns>
        internal static string GetCallee(InvocationExpressionSyntax call)
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
        internal static bool TryGetNamespaceDeclarationOfSyntaxNode(SyntaxNode syntaxNode,
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

            return Utilities.TryGetNamespaceDeclarationOfSyntaxNode(syntaxNode, out result);
        }
    }
}
