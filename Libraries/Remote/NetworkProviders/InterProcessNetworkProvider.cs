//-----------------------------------------------------------------------
// <copyright file="InterProcessNetworkProvider.cs" company="Microsoft">
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
using System.Reflection;
using System.ServiceModel;

using Microsoft.PSharp.Net;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.Remote
{
    /// <summary>
    /// Class implementing a network provider for inter-process communication.
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal class InterProcessNetworkProvider : INetworkProvider, IRemoteCommunication
    {
        #region fields

        /// <summary>
        /// Instance of the P# runtime.
        /// </summary>
        private PSharpRuntime Runtime;

        /// <summary>
        /// The local id address.
        /// </summary>
        private string IpAddress;

        /// <summary>
        /// The local port.
        /// </summary>
        private string Port;

        /// <summary>
        /// Channel for remote communication.
        /// </summary>
        private IRemoteCommunication Channel;

        /// <summary>
        /// The application assembly.
        /// </summary>
        private Assembly ApplicationAssembly;

        #endregion

        #region initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ipAddress">IpAddress</param>
        /// <param name="port">Port</param>
        public InterProcessNetworkProvider(string ipAddress, string port)
        {
            this.IpAddress = ipAddress;
            this.Port = port;
        }

        /// <summary>
        /// Initializes the network provider.
        /// </summary>
        /// <param name="runtime">PSharpRuntime</param>
        /// <param name="applicationAssembly">ApplicationAssembly</param>
        public void Initialize(PSharpRuntime runtime, Assembly applicationAssembly)
        {
            this.Runtime = runtime;
            this.ApplicationAssembly = applicationAssembly;

            //var channels = new Dictionary<string, IRemoteCommunication>();

            if (runtime.Configuration.ContainerId == 0)
            {
                Uri address = new Uri("http://" + this.IpAddress + ":" + this.Port + "/request/" + 1 + "/");

                WSHttpBinding binding = new WSHttpBinding();
                EndpointAddress endpoint = new EndpointAddress(address);

                this.Channel = ChannelFactory<IRemoteCommunication>.CreateChannel(binding, endpoint);
            }
            else
            {
                Uri address = new Uri("http://" + this.IpAddress + ":" + this.Port + "/request/" + 0 + "/");

                WSHttpBinding binding = new WSHttpBinding();
                EndpointAddress endpoint = new EndpointAddress(address);

                this.Channel = ChannelFactory<IRemoteCommunication>.CreateChannel(binding, endpoint);
            }
        }

        #endregion

        #region network provider methods

        /// <summary>
        /// Creates a new remote machine of the given type.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="endpoint">Endpoint</param>
        /// <returns>MachineId</returns> 
        MachineId INetworkProvider.RemoteCreateMachine(Type type, string endpoint)
        {
            return this.Channel.CreateMachine(type.FullName);
        }

        /// <summary>
        /// Sends an event to a remote machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        void INetworkProvider.RemoteSend(MachineId target, Event e)
        {
            this.Channel.SendEvent(target, e);
        }

        /// <summary>
        /// Returns the local endpoint.
        /// </summary>
        /// <returns>Endpoint</returns>
        string INetworkProvider.GetLocalEndPoint()
        {
            return this.IpAddress + ":" + this.Port;
        }

        #endregion

        #region remote communication methods

        /// <summary>
        /// Creates a new machine of the given type.
        /// </summary>
        /// <param name="typeName">Type of the machine</param>
        /// <returns>MachineId</returns> 
        MachineId IRemoteCommunication.CreateMachine(string typeName)
        {
            IO.PrintLine("Received request to create machine of type {0}", typeName);
            var resolvedType = this.GetMachineType(typeName);
            return this.Runtime.CreateMachine(resolvedType);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        void IRemoteCommunication.SendEvent(MachineId target, Event e)
        {
            IO.PrintLine("Received sent event {0}", e.GetType());
            this.Runtime.SendEvent(target, e);
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Gets the Type object of the machine with the specified type.
        /// </summary>
        /// <param name="typeName">TypeName</param>
        internal Type GetMachineType(string typeName)
        {
            Type machineType = this.ApplicationAssembly.GetType(typeName);
            this.Runtime.Assert(machineType != null, "Could not infer type of " + typeName + ".");
            this.Runtime.Assert(machineType.IsSubclassOf(typeof(Machine)), typeName +
                " is not a subclass of type Machine.");
            return machineType;
        }

        #endregion
    }
}
