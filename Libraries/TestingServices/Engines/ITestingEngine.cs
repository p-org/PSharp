//-----------------------------------------------------------------------
// <copyright file="TestingEngine.cs">
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

using Microsoft.PSharp.TestingServices.Coverage;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Interface of a P# testing engine.
    /// </summary>
    public interface ITestingEngine
    {
        #region properties

        /// <summary>
        /// Data structure containing information
        /// regarding testing coverage.
        /// </summary>
        CoverageInfo CoverageInfo { get; set; }

        /// <summary>
        /// The latest bug report, if any.
        /// </summary>
        string BugReport { get; }

        /// <summary>
        /// Number of found bugs.
        /// </summary>
        int NumOfFoundBugs { get; }

        /// <summary>
        /// Explored depth of scheduling decisions.
        /// </summary>
        int ExploredDepth { get; }

        #endregion

        #region methods

        /// <summary>
        /// Runs the P# testing engine.
        /// </summary>
        /// <returns>ITestingEngine</returns>
        ITestingEngine Run();

        /// <summary>
        /// Stops the P# testing engine.
        /// </summary>
        void Stop();

        /// <summary>
        /// Tries to emit the testing traces, if any.
        /// </summary>
        void TryEmitTraces();

        /// <summary>
        /// Tries to emit the testing coverage report, if any.
        /// </summary>
        void TryEmitCoverageReport();

        /// <summary>
        /// Registers a callback to invoke at the end
        /// of each iteration. The callback takes as
        /// a parameter an integer representing the
        /// current iteration.
        /// </summary>
        /// <param name="callback">Callback</param>
        void RegisterPerIterationCallBack(Action<int> callback);

        /// <summary>
        /// Returns a report with the testing results.
        /// </summary>
        /// <returns>Report</returns>
        string Report();

        #endregion
    }
}
