using Microsoft.ExtendedReflection.Collections;

namespace Microsoft.PSharp.Monitoring.CallsOnly
{
    /// <summary>
    /// A collection of thread monitors.
    /// </summary>
    internal sealed class ThreadMonitorCollection : SafeList<IThreadMonitor>
    {

    }
}
