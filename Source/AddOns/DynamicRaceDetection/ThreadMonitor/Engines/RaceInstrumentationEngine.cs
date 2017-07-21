//-----------------------------------------------------------------------
// <copyright file="RaceInstrumentationEngine.cs">
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
using Microsoft.ExtendedReflection.Symbols;
using Microsoft.ExtendedReflection.ComponentModel;

using Microsoft.PSharp.Monitoring.CallsOnly;
using Microsoft.PSharp.Utilities;
using Microsoft.PSharp.TestingServices;

namespace Microsoft.PSharp.Monitoring
{
    /// <summary>
    /// Race instrumentation engine.
    /// </summary>
    internal sealed class RaceInstrumentationEngine : Engine
    {
        #region classes

        /// <summary>
        /// A stack frame filter.
        /// </summary>
        private sealed class StackFrameFilter : IStackFrameFilter
        {
            public bool Exclude(StackFrameName frame)
            {
                string v = frame.Value;
                return v.Contains("Microsoft.ExtendedReflection") ||
                    v.Contains("___redirect") ||
                    v.Contains("___lateredirect") ||
                    v.Contains("__Substitutions") ||
                    v.Contains("mscorlib") ||
                    v.Contains("System.Reflection");
            }
        }

        #endregion

        #region fields

        /// <summary>
        /// The singleton engine.
        /// </summary>
        private static RaceInstrumentationEngine SingletonEngine;

        /// <summary>
        /// The execution monitor.
        /// </summary>
        private IExecutionMonitor ExecutionMonitor
        {
            get { return this.GetService<MonitorManager>(); }
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="raceDetectionEngine">IRaceDetectionEngine</param>
        /// <param name="configuration">Configuration</param>
        public RaceInstrumentationEngine(IRegisterRuntimeOperation raceDetectionEngine, Configuration configuration)
            : base(new Container(), new EngineOptions(),
                  new MonitorManager(raceDetectionEngine, configuration),
                  new ThreadMonitorManager(configuration))
        {
            if (SingletonEngine != null)
            {
                throw new InvalidOperationException("RaceInstrumentationEngine created more than once.");
            }

            SingletonEngine = this;

            // required?
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomainProcessExit);
            this.GetService<ISymbolManager>().AddStackFrameFilter(new StackFrameFilter());

            if (!ControllerEnvironment.IsMonitoringEnabled)
            {
                Console.WriteLine("ExtendedReflection monitor not enabled");
                throw new NotImplementedException("ExtendedReflection monitor not enabled");
            }

            ((IMonitorManager)this.GetService<MonitorManager>()).RegisterThreadMonitor(
               new ThreadMonitorFactory(this.GetService<ThreadMonitorManager>(),
               raceDetectionEngine, configuration));

            ((IMonitorManager)this.GetService<MonitorManager>()).RegisterObjectAccessThreadMonitor();

            this.ExecutionMonitor.Initialize();
            var tid = this.ExecutionMonitor.CreateThread();
            _ThreadContext.Start(this.ExecutionMonitor, tid);
        }

        #endregion

        #region methods
        
        /// <summary>
        /// Adds the symbol manager.
        /// </summary>
        protected override void AddSymbolManager()
        {
            base.AddSymbolManager();
        }

        /// <summary>
        /// Adds the components.
        /// </summary>
        protected override void AddComponents()
        {
            base.AddComponents();
        }

        /// <summary>
        /// Disposes the execution monitors.
        /// </summary>
        public static void DisposeExecutionMonitors()
        {
            SingletonEngine.GetService<IMonitorManager>().DisposeExecutionMonitors();
        }

        /// <summary>
        /// Event handler.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">EventArgs</param>
        void CurrentDomainProcessExit(object sender, EventArgs e)
        {
            this.ExecutionMonitor.Terminate();
            this.Log.Close();
        }

        #endregion
    }
}
