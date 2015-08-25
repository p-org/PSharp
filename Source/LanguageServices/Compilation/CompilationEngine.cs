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
    /// The P# compilation engine.
    /// </summary>
    public static class CompilationEngine
    {
        #region fields

        /// <summary>
        /// Map from project assembly names to assembly paths.
        /// </summary>
        private static Dictionary<string, string> ProjectAssemblyPathMap;

        /// <summary>
        /// Map from project names to output directories.
        /// </summary>
        private static Dictionary<string, string> OutputDirectoryMap;

        #endregion

        #region public API

        /// <summary>
        /// Runs the P# compilation engine.
        /// </summary>
        public static void Run()
        {
            CompilationEngine.ProjectAssemblyPathMap = new Dictionary<string, string>();
            CompilationEngine.OutputDirectoryMap = new Dictionary<string, string>();

            var graph = ProgramInfo.Solution.GetProjectDependencyGraph();

            if (Configuration.ProjectName.Equals(""))
            {
                foreach (var projectId in graph.GetTopologicallySortedProjects())
                {
                    var project = ProgramInfo.Solution.GetProject(projectId);
                    CompilationEngine.CompileProject(project);
                    CompilationEngine.LinkSolutionAssembliesToProject(project, graph);
                }
            }
            else
            {
                // Find the project specified by the user.
                var targetProject = ProgramInfo.GetProjectWithName(Configuration.ProjectName);
                var projectDependencies = graph.GetProjectsThatThisProjectTransitivelyDependsOn(targetProject.Id);

                foreach (var projectId in graph.GetTopologicallySortedProjects())
                {
                    if (!projectDependencies.Contains(projectId) && !projectId.Equals(targetProject.Id))
                    {
                        continue;
                    }

                    var project = ProgramInfo.Solution.GetProject(projectId);
                    CompilationEngine.CompileProject(project);
                    CompilationEngine.LinkSolutionAssembliesToProject(project, graph);
                }
            }

            // Links the P# core library.
            CompilationEngine.LinkAssemblyToAllProjects(typeof(Machine).Assembly, "Microsoft.PSharp.dll");

            // Links the P# runtime.
            if (Configuration.RunStaticAnalysis || Configuration.RunDynamicAnalysis)
            {
                CompilationEngine.LinkAssemblyToAllProjects(typeof(BugFindingDispatcher).Assembly,
                    "Microsoft.PSharp.BugFindingRuntime.dll");
            }
            else
            {
                CompilationEngine.LinkAssemblyToAllProjects(typeof(Dispatcher).Assembly,
                    "Microsoft.PSharp.Runtime.dll");
            }
        }

        #endregion

        #region private API

        /// <summary>
        /// Compiles the given P# project.
        /// </summary>
        /// <param name="project">Project</param>
        private static void CompileProject(Project project)
        {
            var runtimeDllPath = typeof(Dispatcher).Assembly.Location;
            var bugFindingRuntimeDllPath = typeof(BugFindingDispatcher).Assembly.Location;

            var runtimeDll = project.MetadataReferences.FirstOrDefault(val => val.Display.EndsWith(
                Path.DirectorySeparatorChar + "Microsoft.PSharp.Runtime.dll"));

            if (runtimeDll != null && (Configuration.RunStaticAnalysis ||
                Configuration.RunDynamicAnalysis))
            {
                project = project.RemoveMetadataReference(runtimeDll);
            }

            if ((Configuration.RunStaticAnalysis || Configuration.RunDynamicAnalysis) &&
                !project.MetadataReferences.Any(val => val.Display.EndsWith(
                Path.DirectorySeparatorChar + "Microsoft.PSharp.BugFindingRuntime.dll")))
            {
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(
                    bugFindingRuntimeDllPath));
            }

            var compilation = project.GetCompilationAsync().Result;

            try
            {
                if (Configuration.RunDynamicAnalysis)
                {
                    var dll = CompilationEngine.ToFile(compilation, OutputKind.DynamicallyLinkedLibrary,
                        project.OutputFilePath);

                    if (Configuration.ProjectName.Equals(project.Name))
                    {
                        Configuration.AssembliesToBeAnalyzed.Add(dll);
                    }
                }
                else if (Configuration.CompileForDistribution)
                {
                    CompilationEngine.ToFile(compilation, OutputKind.DynamicallyLinkedLibrary,
                        project.OutputFilePath);
                }
                else
                {
                    CompilationEngine.ToFile(compilation, project.CompilationOptions.OutputKind,
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
        private static string ToFile(CodeAnalysis.Compilation compilation, OutputKind outputKind, string outputPath)
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
            if (!Configuration.OutputFilePath.Equals(""))
            {
                outputDirectory = Configuration.OutputFilePath;
            }
            else
            {
                outputDirectory = Path.GetDirectoryName(outputPath);
            }
            CompilationEngine.OutputDirectoryMap.Add(compilation.AssemblyName, outputDirectory);

            string fileName = outputDirectory + Path.DirectorySeparatorChar + assemblyFileName,
                pdbFileName = outputDirectory + Path.DirectorySeparatorChar + compilation.AssemblyName + ".pdb";
            CompilationEngine.ProjectAssemblyPathMap.Add(compilation.AssemblyName, fileName);

            // Link external references.
            CompilationEngine.LinkExternalAssembliesToProject(compilation);

            EmitResult emitResult = null;
            using (var outputFile = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            using (var outputPdbFile = new FileStream(pdbFileName, FileMode.Create, FileAccess.Write))
            {
                emitResult = compilation.Emit(outputFile, outputPdbFile);
                if (emitResult.Success)
                {
                    Output.Print("... Writing " + fileName);
                    return fileName;
                }
            }

            Output.Print("---");
            Output.Print("Note: the errors below correspond to the intermediate C#-IR, " +
                "which can be printed using /debug.");
            Output.Print("---");

            var message = string.Join("\r\n", emitResult.Diagnostics);
            throw new ApplicationException(message);
        }

        /// <summary>
        /// Compiles the given compilation and returns the assembly.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="outputKind">OutputKind</param>
        /// <returns>Assembly</returns>
        private static Assembly ToAssembly(CodeAnalysis.Compilation compilation, OutputKind outputKind)
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

            Output.Print("---");
            Output.Print("Note: the errors below correspond to the intermediate C#-IR, " +
                "which can be printed using /debug.");
            Output.Print("---");

            var message = string.Join("\r\n", emitResult.Diagnostics);
            throw new ApplicationException(message);
        }

        #endregion

        #region linking methods

        private static void LinkAssembly(string src, string dest)
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

        /// <summary>
        /// Links the solution project assemblies to the given P# project.
        /// </summary>
        /// <param name="project">Project</param>
        /// <param name="graph">ProjectDependencyGraph</param>
        private static void LinkSolutionAssembliesToProject(Project project, ProjectDependencyGraph graph)
        {
            var projectPath = CompilationEngine.OutputDirectoryMap[project.AssemblyName];

            foreach (var projectId in graph.GetProjectsThatThisProjectTransitivelyDependsOn(project.Id))
            {
                var requiredProject = ProgramInfo.Solution.GetProject(projectId);
                var assemblyPath = CompilationEngine.ProjectAssemblyPathMap[requiredProject.AssemblyName];
                var fileName = projectPath + Path.DirectorySeparatorChar + requiredProject.AssemblyName + ".dll";

                LinkAssembly(assemblyPath, fileName);
            }
        }

        /// <summary>
        /// Links the external references to the given P# compilation.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        private static void LinkExternalAssembliesToProject(CodeAnalysis.Compilation compilation)
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
        private static void LinkAssemblyToAllProjects(Assembly assembly, string dll)
        {
            Output.Print("... Linking {0}", dll);

            foreach (var outputDir in CompilationEngine.OutputDirectoryMap.Values)
            {
                var localFileName = (new System.Uri(assembly.CodeBase)).LocalPath;
                var fileName = outputDir + Path.DirectorySeparatorChar + dll;

                LinkAssembly(localFileName, fileName);
            }
        }

        #endregion
    }
}
