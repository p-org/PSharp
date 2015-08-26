//-----------------------------------------------------------------------
// <copyright file="Configuration.cs">
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

using System.Collections.Generic;

namespace Microsoft.PSharp.Tooling
{
    public abstract class Configuration
    {
        #region core options

        /// <summary>
        /// The path to the solution file.
        /// </summary>
        public string SolutionFilePath;

        /// <summary>
        /// The output path.
        /// </summary>
        public string OutputFilePath;

        /// <summary>
        /// The name of the project to analyse.
        /// </summary>
        public string ProjectName;

        /// <summary>
        /// Verbosity level.
        /// </summary>
        public int Verbose;

        /// <summary>
        /// Timeout.
        /// </summary>
        public int Timeout;

        #endregion

        #region constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        protected Configuration()
        {
            this.SolutionFilePath = "";
            this.OutputFilePath = "";
            this.ProjectName = "";

            this.Verbose = 1;
            this.Timeout = 0;
        }

        #endregion
    }
}
