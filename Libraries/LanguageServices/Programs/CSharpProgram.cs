//-----------------------------------------------------------------------
// <copyright file="CSharpProgram.cs">
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
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Microsoft.PSharp.LanguageServices.Rewriting.CSharp;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.LanguageServices
{
    /// <summary>
    /// A C# program.
    /// </summary>
    public sealed class CSharpProgram : AbstractPSharpProgram
    {
        #region fields

        /// <summary>
        /// List of event identifiers.
        /// </summary>
        internal List<ClassDeclarationSyntax> EventIdentifiers;

        /// <summary>
        /// List of machine identifiers.
        /// </summary>
        internal List<ClassDeclarationSyntax> MachineIdentifiers;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="project">PSharpProject</param>
        /// <param name="tree">SyntaxTree</param>
        public CSharpProgram(PSharpProject project, SyntaxTree tree)
            : base(project, tree)
        {
            this.EventIdentifiers = new List<ClassDeclarationSyntax>();
            this.MachineIdentifiers = new List<ClassDeclarationSyntax>();
        }

        /// <summary>
        /// Rewrites the P# program to the C#-IR.
        /// </summary>
        public override void Rewrite()
        {
            this.RewriteStatements();
            this.PerformCustomRewriting();

            if (IO.Debugging)
            {
                base.GetProject().CompilationContext.PrintSyntaxTree(base.GetSyntaxTree());
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Rewrites the P# statements to C#.
        /// </summary>
        private void RewriteStatements()
        {
            new RaiseRewriter(this).Rewrite();
            new GotoStateRewriter(this).Rewrite();
            new PopRewriter(this).Rewrite();
            new FairRandomRewriter(this).Rewrite();
        }

        /// <summary>
        /// Performs custom rewriting.
        /// </summary>
        private void PerformCustomRewriting()
        {
            Queue<Type> rewritingPasses = new Queue<Type>();
            Dictionary<Type, List<Type>> passDependencies = new Dictionary<Type, List<Type>>();
            HashSet<Queue<Type>> snapshot = new HashSet<Queue<Type>>();

            foreach (var assembly in base.Project.CompilationContext.CustomCompilerPassAssemblies)
            {
                foreach (var pass in this.FindCustomRewritingPasses(assembly, typeof(CustomCSharpRewritingPass)))
                {
                    rewritingPasses.Enqueue(pass);
                    passDependencies.Add(pass, this.FindDependenciesOfPass(pass));
                }
            }

            while (rewritingPasses.Count > 0)
            {
                Type nextPass = rewritingPasses.Dequeue();


                bool allDependenciesDone = true;
                foreach (var dependency in passDependencies[nextPass])
                {
                    if (rewritingPasses.Contains(dependency))
                    {
                        allDependenciesDone = false;
                        break;
                    }
                }

                if (!allDependenciesDone)
                {
                    rewritingPasses.Enqueue(nextPass);

                    if (snapshot.Any(item => item.SequenceEqual(rewritingPasses)))
                    {
                        IO.Error.ReportAndExit("Possible cycle in the rewriting " +
                            "pass dependencies, or dependency missing.");
                    }

                    snapshot.Add(rewritingPasses);
                    continue;
                }

                CSharpRewriter rewriter = null;

                try
                {
                    rewriter = Activator.CreateInstance(nextPass, this) as CSharpRewriter;
                }
                catch (MissingMethodException)
                {
                    IO.Error.ReportAndExit($"Public constructor of {nextPass} not found.");
                }

                rewriter.Rewrite();
            }
        }

        /// <summary>
        /// Finds the custom rewriting passes with the specified attribute.
        /// Returns null if no such method is found.
        /// </summary>
        /// <param name="assembly">Assembly</param>
        /// <param name="attribute">Type</param>
        /// <returns>Types</returns>
        private List<Type> FindCustomRewritingPasses(Assembly assembly, Type attribute)
        {
            List<Type> passes = null;

            try
            {
                passes = assembly.GetTypes().Where(m => m.GetCustomAttributes(attribute, false).Length > 0).ToList();
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (var le in ex.LoaderExceptions)
                {
                    ErrorReporter.Report(le.Message);
                }

                IO.Error.ReportAndExit($"Failed to load assembly '{assembly.FullName}'");
            }
            catch (Exception ex)
            {
                ErrorReporter.Report(ex.Message);
                IO.Error.ReportAndExit($"Failed to load assembly '{assembly.FullName}'");
            }

            return passes;
        }

        /// <summary>
        /// Finds the dependencies of the specified pass.
        /// </summary>
        /// <param name="pass">Pass</param>
        /// <returns>Types</returns>
        private List<Type> FindDependenciesOfPass(Type pass)
        {
            var result = new List<Type>();

            if (pass.IsDefined(typeof(RewritingPassDependency), false))
            {
                var dependencyAttribute = pass.GetCustomAttribute(
                    typeof(RewritingPassDependency), false) as RewritingPassDependency;
                result.AddRange(dependencyAttribute.Dependencies);
            }

            return result;
        }

        #endregion
    }
}
