﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        internal static bool IsMachine(CodeAnalysis.Compilation compilation,
            ClassDeclarationSyntax classDecl)
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
        internal static bool IsMachineState(CodeAnalysis.Compilation compilation,
            ClassDeclarationSyntax classDecl)
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
        /// Returns true if the given class declaration is a P# machine state group.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="classDecl">Class declaration</param>
        /// <returns>Boolean</returns>
        internal static bool IsMachineStateGroup(CodeAnalysis.Compilation compilation,
            ClassDeclarationSyntax classDecl)
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
                if (symbol.ToString() == typeof(StateGroup).FullName)
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
        internal static bool IsEventDeclaration(CodeAnalysis.Compilation compilation,
            ClassDeclarationSyntax classDecl)
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
        internal static bool IsMonitor(CodeAnalysis.Compilation compilation,
            ClassDeclarationSyntax classDecl)
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
        internal static bool IsMonitorState(CodeAnalysis.Compilation compilation,
            ClassDeclarationSyntax classDecl)
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

        #endregion

        #region private methods

        /// <summary>
        /// Returns the callee of the given call expression.
        /// </summary>
        /// <param name="invocation">Invocation</param>
        /// <returns>Callee</returns>
        private static string GetCalleeOfInvocation(InvocationExpressionSyntax invocation)
        {
            string callee = string.Empty;

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

        #endregion
    }
}
