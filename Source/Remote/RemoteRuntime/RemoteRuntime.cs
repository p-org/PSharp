//-----------------------------------------------------------------------
// <copyright file="RemoteRuntime.cs" company="Microsoft">
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
using System.Threading.Tasks;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.Remote
{
    /// <summary>
    /// Class implementing the P# remote runtime.
    /// </summary>
    public class PSharpRemoteRuntime : PSharpRuntime
    {
        #region fields

        /// <summary>
        /// Channel for remote communication.
        /// </summary>
        internal IRemoteCommunication Channel;

        /// <summary>
        /// Ip address.
        /// </summary>
        internal string IpAddress;

        /// <summary>
        /// Port.
        /// </summary>
        internal string Port;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        public PSharpRemoteRuntime()
            : base()
        {
            this.IpAddress = "";
            this.Port = "";
        }

        #endregion

        #region internal API

        /// <summary>
        /// Tries to create a new machine of the given type.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <returns>MachineId</returns>
        internal override MachineId TryCreateMachine(Type type)
        {
            if (type.IsSubclassOf(typeof(Machine)))
            {
                Machine machine = Activator.CreateInstance(type) as Machine;

                MachineId mid = machine.Id;
                mid.IpAddress = this.IpAddress;
                mid.Port = this.Port;

                if (!this.MachineMap.TryAdd(mid.Value, machine))
                {
                    ErrorReporter.ReportAndExit("Machine {0}({1}) was already created.",
                        type.Name, mid.Value);
                }

                Task task = new Task(() =>
                {
                    this.TaskMap.TryAdd(Task.CurrentId.Value, machine);

                    try
                    {
                        machine.GotoStartState();
                        machine.RunEventHandler();
                    }
                    finally
                    {
                        this.TaskMap.TryRemove(Task.CurrentId.Value, out machine);
                    }
                });

                task.Start();

                return mid;
            }
            else
            {
                ErrorReporter.ReportAndExit("Type '{0}' is not a machine.", type.Name);
                return null;
            }
        }

        /// <summary>
        /// Tries to create a new remote machine of the given type.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <returns>MachineId</returns>
        internal override MachineId TryCreateRemoteMachine(Type type)
        {
            return this.Channel.CreateMachine(type.FullName);
        }

        /// <summary>
        /// Sends an asynchronous event to a remote machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="e">Event</param>
        internal override void SendRemotely(MachineId mid, Event e)
        {
            this.Channel.SendEvent(mid, e);
        }

        #endregion
    }
}
