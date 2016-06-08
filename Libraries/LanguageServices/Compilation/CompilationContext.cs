//-----------------------------------------------------------------------
// <copyright file="CompilationContext.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
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
using Microsoft.CodeAnalysis.Text;

using Microsoft.PSharp.Utilities;

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
        internal Configuration Configuration;

        /// <summary>
        /// The active compilation target.
        /// </summary>
        internal CompilationTarget ActiveCompilationTarget;

        /// <summary>
        /// The solution of the P# program per compilation target.
        /// </summary>
        private Dictionary<CompilationTarget, Solution> SolutionMap;

        /// <summary>
        /// List of P# projects per compilation target.
        /// </summary>
        private Dictionary<CompilationTarget, List<PSharpProject>> PSharpProjectMap;

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
        /// <returns>CompilationContext</returns>
        public static CompilationContext Create()
        {
            var configuration = Configuration.Create();
            return new CompilationContext(configuration);
        }

        /// <summary>
        /// Create a new P# compilation context.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <returns>CompilationContext</returns>
        public static CompilationContext Create(Configuration configuration)
        {
            return new CompilationContext(configuration);
        }

        /// <summary>
        /// Loads the user-specified solution.
        /// </summary>
        /// <returns>CompilationContext</returns>
        public CompilationContext LoadSolution()
        {
            // Create a new workspace.
            var workspace = MSBuildWorkspace.Create();
            Solution solution = null;

            try
            {
                // Populate the workspace with the user defined solution.
                solution = (workspace as MSBuildWorkspace).OpenSolutionAsync(
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

            this.InstallCompilationTargets(solution);

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
        /// Loads the specified solution.
        /// </summary>
        /// <returns>CompilationContext</returns>
        public CompilationContext LoadSolution(Solution solution)
        {
            // Create a new workspace.
            var workspace = MSBuildWorkspace.Create();

            this.InstallCompilationTargets(solution);
            this.HasInitialized = true;

            return this;
        }

        /// <summary>
        /// Loads a solution from the specified text.
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="extension">Extension</param>
        /// <returns>CompilationContext</returns>
        public CompilationContext LoadSolution(string text, string extension = "psharp")
        {
            // Create a new solution from the specified text.
            var solution = this.GetSolution(text, extension);

            this.InstallCompilationTargets(solution);
            this.HasInitialized = true;

            return this;
        }

        /// <summary>
        /// Loads a solution from the specified text.
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="references">MetadataReferences</param>
        /// <param name="extension">Extension</param>
        /// <returns>CompilationContext</returns>
        public CompilationContext LoadSolution(string text, ISet<MetadataReference> references,
            string extension = "psharp")
        {
            // Create a new solution from the specified text.
            var solution = this.GetSolution(text, references, extension);

            this.InstallCompilationTargets(solution);
            this.HasInitialized = true;

            return this;
        }

        /// <summary>
        /// Returns the P# solution associated with the active
        /// compilation target.
        /// </summary>
        /// <returns>Solution</returns>
        public Solution GetSolution()
        {
            return this.SolutionMap[this.ActiveCompilationTarget];
        }

        /// <summary>
        /// Returns a P# solution from the specified text.
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="extension">Extension</param>
        /// <returns>Solution</returns>
        public Solution GetSolution(string text, string extension = "psharp")
        {
            var references = new HashSet<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Machine).Assembly.Location)
            };

            Project project = this.CreateProject(references);

            var sourceText = SourceText.From(text);
            var doc = project.AddDocument("Program", sourceText, null, "Program." + extension);

            return doc.Project.Solution;
        }

        /// <summary>
        /// Returns a P# solution from the specified text.
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="references">MetadataReferences</param>
        /// <param name="extension">Extension</param>
        /// <returns>Solution</returns>
        public Solution GetSolution(string text, ISet<MetadataReference> references,
            string extension = "psharp")
        {
            references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Machine).Assembly.Location));

            Project project = this.CreateProject(references);

            var sourceText = SourceText.From(text);
            var doc = project.AddDocument("Program", sourceText, null, "Program." + extension);

            return doc.Project.Solution;
        }

        /// <summary>
        /// Returns the P# projects associated with the active
        /// compilation target.
        /// </summary>
        /// <returns>List of P# projects</returns>
        public List<PSharpProject> GetProjects()
        {
            return this.PSharpProjectMap[this.ActiveCompilationTarget];
        }

        /// <summary>
        /// Returns the project with the specified name.
        /// </summary>
        /// <param name="name">Project name</param>
        /// <returns>Project</returns>
        public Project GetProjectWithName(string name)
        {
            var project = this.SolutionMap[this.ActiveCompilationTarget].Projects.
                Where(p => p.Name.Equals(name)).FirstOrDefault();
            return project;
        }

        /// <summary>
        /// Replaces the syntax tree with the specified text in the project.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <param name="text">Text</param>
        /// <param name="project">Project</param>
        /// <returns>SyntaxTree</returns>
        public SyntaxTree ReplaceSyntaxTree(SyntaxTree tree, string text, Project project)
        {
            if (!this.HasInitialized)
            {
                throw new PSharpException("ProgramInfo has not been initialized.");
            }
            
            var doc = project.Documents.First(val => val.FilePath.Equals(tree.FilePath));
            var source = SourceText.From(text);

            tree = tree.WithChangedText(source);
            doc = doc.WithSyntaxRoot(tree.GetRoot());
            project = doc.Project;

            this.SolutionMap[this.ActiveCompilationTarget] = project.Solution;

            return doc.GetSyntaxTreeAsync().Result;
        }

        /// <summary>
        /// Prints the syntax tree.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        public void PrintSyntaxTree(SyntaxTree tree)
        {
            var root = (CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)tree.GetRoot();
            var lines = System.Text.RegularExpressions.Regex.Split(root.ToFullString(), "\r\n|\r|\n");
            for (int idx = 0; idx < lines.Length; idx++)
            {
                IO.PrintLine(idx + 1 + " " + lines[idx]);
            }
        }

        /// <summary>
        /// True if the syntax tree belongs to a P# program, else false.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>Boolean</returns>
        public bool IsPSharpFile(SyntaxTree tree)
        {
            var ext = Path.GetExtension(tree.FilePath);
            return ext.Equals(".psharp") ? true : false;
        }

        /// <summary>
        /// True if the syntax tree belongs to a C# program, else false.
        /// </summary>
        /// <param name="tree">SyntaxTree</param>
        /// <returns>Boolean</returns>
        public bool IsCSharpFile(SyntaxTree tree)
        {
            var ext = Path.GetExtension(tree.FilePath);
            return ext.Equals(".cs") ? true : false;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        private CompilationContext(Configuration configuration)
        {
            this.Configuration = configuration;
            this.ActiveCompilationTarget = configuration.CompilationTargets.First();
            this.SolutionMap = new Dictionary<CompilationTarget, Solution>();
            this.PSharpProjectMap = new Dictionary<CompilationTarget, List<PSharpProject>>();
        }

        /// <summary>
        /// Installs the requested compilation targets.
        /// </summary>
        /// <param name="solution">Solution</param>
        private void InstallCompilationTargets(Solution solution)
        {
            foreach (var target in this.Configuration.CompilationTargets)
            {
                this.SolutionMap.Add(target, solution);
                this.PSharpProjectMap.Add(target, new List<PSharpProject>());
            }
        }

        /// <summary>
        /// Creates a new P# project using the specified references.
        /// </summary>
        /// <param name="references">MetadataReferences</param>
        /// <returns>Project</returns>
        private Project CreateProject(ISet<MetadataReference> references)
        {
            var workspace = new AdhocWorkspace();
            var solutionInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create());
            var solution = workspace.AddSolution(solutionInfo);
            var project = workspace.AddProject("Test", "C#");

            project = project.AddMetadataReferences(references);
            workspace.TryApplyChanges(project.Solution);

            return project;
        }

        #endregion
    }
}
