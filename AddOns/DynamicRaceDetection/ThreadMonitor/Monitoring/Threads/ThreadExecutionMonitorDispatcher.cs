//-----------------------------------------------------------------------
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

using Microsoft.ExtendedReflection.Collections;
using Microsoft.ExtendedReflection.Logging;
using Microsoft.ExtendedReflection.Metadata;
using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.Utilities.Safe.Diagnostics;

using Microsoft.PSharp.Monitoring.CallsOnly;
using Microsoft.PSharp.TestingServices;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.Monitoring.AllCallbacks
{
    /// <summary>
    /// Ignores all callbacks, except method/constructor
    /// calls and corresponding returns.
    /// </summary>
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
        /// The thread trace.
        /// </summary>
        private List<ThreadTrace> ThreadTrace;

        /// <summary>
        /// The debugging trace.
        /// </summary>
        private SafeList<string> DebugTrace;

        /// <summary>
        /// The call stack.
        /// </summary>
        private SafeStack<Method> CallStack;
        
        /// <summary>
        /// Is event action handler called.
        /// </summary>
        private bool IsDoHandlerCalled;

        /// <summary>
        /// Is entry action called.
        /// </summary>
        private bool IsEntryActionCalled;

        /// <summary>
        /// Is exit action called.
        /// </summary>
        private bool IsExitActionCalled;

        /// <summary>
        /// Is method an action.
        /// </summary>
        private bool IsAction;

        /// <summary>
        /// Record read-write.
        /// </summary>
        private bool RecordRW;

        /// <summary>
        /// The currently executing action.
        /// </summary>
        private string CurrentlyExecutingAction;

        /// <summary>
        /// The currently executing machine id.
        /// </summary>
        private int CurrentMachineId;

        /// <summary>
        /// Is the create machine method
        /// </summary>
        private bool IsCreateMachineMethod;

        /// <summary>
        /// Machine ID of the machine where the action is invoked. 
        /// </summary>
        private int machineIdOfAction;

        /// <summary>
        /// The action ids.
        /// </summary>
        private static Dictionary<int, int> ActionIds;

        /// <summary>
        /// The send ids.
        /// </summary>
        private static Dictionary<int, int> SendIds;
        
        /// <summary>
        /// The task methods.
        /// </summary>
        private static List<Tuple<Method, int>> TaskMethods;

        #endregion

        #region constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static ThreadExecutionMonitorDispatcher()
        {
            ActionIds = new Dictionary<int, int>();
            SendIds = new Dictionary<int, int>();
            TaskMethods = new List<Tuple<Method, int>>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="log">IEventLog</param>
        /// <param name="threadIndex">Thread index</param>
        /// <param name="callMonitor">IThreadMonitor</param>
        /// <param name="testingEngine">ITestingEngine</param>
        /// <param name="configuration">Configuration</param>
        public ThreadExecutionMonitorDispatcher(IEventLog log, int threadIndex,
            IThreadMonitor callMonitor, ITestingEngine testingEngine, Configuration configuration)
            : base(threadIndex)
        {
            SafeDebug.AssertNotNull(callMonitor, "callMonitor");
            
            this.ThreadIndex = threadIndex;
            this.Configuration = configuration;
            
            this.ThreadTrace = new List<ThreadTrace>();
            this.DebugTrace = new SafeList<string>();
            this.CallStack = new SafeStack<Method>();

            this.IsDoHandlerCalled = false;
            this.IsEntryActionCalled = false;
            this.IsExitActionCalled = false;
            this.IsAction = false;
            this.RecordRW = false;
            this.IsCreateMachineMethod = false;

            // Registers a callback to emit the thread trace. The callback
            // is invoked at the end of each testing iteration.
            testingEngine.RegisterPerIterationCallBack(EmitThreadTrace);
        }

        #endregion

        #region methods

        /// <summary>
        /// Emits the thread trace.
        /// </summary>
        /// <param name="iteration">Testing iteration</param>
        void EmitThreadTrace(int iteration)
        {
            if (this.ThreadTrace.Count > 0)
            {
                string directoryPath = Path.GetDirectoryName(this.Configuration.AssemblyToBeAnalyzed) +
                    Path.DirectorySeparatorChar + "Output";
                string traceDirectoryPath = directoryPath + Path.DirectorySeparatorChar +
                    "ThreadTraces" + Path.DirectorySeparatorChar;
                Directory.CreateDirectory(traceDirectoryPath);

                //Console.WriteLine("thread id: " + this.ThreadId);
                //foreach (var item in ThreadTrace)
                //{
                //    Console.WriteLine(">>> " + item.MachineId + "; " + item.ActionName + "; " + item.ActionId + "; " + item.TaskId);
                //    foreach (var acc in item.Accesses)
                //    {
                //        if (acc.IsSend)
                //        {
                //            Console.WriteLine("Send: " + acc.SendId);
                //        }
                //        else if (acc.IsCreate)
                //        {
                //            Console.WriteLine("create: " + acc.CreateMachineId);
                //        }
                //        else if (acc.IsTask)
                //        {
                //            Console.WriteLine("task created: " + acc.TaskId);
                //        }
                //        else
                //        {
                //            Console.WriteLine(acc.IsWrite + " " + acc.SrcLocation);
                //        }
                //    }
                //}

                var name = Path.GetFileNameWithoutExtension(this.Configuration.AssemblyToBeAnalyzed);

                string path = traceDirectoryPath + name + "_iteration_" +
                    iteration + "_tid_" + ThreadIndex + ".osl";

                using (Stream stream = File.Open(path, FileMode.Create))
                {
                    BinaryFormatter bformatter = new BinaryFormatter();
                    bformatter.Serialize(stream, this.ThreadTrace);
                }
            }

            //if (this.Configuration.EnableDebugging)
            //{
            //    foreach (var log in this.DebugTrace)
            //    {
            //        IO.Debug(log);
            //    }
            //}

            this.ThreadTrace.Clear();
            ActionIds.Clear();
            SendIds.Clear();
            TaskMethods.Clear();
            //this.DebugTrace.Clear();
        }

        [DebuggerNonUserCodeAttribute]
        public override void Load(UIntPtr location, uint size, int codeLabel, bool is_volatile)
        {
            UIntPtr objH, objO;
            ObjectTracking.GetObjectHandle(location, out objH, out objO);

            if (this.RecordRW && !this.CallStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                // TODO: Hack
                if (this.CallStack.Peek().ToString().Contains("Monitor"))
                {
                    return;
                }
                // End hack

                ThreadTrace obj = this.ThreadTrace[this.ThreadTrace.Count - 1];
                obj.Accesses.Add(new ActionInstr(false, location, objH, objO, GetSourceLocation(location)));
                this.DebugTrace.Add($"<ThreadMonitorLog> Load '{objH}' '{objO}' " +
                    $"'{this.CallStack.Peek()}' '{GetSourceLocation(location)}'.");
            }
            else if (!this.CallStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                foreach (Tuple<Method, int> m in TaskMethods)
                {
                    // TODO: This is fragile (for tasks)
                    if (this.CallStack.Peek().ShortName.Contains(m.Item1.ShortName))
                    {
                        ThreadTrace obj = Monitoring.ThreadTrace.CreateTraceForTask(m.Item2);
                        obj.Accesses.Add(new ActionInstr(false, location, objH, objO, GetSourceLocation(location)));
                        this.ThreadTrace.Add(obj);
                    }
                }
            }
        }

        [DebuggerNonUserCodeAttribute]
        public override void Store(UIntPtr location, uint size, int codeLabel, bool is_volatile)
        {
            //TODO: Do not record writes within a constructor (BoundedAsyncRacy)
            if (this.CallStack.Peek().IsConstructor)
            {
                return;
            }

            UIntPtr objH, objO;
            ObjectTracking.GetObjectHandle(location, out objH, out objO);

            if (this.RecordRW && !this.CallStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                // TODO: Hack
                if (this.CallStack.Peek().ToString().Contains("Monitor"))
                {
                    return;
                }
                // End hack

                ThreadTrace obj = this.ThreadTrace[this.ThreadTrace.Count - 1];
                obj.Accesses.Add(new ActionInstr(true, location, objH, objO, GetSourceLocation(location)));
                this.DebugTrace.Add($"<ThreadMonitorLog> Store '{location}' '{objH} '{objO}'" +
                    $"'{this.CallStack.Peek()}' '{GetSourceLocation(location)}'.");
            }
            else if(!this.CallStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                foreach(Tuple<Method, int> m in TaskMethods)
                {
                    //TODO: This is fragile (for tasks)
                    if (this.CallStack.Peek().ShortName.Contains(m.Item1.ShortName))
                    {
                        ThreadTrace obj = Monitoring.ThreadTrace.CreateTraceForTask(m.Item2);
                        obj.Accesses.Add(new ActionInstr(true, location, objH, objO, GetSourceLocation(location)));
                        this.ThreadTrace.Add(obj);
                    }
                }
            }
        }

        /// <summary>
        /// Called at the start of a method body.
        /// </summary>
        /// <remarks>Only one to push on callstack.</remarks>
        public override bool EnterMethod(Method method)
        {
            this.DebugTrace.Add($"<ThreadMonitorLog> Entering '{method.FullName}'.");
            this.CallStack.Push(method);
            if (this.IsAction && !method.FullName.Contains("Microsoft.PSharp"))
            {
                this.DebugTrace.Add($"<ThreadMonitorLog> Action '{method.FullName}'.");

                ThreadTrace obj = this.ThreadTrace[this.ThreadTrace.Count - 1];
                obj.ActionName = method.FullName;

                this.IsAction = false;
                this.RecordRW = true;
                this.CurrentlyExecutingAction = method.FullName;
            }

            return false;
        }

        /// <summary>
        /// Just before returning from method body.
        /// </summary>
        /// <remarks>Only method allowed to pop from callstack.</remarks>
        public override void LeaveMethod()
        {
            Method leaving = this.CallStack.Pop();
            if (leaving.FullName.Equals(this.CurrentlyExecutingAction))
            {
                this.RecordRW = false;
            }
        }

        /// <summary>
        /// Regular instruction.
        /// </summary>
        /// <param name="method">Callee</param>
        public override void Call(Method method)
        {
            this.DebugTrace.Add($"<ThreadMonitorLog> Method call '{method.FullName}'.");

            if ((method.FullName.Contains("Microsoft.PSharp.Machine.CreateMachine") ||
                method.FullName.Contains("Microsoft.PSharp.PSharpRuntime.CreateMachine")) &&
                !this.CallStack.Peek().FullName.Contains(".Main"))
            {
                this.IsCreateMachineMethod = true;
                this.DebugTrace.Add($"<ThreadMonitorLog> Call '{method}' '{this.IsCreateMachineMethod}'.");
            }

            else if (method.FullName.Equals("Microsoft.PSharp.Machine.Do"))
            {
                this.IsDoHandlerCalled = true;
            }

            else if (method.FullName.Equals("Microsoft.PSharp.Machine.ExecuteCurrentStateOnEntry"))
            {
                Console.WriteLine("IsEntryActionCalled set to true in: " + CallStack.Peek());
                this.IsEntryActionCalled = true;
            }

            else if (method.FullName.Equals("Microsoft.PSharp.Machine.ExecuteCurrentStateOnExit"))
            {
                this.IsExitActionCalled = true;
            }

            else if ((method.FullName.Equals("Microsoft.PSharp.Machine.Send") ||
                method.FullName.Equals("Microsoft.PSharp.PSharpRuntime.SendEvent")) &&
                !this.CallStack.Peek().FullName.Contains(".Main"))
            {
                this.DebugTrace.Add($"<ThreadMonitorLog> Send '{method.FullName}'.");
                ThreadTrace obj = this.ThreadTrace[this.ThreadTrace.Count - 1];

                if (SendIds.ContainsKey(this.CurrentMachineId))
                {
                    SendIds[this.CurrentMachineId]++;
                }
                else
                {
                    SendIds.Add(this.CurrentMachineId, 1);
                }

                obj.Accesses.Add(new ActionInstr(SendIds[this.CurrentMachineId]));
            }
        }

        // Unable to cast from object to MachineId.
        public override void CallResultObject(object value)
        {
            try
            {
                if (value == null)
                    return;
                MachineId r = (MachineId)value;
                if (this.IsCreateMachineMethod)
                {
                    this.IsCreateMachineMethod = false;

                    ThreadTrace obj = this.ThreadTrace[this.ThreadTrace.Count - 1];
                    obj.Accesses.Add(new ActionInstr(r.GetHashCode(), true));
                }
            }
            catch (Exception)
            {
                try
                {
                    Task tid = (Task)value;
                    this.DebugTrace.Add($"<ThreadMonitorLog> Task {tid.Id} created.");
                    if (TaskMethods.Count > 0)
                    {
                        Method m = TaskMethods[TaskMethods.Count - 1].Item1;
                        TaskMethods[TaskMethods.Count - 1] = new Tuple<Method, int>(m, tid.Id);
                        ThreadTrace obj = this.ThreadTrace[this.ThreadTrace.Count - 1];
                        obj.Accesses.Add(new ActionInstr(true, tid.Id));
                    }
                }
                catch (Exception)
                {
                    // TODO: this is a hack.
                }
            }
        }
        
        public override void CallReceiver(object receiver)
        {
            if (this.IsDoHandlerCalled ||
                this.IsEntryActionCalled ||
                this.IsExitActionCalled)
            {
                Machine machine = (Machine)receiver;
                int machineId = machine.GetHashCode();
                this.CurrentMachineId = machineId;

                this.DebugTrace.Add("<ThreadMonitorLog> Call receiver " +
                    $"'{machine.GetType()}' '{machineId}'.");

                this.IsDoHandlerCalled = false;
                this.IsEntryActionCalled = false;
                this.IsExitActionCalled = false;

                this.IsAction = true;

                machineIdOfAction = machineId;
            }
        }

        /// <summary>
        /// Regular instruction.
        /// </summary>
        /// <param name="method">Callee before vtable lookup</param>
        public override void Callvirt(Method method)
        {
            this.DebugTrace.Add($"<ThreadMonitorLog> Virtual call '{method.FullName}'.");

            if (method.FullName.Contains("Microsoft.PSharp") && method.FullName.Contains("NotifyInvokedAction") &&
                !this.CallStack.Peek().FullName.Contains(".Main"))
            {
                ThreadTrace obj = Monitoring.ThreadTrace.CreateTraceForMachine(machineIdOfAction);

                if (ActionIds.ContainsKey(machineIdOfAction))
                {
                    ActionIds[machineIdOfAction]++;
                }
                else
                {
                    ActionIds.Add(machineIdOfAction, 1);
                }

                obj.ActionId = ActionIds[machineIdOfAction];
                this.ThreadTrace.Add(obj);
            }

            if (method.FullName.Contains("System.Threading.Tasks.Task.Start"))
            {
                TaskMethods.Add(new Tuple<Method, int>(this.CallStack.Peek(), -1));
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

                string file = sf.GetFileName();

                if (file == null || file == "")
                {
                    file = "NONE";
                }


                string method = sf.GetMethod().Name;
                int lineno = sf.GetFileLineNumber();
                lineCount++;

                result = file + ";" + method + ";" + lineno;

                if (lineCount > 3)
                {
                    break;
                }
            }

            return result;
        }

        #endregion
    }
}
