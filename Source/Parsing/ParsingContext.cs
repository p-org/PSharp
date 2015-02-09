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
using System.Linq;

using Microsoft.PSharp.Tooling;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.PSharp.Parsing
{
    /// <summary>
    /// The P# parsing context.
    /// </summary>
    public static class ParsingContext
    {
        #region fields

        /// <summary>
        /// The solution of the P# program.
        /// </summary>
        internal static Solution Solution = null;

        /// <summary>
        /// The project's compilation.
        /// </summary>
        internal static Compilation Compilation = null;

        #endregion

        #region public API

        /// <summary>
        /// Creates a new parsing context.
        /// </summary>
        public static void Create()
        {
            // Create a new workspace.
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();

            try
            {
                // Populate the workspace with the user defined solution.
                ParsingContext.Solution = workspace.OpenSolutionAsync(
                    @"" + Configuration.SolutionFilePath + "").Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                ErrorReporter.Report("Please give a valid solution path.");
                Environment.Exit(1);
            }

            // Find the project specified by the user.
            Project project = ParsingContext.Solution.Projects.Where(
                p => p.Name.Equals(Configuration.ProjectName)).FirstOrDefault();

            if (project == null)
            {
                ErrorReporter.Report("Please give a valid project name.");
                Environment.Exit(1);
            }

            // Get the project's compilation.
            ParsingContext.Compilation = project.GetCompilationAsync().Result;

            ParsingContext.RewriteSyntaxTrees();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrite P# syntax trees to C# syntax trees.
        /// </summary>
        private static void RewriteSyntaxTrees()
        {
            // Iterate the syntax trees for each project file.
            foreach (var tree in ParsingContext.Compilation.SyntaxTrees)
            {
                if (!ParsingContext.IsProgramSyntaxTree(tree))
                {
                    continue;
                }

                // Get the tree's semantic model.
                var model = ParsingContext.Compilation.GetSemanticModel(tree);

                // Get the tree's root node compilation unit.
                var root = (CompilationUnitSyntax)tree.GetRoot();

                var newRoot = new MachineDeclarationRewriter(root).Run();
                
                //Console.WriteLine(newRoot.GetText());

                //foreach (var node in newRoot.ChildNodes())
                //{
                //    Console.WriteLine(" >> " + node.ToString());
                //}
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
