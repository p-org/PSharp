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
using System.Diagnostics;
using Microsoft.ExtendedReflection.Utilities.Safe.Diagnostics;
using Microsoft.ExtendedReflection.Monitoring;
using EREngine.ComponentModel;

namespace EREngine.CallsOnly
{
    /// <summary>
    /// Memory access monitor. 
    /// to enabled.
    /// </summary>
    /// <remarks>
    /// Notifes listener of read/write events
    /// </remarks>
    internal sealed class ObjectAccessThreadMonitor : ThreadMonitorBase
    {
        /// <summary>
        /// Raised on memory read access. Returns exception to throw if any.
        /// </summary>
        public static event RawAccessHandler ReadRawAccess;

        /// <summary>
        /// Raised on memory write access. Returns exception to throw if any.
        /// </summary>
        public static event RawAccessHandler WriteRawAccess;

        /// <summary>
        /// Raised after object allocation. Returns exception to throw if any.
        /// </summary>
        public static event ObjectAllocationHandler ObjectAllocationHandlerEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectAccessThreadMonitor"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        internal ObjectAccessThreadMonitor(ICopComponent host)
            : base(host)
        { }

        /// <summary>
        /// Notifies a write access
        /// </summary>
        /// <param name="interiorPointer">The interior pointer.</param>
        /// <param name="size">The size.</param>
        /// <param name="volatile">indicates if the access is volatile</param>
        /// <returns></returns>
        [DebuggerNonUserCodeAttribute]
        public override Exception Store(UIntPtr interiorPointer, uint size, bool @volatile)
        {
            RawAccessHandler rh = WriteRawAccess;
            if (rh != null)
            {
                return rh(interiorPointer, size, @volatile);
            }
            return null;
        }

        /// <summary>
        /// Notifies a read access
        /// </summary>
        /// <param name="interiorPointer">The interior pointer.</param>
        /// <param name="size">The size.</param>
        /// <param name="volatile">indicates if the access is volatile</param>
        /// <returns></returns>
        [DebuggerNonUserCodeAttribute]
        public override Exception Load(UIntPtr interiorPointer, uint size, bool @volatile)
        {
            RawAccessHandler rh = ReadRawAccess;
            if (rh != null)
            {
                return rh(interiorPointer, size, @volatile);
            }
            return null;
        }

        [DebuggerNonUserCodeAttribute]
        public override Exception ObjectAllocationAccess(object newObject)
        {
            ObjectAllocationHandler rh = ObjectAllocationHandlerEvent;
            if (rh != null)
            {
                return rh(newObject);
            }
            return null;
        }
    }
}
