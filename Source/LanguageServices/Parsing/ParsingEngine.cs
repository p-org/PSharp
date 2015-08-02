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

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.LanguageServices.Parsing
{
    /// <summary>
    /// The P# parsing engine.
    /// </summary>
    public static class ParsingEngine
    {
        #region public API

        /// <summary>
        /// List of P# projects.
        /// </summary>
        private static List<PSharpProject> PSharpProjects;

        #endregion

        #region public API

        /// <summary>
        /// Runs the P# parsing engine.
        /// </summary>
        public static void Run()
        {
            ParsingEngine.PSharpProjects = new List<PSharpProject>();
            
            // Parse the projects.
            if (Configuration.ProjectName.Equals(""))
            {
                foreach (var project in ProgramInfo.Solution.Projects)
                {
                    var psharpProject = new PSharpProject(project.Name);
                    psharpProject.Parse();
                    ParsingEngine.PSharpProjects.Add(psharpProject);
                }
            }
            else
            {
                // Find the project specified by the user.
                var targetProject = ProgramInfo.Solution.Projects.Where(
                    p => p.Name.Equals(Configuration.ProjectName)).FirstOrDefault();

                var projectDependencyGraph = ProgramInfo.Solution.GetProjectDependencyGraph();
                var projectDependencies = projectDependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(targetProject.Id);

                foreach (var project in ProgramInfo.Solution.Projects)
                {
                    if (!projectDependencies.Contains(project.Id) && !project.Id.Equals(targetProject.Id))
                    {
                        continue;
                    }

                    var psharpProject = new PSharpProject(project.Name);
                    psharpProject.Parse();
                    ParsingEngine.PSharpProjects.Add(psharpProject);
                }
            }

            // Rewrite the projects.
            for (int idx = 0; idx < ParsingEngine.PSharpProjects.Count; idx++)
            {
                ParsingEngine.PSharpProjects[idx].Rewrite();
            }
        }

        #endregion
    }
}
