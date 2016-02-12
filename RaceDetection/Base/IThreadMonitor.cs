/********************************************************
*                                                       *
*     Copyright (C) Microsoft. All rights reserved.     *
*                                                       *
********************************************************/

// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ExtendedReflection.Collections;
using Microsoft.ExtendedReflection.Monitoring;

namespace EREngine.CallsOnly
{
    /// <summary>
    /// Stripped-down version of <see cref="IThreadExecutionMonitor"/>.
    /// </summary>
    internal interface IThreadMonitor
    {
        #region Memory access addresses
        /// <summary>
        /// This method is called when a (non-local) memory location is loaded from.
        /// </summary>
        /// <param name="location">An identifier of the memory address.</param>
        /// <param name="size">The size of the data loaded.</param>
        /// <param name="volatile">indicates if the access is volatile</param>
        /// <returns>an exception that should be thrown, if any</returns>
        Exception Load(UIntPtr location, uint size, bool @volatile);

        /// <summary>
        /// This method is called when a (non-local) memory location is stored to.
        /// </summary>
        /// <param name="location">An identifier of the memory address.</param>
        /// <param name="size">The size of the data stored.</param>
        /// <param name="volatile">indicates if the access is volatile</param>
        /// <returns>an exception that should be thrown, if any</returns>
        Exception Store(UIntPtr location, uint size, bool @volatile);
        #endregion

        /// <summary>
        /// This method is called after an object allocation
        /// </summary>
        /// <param name="newObject">allocated object</param>
        /// <returns></returns>
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
