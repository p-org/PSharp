//-----------------------------------------------------------------------
// <copyright file="IContainerService.cs" company="Microsoft">
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

using System.ServiceModel;

namespace Microsoft.PSharp.Remote
{
    /// <summary>
    /// Interface for sending notifications to a container.
    /// </summary>
    [ServiceContract(Namespace = "Microsoft.PSharp")]
    internal interface IContainerService
    {
        /// <summary>
        /// Notifies the container to start the runtime.
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void NotifyStartRuntime();

        /// <summary>
        /// Notifies the container to exit.
        /// </summary>
        [OperationContract(IsOneWay = true)]
        void NotifyExit();
    }
}
