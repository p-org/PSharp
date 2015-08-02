//-----------------------------------------------------------------------
// <copyright file="RemoteRequestListener.cs" company="Microsoft">
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

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Remote
{
    /// <summary>
    /// Class implementing a remote request listening service.
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal class RemoteRequestListener : IRemoteCommunication
    {
        /// <summary>
        /// Creates a new machine of the given type with an optional payload.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <param name="payload">Optional payload</param>
        /// <returns>Machine id</returns> 
        MachineId IRemoteCommunication.CreateMachine(string type, params Object[] payload)
        {
            Output.Print("Received request to create machine of type {0}", type);
            var resolvedType = PSharpRuntime.AppAssembly.GetType(type);
            return PSharpRuntime.CreateMachine(resolvedType, payload);
        }

        /// <summary>
        /// Sends an asynchronous event to a machine.
        /// </summary>
        /// <param name="target">Target machine id</param>
        /// <param name="e">Event</param>
        void IRemoteCommunication.SendEvent(MachineId target, Event e)
        {
            Output.Print("Received sent event {0}", e.GetType());
            PSharpRuntime.SendEvent(target, e);
        }
    }
}
