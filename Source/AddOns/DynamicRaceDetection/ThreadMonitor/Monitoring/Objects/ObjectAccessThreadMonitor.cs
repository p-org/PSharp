// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;

using Microsoft.PSharp.Monitoring.ComponentModel;

namespace Microsoft.PSharp.Monitoring.CallsOnly
{
    /// <summary>
    /// Memory access monitor.
    /// </summary>
    /// <remarks>
    /// Notifies listener of read/write events.
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
        /// <param name="host">ICopComponent</param>
        internal ObjectAccessThreadMonitor(ICopComponent host)
            : base(host)
        { }

        /// <summary>
        /// Notifies a write access.
        /// </summary>
        /// <param name="interiorPointer">UIntPtr</param>
        /// <param name="size">Dize</param>
        /// <param name="volatile">Indicates if the access is volatile</param>
        /// <returns>Exception</returns>
        [DebuggerNonUserCode]
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
        /// Notifies a read access.
        /// </summary>
        /// <param name="interiorPointer">UIntPtr</param>
        /// <param name="size">Size</param>
        /// <param name="volatile">Indicates if the access is volatile</param>
        /// <returns>Exception</returns>
        [DebuggerNonUserCode]
        public override Exception Load(UIntPtr interiorPointer, uint size, bool @volatile)
        {
            RawAccessHandler rh = ReadRawAccess;

            if (rh != null)
            {
                return rh(interiorPointer, size, @volatile);
            }

            return null;
        }

        /// <summary>
        /// Notifies an object allocation.
        /// </summary>
        /// <param name="newObject">Object</param>
        /// <returns>Exception</returns>
        [DebuggerNonUserCode]
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
