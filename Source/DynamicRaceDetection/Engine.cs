//-----------------------------------------------------------------------
// <copyright file="Engine.cs">
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

using Microsoft.PSharp.DynamicRaceDetection.CallsOnly;

namespace Microsoft.PSharp.DynamicRaceDetection
{
    /// <summary>
    /// Extended Reflection Engine
    /// </remarks>
    internal sealed class MyEngine : Microsoft.ExtendedReflection.ComponentModel.Engine
    {
        private static MyEngine theOnlyOne;
        public static void DisposeExecutionMonitors()
        {
            Debug.Assert(theOnlyOne != null);
            theOnlyOne.GetService<IMonitorManager>().DisposeExecutionMonitors();
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="MyEngine"/> class.
        /// </summary>
        public MyEngine()
            : base(new Container(), new EngineOptions(),
                   new MonitorManager(), new ThreadMonitorManager())
        {
            if (theOnlyOne != null)
            {
                throw new InvalidOperationException("MyEngine created more than once");
            }
            theOnlyOne = this;

            //required?
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            this.GetService<ISymbolManager>().AddStackFrameFilter(new StackFrameFilter());

            if (!ControllerEnvironment.IsMonitoringEnabled)
            {
                Console.WriteLine("ExtendedReflection monitor not enabled");
                throw new NotImplementedException("ExtendedReflection monitor not enabled");
            }

            ((IMonitorManager)this.GetService<MonitorManager>()).RegisterThreadMonitor(
               new ThreadMonitorFactory(
                   (ThreadMonitorManager)this.GetService<ThreadMonitorManager>()
                   ));

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
            get { return (IExecutionMonitor)this.GetService<MonitorManager>(); }
        }


        private sealed class StackFrameFilter : IStackFrameFilter
        {
            public bool Exclude(StackFrameName frame)
            {
                string v = frame.Value;
                return
                    v.Contains("Microsoft.ExtendedReflection") ||
                    v.Contains("___redirect") ||
                    v.Contains("___lateredirect") ||
                    v.Contains("__Substitutions") ||
                    v.Contains("mscorlib") ||
                    v.Contains("System.Reflection")
                    ;
            }
        }

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            this.ExecutionMonitor.Terminate();
            this.Log.Close();
        }
    }
}
