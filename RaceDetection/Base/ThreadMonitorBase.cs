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
using Microsoft.ExtendedReflection.Metadata;
using Microsoft.ExtendedReflection.Collections;
using Microsoft.ExtendedReflection.XmlDocumentation;
using Microsoft.ExtendedReflection.Utilities.Safe.Diagnostics;
using Microsoft.ExtendedReflection.Logging;
using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.ComponentModel;
using EREngine.ComponentModel;

namespace EREngine.CallsOnly
{
    /// <summary>
    /// Abstract base class to help implement
    /// <see cref="IThreadMonitor"/>
    /// </summary>
    internal abstract class ThreadMonitorBase
        : ComponentElementBase
        , IThreadMonitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadMonitorBase"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        protected ThreadMonitorBase(ICopComponent host)
            : base(host)
        {
        }

        #region Memory access addresses
        /// <summary>
        /// This method is called when a (non-local) memory location is loaded from.
        /// </summary>
        /// <param name="location">An identifier of the memory address.</param>
        /// <param name="size">The size of the data loaded.</param>
        /// <param name="volatile">indicates if the access is volatile</param>
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public virtual Exception Load(UIntPtr location, uint size, bool @volatile) { return null; }

        /// <summary>
        /// This method is called when a (non-local) memory location is stored to.
        /// </summary>
        /// <param name="location">An identifier of the memory address.</param>
        /// <param name="size">The size of the data stored.</param>
        /// <param name="volatile">indicates if the access is volatile</param>
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public virtual Exception Store(UIntPtr location, uint size, bool @volatile) { return null; }
        #endregion

        /// <summary>
        /// This method is called after an object allocation
        /// </summary>
        /// <param name="newObject">allocated object</param>
        /// <returns></returns>
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public virtual Exception ObjectAllocationAccess(object newObject) { return null; }

        /// <summary>
        /// Null out references to the testee so that the dispose tracker
        /// can do its work.
        /// This is a good place to log details.
        /// </summary>
        /// <remarks>
        /// Only RunCompleted is called afterwards.
        /// </remarks>
        public virtual void DisposeTesteeReferences() { }

        /// <summary>
        /// Program under test terminated.
        /// This is a good place to log summaries.
        /// </summary>
        public virtual void RunCompleted() { }

        /// <summary>
        /// Thread destroyed
        /// </summary>
        public virtual void Destroy() { }
    }
}
