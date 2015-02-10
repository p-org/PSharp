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
using System.Linq;
using System.IO;
using System.Reflection;

using Microsoft.PSharp.Tooling;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.PSharp.Compilation
{
    /// <summary>
    /// The P# compilation engine.
    /// </summary>
    public static class CompilationEngine
    {
        #region public API

        /// <summary>
        /// Runs the P# compilation engine.
        /// </summary>
        public static void Run()
        {
            var projectDependencyGraph = ProgramInfo.Solution.GetProjectDependencyGraph();

            foreach (var projectId in projectDependencyGraph.GetTopologicallySortedProjects())
            {
                // Compiles the project.
                CompilationEngine.CompileProject(ProgramInfo.Solution.GetProject(projectId));
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
            var compilation = project.GetCompilationAsync().Result;

            try
            {
                CompilationEngine.ToFile(compilation, project.CompilationOptions.OutputKind, project.OutputFilePath);
            }
            catch (ApplicationException ex)
            {
                ErrorReporter.ReportErrorAndExit(ex.Message);
            }
        }

        /// <summary>
        /// Compiles the given compilation to a file.
        /// </summary>
        /// <param name="compilation">Compilation</param>
        /// <param name="outputKind">OutputKind</param>
        private static void ToFile(CodeAnalysis.Compilation compilation, OutputKind outputKind, string outputPath)
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
            }
            else
            {
                fileName = outputPath;
            }

            EmitResult emitResult = null;
            using (var outputFile = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                emitResult = targetCompilation.Emit(outputFile);
                if (emitResult.Success)
                {
                    Console.WriteLine(". Writing " + outputPath);
                    return;
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
