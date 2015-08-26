//-----------------------------------------------------------------------
// <copyright file="RuntimeContainerConfiguration.cs">
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

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Remote
{
    public sealed class RuntimeContainerConfiguration : Configuration
    {
        #region options

        /// <summary>
        /// Number of containers.
        /// </summary>
        public int NumberOfContainers;

        /// <summary>
        /// The unique container id.
        /// </summary>
        public int ContainerId;

        /// <summary>
        /// The path to the P# application to run in a
        /// distributed setting.
        /// </summary>
        public string ApplicationFilePath;

        #endregion

        #region constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        internal RuntimeContainerConfiguration()
            : base()
        {
            this.NumberOfContainers = 1;
            this.ContainerId = 0;
            this.ApplicationFilePath = "";
        }

        #endregion
    }
}
