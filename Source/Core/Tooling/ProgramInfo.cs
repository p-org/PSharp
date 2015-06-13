//-----------------------------------------------------------------------
// <copyright file="ProgramInfo.cs">
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
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.PSharp.Tooling
{
    /// <summary>
    /// The P# program info.
    /// </summary>
    public static class ProgramInfo
    {
        #region fields

        /// <summary>
        /// The workspace of the P# program.
        /// </summary>
        public static Workspace Workspace = null;

        /// <summary>
        /// The solution of the P# program.
        /// </summary>
        public static Solution Solution = null;

        /// <summary>
        /// Collection of the program units in the P# program.
        /// </summary>
        public static HashSet<ProgramUnit> ProgramUnits = null;

        /// <summary>
        /// True if program info has been initialized.
        /// </summary>
        private static bool HasInitialized = false;

        #endregion

        #region public API

        /// <summary>
        /// Initializes the P# program info.
        /// </summary>
        public static void Initialize()
        {
            // Create a new workspace.
            ProgramInfo.Workspace = MSBuildWorkspace.Create();

            try
            {
                // Populate the workspace with the user defined solution.
                ProgramInfo.Solution = (ProgramInfo.Workspace as MSBuildWorkspace).OpenSolutionAsync(
                    @"" + Configuration.SolutionFilePath + "").Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                ErrorReporter.ReportAndExit("Please give a valid solution path.");
            }

            ProgramInfo.ProgramUnits = new HashSet<ProgramUnit>();
            if (Configuration.ProjectName.Equals(""))
            {
                foreach (var project in ProgramInfo.Solution.Projects)
                {
                    ProgramInfo.ProgramUnits.Add(ProgramUnit.Create(project));
                }
            }
            else
            {
                // Find the project specified by the user.
                var project = ProgramInfo.Solution.Projects.Where(
                    p => p.Name.Equals(Configuration.ProjectName)).FirstOrDefault();

                if (project == null)
                {
                    ErrorReporter.ReportAndExit("Please give a valid project name.");
                }

                ProgramInfo.ProgramUnits.Add(ProgramUnit.Create(project));
            }

            ProgramInfo.HasInitialized = true;
        }

        /// <summary>
        /// Replaces an existing syntax tree with the given one.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="project">Project</param>
        public static void ReplaceSyntaxTree(SyntaxTree tree, ref Project project)
        {
            if (!ProgramInfo.HasInitialized)
            {
                throw new PSharpGenericException("ProgramInfo has not been initialized.");
            }

            var doc = project.Documents.First(val => val.FilePath.Equals(tree.FilePath));
            doc = doc.WithSyntaxRoot(tree.GetRoot());

            var textTask = doc.GetTextAsync();
            project = project.RemoveDocument(doc.Id);
            project = project.AddDocument(doc.Name, textTask.Result, doc.Folders).Project;

            ProgramInfo.Solution = project.Solution;
            ProgramInfo.Workspace = project.Solution.Workspace;

            if (Configuration.Debug == DebugType.Parsing ||
                Configuration.Debug == DebugType.All)
            {
                ProgramInfo.PrintSyntaxTree(tree);
            }
        }

        /// <summary>
        /// True if the syntax tree belongs to a P# program, else false.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>Boolean value</returns>
        public static bool IsPSharpFile(SyntaxTree tree)
        {
            var ext = Path.GetExtension(tree.FilePath);
            return ext.Equals(".psharp") ? true : false;
        }

        /// <summary>
        /// True if the syntax tree belongs to a P program, else false.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>Boolean value</returns>
        public static bool IsPFile(SyntaxTree tree)
        {
            var ext = Path.GetExtension(tree.FilePath);
            return ext.Equals(".p") ? true : false;
        }

        #endregion

        #region private API

        /// <summary>
        /// Print the syntax tree for debug.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        private static void PrintSyntaxTree(SyntaxTree tree)
        {
            var root = (CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
            var lines = System.Text.RegularExpressions.Regex.Split(root.ToFullString(), "\r\n|\r|\n");
            for (int idx = 0; idx < lines.Length; idx++)
            {
                Console.WriteLine(idx + 1 + " " + lines[idx]);
            }
        }

        #endregion
    }
}
