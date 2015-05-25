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

using Microsoft.PSharp.Core;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Compilation
{
    /// <summary>
    /// The P# compilation engine.
    /// </summary>
    public static class CompilationEngine
    {
        #region fields

        private static HashSet<string> OutputDirectories = null;

        #endregion

        #region public API

        /// <summary>
        /// Runs the P# compilation engine.
        /// </summary>
        public static void Run()
        {
            CompilationEngine.OutputDirectories = new HashSet<string>();

            var projectDependencyGraph = ProgramInfo.Solution.GetProjectDependencyGraph();

            if (Configuration.ProjectName.Equals(""))
            {
                foreach (var projectId in projectDependencyGraph.GetTopologicallySortedProjects())
                {
                    // Compiles the project.
                    CompilationEngine.CompileProject(ProgramInfo.Solution.GetProject(projectId));
                }
            }
            else
            {
                // Find the project specified by the user.
                var project = ProgramInfo.Solution.Projects.Where(
                    p => p.Name.Equals(Configuration.ProjectName)).FirstOrDefault();

                var projectDependencies = projectDependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(project.Id);

                foreach (var projectId in projectDependencyGraph.GetTopologicallySortedProjects())
                {
                    if (!projectDependencies.Contains(projectId) && !projectId.Equals(project.Id))
                    {
                        continue;
                    }

                    // Compiles the project.
                    CompilationEngine.CompileProject(ProgramInfo.Solution.GetProject(projectId));
                }
            }

            // Links the P# runtime.
            CompilationEngine.LinkAssembly(typeof(ProgramUnit).Assembly, "Microsoft.PSharp.Core.dll");
            CompilationEngine.LinkAssembly(typeof(Runtime).Assembly, "Microsoft.PSharp.dll");
        }

        #endregion

        #region private API

        /// <summary>
        /// Compiles the given P# project.
        /// </summary>
        /// <param name="project">Project</param>
        private static void CompileProject(Project project)
        {
            var compilation = project.GetCompilationAsync().Result;

            try
            {
                CompilationEngine.ToFile(compilation, project.CompilationOptions.OutputKind, project.OutputFilePath);

                if (Configuration.RunDynamicAnalysis)
                {
                    var dll = CompilationEngine.ToFile(compilation, OutputKind.DynamicallyLinkedLibrary,
                        project.OutputFilePath);
                    Configuration.AssembliesToBeAnalyzed.Add(dll);
                }
            }
            catch (ApplicationException ex)
            {
                ErrorReporter.ReportAndExit(ex.Message);
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

            foreach (var outputDir in CompilationEngine.OutputDirectories)
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

            var targetCompilation = CSharpCompilation.Create(assemblyFileName, compilation.SyntaxTrees,
                compilation.References, new CSharpCompilationOptions(outputKind));
            
            string fileName = null;
            if (!Configuration.OutputFilePath.Equals(""))
            {
                fileName = Configuration.OutputFilePath + Path.DirectorySeparatorChar + assemblyFileName;
                CompilationEngine.OutputDirectories.Add(Configuration.OutputFilePath);
            }
            else
            {
                fileName = Path.GetDirectoryName(outputPath) + Path.DirectorySeparatorChar + assemblyFileName;
                CompilationEngine.OutputDirectories.Add(Path.GetDirectoryName(outputPath));
            }

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
