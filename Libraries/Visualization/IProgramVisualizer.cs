//-----------------------------------------------------------------------
// <copyright file="IProgramVisualizer.cs">
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

using System.Threading.Tasks;

namespace Microsoft.PSharp.Visualization
{
    /// <summary>
    /// Interface of a program visualizer.
    /// </summary>
    public interface IProgramVisualizer
    {
        #region properties

        /// <summary>
        /// Data structure containing information
        /// regarding testing coverage.
        /// </summary>
        CoverageInfo CoverageInfo { get; }

        #endregion

        #region methods

        /// <summary>
        /// Starts the visualisation asynchronously.
        /// </summary>
        /// <returns>Task</returns>
        Task StartAsync();

        /// <summary>
        /// Called when a testing iteration finishes.
        /// </summary>
        void Step();

        /// <summary>
        /// Refreshes the visualization.
        /// </summary>
        void Refresh();

        #endregion
    }
}