// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.Net
{
    /// <summary>
    /// The local P# network provider.
    /// </summary>
    internal class LocalNetworkProvider : INetworkProvider
    {
        /// <summary>
        /// Instance of the P# runtime.
        /// </summary>
        private readonly IMachineRuntime Runtime;

        /// <summary>
        /// The local endpoint.
        /// </summary>
        private readonly string LocalEndpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalNetworkProvider"/> class.
        /// </summary>
        public LocalNetworkProvider(IMachineRuntime runtime)
        {
            this.Runtime = runtime;
            this.LocalEndpoint = string.Empty;
        }

        /// <summary>
        /// Creates a new remote machine of the specified type
        /// and with the specified event. An optional friendly
        /// name can be specified. If the friendly name is null
        /// or the empty string, a default value will be given.
        /// </summary>
        MachineId INetworkProvider.RemoteCreateMachine(Type type, string friendlyName,
            string endpoint, Event e)
        {
            return this.Runtime.CreateMachine(type, friendlyName, e);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        void INetworkProvider.RemoteSend(MachineId target, Event e)
        {
            this.Runtime.SendEvent(target, e);
        }

        /// <summary>
        /// Returns the local endpoint.
        /// </summary>
        string INetworkProvider.GetLocalEndpoint()
        {
            return this.LocalEndpoint;
        }

        /// <summary>
        /// Disposes the network provider.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
