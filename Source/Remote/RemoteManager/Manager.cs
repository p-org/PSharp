//-----------------------------------------------------------------------
// <copyright file="Manager.cs" company="Microsoft">
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.Remote
{
    /// <summary>
    /// Implements a remote manager.
    /// </summary>
    internal static class Manager
    {
        #region fields

        /// <summary>
        /// Configuration.
        /// </summary>
        private static Configuration Configuration;

        /// <summary>
        /// The notification listening service.
        /// </summary>
        private static ServiceHost NotificationService;

        /// <summary>
        /// Map from ids to containers.
        /// </summary>
        private static Dictionary<int, Process> Containers =
            new Dictionary<int, Process>();

        /// <summary>
        /// Map from ids to container services.
        /// </summary>
        private static Dictionary<int, IContainerService> ContainerServices =
            new Dictionary<int, IContainerService>();

        /// <summary>
        /// Monotonically increasing container id counter.
        /// </summary>
        private static int ContainerIdCounter = 0;

        #endregion

        #region internal API

        /// <summary>
        /// Configures the remote manager.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        internal static void Configure(Configuration configuration)
        {
            Manager.Configuration = configuration;
        }

        /// <summary>
        /// Runs the remote manager.
        /// </summary>
        internal static void Run()
        {
            Output.PrettyPrintLine(". Running");

            Manager.OpenRemoteManagingListener();

            for (int idx = 0; idx < Manager.Configuration.NumberOfContainers; idx++)
            {
                Manager.CreateContainer();
            }

            Console.ReadLine();

            Output.PrettyPrintLine(". Cleaning resources");

            Manager.KillContainers();
            Manager.NotificationService.Close();

            Output.PrettyPrintLine("... Closed listener");
        }

        /// <summary>
        /// Notifies that a container has been initialized.
        /// </summary>
        /// <param name="id">Container id</param>
        internal static void NotifyInitializedContainer(int id)
        {
            Output.PrettyPrintLine("..... Container '{0}' is initialized", id);

            Uri address = new Uri("http://localhost:8000/notify/" + id + "/");

            var binding = new WSHttpBinding();
            var endpoint = new EndpointAddress(address);

            var channel = ChannelFactory<IContainerService>.CreateChannel(binding, endpoint);
            
            Manager.ContainerServices.Add(id, channel);
            
            if (Manager.ContainerServices.Count == Manager.Configuration.NumberOfContainers)
            {
                Output.PrettyPrintLine("... Notifying container '0' [start]");
                Manager.ContainerServices[0].NotifyStartRuntime();
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Creates a new container.
        /// </summary>
        /// <returns>Process</returns>
        private static void CreateContainer()
        {
            Output.PrettyPrintLine("... Creating container '{0}'", Manager.ContainerIdCounter);

            Process process = new Process();
            process.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "PSharpRuntimeContainer.exe");
            process.StartInfo.Arguments = "/id:" + Manager.ContainerIdCounter;
            process.StartInfo.Arguments += " /main:" + Manager.Configuration.ApplicationFilePath;

            Manager.Containers.Add(Manager.ContainerIdCounter, process);

            Manager.ContainerIdCounter++;

            try
            {
                process.Start();
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                ErrorReporter.ReportAndExit(ex.Message);
            }
        }

        /// <summary>
        /// Notifies the containers to exit.
        /// </summary>
        private static void KillContainers()
        {
            Output.PrettyPrintLine("... Shutting down containers");
            foreach (var container in Manager.ContainerServices)
            {
                container.Value.NotifyExit();
            }
        }

        /// <summary>
        /// Opens the remote managing listener.
        /// </summary>
        private static void OpenRemoteManagingListener()
        {
            Output.PrettyPrintLine("... Opening remote managing listener");

            Uri address = new Uri("http://localhost:8000/manager/");
            var binding = new WSHttpBinding();

            Manager.NotificationService = new ServiceHost(typeof(NotificationListener));

            //host.Description.Behaviors.Remove(typeof(ServiceDebugBehavior));
            //host.Description.Behaviors.Add(new ServiceDebugBehavior {
            //    IncludeExceptionDetailInFaults = true });

            Manager.NotificationService.AddServiceEndpoint(typeof(IManagerService), binding, address);
            Manager.NotificationService.Open();
        }

        #endregion
    }
}
