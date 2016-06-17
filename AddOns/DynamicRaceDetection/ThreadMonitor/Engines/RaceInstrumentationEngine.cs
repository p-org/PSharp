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
using System.Diagnostics;

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
        private static RaceInstrumentationEngine SingletonEngine;

        public static void DisposeExecutionMonitors()
        {
            Debug.Assert(RaceInstrumentationEngine.SingletonEngine != null);
            RaceInstrumentationEngine.SingletonEngine.GetService<IMonitorManager>().DisposeExecutionMonitors();
        }

        /// <summary>
        /// Initializes a new instance of the race instrumentation engine.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public RaceInstrumentationEngine(ITestingEngine testingEngine, Configuration configuration)
            : base(new Container(), new EngineOptions(),
                  new MonitorManager(testingEngine, configuration),
                  new ThreadMonitorManager(configuration))
        {
            if (RaceInstrumentationEngine.SingletonEngine != null)
            {
                throw new InvalidOperationException("RaceInstrumentationEngine created more than once");
            }

            RaceInstrumentationEngine.SingletonEngine = this;

            // required?
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            this.GetService<ISymbolManager>().AddStackFrameFilter(new StackFrameFilter());

            if (!ControllerEnvironment.IsMonitoringEnabled)
            {
                Console.WriteLine("ExtendedReflection monitor not enabled");
                throw new NotImplementedException("ExtendedReflection monitor not enabled");
            }

            ((IMonitorManager)this.GetService<MonitorManager>()).RegisterThreadMonitor(
               new ThreadMonitorFactory(this.GetService<ThreadMonitorManager>(),
               testingEngine, configuration));

            ((IMonitorManager)this.GetService<MonitorManager>()).RegisterObjectAccessThreadMonitor();

            this.ExecutionMonitor.Initialize();
            var tid = this.ExecutionMonitor.CreateThread();
            _ThreadContext.Start(this.ExecutionMonitor, tid);
        }

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

        private IExecutionMonitor ExecutionMonitor
        {
            get { return this.GetService<MonitorManager>(); }
        }

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

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            this.ExecutionMonitor.Terminate();
            this.Log.Close();
        }
    }
}
