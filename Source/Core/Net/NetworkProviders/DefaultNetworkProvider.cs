//-----------------------------------------------------------------------
// <copyright file="DefaultNetworkProvider.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.Net
{
    /// <summary>
    /// Class implementing the default P# network provider.
    /// </summary>
    internal class DefaultNetworkProvider : INetworkProvider
    {
        #region fields

        /// <summary>
        /// Instance of the P# runtime.
        /// </summary>
        private PSharpRuntime Runtime;

        /// <summary>
        /// The local endpoint.
        /// </summary>
        private string LocalEndPoint;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">PSharpRuntime</param>
        public DefaultNetworkProvider(PSharpRuntime runtime)
        {
            this.Runtime = runtime;
            this.LocalEndPoint = "";
        }

        #endregion

        #region methods

        /// <summary>
        /// Creates a new remote machine of the given
        /// type and with the given event.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="e">Event</param>
        /// <returns>MachineId</returns> 
        MachineId INetworkProvider.RemoteCreateMachine(Type type, string endpoint, Event e)
        {
            return this.Runtime.CreateMachine(type);
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
        string INetworkProvider.GetLocalEndPoint()
        {
            return this.LocalEndPoint;
        }

        #endregion
    }
}
