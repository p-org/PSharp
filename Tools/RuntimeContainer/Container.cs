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

using Microsoft.PSharp.Net;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.Remote
{
    /// <summary>
    /// Implements a runtime container.
    /// </summary>
    internal static class Container
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
        /// The request listening service.
        /// </summary>
        private static ServiceHost RequestService;

        /// <summary>
        /// Network provider for remote communication.
        /// </summary>
        private static InterProcessNetworkProvider NetworkProvider;

        /// <summary>
        /// Channel for remote communication.
        /// </summary>
        internal static IManagerService Channel;

        /// <summary>
        /// The remote application assembly.
        /// </summary>
        private static Assembly RemoteApplicationAssembly;

        #endregion

        #region internal API

        /// <summary>
        /// Configures the container.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        internal static void Configure(Configuration configuration)
        {
            Container.Configuration = configuration;

            IO.PrettyPrintLine(". Setting up the runtime container");

            Container.LoadApplicationAssembly();
            Container.RegisterSerializableTypes();

            IO.PrettyPrintLine("... Configured as container '{0}'", Configuration.ContainerId);
        }

        /// <summary>
        /// Runs the container.
        /// </summary>
        internal static void Run()
        {
            IO.PrettyPrintLine(". Running");

            Container.OpenNotificationListener();
            Container.CreateNetworkProvider();
            Container.InitializePSharpRuntime();

            Console.ReadLine();

            Container.RequestService.Close();
            Container.NotificationService.Close();

            IO.PrettyPrintLine(". Closed listeners");
        }

        /// <summary>
        /// Invoke the distributed runtime.
        /// </summary>
        internal static void InvokeDistributedRuntime()
        {
            Task.Factory.StartNew(() =>
            {
                IO.PrettyPrintLine("... Starting P# runtime");

                MethodInfo entry = Container.FindEntryPointOfRemoteApplication();
                entry.Invoke(null, null);
            });
        }

        #endregion

        #region private methods

        /// <summary>
        /// Loads the application assembly.
        /// </summary>
        private static void LoadApplicationAssembly()
        {
            try
            {
                Container.RemoteApplicationAssembly = Assembly.LoadFrom(
                    Configuration.RemoteApplicationFilePath);
            }
            catch (FileNotFoundException ex)
            {
                ErrorReporter.ReportAndExit(ex.Message);
            }
        }

        /// <summary>
        /// Registers all discoverable serializable types.
        /// </summary>
        private static void RegisterSerializableTypes()
        {
            var eventTypes = from type in Container.RemoteApplicationAssembly.GetTypes()
                             where type.IsSubclassOf(typeof(Event))
                             select type;
            KnownTypesProvider.KnownTypes.AddRange(eventTypes);
        }

        /// <summary>
        /// Opens the remote notification listener.
        /// </summary>
        private static void OpenNotificationListener()
        {
            IO.PrettyPrintLine("... Opening notification listener");

            Uri address = new Uri("http://localhost:8000/notify/" + Configuration.ContainerId + "/");
            WSHttpBinding binding = new WSHttpBinding();

            Container.NotificationService = new ServiceHost(typeof(NotificationListener));

            //host.Description.Behaviors.Remove(typeof(ServiceDebugBehavior));
            //host.Description.Behaviors.Add(new ServiceDebugBehavior {
            //    IncludeExceptionDetailInFaults = true });

            Container.NotificationService.AddServiceEndpoint(typeof(IContainerService), binding, address);
            Container.NotificationService.Open();
        }

        /// <summary>
        /// Creates the P# network provider.
        /// </summary>
        private static void CreateNetworkProvider()
        {
            IO.PrettyPrintLine("... Creating network provider");

            Container.NetworkProvider = new InterProcessNetworkProvider("localhost", "8000");
            Container.RequestService = new ServiceHost(Container.NetworkProvider);

            //host.Description.Behaviors.Remove(typeof(ServiceDebugBehavior));
            //host.Description.Behaviors.Add(new ServiceDebugBehavior {
            //    IncludeExceptionDetailInFaults = true });

            Uri address = new Uri("http://localhost:8000/request/" + Configuration.ContainerId + "/");
            WSHttpBinding binding = new WSHttpBinding();

            Container.RequestService.AddServiceEndpoint(typeof(IRemoteCommunication), binding, address);
            Container.RequestService.Open();
        }

        /// <summary>
        /// Initializes the P# runtime.
        /// </summary>
        private static void InitializePSharpRuntime()
        {
            PSharpRuntime runtime = PSharpRuntime.Create(Container.Configuration, Container.NetworkProvider);

            Container.NetworkProvider.Initialize(runtime, Container.RemoteApplicationAssembly);
            Container.NotifyManagerInitialization();
        }

        /// <summary>
        /// Notifies the remote manager about successful initialization.
        /// </summary>
        private static void NotifyManagerInitialization()
        {
            IO.PrettyPrintLine("... Notifying remote manager [initialization]");

            Uri address = new Uri("http://localhost:8000/manager/");

            WSHttpBinding binding = new WSHttpBinding();
            EndpointAddress endpoint = new EndpointAddress(address);

            Container.Channel = ChannelFactory<IManagerService>.CreateChannel(binding, endpoint);
            Container.Channel.NotifyInitialized(Configuration.ContainerId);
        }

        #endregion

        #region helper methods
        
        /// <summary>
        /// Finds the entry point to the P# program.
        /// </summary>
        /// <returns>MethodInfo</returns>
        private static MethodInfo FindEntryPointOfRemoteApplication()
        {
            var entrypoints = Container.RemoteApplicationAssembly.GetTypes().SelectMany(t => t.GetMethods()).
                Where(m => m.GetCustomAttributes(typeof(Test), false).Length > 0).ToList();
            if (entrypoints.Count == 0)
            {
                ErrorReporter.ReportAndExit("Cannot detect a P# test method. " +
                    "Use the attribute [Test] to declare a test method.");
            }
            else if (entrypoints.Count > 1)
            {
                ErrorReporter.ReportAndExit("Only one test method to the P# program can be declared. " +
                    "{0} test methods were found instead.", entrypoints.Count);
            }

            return entrypoints[0];
        }

        #endregion
    }
}
