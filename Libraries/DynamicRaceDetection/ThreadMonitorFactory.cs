//-----------------------------------------------------------------------
// <copyright file="ThreadMonitorFactory.cs">
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

using Microsoft.PSharp.DynamicRaceDetection.AllCallbacks;
using Microsoft.PSharp.DynamicRaceDetection.ComponentModel;

namespace Microsoft.PSharp.DynamicRaceDetection.CallsOnly
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
        /// Constructor.
        /// </summary>
        public ThreadMonitorFactory(ICopComponent host) : base(host) { }

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
