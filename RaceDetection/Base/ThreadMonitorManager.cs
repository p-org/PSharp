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
using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.Collections;
using Microsoft.ExtendedReflection.ComponentModel;
using Microsoft.ExtendedReflection.Utilities.Safe.Diagnostics;
using System.Diagnostics;
using EREngine.ComponentModel;

namespace EREngine
{
    internal sealed class ThreadMonitorManager
        : CopComponentBase
        , IInternalService
    {
        private readonly SafeList<IThreadMonitorFactory> monitorFactories = new SafeList<IThreadMonitorFactory>();

        public void AddMonitorFactory(IThreadMonitorFactory monitorFactory)
        {
            SafeDebug.AssumeNotNull(monitorFactory, "monitorFactory");
            this.monitorFactories.Add(monitorFactory);
        }

        private readonly SafeList<ThreadExecutionMonitorMultiplexer> executionMonitors = new SafeList<ThreadExecutionMonitorMultiplexer>();
        private readonly SafeQueue<int> destroyedExecutionMonitorIds = new SafeQueue<int>();
        public IEnumerable<ThreadExecutionMonitorMultiplexer> ExecutionMonitors
        {
            get { return this.executionMonitors; }
        }

        public IThreadExecutionMonitor GetThread(int threadID)
        {
            return this.executionMonitors[threadID];
        }

        public int CreateThread()
        {
            int threadId;
            if (!this.destroyedExecutionMonitorIds.TryDequeue(out threadId))
            {
                threadId = this.executionMonitors.Count;
                this.executionMonitors.Add(null);
            }
            SafeDebug.Assert(this.executionMonitors[threadId] == null, "this.destroyedExecutionMonitorIds[threadId] == null");

            SafeList<IThreadExecutionMonitor> childExecutionMonitors = new SafeList<IThreadExecutionMonitor>(2);    //all callbacks

            foreach (var monitorFactory in this.monitorFactories)
            {
                IThreadExecutionMonitor monitor;
                if (monitorFactory.TryCreateThreadMonitor(threadId, out monitor))
                    childExecutionMonitors.Add(monitor);

            }

            this.executionMonitors[threadId] =
                new ThreadExecutionMonitorMultiplexer(
                    childExecutionMonitors
                    );

            return threadId;
        }

        public void DestroyThread(int index)
        {
            SafeDebug.Assert(this.executionMonitors[index] != null, "this.executionMonitors[index] != null");
            IThreadExecutionMonitor m = this.executionMonitors[index];
            m.Destroy();
            this.destroyedExecutionMonitorIds.Enqueue(index);
            this.executionMonitors[index] = null;
        }
    }
}

