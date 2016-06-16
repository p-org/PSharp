//-----------------------------------------------------------------------
// <copyright file="IMonitorManager.cs">
//      Copyright (c) 2016 Microsoft Corporation. All rights reserved.
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

using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.ComponentModel;

using Microsoft.PSharp.Monitoring.CallsOnly;

namespace Microsoft.PSharp.Monitoring
{
    /// <summary>
    /// Service to register monitors
    /// </summary>
    internal interface IMonitorManager
        : IService
    {
        /// <summary>
        /// Registers a thread monitor.
        /// </summary>
        /// <param name="threadMonitor">The thread monitor.</param>
        void RegisterThreadMonitor(IThreadMonitor threadMonitor);

        /// <summary>
        /// Registers a thread monitor factory.
        /// </summary>
        /// <param name="monitorFactory">The monitor factory.</param>
        void RegisterThreadMonitorFactory(IThreadMonitorFactory monitorFactory);

        /// <summary>
        /// Registers the memory access thread monitor.
        /// </summary>
        void RegisterObjectAccessThreadMonitor();

        /// <summary>
        /// get rid of accummulated execution monitors
        /// </summary>
        void DisposeExecutionMonitors();
    }
}
