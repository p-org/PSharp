// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.ExtendedReflection.Monitoring;

using Microsoft.PSharp.Monitoring.AllCallbacks;
using Microsoft.PSharp.Monitoring.ComponentModel;
using Microsoft.PSharp.Utilities;
using Microsoft.PSharp.TestingServices;

namespace Microsoft.PSharp.Monitoring.CallsOnly
{
    /// <summary>
    /// Multiplexes calls to all registered call monitors.
    /// </summary>
    internal sealed class ThreadMonitorFactory : ThreadMonitorBase, IThreadMonitorFactory
    {
        #region fields

        /// <summary>
        /// The P# configuration.
        /// </summary>
        private Configuration Configuration;

        private IRegisterRuntimeOperation RaceDetectionEngine;

        /// <summary>
        /// Thread monitors.
        /// </summary>
        private readonly ThreadMonitorCollection ThreadMonitors;

        /// <summary>
        /// Thread monitors.
        /// </summary>
        public ThreadMonitorCollection Monitors
        {
            get
            {
                return this.ThreadMonitors;
            }
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="host">ICopComponent</param>
        /// <param name="configuration">Configuration</param>
        public ThreadMonitorFactory(ICopComponent host, IRegisterRuntimeOperation raceDetectionEngine,
            Configuration configuration)
            : base(host)
        {
            this.Configuration = configuration;
            this.RaceDetectionEngine = raceDetectionEngine;
            this.ThreadMonitors = new ThreadMonitorCollection();
        }

        #endregion

        #region methods

        public override Exception Load(UIntPtr location, uint size, bool @volatile)
        {
            try
            {
                foreach (IThreadMonitor callMonitor in this.Monitors)
                {
                    Exception exceptionToThrow = callMonitor.Load(location, size, @volatile);
                    if (exceptionToThrow != null)
                    {
                        return exceptionToThrow;
                    }
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
                    {
                        return exceptionToThrow;
                    }
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
                    {
                        return exceptionToThrow;
                    }
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

        /// <summary>
        /// Tries to create a new thread monitor.
        /// </summary>
        /// <param name="threadID">ThreadId</param>
        /// <param name="monitor">IThreadExecutionMonitor</param>
        /// <returns>Boolean</returns>
        bool IThreadMonitorFactory.TryCreateThreadMonitor(int threadID,
            out IThreadExecutionMonitor monitor)
        {
            if (this.Monitors.Count == 0)
            {
                monitor = null;
                return false;
            }
            else
            {
                monitor = new ThreadExecutionMonitorDispatcher(this.Host.Log,
                    threadID, this, this.RaceDetectionEngine, this.Configuration);
                return true;
            }
        }

        #endregion
    }
}
