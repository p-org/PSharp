//-----------------------------------------------------------------------
// <copyright file="RuntimeOnlyDirectAccessAnalysis.cs">
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
using System.Linq;

using Microsoft.PSharp.Tooling;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.PSharp.StaticAnalysis
{
    /// <summary>
    /// This analysis reports a warning if a machine has any fields that are
    /// neither private or protected. We also report an error if a machine
    /// has any public methods. Any exposed outside of the class methods
    /// must only be accessed by the P# runtime.
    /// </summary>
    public static class RuntimeOnlyDirectAccessAnalysis
    {
        #region public API

        /// <summary>
        /// Runs the analysis.
        /// </summary>
        public static void Run()
        {
            RuntimeOnlyDirectAccessAnalysis.CheckFields();
            RuntimeOnlyDirectAccessAnalysis.CheckMethods();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Checks the fields of each machine and report warnings if
        /// any field is not private or protected.
        /// </summary>
        private static void CheckFields()
        {
            foreach (var classDecl in AnalysisContext.Machines)
            {
                foreach (var field in classDecl.ChildNodes().OfType<FieldDeclarationSyntax>())
                {
                    if (field.Modifiers.Any(SyntaxKind.PublicKeyword))
                    {
                        ErrorReporter.ReportErrorAndExit("Field '{0}' of machine '{1}' is declared as " +
                            "'public'.", field.Declaration.ToString(), classDecl.Identifier.ValueText);
                    }
                    else if (field.Modifiers.Any(SyntaxKind.InternalKeyword))
                    {
                        ErrorReporter.ReportErrorAndExit("Field '{0}' of machine '{1}' is declared as " +
                            "'internal'.", field.Declaration.ToString(), classDecl.Identifier.ValueText);
                    }
                }
            }
        }

        /// <summary>
        /// Checks the methods of each machine and report warnings if
        /// any method is directly accessed by anything else than the
        /// P# runtime.
        /// </summary>
        private static void CheckMethods()
        {
            foreach (var classDecl in AnalysisContext.Machines)
            {
                foreach (var method in classDecl.ChildNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (method.Modifiers.Any(SyntaxKind.PublicKeyword))
                    {
                        ErrorReporter.ReportErrorAndExit("Method '{0}' of machine '{1}' is " +
                            "declared as 'public'.", method.Identifier.ValueText,
                            classDecl.Identifier.ValueText);
                    }
                    else if (method.Modifiers.Any(SyntaxKind.InternalKeyword))
                    {
                        ErrorReporter.ReportErrorAndExit("Method '{0}' of machine '{1}' is " +
                            "declared as 'internal'.", method.Identifier.ValueText,
                            classDecl.Identifier.ValueText);
                    }
                }
            }
        }

        #endregion
    }
}
