// ------------------------------------------------------------------------------------------------

using Microsoft.ExtendedReflection.ComponentModel;

namespace Microsoft.PSharp.Monitoring.ComponentModel
{
    /// <summary>
    /// List of available services.
    /// </summary>
    internal class CopComponentServices : ComponentServices, ICopComponentServices
    {
        /// <summary>
        /// Initializes a new instance of the CopComponentServices class.
        /// </summary>
        /// <param name="host">The host.</param>
        public CopComponentServices(IComponent host)
            : base(host)
        { }

        IMonitorManager _monitorManager;

        /// <summary>
        /// Gets the monitor manager.
        /// </summary>
        /// <value>The monitor manager.</value>
        public IMonitorManager MonitorManager
        {
            get
            {
                if (this._monitorManager == null)
                {
                    this._monitorManager = this.GetService<MonitorManager>();
                }

                return this._monitorManager;
            }
        }

    }
}
