//-----------------------------------------------------------------------
// <copyright file="CompilationEngine.cs">
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
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices.Compilation
{
    /// <summary>
    /// A P# compilation engine.
    /// </summary>
    public sealed class CompilationEngine
    {
        #region fields

        /// <summary>
        /// The compilation context.
        /// </summary>
        private CompilationContext CompilationContext;

        /// <summary>
        /// Map from project assembly names to assembly paths.
        /// </summary>
        private Dictionary<string, string> ProjectAssemblyPathMap;

        /// <summary>
        /// Map from project names to output directories.
        /// </summary>
        private Dictionary<string, string> OutputDirectoryMap;

        #endregion

        #region public API

        /// <summary>
        /// Creates a P# compilation engine.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <returns></returns>
        public static CompilationEngine Create(CompilationContext context)
        {
            return new CompilationEngine(context);
        }

        /// <summary>
        /// Runs the P# compilation engine.
        /// </summary>
        public void Run()
        {
            this.ProjectAssemblyPathMap = new Dictionary<string, string>();
            this.OutputDirectoryMap = new Dictionary<string, string>();

            var graph = this.CompilationContext.GetSolution().GetProjectDependencyGraph();

            if (this.CompilationContext.Configuration.ProjectName.Equals(""))
            {
                foreach (var projectId in graph.GetTopologicallySortedProjects())
                {
                    var project = this.CompilationContext.GetSolution().GetProject(projectId);
                    this.CompileProject(project);
                    this.LinkSolutionAssembliesToProject(project, graph);
                    this.LinkExternalAssembliesToProject(project, graph);
                }
            }
            else
            {
                // Find the project specified by the user.
                var targetProject = this.CompilationContext.GetProjectWithName(
                    this.CompilationContext.Configuration.ProjectName);
                var projectDependencies = graph.GetProjectsThatThisProjectTransitivelyDependsOn(targetProject.Id);

                foreach (var projectId in graph.GetTopologicallySortedProjects())
                {
                    if (!projectDependencies.Contains(projectId) && !projectId.Equals(targetProject.Id))
                    {
                        continue;
                    }

                    var project = this.CompilationContext.GetSolution().GetProject(projectId);
                    this.CompileProject(project);
                    this.LinkSolutionAssembliesToProject(project, graph);
                    this.LinkExternalAssembliesToProject(project, graph);
                }
            }

            // Links the P# core library.
            this.LinkAssemblyToAllProjects(typeof(Machine).Assembly, "Microsoft.PSharp.dll");
        }

        #endregion

        #region constructor methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        private CompilationEngine(CompilationContext context)
        {
            this.CompilationContext = context;
        }

        #endregion

        #region compilation methods

        /// <summary>
        /// Compiles the given compilation to a file.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="outputKind">OutputKind</param>
        /// <param name="outputPath">OutputPath</param>
        /// <param name="printResults">Prints the compilation results</param>
        /// <param name="buildDebugFile">Builds the debug file</param>
        /// <returns>Output</returns>
        public string ToFile(CodeAnalysis.Compilation compilation, OutputKind outputKind,
            string outputPath, bool printResults, bool buildDebugFile)
        {
            string assemblyFileName = null;
            if (outputKind == OutputKind.ConsoleApplication)
            {
                assemblyFileName = compilation.AssemblyName + ".exe";
            }
            else if (outputKind == OutputKind.DynamicallyLinkedLibrary)
            {
                assemblyFileName = compilation.AssemblyName + ".dll";
            }

            string outputDirectory;
            if (!this.CompilationContext.Configuration.OutputFilePath.Equals(""))
            {
                outputDirectory = this.CompilationContext.Configuration.OutputFilePath;
            }
            else
            {
                outputDirectory = Path.GetDirectoryName(outputPath);
            }

            string fileName = outputDirectory + Path.DirectorySeparatorChar + assemblyFileName;
            string pdbFileName = outputDirectory + Path.DirectorySeparatorChar + compilation.AssemblyName + ".pdb";

            this.OutputDirectoryMap?.Add(compilation.AssemblyName, outputDirectory);
            this.ProjectAssemblyPathMap?.Add(compilation.AssemblyName, fileName);
            
            EmitResult emitResult = null;
            using (FileStream outputFile = new FileStream(fileName, FileMode.Create, FileAccess.Write),
                outputPdbFile = new FileStream(pdbFileName, FileMode.Create, FileAccess.Write))
            {
                if (buildDebugFile)
                {
                    emitResult = compilation.Emit(outputFile, outputPdbFile);
                }
                else
                {
                    emitResult = compilation.Emit(outputFile, null);
                }
            }

            if (emitResult.Success)
            {
                if (printResults)
                {
                    Output.WriteLine("... Writing {0}", fileName);
                }

                return fileName;
            }

            Output.WriteLine("---");
            Output.WriteLine("Note: the errors below correspond to the intermediate C#-IR, " +
                "which can be printed using /debug.");
            Output.WriteLine("---");

            var message = string.Join("\r\n", emitResult.Diagnostics);
            throw new ApplicationException(message);
        }

        /// <summary>
        /// Compiles the given compilation and returns the assembly.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="outputKind">OutputKind</param>
        /// <returns>Assembly</returns>
        public Assembly ToAssembly(CodeAnalysis.Compilation compilation, OutputKind outputKind)
        {
            string assemblyFileName = null;
            if (outputKind == OutputKind.ConsoleApplication)
            {
                assemblyFileName = compilation.AssemblyName + ".exe";
            }
            else if (outputKind == OutputKind.DynamicallyLinkedLibrary)
            {
                assemblyFileName = compilation.AssemblyName + ".dll";
            }

            Assembly assembly = null;
            EmitResult emitResult = null;
            using (var ms = new MemoryStream())
            {
                emitResult = compilation.Emit(ms);
                assembly = Assembly.Load(ms.GetBuffer());
            }

            if (emitResult.Success)
            {
                return assembly;
            }

            Output.WriteLine("---");
            Output.WriteLine("Note: the errors below correspond to the intermediate C#-IR, " +
                "which can be printed using /debug.");
            Output.WriteLine("---");

            var message = string.Join("\r\n", emitResult.Diagnostics);
            throw new ApplicationException(message);
        }

        /// <summary>
        /// Compiles the given P# project.
        /// </summary>
        /// <param name="project">Project</param>
        private void CompileProject(Project project)
        {
            CompilationOptions options = null;
            if (this.CompilationContext.Configuration.OptimizationTarget == OptimizationTarget.Debug)
            {
                options = project.CompilationOptions.WithOptimizationLevel(OptimizationLevel.Debug);
            }
            else if (this.CompilationContext.Configuration.OptimizationTarget == OptimizationTarget.Release)
            {
                options = project.CompilationOptions.WithOptimizationLevel(OptimizationLevel.Release);
            }

            project = project.WithCompilationOptions(options);

            var compilation = project.GetCompilationAsync().Result;
            
            try
            {
                if (this.CompilationContext.Configuration.CompilationTarget == CompilationTarget.Library ||
                    this.CompilationContext.Configuration.CompilationTarget == CompilationTarget.Testing ||
                    this.CompilationContext.Configuration.CompilationTarget == CompilationTarget.Remote)
                {
                    this.ToFile(compilation, OutputKind.DynamicallyLinkedLibrary,
                        project.OutputFilePath, true, true);
                }
                else
                {
                    this.ToFile(compilation, project.CompilationOptions.OutputKind,
                        project.OutputFilePath, true, true);
                }
            }
            catch (ApplicationException ex)
            {
                Error.ReportAndExit(ex.Message);
            }
        }

        #endregion

        #region linking methods

        /// <summary>
        /// Links the solution project assemblies to the given P# project.
        /// </summary>
        /// <param name="project">Project</param>
        /// <param name="graph">ProjectDependencyGraph</param>
        private void LinkSolutionAssembliesToProject(Project project, ProjectDependencyGraph graph)
        {
            var projectPath = this.OutputDirectoryMap[project.AssemblyName];

            foreach (var projectId in graph.GetProjectsThatThisProjectTransitivelyDependsOn(project.Id))
            {
                var requiredProject = this.CompilationContext.GetSolution().GetProject(projectId);
                var assemblyPath = this.ProjectAssemblyPathMap[requiredProject.AssemblyName];
                var fileName = projectPath + Path.DirectorySeparatorChar + requiredProject.AssemblyName + ".dll";

                this.CopyAssembly(assemblyPath, fileName);
            }
        }

        /// <summary>
        /// Links the external references to the given P# compilation.
        /// </summary>
        /// <param name="project">Project</param>
        /// <param name="graph">ProjectDependencyGraph</param>
        private void LinkExternalAssembliesToProject(Project project, ProjectDependencyGraph graph)
        {
            var projectPath = this.OutputDirectoryMap[project.AssemblyName];

            foreach (var projectId in graph.GetProjectsThatThisProjectTransitivelyDependsOn(project.Id))
            {
                var requiredProject = this.CompilationContext.GetSolution().GetProject(projectId);
                foreach (var reference in requiredProject.MetadataReferences)
                {
                    //if (!(reference is PortableExecutableReference))
                    //{
                    //    continue;
                    //}

                    var fileName = Path.Combine(projectPath, Path.GetFileName(reference.Display));
                    this.CopyAssembly(reference.Display, fileName);
                }
            }
        }

        /// <summary>
        /// Links the given P# assembly.
        /// </summary>
        /// <param name="assembly">Assembly</param>
        /// <param name="dll">Name of dll</param>
        private void LinkAssemblyToAllProjects(Assembly assembly, string dll)
        {
            Output.WriteLine("... Linking {0}", dll);

            foreach (var outputDir in this.OutputDirectoryMap.Values)
            {
                var localFileName = (new System.Uri(assembly.CodeBase)).LocalPath;
                var fileName = outputDir + Path.DirectorySeparatorChar + dll;

                this.CopyAssembly(localFileName, fileName);
            }
        }

        /// <summary>
        /// Copies the assembly from the source to the destination.
        /// </summary>
        /// <param name="src">Source</param>
        /// <param name="dest">Destination</param>
        private void CopyAssembly(string src, string dest)
        {
            try
            {
                if (src.Equals(dest))
                {
                    return;
                }

                if (File.Exists(src))
                {
                    File.Delete(dest);
                    File.Copy(src, dest);
                }

                if (src.EndsWith(".dll") && dest.EndsWith(".dll"))
                {
                    string srcPdb = src.Substring(0, src.Length - 4) + ".pdb",
                        destPdb = dest.Substring(0, dest.Length - 4) + ".pdb";

                    if (File.Exists(srcPdb))
                    {
                        File.Delete(destPdb);
                        File.Copy(srcPdb, destPdb);
                    }
                }
            }
            catch (NotSupportedException)
            {
                Debug.WriteLine("... Unable to copy {0}", src);
            }
        }

        #endregion
    }
}
