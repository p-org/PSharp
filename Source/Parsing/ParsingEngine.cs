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
using Microsoft.CodeAnalysis.MSBuild;

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
            // Perform rewriting.
            ParsingEngine.RewriteSyntaxTrees();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrite P# syntax trees to C# syntax trees.
        /// </summary>
        private static void RewriteSyntaxTrees()
        {
            var rewritternTrees = new HashSet<SyntaxTree>();

            // Iterate the syntax trees for each project file.
            foreach (var tree in ProgramContext.Compilation.SyntaxTrees)
            {
                if (!ParsingEngine.IsProgramSyntaxTree(tree))
                {
                    continue;
                }

                // Get the tree's semantic model.
                var model = ProgramContext.Compilation.GetSemanticModel(tree);

                // Get the tree's root node compilation unit.
                var root = (CompilationUnitSyntax)tree.GetRoot();
                var rewrittenUnit = new MachineDeclarationRewriter(root).Run();

                rewritternTrees.Add(rewrittenUnit.SyntaxTree);
            }

            ProgramContext.ReplaceSyntaxTrees(rewritternTrees);

            foreach (var tree in ProgramContext.Compilation.SyntaxTrees)
            {
                if (!ParsingEngine.IsProgramSyntaxTree(tree))
                {
                    continue;
                }

                // Get the tree's semantic model.
                var model = ProgramContext.Compilation.GetSemanticModel(tree);

                // Get the tree's root node compilation unit.
                var root = (CompilationUnitSyntax)tree.GetRoot();
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
