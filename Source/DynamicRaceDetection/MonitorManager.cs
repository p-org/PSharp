//-----------------------------------------------------------------------
// <copyright file="MonitorManager.cs">
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

using System;

using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.Utilities.Safe.Diagnostics;

using Microsoft.PSharp.DynamicRaceDetection.ComponentModel;
using Microsoft.PSharp.DynamicRaceDetection.CallsOnly;

namespace Microsoft.PSharp.DynamicRaceDetection
{
    /// <summary>
    /// Manager of registered monitors
    /// </summary>
    internal class MonitorManager
        : CopComponentBase
        , IMonitorManager
        , IExecutionMonitor
    {
        /// <summary>
        /// (Calls-only)
        /// </summary>
        internal ThreadMonitorManager ThreadMonitorManager
        {
            get { return this.GetService<ThreadMonitorManager>(); }
        }

        private ThreadMonitorFactory threadMonitorFactory;
        internal ThreadMonitorFactory ThreadMonitorFactory
        {
            get
            {
                if (this.threadMonitorFactory == null)
                {
                    this.threadMonitorFactory = new ThreadMonitorFactory(this.ThreadMonitorManager);
                    this.ThreadMonitorManager.AddMonitorFactory(this.threadMonitorFactory);
                }
                return this.threadMonitorFactory;
            }
        }

        /// <summary>
        /// Clears the execution monitors monitor.
        /// </summary>
        void IMonitorManager.DisposeExecutionMonitors()
        {
        }

        /// <summary>
        /// Registers the thread monitor.
        /// </summary>
        /// <param name="threadMonitor">The thread monitor.</param>
        void IMonitorManager.RegisterThreadMonitor(IThreadMonitor threadMonitor)
        {
            if (threadMonitor == null)
                throw new ArgumentNullException("threadMonitor");
            this.ThreadMonitorFactory.Monitors.Add(threadMonitor);
        }

        /// <summary>
        /// Registers the object access thread monitor.
        /// </summary>
        void IMonitorManager.RegisterObjectAccessThreadMonitor()
        {
            // ensure we only have 1
            foreach (IThreadMonitor monitor in this.ThreadMonitorFactory.Monitors)
                if (monitor is ObjectAccessThreadMonitor)
                    return;

            this.ThreadMonitorFactory.Monitors.Add(new ObjectAccessThreadMonitor(this.ThreadMonitorManager));
        }

        /// <summary>
        /// Registers the thread monitor factory.
        /// </summary>
        /// <param name="monitorFactory">The monitor factory.</param>
        void IMonitorManager.RegisterThreadMonitorFactory(IThreadMonitorFactory monitorFactory)
        {
            SafeDebug.AssertNotNull(monitorFactory, "monitorFactory");
            this.ThreadMonitorManager.AddMonitorFactory(monitorFactory);
        }

        /// <summary>
        /// Create a new thread execution monitor, 
        /// which multiplexes each callback to every 
        /// subscribed child execution monitor.
        /// </summary>
        int IExecutionMonitor.CreateThread()
        {
            int threadId = this.ThreadMonitorManager.CreateThread();
            return threadId;
        }

        /// <summary>
        /// Retrieve existing thread. 
        /// </summary>
        IThreadExecutionMonitor IExecutionMonitor.GetThreadExecutionMonitor(int threadIndex)
        {
            return this.ThreadMonitorManager.GetThread(threadIndex);
        }

        void IExecutionMonitor.Initialize()
        {

        }

        void IExecutionMonitor.BeforeMain()
        {
        }

        void IExecutionMonitor.Terminate()
        {
            this.ThreadMonitorFactory.RunCompleted();
        }

        /// <summary>
        /// Destroys a previously created thread.
        /// </summary>
        void IExecutionMonitor.DestroyThread(int index)
        {
            this.ThreadMonitorManager.DestroyThread(index);
        }

        #region Injection (disabled)
        IExecutionValueInjector IExecutionMonitor.ValueInjector
        {
            get { return ThreadExecutionValueInjectorEmpty.Instance; }
        }
        #endregion
    }
}
