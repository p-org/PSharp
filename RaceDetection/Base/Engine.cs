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
using System.IO;
using System.Reflection;
using System.Diagnostics;

using Microsoft.ExtendedReflection.Interpretation;
using Microsoft.ExtendedReflection.Logging;
using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.Symbols;
using Microsoft.ExtendedReflection.Metadata;
using Microsoft.ExtendedReflection.Utilities.Safe.Diagnostics;
using Microsoft.ExtendedReflection.Utilities.Safe.IO;
using Microsoft.ExtendedReflection.Utilities;
using Microsoft.ExtendedReflection.ComponentModel;

using EREngine.CallsOnly;
using System.Runtime.CompilerServices;

namespace EREngine
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
