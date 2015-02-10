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
                ErrorReporter.ReportErrorAndExit("Please give a valid solution path.");
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
                    ErrorReporter.ReportErrorAndExit("Please give a valid project name.");
                }

                ProgramInfo.ProgramUnits.Add(ProgramUnit.Create(project));
            }

            ProgramInfo.HasInitialized = true;
        }

        /// <summary>
        /// Replaces the existing syntax trees with the given ones.
        /// </summary>
        public static void ReplaceSyntaxTrees(Project project, IEnumerable<SyntaxTree> syntaxTrees)
        {
            if (!ProgramInfo.HasInitialized)
            {
                throw new PSharpToolException("ProgramInfo has not been initialized.");
            }

            var updatedDocs = new HashSet<Document>();
            foreach (var doc in project.Documents)
            {
                var tree = syntaxTrees.FirstOrDefault(val => val.FilePath.Equals(doc.FilePath));
                if (tree == null)
                {
                    continue;
                }

                updatedDocs.Add(doc.WithSyntaxRoot(tree.GetRoot()));
            }

            foreach (var doc in updatedDocs)
            {
                var textTask = doc.GetTextAsync();
                project = project.RemoveDocument(doc.Id);
                project = project.AddDocument(doc.Name, textTask.Result, doc.Folders).Project;
            }

            ProgramInfo.Solution = project.Solution;
            ProgramInfo.Workspace = project.Solution.Workspace;
        }

        /// <summary>
        /// Recomputes and updates the program info.
        /// </summary>
        public static void Update()
        {
            if (!ProgramInfo.HasInitialized)
            {
                throw new PSharpToolException("ProgramInfo has not been initialized.");
            }

            ProgramInfo.ProgramUnits.Clear();
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
                ProgramInfo.ProgramUnits.Add(ProgramUnit.Create(project));
            }
        }

        #endregion
    }
}
