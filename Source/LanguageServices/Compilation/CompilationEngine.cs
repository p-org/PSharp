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
                    CompilationEngine.LinkSolutionToProject(project, graph);
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
                    CompilationEngine.LinkSolutionToProject(project, graph);
                }
            }

            // Links the P# runtime.
            CompilationEngine.LinkAssembly(typeof(Machine).Assembly, "Microsoft.PSharp.dll");

            if (Configuration.RunStaticAnalysis || Configuration.RunDynamicAnalysis)
            {
                CompilationEngine.LinkAssembly(typeof(BugFindingDispatcher).Assembly,
                    "Microsoft.PSharp.BugFindingRuntime.dll");
            }
            else
            {
                CompilationEngine.LinkAssembly(typeof(Dispatcher).Assembly,
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

        /// <summary>
        /// Links the solution projects to the given P# project.
        /// </summary>
        /// <param name="project">Project</param>
        /// <param name="graph">ProjectDependencyGraph</param>
        private static void LinkSolutionToProject(Project project, ProjectDependencyGraph graph)
        {
            var projectPath = CompilationEngine.OutputDirectoryMap[project.AssemblyName];

            foreach (var projectId in graph.GetProjectsThatThisProjectDirectlyDependsOn(project.Id))
            {
                var requiredProject = ProgramInfo.Solution.GetProject(projectId);
                var assemblyPath = CompilationEngine.ProjectAssemblyPathMap[requiredProject.AssemblyName];
                var fileName = projectPath + Path.DirectorySeparatorChar + requiredProject.AssemblyName + ".dll";

                File.Delete(fileName);
                File.Copy(assemblyPath, fileName);
            }
        }

        /// <summary>
        /// Links the given P# assembly.
        /// </summary>
        /// <param name="assembly">Assembly</param>
        /// <param name="dll">Name of dll</param>
        private static void LinkAssembly(Assembly assembly, string dll)
        {
            Console.WriteLine("... Linking {0}", dll);

            foreach (var outputDir in CompilationEngine.OutputDirectoryMap.Values)
            {
                var localFileName = (new System.Uri(assembly.CodeBase)).LocalPath;
                var fileName = outputDir + Path.DirectorySeparatorChar + dll;

                File.Delete(fileName);
                File.Copy(localFileName, fileName);
            }
        }

        #endregion

        #region helper methods

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

            var compilationOptions = new CSharpCompilationOptions(outputKind, compilation.Options.ModuleName,
                compilation.Options.MainTypeName, compilation.Options.ScriptClassName, null,
                compilation.Options.OptimizationLevel, compilation.Options.CheckOverflow, false,
                compilation.Options.CryptoKeyContainer, compilation.Options.CryptoKeyFile,
                compilation.Options.CryptoPublicKey, compilation.Options.DelaySign,
                Platform.AnyCpu, compilation.Options.GeneralDiagnosticOption,
                compilation.Options.WarningLevel, compilation.Options.SpecificDiagnosticOptions,
                compilation.Options.ConcurrentBuild, compilation.Options.XmlReferenceResolver,
                compilation.Options.SourceReferenceResolver, compilation.Options.MetadataReferenceResolver,
                compilation.Options.AssemblyIdentityComparer, compilation.Options.StrongNameProvider);

            var targetCompilation = CSharpCompilation.Create(assemblyFileName, compilation.SyntaxTrees,
                compilation.References, compilationOptions);

            string fileName = null;
            if (!Configuration.OutputFilePath.Equals(""))
            {
                fileName = Configuration.OutputFilePath + Path.DirectorySeparatorChar + assemblyFileName;
                CompilationEngine.OutputDirectoryMap.Add(compilation.AssemblyName, Configuration.OutputFilePath);
            }
            else
            {
                fileName = Path.GetDirectoryName(outputPath) + Path.DirectorySeparatorChar + assemblyFileName;
                CompilationEngine.OutputDirectoryMap.Add(compilation.AssemblyName, Path.GetDirectoryName(outputPath));
            }

            CompilationEngine.ProjectAssemblyPathMap.Add(compilation.AssemblyName, fileName);

            EmitResult emitResult = null;
            using (var outputFile = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                emitResult = targetCompilation.Emit(outputFile);
                if (emitResult.Success)
                {
                    Console.WriteLine("... Writing " + fileName);
                    return fileName;
                }
            }

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

            var targetCompilation = CSharpCompilation.Create(assemblyFileName, compilation.SyntaxTrees,
                compilation.References, new CSharpCompilationOptions(outputKind));

            EmitResult emitResult = null;
            using (var ms = new MemoryStream())
            {
                emitResult = targetCompilation.Emit(ms);
                if (emitResult.Success)
                {
                    var assembly = Assembly.Load(ms.GetBuffer());
                    return assembly;
                }
            }

            var message = string.Join("\r\n", emitResult.Diagnostics);
            throw new ApplicationException(message);
        }

        #endregion
    }
}
