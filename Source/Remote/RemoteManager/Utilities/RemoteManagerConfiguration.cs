//-----------------------------------------------------------------------
// <copyright file="RemoteManagerConfiguration.cs">
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
    public sealed class RemoteManagerConfiguration : Configuration
    {
        #region options

        /// <summary>
        /// Number of containers.
        /// </summary>
        public int NumberOfContainers;

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
        private RemoteManagerConfiguration()
            : base()
        {
            this.NumberOfContainers = 1;
            this.ApplicationFilePath = "";
        }

        #endregion

        #region methods

        /// <summary>
        /// Creates a new remote manager configuration.
        /// </summary>
        /// <returns>RemoteManagerConfiguration</returns>
        public static RemoteManagerConfiguration Create()
        {
            return new RemoteManagerConfiguration();
        }

        #endregion
    }
}
