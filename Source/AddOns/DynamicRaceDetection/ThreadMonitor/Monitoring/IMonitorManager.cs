// ------------------------------------------------------------------------------------------------

using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.ComponentModel;

using Microsoft.PSharp.Monitoring.CallsOnly;

namespace Microsoft.PSharp.Monitoring
{
    /// <summary>
    /// Service to register monitors.
    /// </summary>
    internal interface IMonitorManager
        : IService
    {
        /// <summary>
        /// Registers a thread monitor.
        /// </summary>
        /// <param name="threadMonitor">IThreadMonitor</param>
        void RegisterThreadMonitor(IThreadMonitor threadMonitor);

        /// <summary>
        /// Registers a thread monitor factory.
        /// </summary>
        /// <param name="monitorFactory">IThreadMonitorFactory</param>
        void RegisterThreadMonitorFactory(IThreadMonitorFactory monitorFactory);

        /// <summary>
        /// Registers the memory access thread monitor.
        /// </summary>
        void RegisterObjectAccessThreadMonitor();

        /// <summary>
        /// Gets rid of accummulated execution monitors.
        /// </summary>
        void DisposeExecutionMonitors();
    }
}
