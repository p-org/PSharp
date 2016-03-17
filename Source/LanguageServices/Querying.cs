//-----------------------------------------------------------------------
// <copyright file="Querying.cs">
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

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// Class implementing common P# language queries.
    /// </summary>
    internal static class Querying
    {
        #region state-machine specific queries

        /// <summary>
        /// Returns true if the given class declaration is a P# machine.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean</returns>
        internal static bool IsMachine(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl)
        {
            var result = false;
            if (classDecl.BaseList == null)
            {
                return result;
            }

            var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(classDecl);
            
            while (true)
            {
                if (symbol.ToString() == typeof(Machine).FullName)
                {
                    result = true;
                    break;
                }
                else if (symbol.BaseType != null)
                {
                    symbol = symbol.BaseType;
                    continue;
                }

                break;
            }

            return result;
        }

        /// <summary>
        /// Returns true if the given class declaration is a P# machine state.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean</returns>
        internal static bool IsMachineState(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl)
        {
            var result = false;
            if (classDecl.BaseList == null)
            {
                return result;
            }

            var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(classDecl);

            while (true)
            {
                if (symbol.ToString() == typeof(MachineState).FullName)
                {
                    result = true;
                    break;
                }
                else if (symbol.BaseType != null)
                {
                    symbol = symbol.BaseType;
                    continue;
                }

                break;
            }

            return result;
        }

        /// <summary>
        /// Returns true if the given class declaration is a P# event.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean</returns>
        internal static bool IsEventDeclaration(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl)
        {
            var result = false;
            if (classDecl.BaseList == null)
            {
                return result;
            }

            var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(classDecl);

            while (true)
            {
                if (symbol.ToString() == typeof(Event).FullName)
                {
                    result = true;
                    break;
                }
                else if (symbol.BaseType != null)
                {
                    symbol = symbol.BaseType;
                    continue;
                }

                break;
            }

            return result;
        }

        /// <summary>
        /// Returns true if the given class declaration is a P# monitor.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean</returns>
        internal static bool IsMonitor(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl)
        {
            var result = false;
            if (classDecl.BaseList == null)
            {
                return result;
            }

            var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(classDecl);

            while (true)
            {
                if (symbol.ToString() == typeof(Monitor).FullName)
                {
                    result = true;
                    break;
                }
                else if (symbol.BaseType != null)
                {
                    symbol = symbol.BaseType;
                    continue;
                }

                break;
            }

            return result;
        }

        /// <summary>
        /// Returns true if the given class declaration is a P# monitor state.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean</returns>
        internal static bool IsMonitorState(CodeAnalysis.Compilation compilation, ClassDeclarationSyntax classDecl)
        {
            var result = false;
            if (classDecl.BaseList == null)
            {
                return result;
            }

            var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
            var symbol = model.GetDeclaredSymbol(classDecl);

            while (true)
            {
                if (symbol.ToString() == typeof(MonitorState).FullName)
                {
                    result = true;
                    break;
                }
                else if (symbol.BaseType != null)
                {
                    symbol = symbol.BaseType;
                    continue;
                }

                break;
            }

            return result;
        }

        /// <summary>
        /// Returns true if the given invocation is able to send
        /// an event to another machine. Returns false if not.
        /// </summary>
        /// <param name="invocation">Invocation</param>
        /// <param name="callee">Callee</param>
        /// <param name="model">Semantic model</param>
        /// <returns>Boolean</returns>
        internal static bool IsEventSenderInvocation(InvocationExpressionSyntax invocation,
            string callee, SemanticModel model)
        {
            if (callee == null)
            {
                callee = Querying.GetCalleeOfInvocation(invocation);
            }

            if (!(callee.Equals("Send") || callee.Equals("CreateMachine")))
            {
                return false;
            }

            string qualifier = model.GetSymbolInfo(invocation).Symbol.ContainingSymbol.ToString();
            if (!qualifier.Equals("Microsoft.PSharp.Machine"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the full name of the given method (including
        /// namespace if it can detect it).
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="machine">Machine</param>
        /// <returns>Full name of the method</returns>
        internal static string GetFullMethodName(BaseMethodDeclarationSyntax method,
            ClassDeclarationSyntax machine = null)
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

            if (machine == null)
            {
                return name;
            }

            name = machine.Identifier.ValueText + "." + name;

            SyntaxNode node = machine.Parent;
            if (node == null)
            {
                return name;
            }

            NamespaceDeclarationSyntax namespaceDecl = null;
            Querying.TryGetNamespaceDeclarationOfSyntaxNode(machine, out namespaceDecl);
            return namespaceDecl.Name + "." + name;
        }

        #endregion

        #region generic queries

        /// <summary>
        /// Returns the callee of the given call expression.
        /// </summary>
        /// <param name="invocation">Invocation</param>
        /// <returns>Callee</returns>
        internal static string GetCalleeOfInvocation(InvocationExpressionSyntax invocation)
        {
            string callee = "";

            if (invocation.Expression is MemberAccessExpressionSyntax)
            {
                var memberAccessExpr = invocation.Expression as MemberAccessExpressionSyntax;
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
                callee = invocation.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().
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

            return Querying.TryGetNamespaceDeclarationOfSyntaxNode(syntaxNode, out result);
        }

        #endregion
    }
}
