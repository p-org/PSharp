//-----------------------------------------------------------------------
// <copyright file="ParsingEngine.cs">
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
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Microsoft.PSharp.LanguageServices.Compilation;
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// A P# parsing engine.
    /// </summary>
    public sealed class ParsingEngine
    {
        #region fields

        /// <summary>
        /// The compilation context.
        /// </summary>
        private CompilationContext CompilationContext;

        /// <summary>
        /// List of P# projects.
        /// </summary>
        private List<PSharpProject> PSharpProjects;

        #endregion

        #region public API

        /// <summary>
        /// Creates a P# parsing engine.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <returns></returns>
        public static ParsingEngine Create(CompilationContext context)
        {
            return new ParsingEngine(context);
        }

        /// <summary>
        /// Runs the P# parsing engine.
        /// </summary>
        public void Run()
        {
            this.PSharpProjects = new List<PSharpProject>();
            
            // Parse the projects.
            if (this.CompilationContext.Configuration.ProjectName.Equals(""))
            {
                foreach (var project in this.CompilationContext.Solution.Projects)
                {
                    var psharpProject = new PSharpProject(this.CompilationContext, project.Name);
                    psharpProject.Parse();
                    this.PSharpProjects.Add(psharpProject);
                }
            }
            else
            {
                // Find the project specified by the user.
                var targetProject = this.CompilationContext.Solution.Projects.Where(
                    p => p.Name.Equals(this.CompilationContext.Configuration.ProjectName)).FirstOrDefault();

                var projectDependencyGraph = this.CompilationContext.Solution.GetProjectDependencyGraph();
                var projectDependencies = projectDependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(targetProject.Id);

                foreach (var project in this.CompilationContext.Solution.Projects)
                {
                    if (!projectDependencies.Contains(project.Id) && !project.Id.Equals(targetProject.Id))
                    {
                        continue;
                    }

                    var psharpProject = new PSharpProject(this.CompilationContext, project.Name);
                    psharpProject.Parse();
                    this.PSharpProjects.Add(psharpProject);
                }
            }

            // Rewrite the projects.
            for (int idx = 0; idx < this.PSharpProjects.Count; idx++)
            {
                this.PSharpProjects[idx].Rewrite();
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        private ParsingEngine(CompilationContext context)
        {
            this.CompilationContext = context;
        }

        #endregion
    }
}
