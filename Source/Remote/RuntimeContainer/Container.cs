//-----------------------------------------------------------------------
// <copyright file="Container.cs" company="Microsoft">
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
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading.Tasks;

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Remote
{
    /// <summary>
    /// Implements a runtime container.
    /// </summary>
    internal static class Container
    {
        #region fields

        /// <summary>
        /// The notification listening service.
        /// </summary>
        private static ServiceHost NotificationService;

        /// <summary>
        /// The request listening service.
        /// </summary>
        private static ServiceHost RequestService;

        /// <summary>
        /// Channel for remote communication.
        /// </summary>
        internal static IManagerService Channel;

        #endregion

        #region internal API

        /// <summary>
        /// Configures the container.
        /// </summary>
        internal static void Configure()
        {
            Output.WriteLine(". Setting up");

            Container.LoadApplicationAssembly();
            Container.RegisterSerializableTypes();

            Output.WriteLine("... Configured as container '{0}'", Configuration.ContainerId);
        }

        /// <summary>
        /// Runs the container.
        /// </summary>
        internal static void Run()
        {
            Output.WriteLine(". Running");

            Container.OpenNotificationListener();
            Container.OpenRemoteRequestListener();
            Container.InitializeDistributedRuntime();

            Console.ReadLine();

            Container.RequestService.Close();
            Container.NotificationService.Close();

            Output.WriteLine(". Closed listeners");
        }

        /// <summary>
        /// Invoke the distributed runtime.
        /// </summary>
        internal static void InvokeDistributedRuntime()
        {
            Task.Factory.StartNew(() =>
            {
                Output.WriteLine("... Starting P# runtime");

                var entry = Container.FindEntryPoint(Runtime.AppAssembly);
                entry.Invoke(null, null);
            });
        }

        #endregion

        #region private API

        /// <summary>
        /// Loads the application assembly.
        /// </summary>
        private static void LoadApplicationAssembly()
        {
            Assembly applicationAssembly = null;

            try
            {
                applicationAssembly = Assembly.LoadFrom(Configuration.ApplicationFilePath);
            }
            catch (FileNotFoundException ex)
            {
                ErrorReporter.ReportAndExit(ex.Message);
            }

            Runtime.AppAssembly = applicationAssembly;
        }

        /// <summary>
        /// Registers all discoverable serializable types.
        /// </summary>
        private static void RegisterSerializableTypes()
        {
            var eventTypes = from type in Runtime.AppAssembly.GetTypes()
                             where type.IsSubclassOf(typeof(Event))
                             select type;
            KnownTypesProvider.KnownTypes.AddRange(eventTypes);
        }

        /// <summary>
        /// Opens the remote notification listener.
        /// </summary>
        private static void OpenNotificationListener()
        {
            Output.WriteLine("... Opening notification listener");

            Uri address = new Uri("http://localhost:8000/notify/" + Configuration.ContainerId + "/");
            var binding = new WSHttpBinding();

            Container.NotificationService = new ServiceHost(typeof(NotificationListener));

            //host.Description.Behaviors.Remove(typeof(ServiceDebugBehavior));
            //host.Description.Behaviors.Add(new ServiceDebugBehavior {
            //    IncludeExceptionDetailInFaults = true });

            Container.NotificationService.AddServiceEndpoint(typeof(IContainerService), binding, address);
            Container.NotificationService.Open();
        }

        /// <summary>
        /// Opens the remote request listener.
        /// </summary>
        private static void OpenRemoteRequestListener()
        {
            Output.WriteLine("... Opening remote request listener");

            Uri address = new Uri("http://localhost:8000/request/" + Configuration.ContainerId + "/");
            var binding = new WSHttpBinding();

            Container.RequestService = new ServiceHost(typeof(RemoteRequestListener));

            //host.Description.Behaviors.Remove(typeof(ServiceDebugBehavior));
            //host.Description.Behaviors.Add(new ServiceDebugBehavior {
            //    IncludeExceptionDetailInFaults = true });

            Container.RequestService.AddServiceEndpoint(typeof(IRemoteCommunication), binding, address);
            Container.RequestService.Open();
        }

        /// <summary>
        /// Initializes the distributed runtime and creates
        /// a remote communication channel.
        /// </summary>
        /// <returns>Process</returns>
        private static void InitializeDistributedRuntime()
        {
            Runtime.IpAddress = "localhost";
            Runtime.Port = "8000";

            //var channels = new Dictionary<string, IRemoteCommunication>();

            if (Configuration.ContainerId == 0)
            {
                Uri address = new Uri("http://" + Runtime.IpAddress + ":" +
                    Runtime.Port + "/request/" + 1 + "/");

                var binding = new WSHttpBinding();
                var endpoint = new EndpointAddress(address);

                var channel = ChannelFactory<IRemoteCommunication>.CreateChannel(binding, endpoint);
                Runtime.Channel = channel;
            }
            else
            {
                Uri address = new Uri("http://" + Runtime.IpAddress + ":" +
                    Runtime.Port + "/request/" + 0 + "/");

                var binding = new WSHttpBinding();
                var endpoint = new EndpointAddress(address);

                var channel = ChannelFactory<IRemoteCommunication>.CreateChannel(binding, endpoint);
                Runtime.Channel = channel;
            }

            Runtime.Initialize();
            Container.NotifyManagerInitialization();
        }

        /// <summary>
        /// Notifies the remote manager about successful initialization.
        /// </summary>
        private static void NotifyManagerInitialization()
        {
            Output.WriteLine("... Notifying remote manager [initialization]");

            Uri address = new Uri("http://localhost:8000/manager/");

            var binding = new WSHttpBinding();
            var endpoint = new EndpointAddress(address);

            Container.Channel = ChannelFactory<IManagerService>.CreateChannel(binding, endpoint);
            Container.Channel.NotifyInitialized(Configuration.ContainerId);
        }

        #endregion

        #region helper methods
        
        /// <summary>
        /// Finds the entry point to the P# program.
        /// </summary>
        /// <param name="assembly">Assembly</param>
        /// <returns>MethodInfo</returns>
        private static MethodInfo FindEntryPoint(Assembly assembly)
        {
            var entrypoints = assembly.GetTypes().SelectMany(t => t.GetMethods()).
                Where(m => m.GetCustomAttributes(typeof(EntryPoint), false).Length > 0).ToList();
            if (entrypoints.Count == 0)
            {
                ErrorReporter.ReportAndExit("No entry point found to the P# program. " +
                    "Use the attribute [EntryPoint] to declare an entry point method.");
            }
            else if (entrypoints.Count > 1)
            {
                ErrorReporter.ReportAndExit("Only one entry point to the P# program can be declared. " +
                    "{0} entry points were found instead.", entrypoints.Count);
            }

            return entrypoints[0];
        }

        #endregion
    }
}
