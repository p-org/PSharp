//-----------------------------------------------------------------------
// <copyright file="ThreadMonitorManager.cs">
//      Copyright (c) 2016 Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.Collections;
using Microsoft.ExtendedReflection.Utilities.Safe.Diagnostics;

using Microsoft.PSharp.Monitoring.ComponentModel;

namespace Microsoft.PSharp.Monitoring
{
    internal sealed class ThreadMonitorManager : CopComponentBase, IInternalService
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

            SafeList<IThreadExecutionMonitor> childExecutionMonitors = new SafeList<IThreadExecutionMonitor>(2); // all callbacks

            foreach (var monitorFactory in this.monitorFactories)
            {
                IThreadExecutionMonitor monitor;
                if (monitorFactory.TryCreateThreadMonitor(threadId, out monitor))
                {
                    childExecutionMonitors.Add(monitor);
                }
            }

            this.executionMonitors[threadId] =
                new ThreadExecutionMonitorMultiplexer(childExecutionMonitors);

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

