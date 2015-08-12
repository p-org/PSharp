//-----------------------------------------------------------------------
// <copyright file="Tasks.cs" company="Microsoft">
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
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Threading
{
    /// <summary>
    /// Class implementing a P# task machine.
    /// </summary>
    public static class Tasks
    {
        #region fields

        /// <summary>
        /// The P# task machine scheduler.
        /// </summary>
        internal static TaskScheduler TaskMachineScheduler;

        #endregion

        #region P# task API

        /// <summary>
        /// Static constructor.
        /// </summary>
        static Tasks()
        {
            Tasks.TaskMachineScheduler = TaskScheduler.Default;
        }

        #endregion
    }
}
