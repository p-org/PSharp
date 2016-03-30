//-----------------------------------------------------------------------
// <copyright file="VirtualDispatchQuerying.cs">
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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.DataFlowAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

using Microsoft.PSharp.LanguageServices;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// Virtual dispatch querying API.
    /// </summary>
    internal static class VirtualDispatchQuerying
    {
        #region internal API

        /// <summary>
        /// Tries to get the list of potential methods that can override the given virtual call.
        /// If it cannot find such methods then it returns false.
        /// </summary>
        /// <param name="overriders">List of overrider methods</param>
        /// <param name="virtualCall">Virtual call</param>
        /// <param name="syntaxNode">SyntaxNode</param>
        /// <param name="cfgNode">ControlFlowGraphNode</param>
        /// <param name="originalClass">Original class</param>
        /// <param name="model">SemanticModel</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>Boolean</returns>
        internal static bool TryGetPotentialMethodOverriders(out HashSet<MethodDeclarationSyntax> overriders,
            InvocationExpressionSyntax virtualCall, SyntaxNode syntaxNode, ControlFlowGraphNode cfgNode,
            ClassDeclarationSyntax originalClass, SemanticModel model, AnalysisContext context)
        {
            overriders = new HashSet<MethodDeclarationSyntax>();

            ISymbol calleeSymbol = null;
            SimpleNameSyntax callee = null;
            bool isThis = false;

            if (virtualCall.Expression is MemberAccessExpressionSyntax)
            {
                var expr = virtualCall.Expression as MemberAccessExpressionSyntax;
                var identifier = expr.Expression.DescendantNodesAndSelf().
                    OfType<IdentifierNameSyntax>().Last();
                calleeSymbol = model.GetSymbolInfo(identifier).Symbol;
                
                if (expr.Expression is ThisExpressionSyntax)
                {
                    callee = expr.Name;
                    isThis = true;
                }
            }
            else
            {
                callee = virtualCall.Expression as IdentifierNameSyntax;
                isThis = true;
            }

            if (isThis)
            {
                foreach (var nestedClass in originalClass.ChildNodes().OfType<ClassDeclarationSyntax>())
                {
                    foreach (var method in nestedClass.ChildNodes().OfType<MethodDeclarationSyntax>())
                    {
                        if (method.Identifier.ToString().Equals(callee.Identifier.ToString()))
                        {
                            overriders.Add(method);
                            return true;
                        }
                    }
                }

                foreach (var method in originalClass.ChildNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (method.Identifier.ToString().Equals(callee.Identifier.ToString()))
                    {
                        overriders.Add(method);
                        return true;
                    }
                }

                return false;
            }

            if (calleeSymbol == null)
            {
                return false;
            }

            Dictionary<ISymbol, HashSet<ITypeSymbol>> referenceTypeMap = null;
            if (!cfgNode.GetMethodSummary().DataFlowAnalysis.TryGetReferenceTypeMapForSyntaxNode(
                syntaxNode, cfgNode, out referenceTypeMap))
            {
                return false;
            }

            if (!referenceTypeMap.ContainsKey(calleeSymbol))
            {
                return false;
            }

            foreach (var objectType in referenceTypeMap[calleeSymbol])
            {
                MethodDeclarationSyntax m = null;
                if (VirtualDispatchQuerying.TryGetMethodFromType(out m, objectType, virtualCall, context))
                {
                    overriders.Add(m);
                }
            }

            return true;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Tries to get the method from the given type and virtual call.
        /// </summary>
        /// <param name="method">Method</param>
        /// <param name="type">Type</param>
        /// <param name="virtualCall">Virtual call</param>
        /// <param name="context">AnalysisContext</param>
        /// <returns>Boolean</returns>
        private static bool TryGetMethodFromType(out MethodDeclarationSyntax method, ITypeSymbol type,
            InvocationExpressionSyntax virtualCall, AnalysisContext context)
        {
            method = null;

            var definition = SymbolFinder.FindSourceDefinitionAsync(type, context.Solution).Result;
            if (definition == null)
            {
                return false;
            }

            var calleeClass = definition.DeclaringSyntaxReferences.First().GetSyntax()
                as ClassDeclarationSyntax;
            foreach (var m in calleeClass.ChildNodes().OfType<MethodDeclarationSyntax>())
            {
                if (m.Identifier.ValueText.Equals(Querying.GetCalleeOfInvocation(virtualCall)))
                {
                    method = m;
                    break;
                }
            }

            return true;
        }

        #endregion
    }
}
