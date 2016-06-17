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
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.Monitoring
{
    /// <summary>
    /// Class implementing a thread monitor manager.
    /// </summary>
    internal sealed class ThreadMonitorManager : CopComponentBase, IInternalService
    {
        #region fields

        /// <summary>
        /// The P# configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// The monitor factories.
        /// </summary>
        private readonly SafeList<IThreadMonitorFactory> MonitorFactories;

        /// <summary>
        /// The thread execution monitors.
        /// </summary>
        private readonly SafeList<ThreadExecutionMonitorMultiplexer> ThreadExecutionMonitors;

        /// <summary>
        /// The destroyed execution monitor ids.
        /// </summary>
        private readonly SafeQueue<int> DestroyedExecutionMonitorIds;

        /// <summary>
        /// Collection of execution monitors.
        /// </summary>
        public IEnumerable<ThreadExecutionMonitorMultiplexer> ExecutionMonitors
        {
            get { return this.ThreadExecutionMonitors; }
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public ThreadMonitorManager(Configuration configuration)
            : base()
        {
            this.Configuration = configuration;
            this.MonitorFactories = new SafeList<IThreadMonitorFactory>();
            this.ThreadExecutionMonitors = new SafeList<ThreadExecutionMonitorMultiplexer>();
            this.DestroyedExecutionMonitorIds = new SafeQueue<int>();
        }

        #endregion

        #region methods

        /// <summary>
        /// Adds the monitor to the specified factory.
        /// </summary>
        /// <param name="monitorFactory">IThreadMonitorFactory</param>
        public void AddMonitorFactory(IThreadMonitorFactory monitorFactory)
        {
            SafeDebug.AssumeNotNull(monitorFactory, "monitorFactory");
            this.MonitorFactories.Add(monitorFactory);
        }

        /// <summary>
        /// Gets the thread with the specified id.
        /// </summary>
        /// <param name="threadID"></param>
        /// <returns></returns>
        public IThreadExecutionMonitor GetThread(int threadID)
        {
            return this.ThreadExecutionMonitors[threadID];
        }

        /// <summary>
        /// Creates a thread.
        /// </summary>
        /// <returns>ThreadId</returns>
        public int CreateThread()
        {
            int threadId;
            if (!this.DestroyedExecutionMonitorIds.TryDequeue(out threadId))
            {
                threadId = this.ThreadExecutionMonitors.Count;
                this.ThreadExecutionMonitors.Add(null);
            }

            SafeDebug.Assert(this.ThreadExecutionMonitors[threadId] == null,
                "this.destroyedExecutionMonitorIds[threadId] == null");

            SafeList<IThreadExecutionMonitor> childExecutionMonitors =
                new SafeList<IThreadExecutionMonitor>(2); // all callbacks

            foreach (var monitorFactory in this.MonitorFactories)
            {
                IThreadExecutionMonitor monitor;
                if (monitorFactory.TryCreateThreadMonitor(threadId, out monitor))
                {
                    childExecutionMonitors.Add(monitor);
                }
            }

            this.ThreadExecutionMonitors[threadId] =
                new ThreadExecutionMonitorMultiplexer(childExecutionMonitors);

            return threadId;
        }

        /// <summary>
        /// Destroys the specified thread.
        /// </summary>
        /// <param name="index">Index</param>
        public void DestroyThread(int index)
        {
            SafeDebug.Assert(this.ThreadExecutionMonitors[index] != null,
                "this.executionMonitors[index] != null");
            IThreadExecutionMonitor m = this.ThreadExecutionMonitors[index];
            m.Destroy();
            this.DestroyedExecutionMonitorIds.Enqueue(index);
            this.ThreadExecutionMonitors[index] = null;
        }

        #endregion
    }
}

