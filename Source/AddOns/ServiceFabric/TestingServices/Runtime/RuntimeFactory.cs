//-----------------------------------------------------------------------
// <copyright file="RuntimeFactory.cs">
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

using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;

namespace Microsoft.PSharp.ServiceFabric.TestingServices
{
    /// <summary>
    /// Runtime factory
    /// </summary>
    internal static class RuntimeFactory
    {
        /// <summary>
        /// Creates a new P# Service Fabric testing runtime.
        /// </summary>
        /// <returns>Runtime</returns>
        [TestRuntimeCreate]
        internal static PSharp.TestingServices.BugFindingRuntime Create(Configuration configuration,
            ISchedulingStrategy strategy, IRegisterRuntimeOperation reporter)
        {
            return new BugFindingRuntime(configuration, strategy, reporter);
        }
    }
}