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
using Microsoft.ExtendedReflection.Logging;
using Microsoft.ExtendedReflection.Collections;
using Microsoft.ExtendedReflection.Utilities.Safe.Diagnostics;
using Microsoft.ExtendedReflection.Monitoring;
using EREngine.ComponentModel;
using EREngine.AllCallbacks;

namespace EREngine.CallsOnly
{
    /// <summary>
    /// Multiplexes calls to all registered call monitors.
    /// </summary>
    internal sealed class ThreadMonitorFactory
        : ThreadMonitorBase
        , IThreadMonitorFactory
    {
        private readonly ThreadMonitorCollection monitors = new ThreadMonitorCollection();
        public ThreadMonitorCollection Monitors
        {
            get { return this.monitors; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ThreadMonitorFactory(ICopComponent host)
            : base(host)
        { }

        public override Exception Load(UIntPtr location, uint size, bool @volatile)
        {
            try
            {
                foreach (IThreadMonitor callMonitor in this.Monitors)
                {
                    Exception exceptionToThrow = callMonitor.Load(location, size, @volatile);
                    if (exceptionToThrow != null)
                        return exceptionToThrow;
                }
            }
            catch (Exception ex)
            {
                this.Host.Log.LogErrorFromException(
                    ex,
                    null,
                    "CallMonitor",
                    "unexpected error occurred");
            }
            return null;
        }

        public override Exception Store(UIntPtr location, uint size, bool @volatile)
        {
            try
            {
                foreach (IThreadMonitor callMonitor in this.Monitors)
                {
                    Exception exceptionToThrow = callMonitor.Store(location, size, @volatile);
                    if (exceptionToThrow != null)
                        return exceptionToThrow;
                }
            }
            catch (Exception ex)
            {
                this.Host.Log.LogErrorFromException(
                    ex,
                    null,
                    "CallMonitor",
                    "unexpected error occurred");
            }
            return null;
        }

        public override Exception ObjectAllocationAccess(object newObject)
        {
            try
            {
                foreach (IThreadMonitor callMonitor in this.Monitors)
                {
                    Exception exceptionToThrow = callMonitor.ObjectAllocationAccess(newObject);
                    if (exceptionToThrow != null)
                        return exceptionToThrow;
                }
            }
            catch (Exception ex)
            {
                this.Host.Log.LogErrorFromException(
                    ex,
                    null,
                    "CallMonitor",
                    "unexpected error occurred");
            }
            return null;
        }

        /// <summary>
        /// Never called.
        /// </summary>
        public override void DisposeTesteeReferences()
        {

        }

        public override void Destroy()
        {

        }

        public override void RunCompleted()
        {
            try
            {
                foreach (IThreadMonitor callMonitor in this.Monitors)
                {
                    callMonitor.DisposeTesteeReferences();
                }
            }
            catch (Exception ex)
            {
                this.Host.Log.LogErrorFromException(
                    ex,
                    null,
                    "CallMonitor",
                    "unexpected error occurred");
            }



            // Print summaries
            try
            {
                foreach (IThreadMonitor callMonitor in this.Monitors)
                {
                    callMonitor.RunCompleted();
                }
            }
            catch (Exception ex)
            {
                this.Host.Log.LogErrorFromException(
                    ex,
                    null,
                    "CallMonitor",
                    "unexpected error occurred");
            }
        }

        bool IThreadMonitorFactory.TryCreateThreadMonitor(int threadID, out IThreadExecutionMonitor monitor)
        {
            if (this.Monitors.Count == 0)
            {
                monitor = null;
                return false;
            }
            else
            {
                monitor = new ThreadExecutionMonitorDispatcher(
                    this.Host.Log,
                    threadID,
                    this);
                return true;
            }
        }
    }
}
