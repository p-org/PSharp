//-----------------------------------------------------------------------
// <copyright file="CompilationEngine.cs">
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
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using Microsoft.PSharp.Tooling;

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
                }
            }

            // Links the P# core library.
            this.LinkAssemblyToAllProjects(typeof(Machine).Assembly, "Microsoft.PSharp.dll");

            // Links the P# runtime.
            if (this.CompilationContext.ActiveCompilationTarget == CompilationTarget.Testing)
            {
                this.LinkAssemblyToAllProjects(typeof(BugFindingDispatcher).Assembly,
                    "Microsoft.PSharp.BugFindingRuntime.dll");
            }
            else
            {
                this.LinkAssemblyToAllProjects(typeof(Dispatcher).Assembly,
                    "Microsoft.PSharp.Runtime.dll");
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        private CompilationEngine(CompilationContext context)
        {
            this.CompilationContext = context;
        }

        /// <summary>
        /// Compiles the given P# project.
        /// </summary>
        /// <param name="project">Project</param>
        private void CompileProject(Project project)
        {
            var runtimeDllPath = typeof(Dispatcher).Assembly.Location;
            var bugFindingRuntimeDllPath = typeof(BugFindingDispatcher).Assembly.Location;

            var runtimeDll = project.MetadataReferences.FirstOrDefault(val => val.Display.EndsWith(
                Path.DirectorySeparatorChar + "Microsoft.PSharp.Runtime.dll"));

            if (runtimeDll != null && this.CompilationContext.ActiveCompilationTarget == CompilationTarget.Testing)
            {
                project = project.RemoveMetadataReference(runtimeDll);
            }

            if (this.CompilationContext.ActiveCompilationTarget == CompilationTarget.Testing &&
                !project.MetadataReferences.Any(val => val.Display.EndsWith(
                Path.DirectorySeparatorChar + "Microsoft.PSharp.BugFindingRuntime.dll")))
            {
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(
                    bugFindingRuntimeDllPath));
            }

            var compilation = project.GetCompilationAsync().Result;

            try
            {
                if (this.CompilationContext.ActiveCompilationTarget == CompilationTarget.Testing ||
                    this.CompilationContext.ActiveCompilationTarget == CompilationTarget.Distribution)
                {
                    this.ToFile(compilation, OutputKind.DynamicallyLinkedLibrary,
                        project.OutputFilePath);
                }
                else
                {
                    this.ToFile(compilation, project.CompilationOptions.OutputKind,
                        project.OutputFilePath);
                }
            }
            catch (ApplicationException ex)
            {
                ErrorReporter.ReportAndExit(ex.Message);
            }
        }

        #endregion

        #region compilation methods

        /// <summary>
        /// Compiles the given compilation to a file.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="outputKind">OutputKind</param>
        /// <param name="outputPath">OutputPath</param>
        /// <returns>Output</returns>
        private string ToFile(CodeAnalysis.Compilation compilation, OutputKind outputKind, string outputPath)
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

            this.OutputDirectoryMap.Add(compilation.AssemblyName, outputDirectory);

            string fileName = outputDirectory + Path.DirectorySeparatorChar + assemblyFileName;
            string pdbFileName = outputDirectory + Path.DirectorySeparatorChar + compilation.AssemblyName + ".pdb";
            this.ProjectAssemblyPathMap.Add(compilation.AssemblyName, fileName);

            // Link external references.
            this.LinkExternalAssembliesToProject(compilation);

            EmitResult emitResult = null;
            using (FileStream outputFile = new FileStream(fileName, FileMode.Create, FileAccess.Write),
                outputPdbFile = new FileStream(pdbFileName, FileMode.Create, FileAccess.Write))
            {
                emitResult = compilation.Emit(outputFile, outputPdbFile);
                if (emitResult.Success)
                {
                    Output.PrintLine("... Writing " + fileName);
                    return fileName;
                }
            }

            Output.PrintLine("---");
            Output.PrintLine("Note: the errors below correspond to the intermediate C#-IR, " +
                "which can be printed using /debug.");
            Output.PrintLine("---");

            var message = string.Join("\r\n", emitResult.Diagnostics);
            throw new ApplicationException(message);
        }

        /// <summary>
        /// Compiles the given compilation and returns the assembly.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="outputKind">OutputKind</param>
        /// <returns>Assembly</returns>
        private Assembly ToAssembly(CodeAnalysis.Compilation compilation, OutputKind outputKind)
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

            EmitResult emitResult = null;
            using (var ms = new MemoryStream())
            {
                emitResult = compilation.Emit(ms);
                if (emitResult.Success)
                {
                    var assembly = Assembly.Load(ms.GetBuffer());
                    return assembly;
                }
            }

            Output.PrintLine("---");
            Output.PrintLine("Note: the errors below correspond to the intermediate C#-IR, " +
                "which can be printed using /debug.");
            Output.PrintLine("---");

            var message = string.Join("\r\n", emitResult.Diagnostics);
            throw new ApplicationException(message);
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
        /// <param name="compilation">Compilation</param>
        private void LinkExternalAssembliesToProject(CodeAnalysis.Compilation compilation)
        {
            //foreach (var reference in compilation.ExternalReferences)
            //{
            //    if (!(reference is PortableExecutableReference))
            //    {
            //        continue;
            //    }
            //}
        }

        /// <summary>
        /// Links the given P# assembly.
        /// </summary>
        /// <param name="assembly">Assembly</param>
        /// <param name="dll">Name of dll</param>
        private void LinkAssemblyToAllProjects(Assembly assembly, string dll)
        {
            Output.PrintLine("... Linking {0}", dll);

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
            File.Delete(dest);
            File.Copy(src, dest);

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

        #endregion
    }
}
