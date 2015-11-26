//-----------------------------------------------------------------------
// <copyright file="Querying.cs">
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
    }
}
