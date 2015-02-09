//-----------------------------------------------------------------------
// <copyright file="ProgramContext.cs">
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.PSharp.Tooling
{
    /// <summary>
    /// The P# program context.
    /// </summary>
    public static class ProgramContext
    {
        #region fields

        /// <summary>
        /// The solution of the P# program.
        /// </summary>
        public static Solution Solution = null;

        /// <summary>
        /// The project's compilation.
        /// </summary>
        public static Compilation Compilation = null;

        #endregion

        #region public API

        /// <summary>
        /// Creates a new P# program context.
        /// </summary>
        public static void Create()
        {
            // Create a new workspace.
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();

            try
            {
                // Populate the workspace with the user defined solution.
                ProgramContext.Solution = workspace.OpenSolutionAsync(
                    @"" + Configuration.SolutionFilePath + "").Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                ErrorReporter.Report("Please give a valid solution path.");
                Environment.Exit(1);
            }

            // Find the project specified by the user.
            Project project = ProgramContext.Solution.Projects.Where(
                p => p.Name.Equals(Configuration.ProjectName)).FirstOrDefault();

            if (project == null)
            {
                ErrorReporter.Report("Please give a valid project name.");
                Environment.Exit(1);
            }

            // Get the project's compilation.
            ProgramContext.Compilation = project.GetCompilationAsync().Result;
        }

        /// <summary>
        /// Replaces the existing syntax trees with the given ones.
        /// </summary>
        public static void ReplaceSyntaxTrees(IEnumerable<SyntaxTree> syntaxTrees)
        {
            //ProgramContext.Compilation = ProgramContext.Compilation.RemoveAllSyntaxTrees();
            //ProgramContext.Compilation = ProgramContext.Compilation.AddSyntaxTrees(syntaxTrees);
        }

        #endregion
    }
}
