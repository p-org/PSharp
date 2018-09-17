// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.Monitoring.CallsOnly
{
    /// <summary>
    /// Stripped-down version of IThreadExecutionMonitor.
    /// </summary>
    internal interface IThreadMonitor
    {
        /// <summary>
        /// This method is called when a (non-local) memory location is loaded from.
        /// </summary>
        /// <param name="location">An identifier of the memory address</param>
        /// <param name="size">The size of the data loaded</param>
        /// <param name="volatile">Indicates if the access is volatile</param>
        /// <returns>Exception</returns>
        Exception Load(UIntPtr location, uint size, bool @volatile);

        /// <summary>
        /// This method is called when a (non-local) memory location is stored to.
        /// </summary>
        /// <param name="location">An identifier of the memory address</param>
        /// <param name="size">The size of the data stored</param>
        /// <param name="volatile">Indicates if the access is volatile</param>
        /// <returns>Exception</returns>
        Exception Store(UIntPtr location, uint size, bool @volatile);

        /// <summary>
        /// This method is called after an object allocation.
        /// </summary>
        /// <param name="newObject">Object</param>
        /// <returns>Exception</returns>
        Exception ObjectAllocationAccess(object newObject);
        
        /// <summary>
        /// Null out references to the testee so that the dispose tracker
        /// can do its work.
        /// This is a good place to log details.
        /// </summary>
        /// <remarks>
        /// Only RunCompleted is called afterwards.
        /// </remarks>
        void DisposeTesteeReferences();

        /// <summary>
        /// Program under test terminated.
        /// This is a good place to log summaries.
        /// </summary>
        void RunCompleted();

        /// <summary>
        /// Destroy this thread.
        /// </summary>
        void Destroy();
    }
}
