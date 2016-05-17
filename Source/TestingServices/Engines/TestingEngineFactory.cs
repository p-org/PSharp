//-----------------------------------------------------------------------
// <copyright file="TestingEngineFactory.cs">
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
using System.Reflection;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// The P# testing engine factory.
    /// </summary>
    public static class TestingEngineFactory
    {
        #region factory methods

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        /// <returns>BugFindingEngine</returns>
        public static ITestingEngine CreateBugFindingEngine(Configuration configuration, Action<PSharpRuntime> action)
        {
            return BugFindingEngine.Create(configuration, action);
        }

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        /// <returns>BugFindingEngine</returns>
        internal static ITestingEngine CreateBugFindingEngine(Configuration configuration, Assembly assembly)
        {
            return BugFindingEngine.Create(configuration, assembly);
        }

        /// <summary>
        /// Creates a new P# bug-finding engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assemblyName">Assembly name</param>
        /// <returns>BugFindingEngine</returns>
        internal static ITestingEngine CreateBugFindingEngine(Configuration configuration, string assemblyName)
        {
            return BugFindingEngine.Create(configuration, assemblyName);
        }

        /// <summary>
        /// Creates a new P# replay engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="action">Action</param>
        /// <returns>BugFindingEngine</returns>
        public static ITestingEngine CreateReplayEngine(Configuration configuration, Action<PSharpRuntime> action)
        {
            return ReplayEngine.Create(configuration, action);
        }

        /// <summary>
        /// Creates a new P# replay engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assembly">Assembly</param>
        /// <returns>BugFindingEngine</returns>
        internal static ITestingEngine CreateReplayEngine(Configuration configuration, Assembly assembly)
        {
            return ReplayEngine.Create(configuration, assembly);
        }

        /// <summary>
        /// Creates a new P# replay engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="assemblyName">Assembly name</param>
        /// <returns>BugFindingEngine</returns>
        internal static ITestingEngine CreateReplayEngine(Configuration configuration, string assemblyName)
        {
            return ReplayEngine.Create(configuration, assemblyName);
        }

        #endregion
    }
}
