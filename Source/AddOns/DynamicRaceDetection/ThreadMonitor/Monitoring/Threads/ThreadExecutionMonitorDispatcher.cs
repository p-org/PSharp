// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.ExtendedReflection.Collections;
using Microsoft.ExtendedReflection.Logging;
using Microsoft.ExtendedReflection.Metadata;
using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.Utilities.Safe.Diagnostics;
using Microsoft.PSharp.Monitoring.CallsOnly;
using Microsoft.PSharp.TestingServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
            if (objH == UIntPtr.Zero)
                return;
            if (Reporter.InAction[machineId])// && !this.CallStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                if (is_volatile)
                {
                    throw new Exception("Volatile variables not permitted in PSharp state machines");
                }
                var sourceInfo = GetDebugInformation(location, objH);
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
            if (objH == UIntPtr.Zero)
                return;
            if (Reporter.InAction[machineId] /*&& !this.CallStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null*/)
            {
                if (is_volatile)
                {
                    throw new Exception("Volatile variables not permitted in PSharp state machines");
                }
                var sourceInfo = GetDebugInformation(location, objH);
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

        private static string TryGetClassName(UIntPtr location, UIntPtr objH)
        {
            var className = "Unk";
            if (objH == UIntPtr.Zero)
            {
                return className;
            }
            UIntPtr classHandle;
            if (ObjectTracking.TryGetClassHandle(objH, out classHandle))
            {
                className = ObjectTracking.GetClassName(classHandle);
            }
            return className;
        }

        private string GetDebugInformation(UIntPtr location, UIntPtr objH)
        {
            string sourceInfo = "";
            List<string> debugInfo = new List<string>();
            if (Configuration.EnableReadWriteTracing)
            {
                for (int i = 4; i < 15; i++)
                {
                    var frame = new StackFrame(i, true);
                    var info = GetSourceInformation(frame);
                    if (Regex.Match(info,@"\bMachine.cs\b").Success) { break; }
                    debugInfo.Add(info);
                }
                var caller = new StackFrame(5, true);
                string sep = $"{Environment.NewLine}\t\t\t";
                string className = TryGetClassName(location, objH);
                sourceInfo = String.Format("ObjectType[{0}] {1}",
                    className, string.Join(sep, debugInfo));
            }

            return sourceInfo;
        }

        private string GetSourceInformation(StackFrame callStack)
        {
            return String.Format("Line {0} in Method {1} in File {2}.",
                    callStack.GetFileLineNumber(), callStack.GetMethod(), callStack.GetFileName());
        }

        #endregion methods
    }
}