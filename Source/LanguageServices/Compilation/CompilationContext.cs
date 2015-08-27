//-----------------------------------------------------------------------
// <copyright file="CompilationContext.cs">
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
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.LanguageServices.Compilation
{
    /// <summary>
    /// A P# compilation context.
    /// </summary>
    public sealed class CompilationContext
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        internal LanguageServicesConfiguration Configuration;

        /// <summary>
        /// The solution of the P# program.
        /// </summary>
        internal Solution Solution;

        /// <summary>
        /// True if program info has been initialized.
        /// </summary>
        private bool HasInitialized = false;

        #endregion

        #region public API

        /// <summary>
        /// Create a new P# compilation context using the default
        /// configuration.
        /// </summary>
        /// <returns>AnalysisContext</returns>
        public static CompilationContext Create()
        {
            var configuration = new LanguageServicesConfiguration();
            return new CompilationContext(configuration);
        }

        /// <summary>
        /// Create a new P# compilation context.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>AnalysisContext</returns>
        public static CompilationContext Create(LanguageServicesConfiguration configuration)
        {
            return new CompilationContext(configuration);
        }

        /// <summary>
        /// Loads the solution.
        /// </summary>
        public CompilationContext LoadSolution()
        {
            // Create a new workspace.
            var workspace = MSBuildWorkspace.Create();

            try
            {
                // Populate the workspace with the user defined solution.
                this.Solution = (workspace as MSBuildWorkspace).OpenSolutionAsync(
                    @"" + this.Configuration.SolutionFilePath + "").Result;
            }
            catch (AggregateException ex)
            {
                ErrorReporter.ReportAndExit(ex.InnerException.Message);
            }
            catch (Exception)
            {
                ErrorReporter.ReportAndExit("Please give a valid solution path.");
            }

            if (!this.Configuration.ProjectName.Equals(""))
            {
                // Find the project specified by the user.
                var project = this.GetProjectWithName(this.Configuration.ProjectName);
                if (project == null)
                {
                    ErrorReporter.ReportAndExit("Please give a valid project name.");
                }
            }

            this.HasInitialized = true;

            return this;
        }

        /// <summary>
        /// Returns the project with the given name.
        /// </summary>
        /// <param name="name">Project name</param>
        /// <returns>Project</returns>
        public Project GetProjectWithName(string name)
        {
            var project = this.Solution.Projects.Where(p => p.Name.Equals(name)).FirstOrDefault();
            return project;
        }

        /// <summary>
        /// Replaces an existing syntax tree with the given one.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="project">Project</param>
        public void ReplaceSyntaxTree(SyntaxTree tree, Project project)
        {
            if (!this.HasInitialized)
            {
                throw new PSharpGenericException("ProgramInfo has not been initialized.");
            }

            var doc = project.Documents.First(val => val.FilePath.Equals(tree.FilePath));
            doc = doc.WithSyntaxRoot(tree.GetRoot());
            project = doc.Project;

            this.Solution = project.Solution;

            if (Output.Debugging)
            {
                this.PrintSyntaxTree(tree);
            }
        }

        /// <summary>
        /// True if the syntax tree belongs to a P# program, else false.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>Boolean value</returns>
        public bool IsPSharpFile(SyntaxTree tree)
        {
            var ext = Path.GetExtension(tree.FilePath);
            return ext.Equals(".psharp") ? true : false;
        }

        /// <summary>
        /// True if the syntax tree belongs to a C# program, else false.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>Boolean value</returns>
        public bool IsCSharpFile(SyntaxTree tree)
        {
            var ext = Path.GetExtension(tree.FilePath);
            return ext.Equals(".cs") ? true : false;
        }

        /// <summary>
        /// True if the syntax tree belongs to a P program, else false.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>Boolean value</returns>
        public bool IsPFile(SyntaxTree tree)
        {
            var ext = Path.GetExtension(tree.FilePath);
            return ext.Equals(".p") ? true : false;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private CompilationContext(LanguageServicesConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        /// <summary>
        /// Print the syntax tree for debug.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        private void PrintSyntaxTree(SyntaxTree tree)
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
