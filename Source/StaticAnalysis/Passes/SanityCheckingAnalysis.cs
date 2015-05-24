//-----------------------------------------------------------------------
// <copyright file="SanityCheckingAnalysis.cs">
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
    /// This analysis reports a warning if methods in a machine do stuff that
    /// they are not supposed to e.g directly initialise a machine's state or
    /// call methods that they should not.
    /// </summary>
    public static class SanityCheckingAnalysis
    {
        #region public API

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        public static void Run()
        {
            SanityCheckingAnalysis.CheckMethods();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Checks the methods of each machine and report warnings if
        /// any method is directly accessed by anything else than the
        /// P# runtime.
        /// </summary>
        private static void CheckMethods()
        {
            foreach (var classDecl in AnalysisContext.Machines)
            {
                SanityCheckingAnalysis.CheckForExternalAsynchronyUseInMachine(classDecl);

                foreach (var nestedClass in classDecl.ChildNodes().OfType<ClassDeclarationSyntax>())
                {
                    if (nestedClass.BaseList == null ||
                        !nestedClass.BaseList.Types.Any(t => t.ToString().Equals("MachineState")))
                    {
                        ErrorReporter.ReportAndExit("Class '{0}' is not a state of the machine '{1}' " +
                            "and, thus, is not allowed to be declared inside the machine body.",
                            nestedClass.Identifier.ValueText, classDecl.Identifier.ValueText);
                    }

                    foreach (var method in nestedClass.ChildNodes().OfType<MethodDeclarationSyntax>())
                    {
                        if (method.Modifiers.Any(SyntaxKind.AbstractKeyword))
                        {
                            continue;
                        }

                        foreach (var stmt in method.Body.Statements)
                        {
                            SanityCheckingAnalysis.CheckStatement(stmt, method, classDecl, nestedClass);
                        }
                    }
                }

                foreach (var method in classDecl.ChildNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (method.Modifiers.Any(SyntaxKind.AbstractKeyword))
                    {
                        continue;
                    }

                    foreach (var stmt in method.Body.Statements)
                    {
                        SanityCheckingAnalysis.CheckStatement(stmt, method, classDecl);
                    }
                }
            }
        }

        /// <summary>
        /// Checks the given machine for using non-P# related asynchrony.
        /// </summary>
        /// <param name="machine"></param>
        private static void CheckForExternalAsynchronyUseInMachine(ClassDeclarationSyntax machine)
        {
            if (machine.SyntaxTree.GetRoot().DescendantNodesAndSelf().Any(v
                    => v.ToString().Contains("System.Threading")))
            {
                Log log = new Log(null, machine, null, null);
                AnalysisErrorReporter.ReportExternalAsynchronyUsage(log);
            }
        }

        /// <summary>
        /// Check the given statement for any missbehaviour.
        /// </summary>
        /// <param name="stmt">Statement</param>
        /// <param name="method">Method</param>
        /// <param name="machine">machine</param>
        /// <param name="state">state</param>
        private static void CheckStatement(StatementSyntax stmt, MethodDeclarationSyntax method,
            ClassDeclarationSyntax machine, ClassDeclarationSyntax state = null)
        {
            if (stmt is ExpressionStatementSyntax)
            {
                var exprStmt = stmt as ExpressionStatementSyntax;
                if (exprStmt.Expression is InvocationExpressionSyntax)
                {
                    var invExprStmt = exprStmt.Expression as InvocationExpressionSyntax;
                    if (invExprStmt.Expression is MemberAccessExpressionSyntax)
                    {
                        var callStmt = invExprStmt.Expression as MemberAccessExpressionSyntax;
                        if (callStmt.Name.Identifier.ValueText.Equals("OnEntry") ||
                            callStmt.Name.Identifier.ValueText.Equals("OnExit") ||
                            callStmt.Name.Identifier.ValueText.Equals("DefineIgnoredEvents") ||
                            callStmt.Name.Identifier.ValueText.Equals("DefineDeferredEvents") ||
                            callStmt.Name.Identifier.ValueText.Equals("DefineGotoStateTransitions") ||
                            callStmt.Name.Identifier.ValueText.Equals("DefinePushStateTransitions") ||
                            callStmt.Name.Identifier.ValueText.Equals("DefineActionBindings"))
                        {
                            Log log = new Log(method, machine, state, null);
                            log.AddTrace(callStmt.ToString(), callStmt.SyntaxTree.FilePath, callStmt.SyntaxTree.
                                GetLineSpan(callStmt.Span).StartLinePosition.Line + 1);
                            AnalysisErrorReporter.ReportRuntimeOnlyMethodAccess(log);
                        }
                    }
                }
                else if (exprStmt.Expression is BinaryExpressionSyntax)
                {
                    var binExprStmt = exprStmt.Expression as BinaryExpressionSyntax;
                    if (binExprStmt.Right is ObjectCreationExpressionSyntax)
                    {
                        var newObjStmt = binExprStmt.Right as ObjectCreationExpressionSyntax;
                        if (SanityCheckingAnalysis.IsStateOfTheMachine(newObjStmt.Type.ToString(), machine))
                        {
                            Log log = new Log(method, machine, state, null);
                            log.AddTrace(newObjStmt.ToString(), newObjStmt.SyntaxTree.FilePath, newObjStmt.SyntaxTree.
                                GetLineSpan(newObjStmt.Span).StartLinePosition.Line + 1);
                            AnalysisErrorReporter.ReportExplicitStateInitialisation(log);
                        }
                    }
                }
            }
            else if (stmt is LocalDeclarationStatementSyntax)
            {
                var localDeclStmt = stmt as LocalDeclarationStatementSyntax;
                foreach (var variable in localDeclStmt.Declaration.Variables)
                {
                    if (variable.Initializer != null &&
                        variable.Initializer.Value is ObjectCreationExpressionSyntax)
                    {
                        var newObjStmt = variable.Initializer.Value as ObjectCreationExpressionSyntax;
                        if (SanityCheckingAnalysis.IsStateOfTheMachine(newObjStmt.Type.ToString(), machine))
                        {
                            Log log = new Log(method, machine, state, null);
                            log.AddTrace(newObjStmt.ToString(), newObjStmt.SyntaxTree.FilePath, newObjStmt.SyntaxTree.
                                GetLineSpan(newObjStmt.Span).StartLinePosition.Line + 1);
                            AnalysisErrorReporter.ReportExplicitStateInitialisation(log);
                        }
                    }
                }
            }
            else if (stmt is IfStatementSyntax)
            {
                var ifStmt = stmt as IfStatementSyntax;
                if (ifStmt.Statement is BlockSyntax)
                {
                    var ifBlockStmt = ifStmt.Statement as BlockSyntax;
                    foreach (var ibs in ifBlockStmt.Statements)
                    {
                        SanityCheckingAnalysis.CheckStatement(ibs, method, machine, state);
                    }

                    if (ifStmt.Else != null)
                    {
                        if (ifStmt.Else.Statement is IfStatementSyntax)
                        {
                            SanityCheckingAnalysis.CheckStatement(ifStmt.Else.Statement,
                                method, machine, state);
                        }
                        else if (ifStmt.Else.Statement is BlockSyntax)
                        {
                            var elseBlockStmt = ifStmt.Else.Statement as BlockSyntax;
                            foreach (var ebs in elseBlockStmt.Statements)
                            {
                                SanityCheckingAnalysis.CheckStatement(ebs, method, machine, state);
                            }
                        }
                    }
                }
                else
                {
                    SanityCheckingAnalysis.CheckStatement(ifStmt.Statement, method, machine, state);
                }
            }
            else if (stmt is ForStatementSyntax)
            {
                var forStmt = stmt as ForStatementSyntax;
                if (forStmt.Statement is BlockSyntax)
                {
                    var forBlockStmt = forStmt.Statement as BlockSyntax;
                    foreach (var fbs in forBlockStmt.Statements)
                    {
                        SanityCheckingAnalysis.CheckStatement(fbs, method, machine, state);
                    }
                }
                else
                {
                    SanityCheckingAnalysis.CheckStatement(forStmt.Statement, method, machine, state);
                }
            }
            else if (stmt is ForEachStatementSyntax)
            {
                var forEachStmt = stmt as ForEachStatementSyntax;
                if (forEachStmt.Statement is BlockSyntax)
                {
                    var forEachBlockStmt = forEachStmt.Statement as BlockSyntax;
                    foreach (var fbs in forEachBlockStmt.Statements)
                    {
                        SanityCheckingAnalysis.CheckStatement(fbs, method, machine, state);
                    }
                }
                else
                {
                    SanityCheckingAnalysis.CheckStatement(forEachStmt.Statement, method, machine, state);
                }
            }
            else if (stmt is WhileStatementSyntax)
            {
                var whileStmt = stmt as WhileStatementSyntax;
                if (whileStmt.Statement is BlockSyntax)
                {
                    var whileBlockStmt = whileStmt.Statement as BlockSyntax;
                    foreach (var wbs in whileBlockStmt.Statements)
                    {
                        SanityCheckingAnalysis.CheckStatement(wbs, method, machine, state);
                    }
                }
                else
                {
                    SanityCheckingAnalysis.CheckStatement(whileStmt.Statement, method, machine, state);
                }
            }
            else if (stmt is DoStatementSyntax)
            {
                var doStmt = stmt as DoStatementSyntax;
                if (doStmt.Statement is BlockSyntax)
                {
                    var doBlockStmt = doStmt.Statement as BlockSyntax;
                    foreach (var dbs in doBlockStmt.Statements)
                    {
                        SanityCheckingAnalysis.CheckStatement(dbs, method, machine, state);
                    }
                }
                else
                {
                    SanityCheckingAnalysis.CheckStatement(doStmt.Statement, method, machine, state);
                }
            }
            else if (stmt is SwitchStatementSyntax)
            {
                var switchStmt = stmt as SwitchStatementSyntax;
                foreach (var section in switchStmt.Sections)
                {
                    foreach (var sbs in section.Statements)
                    {
                        SanityCheckingAnalysis.CheckStatement(sbs, method, machine, state);
                    }
                }
            }
            else if (stmt is TryStatementSyntax)
            {
                var tryStmt = stmt as TryStatementSyntax;
                foreach (var tbs in tryStmt.Block.Statements)
                {
                    SanityCheckingAnalysis.CheckStatement(tbs, method, machine, state);
                }

                foreach (var ctch in tryStmt.Catches)
                {
                    foreach (var cbs in ctch.Block.Statements)
                    {
                        SanityCheckingAnalysis.CheckStatement(cbs, method, machine, state);
                    }
                }

                if (tryStmt.Finally != null)
                {
                    foreach (var tbs in tryStmt.Finally.Block.Statements)
                    {
                        SanityCheckingAnalysis.CheckStatement(tbs, method, machine, state);
                    }
                }
            }
            else if (stmt is UsingStatementSyntax)
            {
                var usingStmt = stmt as UsingStatementSyntax;
                var usingBlockStmt = usingStmt.Statement as BlockSyntax;
                foreach (var ubs in usingBlockStmt.Statements)
                {
                    SanityCheckingAnalysis.CheckStatement(ubs, method, machine, state);
                }
            }
        }

        #endregion
        
        #region helper methods

        /// <summary>
        /// Checks if the given name is a state of the given machine.
        /// </summary>
        /// <param name="name">string</param>
        /// <param name="machine">Machine</param>
        /// <returns>Boolean value</returns>
        private static bool IsStateOfTheMachine(string name, ClassDeclarationSyntax machine)
        {
            List<string> stateNames = new List<string>();

            foreach (var nestedClass in machine.ChildNodes().OfType<ClassDeclarationSyntax>())
            {
                stateNames.Add(nestedClass.Identifier.ValueText);
            }

            if (stateNames.Any(str => str.Equals(name)))
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
