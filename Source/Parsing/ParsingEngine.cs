//-----------------------------------------------------------------------
// <copyright file="ParsingEngine.cs">
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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# parsing engine.
    /// </summary>
    public static class ParsingEngine
    {
        #region public API

        /// <summary>
        /// Runs the P# parsing engine.
        /// </summary>
        public static void Run()
        {
            foreach (var programUnit in ProgramInfo.ProgramUnits)
            {
                var project = programUnit.GetProject();

                // Performs rewriting.
                ParsingEngine.RewriteSyntaxTrees(project);
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrite P# syntax trees to C# syntax trees of the project.
        /// </summary>
        /// <param name="project">Project</param>
        private static void RewriteSyntaxTrees(Project project)
        {
            var compilation = project.GetCompilationAsync().Result;

            foreach (var tree in compilation.SyntaxTrees.ToList())
            {
                if (!ProgramInfo.IsPSharpFile(tree))
                {
                    continue;
                }

                var root = (CompilationUnitSyntax)tree.GetRoot();

                var tokens = new PSharpLexer().Tokenize(root.ToFullString());
                var program = new PSharpParser(tree.FilePath).ParseTokens(tokens);
                var rewrittenTree = program.Rewrite();

                var source = SourceText.From(rewrittenTree);

                ProgramInfo.ReplaceSyntaxTree(tree.WithChangedText(source), ref project);
            }
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
