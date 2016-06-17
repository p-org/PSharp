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
using Microsoft.PSharp.Utilities;
using Microsoft.PSharp.TestingServices;

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
        /// The trace.
        /// </summary>
        private SafeList<string> Trace = new SafeList<string>();

        /// <summary>
        /// The thread trace.
        /// </summary>
        private List<ThreadTrace> ThreadTrace = new List<ThreadTrace>();

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

        private string currentAction;
        private int currentMachineId;
        private bool isCreateMachine = false;

        private static Dictionary<int, int> actionIds = new Dictionary<int, int>();
        private static Dictionary<int, int> sendIds = new Dictionary<int, int>();
        
        private static List<Tuple<Method, int>> taskMethods = new List<Tuple<Method, int>>();

        #endregion

        #region constructors and destructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="log">IEventLog</param>
        /// <param name="threadIndex">Thread index</param>
        /// <param name="callMonitor">IThreadMonitor</param>
        /// <param name="configuration">Configuration</param>
        public ThreadExecutionMonitorDispatcher(IEventLog log, int threadIndex,
            IThreadMonitor callMonitor, ITestingEngine testingEngine, Configuration configuration)
            : base(threadIndex)
        {
            SafeDebug.AssertNotNull(callMonitor, "callMonitor");
            
            this.ThreadIndex = threadIndex;
            this.Configuration = configuration;
            
            this.Trace = new SafeList<string>();
            this.CallStack = new SafeStack<Method>();

            this.IsDoHandlerCalled = false;
            this.IsEntryActionCalled = false;
            this.IsExitActionCalled = false;
            this.IsAction = false;
            this.RecordRW = false;

            testingEngine.RegisterPerIterationCallBack(EmitThreadTrace);
        }

        #endregion

        #region methods

        /// <summary>
        /// Emits the thread trace.
        /// </summary>
        void EmitThreadTrace(int iteration)
        {
            if (this.ThreadTrace.Count > 0)
            {
                string directoryPath = Path.GetDirectoryName(this.Configuration.AssemblyToBeAnalyzed) +
                    Path.DirectorySeparatorChar + "Output";
                string traceDirectoryPath = directoryPath + Path.DirectorySeparatorChar +
                    "ThreadTraces" + Path.DirectorySeparatorChar;
                Directory.CreateDirectory(traceDirectoryPath);

                var name = Path.GetFileNameWithoutExtension(this.Configuration.AssemblyToBeAnalyzed);

                string path = traceDirectoryPath + name + "_iteration_" +
                    iteration + "_tid_" + ThreadIndex + ".osl";

                using (Stream stream = File.Open(path, FileMode.Create))
                {
                    BinaryFormatter bformatter = new BinaryFormatter();

                    try
                    {
                        bformatter.Serialize(stream, this.ThreadTrace);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("EXCEPTION: " + ex);
                    }
                }
            }

            this.ThreadTrace.Clear();
        }

        [DebuggerNonUserCodeAttribute]
        public override void Load(UIntPtr location, uint size, int codeLabel, bool is_volatile)
        {
            UIntPtr objH, objO;
            ObjectTracking.GetObjectHandle(location, out objH, out objO);

            if (this.RecordRW && !this.CallStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                //Hack
                if (this.CallStack.Peek().ToString().Contains("Monitor"))
                {
                    Console.WriteLine("Load in monitor: " + this.CallStack.Peek().ToString());
                    return;
                }
                //end hack

                ThreadTrace obj = this.ThreadTrace[this.ThreadTrace.Count - 1];
                obj.accesses.Add(new ActionInstr(false, location, objH, objO, GetSourceLocation(location)));
                this.Trace.Add("load: " + objH + " " + objO + " " + this.CallStack.Peek() + " " + GetSourceLocation(location));
            }
            else if (!this.CallStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                foreach (Tuple<Method, int> m in taskMethods)
                {
                    //TODO: This is fragile (for tasks)
                    if (this.CallStack.Peek().ShortName.Contains(m.Item1.ShortName))
                    {
                        ThreadTrace obj = new ThreadTrace(m.Item2, m.Item1.ShortName);
                        obj.accesses.Add(new ActionInstr(false, location, objH, objO, GetSourceLocation(location)));
                        this.ThreadTrace.Add(obj);
                        //trace.Add("load: " + objH + " " + objO + " " + this.CallStack.Peek());
                    }
                }
            }
        }

        [DebuggerNonUserCodeAttribute]
        public override void Store(UIntPtr location, uint size, int codeLabel, bool is_volatile)
        {
            //TODO: Do not record writes within a constructor (BoundedAsyncRacy)
            if (this.CallStack.Peek().IsConstructor)
                return;

            UIntPtr objH, objO;
            ObjectTracking.GetObjectHandle(location, out objH, out objO);

            //trace.Add("storing outside: " + location + " " + objH + " " + objO + " " + this.CallStack.Peek() + this.RecordRW);
            if (this.RecordRW && !this.CallStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                //Hack
                if (this.CallStack.Peek().ToString().Contains("Monitor"))
                {
                    Console.WriteLine("store in monitor: " + this.CallStack.Peek().ToString());
                    return;
                }
                //end hack

                //trace.Add("got object handle: " + objH + " offset: " + objO);
                ThreadTrace obj = this.ThreadTrace[this.ThreadTrace.Count - 1];
                obj.accesses.Add(new ActionInstr(true, location, objH, objO, GetSourceLocation(location)));
                this.Trace.Add("store: " + location + " " + objH + " " + objO + " " + this.CallStack.Peek() + " " + GetSourceLocation(location));
            }
            else if(!this.CallStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                foreach(Tuple<Method, int> m in taskMethods)
                {
                    //TODO: This is fragile
                    if (this.CallStack.Peek().ShortName.Contains(m.Item1.ShortName))
                    {
                        ThreadTrace obj = new ThreadTrace(m.Item2, m.Item1.ShortName);
                        obj.accesses.Add(new ActionInstr(true, location, objH, objO, GetSourceLocation(location)));
                        this.ThreadTrace.Add(obj);
                        //trace.Add("store: " + location + " " + objH + " " + objO + " " + this.CallStack.Peek() + " " + m.Item2);
                    }
                }
            }
        }

        /// <summary>
        /// At start of method body.
        /// </summary>
        /// <remarks>Only one to push on callstack.</remarks>
        public override bool EnterMethod(Method method)
        {
            this.Trace.Add("Entering: " + method.FullName);
            this.CallStack.Push(method);
            if (this.IsAction && !method.FullName.Contains("Microsoft.PSharp"))
            {
                this.Trace.Add("ACTION!!!!!!");
                this.Trace.Add("method: " + method.FullName);
                ThreadTrace obj = this.ThreadTrace[this.ThreadTrace.Count - 1];
                obj.set(method.FullName);
                this.IsAction = false;
                this.RecordRW = true;
                currentAction = method.FullName;
            }

            return false;
        }

        /// <summary>
        /// Just before returning from method body.
        /// </summary>
        /// <remarks>Only method allowed to pop from callstack.</remarks>
        public override void LeaveMethod()
        {
            //trace.Add("Leaving: " + this.CallStack.Peek());
            Method leaving = this.CallStack.Pop();
            if (leaving.FullName.Equals(currentAction))
            {
                this.RecordRW = false;
            }
            /*if(!leaving.FullName.Contains("Microsoft.PSharp"))
                trace.Add("leaving: " + leaving);*/
        }

        /// <summary>
        /// Regular instruction; <see cref="System.Reflection.Emit.OpCodes.Call"/>
        /// </summary>
        /// <param name="method">callee</param>
        public override void Call(Method method)
        {
            this.Trace.Add("Method Call: " + method.FullName);
            if ((method.FullName.Contains("Microsoft.PSharp.Machine.CreateMachine") ||
                method.FullName.Contains("Microsoft.PSharp.PSharpRuntime.CreateMachine")) &&
                !this.CallStack.Peek().FullName.Contains(".Main"))
            {
                isCreateMachine = true;
                this.Trace.Add("call: " + method + " " + isCreateMachine);
            }

            else if (method.FullName.Equals("Microsoft.PSharp.Machine.Do"))
            {
                this.IsDoHandlerCalled = true;
                this.Trace.Add("do called");
            }

            else if (method.FullName.Equals("Microsoft.PSharp.Machine.ExecuteCurrentStateOnEntry"))
                this.IsEntryActionCalled = true;

            else if (method.FullName.Equals("Microsoft.PSharp.Machine.ExecuteCurrentStateOnExit"))
                this.IsExitActionCalled = true;

            else if ((method.FullName.Equals("Microsoft.PSharp.Machine.Send") ||
                method.FullName.Equals("Microsoft.PSharp.PSharpRuntime.SendEvent")) &&
                !this.CallStack.Peek().FullName.Contains(".Main"))
            {
                this.Trace.Add("send: " + method.FullName);
                ThreadTrace obj = this.ThreadTrace[this.ThreadTrace.Count - 1];

                if (sendIds.ContainsKey(currentMachineId))
                {
                    sendIds[currentMachineId]++;
                }
                else
                {
                    sendIds.Add(currentMachineId, 1);
                }

                obj.accesses.Add(new ActionInstr(sendIds[currentMachineId]));
            }
        }

        //Unable to cast from object to MachineId
        public override void CallResultObject(object value)
        {
            try
            {
                MachineId r = (MachineId)value;
                if (isCreateMachine)
                {
                    //race.Add("call result object: " + isCreateMachine + " " + r.GetHashCode().Tostring());
                    isCreateMachine = false;

                    ThreadTrace obj = this.ThreadTrace[this.ThreadTrace.Count - 1];
                    obj.accesses.Add(new ActionInstr(r.GetHashCode(), true));
                }
            }
            catch (Exception)
            {
                try
                {
                    Task tid = (Task)value;
                    this.Trace.Add("Task created: " + tid.Id);
                    if(taskMethods.Count > 0)
                    {
                        Method m = taskMethods[taskMethods.Count - 1].Item1;
                        taskMethods[taskMethods.Count - 1] = new Tuple<Method, int>(m, tid.Id);
                        ThreadTrace obj = this.ThreadTrace[this.ThreadTrace.Count - 1];
                        obj.accesses.Add(new ActionInstr(true, tid.Id));
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        public override void CallReceiver(object receiver)
        {
            if (this.IsDoHandlerCalled || this.IsEntryActionCalled || this.IsExitActionCalled)
            {
                /*if (!localIter.Equals(Environment.GetEnvironmentVariable("ITERATION")))
                {
                    //Console.WriteLine("iteration changed for {0} from {1} to {2}", threadIndex, localIter, Environment.GetEnvironmentVariable("ITERATION"));

                    if (this.ThreadTrace.Count > 0 && !localIter.Equals("-1"))
                    {
                        string path = Environment.GetEnvironmentVariable("DIRPATH") + "InstrTrace" + localIter + "\\";  //thTrace_" + threadIndex + ".osl";
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }

                        path += "thTrace_" + threadIndex + ".osl";
                        Stream stream = File.Open(path, FileMode.Create);
                        BinaryFormatter bformatter = new BinaryFormatter();

                        try
                        {
                            bformatter.Serialize(stream, this.ThreadTrace);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("EXCEPTION: " + ex);
                        }
                        stream.Close();
                    }

                    this.ThreadTrace = new List<ThreadTrace>();
                    localIter = Environment.GetEnvironmentVariable("ITERATION");
                    if(cleared != Int32.Parse(localIter))
                    {
                        sendIds.Clear();
                        actionIds.Clear();
                    }
                    cleared = Int32.Parse(localIter);
                    //Console.ReadLine();
                }*/

                Machine mc = (Machine)receiver;
                int mcID = mc.GetHashCode();

                this.currentMachineId = mcID;

                this.Trace.Add("call receiver: " + mc.GetType() + " " + mcID);

                this.IsDoHandlerCalled = false;
                this.IsEntryActionCalled = false;
                this.IsExitActionCalled = false;

                this.IsAction = true;
                ThreadTrace obj = new ThreadTrace(mcID);

                if (actionIds.ContainsKey(mcID))
                {
                    actionIds[mcID]++;
                    this.Trace.Add("action id: " + actionIds[mcID]);
                }
                else
                {
                    actionIds.Add(mcID, 1);
                    this.Trace.Add("action id: 1");
                }

                obj.set(actionIds[mcID]);
                this.ThreadTrace.Add(obj);
            }
        }

        /// <summary>
        /// Regular instruction; <see cref="System.Reflection.Emit.OpCodes.Callvirt"/>
        /// </summary>
        /// <param name="method">callee before vtable lookup</param>
        public override void Callvirt(Method method)
        {
            this.Trace.Add("Virtual call: " + method.FullName);
            if (method.FullName.Contains("System.Threading.Tasks.Task.Start"))
            {
                taskMethods.Add(new Tuple<Method, int>(this.CallStack.Peek(), -1));
            }
        }

        public string GetSourceLocation(UIntPtr location)
        {
            StackTrace st = new StackTrace(true);
            int lineno_cnt = 0;
            string ret = null;

            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame sf = st.GetFrame(i);
                string assembly_name = sf.GetMethod().Module.Assembly.GetName().ToString();

                // this is fragile
                if (assembly_name.Contains("Base") || assembly_name.Contains(".ExtendedReflection") ||
                    assembly_name.Contains(".PSharp") || assembly_name.Contains("mscorlib"))
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
                lineno_cnt++;

                ret = file + ";" + method + ";" + lineno;

                if (lineno_cnt > 3)
                {
                    break;
                }
            }

            return ret;
        }

        #endregion
    }
}
