// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.Net
{
    /// <summary>
    /// The local P# network provider.
    /// </summary>
    internal class LocalNetworkProvider : INetworkProvider
    {
        #region fields

        /// <summary>
        /// Instance of the P# runtime.
        /// </summary>
        private PSharpRuntime Runtime;

        /// <summary>
        /// The local endpoint.
        /// </summary>
        private string LocalEndpoint;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">PSharpRuntime</param>
        public LocalNetworkProvider(PSharpRuntime runtime)
        {
            this.Runtime = runtime;
            this.LocalEndpoint = "";
        }

        #endregion

        #region methods

        /// <summary>
        /// Creates a new remote machine of the specified type
        /// and with the specified event. An optional friendly
        /// name can be specified. If the friendly name is null
        /// or the empty string, a default value will be given.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="friendlyName">Friendly machine name used for logging</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns> 
        MachineId INetworkProvider.RemoteCreateMachine(Type type, string friendlyName,
            string endpoint, Event e)
        {
            return this.Runtime.CreateMachine(type, friendlyName, e);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        void INetworkProvider.RemoteSend(MachineId target, Event e)
        {
            this.Runtime.SendEvent(target, e);
        }

        /// <summary>
        /// Returns the local endpoint.
        /// </summary>
        /// <returns>Endpoint</returns>
        string INetworkProvider.GetLocalEndpoint()
        {
            return this.LocalEndpoint;
        }

        /// <summary>
        /// Disposes the network provider.
        /// </summary>
        public void Dispose() { }

        #endregion
    }
}
