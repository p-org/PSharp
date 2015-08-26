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
        /// Configuration.
        /// </summary>
        public static Configuration Configuration;

        /// <summary>
        /// The solution of the P# program.
        /// </summary>
        public static Solution Solution;

        /// <summary>
        /// True if program info has been initialized.
        /// </summary>
        private static bool HasInitialized = false;

        #endregion

        #region public API

        /// <summary>
        /// Initializes the P# program info.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public static void Initialize(Configuration configuration)
        {
            ProgramInfo.Configuration = configuration;

            // Create a new workspace.
            var workspace = MSBuildWorkspace.Create();

            try
            {
                // Populate the workspace with the user defined solution.
                ProgramInfo.Solution = (workspace as MSBuildWorkspace).OpenSolutionAsync(
                    @"" + ProgramInfo.Configuration.SolutionFilePath + "").Result;
            }
            catch (AggregateException ex)
            {
                ErrorReporter.ReportAndExit(ex.InnerException.Message);
            }
            catch (Exception)
            {
                ErrorReporter.ReportAndExit("Please give a valid solution path.");
            }

            if (!ProgramInfo.Configuration.ProjectName.Equals(""))
            {
                // Find the project specified by the user.
                var project = ProgramInfo.GetProjectWithName(ProgramInfo.Configuration.ProjectName);
                if (project == null)
                {
                    ErrorReporter.ReportAndExit("Please give a valid project name.");
                }
            }

            ProgramInfo.HasInitialized = true;
        }

        /// <summary>
        /// Returns the project with the given name.
        /// </summary>
        /// <param name="name">Project name</param>
        /// <returns>Project</returns>
        public static Project GetProjectWithName(string name)
        {
            var project = ProgramInfo.Solution.Projects.Where(p => p.Name.Equals(name)).FirstOrDefault();
            return project;
        }

        /// <summary>
        /// Replaces an existing syntax tree with the given one.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="project">Project</param>
        public static void ReplaceSyntaxTree(SyntaxTree tree, Project project)
        {
            if (!ProgramInfo.HasInitialized)
            {
                throw new PSharpGenericException("ProgramInfo has not been initialized.");
            }

            var doc = project.Documents.First(val => val.FilePath.Equals(tree.FilePath));
            doc = doc.WithSyntaxRoot(tree.GetRoot());
            project = doc.Project;

            ProgramInfo.Solution = project.Solution;

            if (Output.Debugging)
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
        /// True if the syntax tree belongs to a C# program, else false.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>Boolean value</returns>
        public static bool IsCSharpFile(SyntaxTree tree)
        {
            var ext = Path.GetExtension(tree.FilePath);
            return ext.Equals(".cs") ? true : false;
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

        #region private methods

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
                Output.PrintLine(idx + 1 + " " + lines[idx]);
            }
        }

        #endregion
    }
}
