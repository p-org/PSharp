//-----------------------------------------------------------------------
// <copyright file="ParsingEngine.cs">
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
using System.Linq;

using Microsoft.PSharp.LanguageServices.Compilation;

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

        #endregion

        #region public API

        /// <summary>
        /// Creates a P# parsing engine.
        /// </summary>
        /// <param name="context">CompilationContext</param>
        /// <returns>ParsingEngine</returns>
        public static ParsingEngine Create(CompilationContext context)
        {
            return new ParsingEngine(context);
        }

        /// <summary>
        /// Runs the P# parsing engine.
        /// </summary>
        /// <returns>ParsingEngine</returns>
        public ParsingEngine Run()
        {
            // Parse the projects.
            if (this.CompilationContext.Configuration.ProjectName.Equals(""))
            {
                foreach (var project in this.CompilationContext.GetSolution().Projects)
                {
                    var psharpProject = new PSharpProject(this.CompilationContext, project.Name);
                    psharpProject.Parse();
                    this.CompilationContext.GetProjects().Add(psharpProject);
                }
            }
            else
            {
                // Find the project specified by the user.
                var targetProject = this.CompilationContext.GetSolution().Projects.Where(
                    p => p.Name.Equals(this.CompilationContext.Configuration.ProjectName)).FirstOrDefault();

                var projectDependencyGraph = this.CompilationContext.GetSolution().GetProjectDependencyGraph();
                var projectDependencies = projectDependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(targetProject.Id);

                foreach (var project in this.CompilationContext.GetSolution().Projects)
                {
                    if (!projectDependencies.Contains(project.Id) && !project.Id.Equals(targetProject.Id))
                    {
                        continue;
                    }

                    var psharpProject = new PSharpProject(this.CompilationContext, project.Name);
                    psharpProject.Parse();
                    this.CompilationContext.GetProjects().Add(psharpProject);
                }
            }

            return this;
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
