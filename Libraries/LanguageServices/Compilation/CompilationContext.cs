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
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

using Microsoft.PSharp.IO;
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
        internal readonly Configuration Configuration;

        /// <summary>
        /// The solution of the P# program.
        /// </summary>
        private Solution Solution;

        /// <summary>
        /// List of P# projects in the solution.
        /// </summary>
        private List<PSharpProject> PSharpProjects;

        /// <summary>
        /// Set of custom compiler pass assemblies.
        /// </summary>
        internal readonly ISet<Assembly> CustomCompilerPassAssemblies;

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
                if (this.Configuration.SolutionFilePath != "")
                {
                    // Populate the workspace with the user defined solution.
                    solution = (workspace as MSBuildWorkspace).OpenSolutionAsync(
                        @"" + this.Configuration.SolutionFilePath + "").Result;
                }
                else
                {
                    // Populate the workspace with the user defined solution.
                    solution = (workspace as MSBuildWorkspace).OpenProjectAsync(
                        @"" + this.Configuration.ProjectFilePath + "").Result.Solution;
                }
            }
            catch (AggregateException ex)
            {
                Error.ReportAndExit(ex.InnerException.Message);
            }
            catch (Exception)
            {
                Error.ReportAndExit("Please give a valid solution path.");
            }

            this.Solution = solution;

            if (!this.Configuration.ProjectName.Equals(""))
            {
                // Find the project specified by the user.
                var project = this.GetProjectWithName(this.Configuration.ProjectName);
                if (project == null)
                {
                    Error.ReportAndExit("Please give a valid project name.");
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
            this.Solution = solution;
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
            this.Solution = solution;
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
            this.Solution = solution;
            this.HasInitialized = true;
            return this;
        }

        /// <summary>
        /// Returns the P# solution.
        /// </summary>
        /// <returns>Solution</returns>
        public Solution GetSolution()
        {
            return this.Solution;
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
        /// Returns the P# projects.
        /// </summary>
        /// <returns>List of P# projects</returns>
        public List<PSharpProject> GetProjects()
        {
            return this.PSharpProjects;
        }

        /// <summary>
        /// Returns the project with the specified name.
        /// </summary>
        /// <param name="name">Project name</param>
        /// <returns>Project</returns>
        public Project GetProjectWithName(string name)
        {
            var project = this.Solution.Projects.Where(
                p => p.Name.Equals(name)).FirstOrDefault();
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
                throw new Exception("ProgramInfo has not been initialized.");
            }
            
            var doc = project.Documents.First(val => val.FilePath.Equals(tree.FilePath));
            var source = SourceText.From(text);

            tree = tree.WithChangedText(source);
            doc = doc.WithSyntaxRoot(tree.GetRoot());
            project = doc.Project;

            this.Solution = project.Solution;

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
                Output.WriteLine(idx + 1 + " " + lines[idx]);
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
            this.PSharpProjects = new List<PSharpProject>();
            this.CustomCompilerPassAssemblies = new HashSet<Assembly>();
            this.LoadCustomCompilerPasses();
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

            CompilationOptions options = null;
            if (this.Configuration.OptimizationTarget == OptimizationTarget.Debug)
            {
                options = project.CompilationOptions.WithOptimizationLevel(OptimizationLevel.Debug);
            }
            else if (this.Configuration.OptimizationTarget == OptimizationTarget.Release)
            {
                options = project.CompilationOptions.WithOptimizationLevel(OptimizationLevel.Release);
            }

            project = project.WithCompilationOptions(options);
            project = project.AddMetadataReferences(references);

            workspace.TryApplyChanges(project.Solution);

            return project;
        }

        /// <summary>
        /// Loads the user-specified compiler passes.
        /// </summary>
        private void LoadCustomCompilerPasses()
        {
            foreach (var path in this.Configuration.CustomCompilerPassAssemblyPaths)
            {
                try
                {
                    this.CustomCompilerPassAssemblies.Add(Assembly.LoadFrom(path));
                }
                catch (FileNotFoundException)
                {
                    Error.ReportAndExit($"Could not find compiler pass '{path}'");
                }
            }
        }

        #endregion
    }
}
