﻿//-----------------------------------------------------------------------
// <copyright file="ThreadExecutionMonitorDispatcher.cs">
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

using Microsoft.ExtendedReflection.Collections;
using Microsoft.ExtendedReflection.Logging;
using Microsoft.ExtendedReflection.Metadata;
using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.Utilities.Safe.Diagnostics;
using Microsoft.PSharp.Monitoring.CallsOnly;
using Microsoft.PSharp.TestingServices;
using System;
using System.Diagnostics;

namespace Microsoft.PSharp.Monitoring.AllCallbacks
{
    /// <summary>
    /// Ignores all callbacks, except method/constructor
    /// calls and corresponding returns.
    /// </summary>
    [__DoNotInstrument]
    internal class ThreadExecutionMonitorDispatcher : ThreadExecutionMonitorEmpty
    {
        #region fields

        /// <summary>
        /// The P# configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// The thread index.
        /// </summary>
        private readonly int ThreadIndex;

        /// <summary>
        /// The debugging trace.
        /// </summary>
        private SafeList<string> DebugTrace;

        /// <summary>
        /// The call stack.
        /// </summary>
        private SafeStack<Method> CallStack;

        /// <summary>
        /// The task methods.
        /// </summary>
        private static System.Collections.Generic.List<Tuple<Method, int>> TaskMethods;

        private IRegisterRuntimeOperation Reporter;

        #endregion fields

        #region constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static ThreadExecutionMonitorDispatcher()
        {
            TaskMethods = new System.Collections.Generic.List<Tuple<Method, int>>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="log">IEventLog</param>
        /// <param name="threadIndex">Thread index</param>
        /// <param name="callMonitor">IThreadMonitor</param>
        /// <param name="raceDetectionEngine">IRaceDetectionEngine</param>
        /// <param name="configuration">Configuration</param>
        public ThreadExecutionMonitorDispatcher(IEventLog log, int threadIndex,
            IThreadMonitor callMonitor, IRegisterRuntimeOperation raceDetectionEngine, Configuration configuration)
            : base(threadIndex)
        {
            SafeDebug.AssertNotNull(callMonitor, "callMonitor");
            this.ThreadIndex = threadIndex;
            this.Configuration = configuration;
            this.DebugTrace = new SafeList<string>();
            this.CallStack = new SafeStack<Method>();
            this.Reporter = raceDetectionEngine;
        }

        #endregion constructors

        #region methods

        //[DebuggerNonUserCodeAttribute]
        public override void Load(UIntPtr location, uint size, int codeLabel, bool is_volatile)
        {
            ulong machineId = 0;
            var machineRunning = this.Reporter.TryGetCurrentMachineId(out machineId);
            if (!machineRunning || !Reporter.InAction.ContainsKey(machineId))
            {
                return;
            }
            UIntPtr objH, objO;
            ObjectTracking.GetObjectHandle(location, out objH, out objO);
            if (Reporter.InAction[machineId])// && !this.CallStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                if (is_volatile)
                {
                    throw new Exception("Volatile variables not permitted in PSharp state machines");
                }
                string sourceInfo = "";
                if (Configuration.EnableReadWriteTracing)
                {
                    StackFrame callStack = new StackFrame(3, true);                                                
                    sourceInfo = String.Format ("Line {0} in Method {1} in File {2}.",
                        callStack.GetFileLineNumber(), callStack.GetMethod(), callStack.GetFileName());
                }
                Reporter.RegisterRead(machineId, sourceInfo, location, objH, objO, is_volatile);
            }  
        }

        //[DebuggerNonUserCodeAttribute]
        public override void Store(UIntPtr location, uint size, int codeLabel, bool is_volatile)
        {
            ulong machineId = 0;
            var machineRunning = this.Reporter.TryGetCurrentMachineId(out machineId);
            if (!machineRunning || !Reporter.InAction.ContainsKey(machineId))
            {
                return;
            }
            UIntPtr objH, objO;
            ObjectTracking.GetObjectHandle(location, out objH, out objO);
            if (Reporter.InAction[machineId] /*&& !this.CallStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null*/)
            {
                if (is_volatile)
                {
                    throw new Exception("Volatile variables not permitted in PSharp state machines");
                }
                string sourceInfo = "";
                if (Configuration.EnableReadWriteTracing)
                {
                    StackFrame callStack = new StackFrame(3, true);
                    sourceInfo = String.Format("Line {0} in Method {1} in File {2}.",
                        callStack.GetFileLineNumber(), callStack.GetMethod(), callStack.GetFileName());
                }
                Reporter.RegisterWrite(machineId, sourceInfo, location, objH, objO, is_volatile);
            }
        }

        /// <summary>
        /// Returns the source location.
        /// </summary>
        /// <param name="location">UIntPtr</param>
        /// <returns>Location</returns>
        public string GetSourceLocation(UIntPtr location)
        {
            StackTrace st = new StackTrace(true);
            int lineCount = 0;
            string result = null;

            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame sf = st.GetFrame(i);
                string assembly_name = sf.GetMethod().Module.Assembly.GetName().ToString();

                // TODO: This is fragile.
                if (assembly_name.Contains("Base") ||
                    assembly_name.Contains(".ExtendedReflection") ||
                    assembly_name.Contains(".PSharp") ||
                    assembly_name.Contains("mscorlib"))
                {
                    continue;
                }
                lineCount++;
                if (lineCount > 3)
                {
                    string file = sf.GetFileName();
                    if (file == null || file == "")
                    {
                        file = "NONE";
                    }
                    string method = sf.GetMethod().Name;
                    int lineno = sf.GetFileLineNumber();
                    result = file + ";" + method + ";" + lineno;
                    break;
                }
            }

            return result;
        }

        #endregion methods
    }
}