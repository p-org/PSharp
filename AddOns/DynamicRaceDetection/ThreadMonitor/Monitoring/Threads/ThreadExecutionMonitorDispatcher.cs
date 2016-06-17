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

        private readonly int ThreadIndex;
        private readonly IThreadMonitor CallMonitor;
        private readonly IEventLog Log;

        private SafeList<string> trace = new SafeList<string>();
        private List<ThreadTrace> thTrace = new List<ThreadTrace>();

        private SafeStack<Method> callStack;
        private bool isDoCalled = false;
        private bool isEntryFuntionCalled = false;
        private bool isExitFuntionCalled = false;
        private bool isAction = false;
        private bool recordRW = false;
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
            IThreadMonitor callMonitor, Configuration configuration)
            : base(threadIndex)
        {
            SafeDebug.AssertNotNull(callMonitor, "callMonitor");

            this.Log = log;
            this.ThreadIndex = threadIndex;
            this.CallMonitor = callMonitor;
            this.Configuration = configuration;
            
            trace = new SafeList<string>();
            callStack = new SafeStack<Method>();
        }

        /// <summary>
        /// Destructor.
        /// </summary>
        ~ThreadExecutionMonitorDispatcher()
        {
            if (thTrace.Count > 0)
            {
                string directoryPath = Path.GetDirectoryName(this.Configuration.AssemblyToBeAnalyzed) +
                    Path.DirectorySeparatorChar + "Output";
                string traceDirectoryPath = directoryPath + Path.DirectorySeparatorChar +
                    "ThreadTraces" + Path.DirectorySeparatorChar;
                Directory.CreateDirectory(traceDirectoryPath);

                var name = Path.GetFileNameWithoutExtension(this.Configuration.AssemblyToBeAnalyzed);
                int iteration = Directory.GetFiles(traceDirectoryPath, name +
                    "iteration_*_tid_" + ThreadIndex + ".osl").Length;

                Console.WriteLine(">>>>>>>>>" + iteration);

                string path = traceDirectoryPath + name + "_iteration_" +
                    iteration + "_tid_" + ThreadIndex + ".osl";

                using (Stream stream = File.Open(path, FileMode.Create))
                {
                    BinaryFormatter bformatter = new BinaryFormatter();

                    try
                    {
                        bformatter.Serialize(stream, thTrace);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("EXCEPTION: " + ex);
                    }
                }
            }
        }

        #endregion

        #region methods

        [Conditional("DEBUG")]
        protected void Trace(string arg)
        {

        }

        /// <summary>
        /// Log an exception
        /// </summary>
        protected void LogException(Exception e)
        {
            // throw e;
        }

        #region callbacks

        [DebuggerNonUserCodeAttribute]
        public override void Load(UIntPtr location, uint size, int codeLabel, bool is_volatile)
        {
            UIntPtr objH, objO;
            ObjectTracking.GetObjectHandle(location, out objH, out objO);

            if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                //Hack
                if (callStack.Peek().ToString().Contains("Monitor"))
                {
                    Console.WriteLine("Load in monitor: " + callStack.Peek().ToString());
                    return;
                }
                //end hack

                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses.Add(new ActionInstr(false, location, objH, objO, GetSourceLocation(location)));
                trace.Add("load: " + objH + " " + objO + " " + callStack.Peek() + " " + GetSourceLocation(location));
            }
            else if (!callStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                foreach (Tuple<Method, int> m in taskMethods)
                {
                    //TODO: This is fragile (for tasks)
                    if (callStack.Peek().ShortName.Contains(m.Item1.ShortName))
                    {
                        ThreadTrace obj = new ThreadTrace(m.Item2, m.Item1.ShortName);
                        obj.accesses.Add(new ActionInstr(false, location, objH, objO, GetSourceLocation(location)));
                        thTrace.Add(obj);
                        //trace.Add("load: " + objH + " " + objO + " " + callStack.Peek());
                    }
                }
            }
        }

        public override void LoadedValue<T>(T value)
        {
            /*if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";val:" + value;
                //trace.Add("loaded value: " + value.GetType());
                // thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }*/
        }

        public override void LoadedValueByRef<T>(ref T value)
        {
            if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                /*trace.Add("loaded value by ref: " + value.GetType());
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";ref: " + value;*/
                //trace.Add("loaded value: " + value.GetType());
                // thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }
        }

        public override void LoadedValueObject(object value)
        {
            if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                /*trace.Add("loaded value object: " + value.GetType());
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";obj: " + value;*/
                //trace.Add("loaded value: " + value.GetType());
                // thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }
        }

        public override void LoadedValuePtr(UIntPtr value, TypeEx pointerType)
        {
            if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                /*trace.Add("loaded value pointer: " + value.GetType() + " " + pointerType.GetType());
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";ptr: " + value;*/
                //trace.Add("loaded value: " + value.GetType());
                // thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }
        }

        public override void LoadedValueTypedReference(TypedReference typedReference)
        {
            if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp"))
            {
                /*trace.Add("loaded value typed reference: ");
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";typ_ref: ";*/
                //trace.Add("loaded value: " + value.GetType());
                // thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }
        }

        [DebuggerNonUserCodeAttribute]
        public override void Store(UIntPtr location, uint size, int codeLabel, bool is_volatile)
        {
            //TODO: Do not record writes within a constructor (BoundedAsyncRacy)
            if (callStack.Peek().IsConstructor)
                return;

            UIntPtr objH, objO;
            ObjectTracking.GetObjectHandle(location, out objH, out objO);

            //trace.Add("storing outside: " + location + " " + objH + " " + objO + " " + callStack.Peek() + recordRW);
            if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                //Hack
                if (callStack.Peek().ToString().Contains("Monitor"))
                {
                    Console.WriteLine("store in monitor: " + callStack.Peek().ToString());
                    return;
                }
                //end hack

                //trace.Add("got object handle: " + objH + " offset: " + objO);
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses.Add(new ActionInstr(true, location, objH, objO, GetSourceLocation(location)));
                trace.Add("store: " + location + " " + objH + " " + objO + " " + callStack.Peek() + " " + GetSourceLocation(location));
            }
            else if(!callStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                foreach(Tuple<Method, int> m in taskMethods)
                {
                    //TODO: This is fragile
                    if (callStack.Peek().ShortName.Contains(m.Item1.ShortName))
                    {
                        ThreadTrace obj = new ThreadTrace(m.Item2, m.Item1.ShortName);
                        obj.accesses.Add(new ActionInstr(true, location, objH, objO, GetSourceLocation(location)));
                        thTrace.Add(obj);
                        //trace.Add("store: " + location + " " + objH + " " + objO + " " + callStack.Peek() + " " + m.Item2);
                    }
                }
            }
        }

        public override void StoredValue<T>(T value)
        {
            //trace.Add("store value outside: " + value);
            if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                //ThreadTrace obj = thTrace[thTrace.Count - 1];
                //obj.accesses[obj.accesses.Count - 1].srcLocation += ";val: " + value.Tostring() + " = " + value.GetHashCode();
                //trace.Add("stored value: " + value + " " + value.GetHashCode());
                //thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }
        }

        public override void StoredValueByRef<T>(ref T value)
        {
            /*if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";ref: " + value.Tostring() + " = " + value.GetHashCode();
                //trace.Add("stored value: " + value + " " + value.GetHashCode());
                //thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }*/
        }

        public override void StoredValueObject(object value)
        {
            /*if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";obj: " + value.Tostring() + " = " + value.GetHashCode();
                //trace.Add("stored value: " + value + " " + value.GetHashCode());
                //thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }*/
        }

        public override void StoredValuePtr(UIntPtr value, TypeEx pointerType)
        {
            /*if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";ptr: " + value.Tostring() + " = " + value.GetHashCode();
                //trace.Add("stored value: " + value + " " + value.GetHashCode());
                //thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }*/
        }

        public override void StoredValueTypedReference(TypedReference typedReference)
        {
            /*if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp"))
            {
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";typ_ref: ";
                //trace.Add("stored value: " + value + " " + value.GetHashCode());
                //thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }*/
        }

        /*public override void StoredValueByRef<T>(ref T value)
        {
            if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";" + value.GetType() + " = " + value.Tostring();
                //trace.Add("stored value: " + value + " " + value.GetHashCode());
                //thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }
        }*/

        [DebuggerNonUserCodeAttribute]
        public override void AfterNewobjObject(object newObject)
        {
            //trace.Add("new object: " + newObject.GetType() + " " + newObject.GetHashCode());
        }

        public override void AfterNewobj<T>(T newValue)
        {
            //trace.Add("new struct? " + newValue.GetType() + " " + newValue.GetHashCode());
        }

        /// <summary>
        /// At start of method body.
        /// </summary>
        /// <remarks>Only one to push on callstack.</remarks>
        public override bool EnterMethod(Method method)
        {
            trace.Add("Entering: " + method.FullName);
            callStack.Push(method);
            if (isAction && !method.FullName.Contains("Microsoft.PSharp"))
            {
                trace.Add("ACTION!!!!!!");
                trace.Add("method: " + method.FullName);
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.set(method.FullName);
                isAction = false;
                recordRW = true;
                currentAction = method.FullName;
            }

            return false;
        }

        /// <Summary>
        /// Retrieve the arguments of a method
        /// </Summary>
        /// <remarks>EnterMethod must return true in order to get the arguments using this method.</remarks>
        public override void Argument<T>(int index, T value)
        {
            //myTrace.add("arg: " + index);
        }

        public override void ArgumentByRef<T>(int index, ref T value)
        {
            //myTrace.add("arg ref: " + index);
        }

        public override void ArgumentPtr(int index, UIntPtr value, TypeEx pointerType)
        {
            //myTrace.add("arg ptr: " + index);
        }

        public override void ArgumentTypedReference(int index, TypedReference typedReference)
        {
            //myTrace.add("arg typRef: " + index);
        }

        public override void ArgumentNotSupported(int index)
        {
            //myTrace.add("arg: done; " + index);
        }

        /// <summary>
        /// Just before returning from method body.
        /// </summary>
        /// <remarks>Only method allowed to pop from callstack.</remarks>
        public override void LeaveMethod()
        {
            //trace.Add("Leaving: " + callStack.Peek());
            Method leaving = callStack.Pop();
            if (leaving.FullName.Equals(currentAction))
            {
                recordRW = false;
            }
            /*if(!leaving.FullName.Contains("Microsoft.PSharp"))
                trace.Add("leaving: " + leaving);*/
        }

        /// <summary>
        /// Constructor call.
        /// </summary>
        /// <param name="method"></param>
        public override void Newobj(Method method)
        {

        }

        /// <summary>
        /// Regular instruction; <see cref="System.Reflection.Emit.OpCodes.Call"/>
        /// </summary>
        /// <param name="method">callee</param>
        public override void Call(Method method)
        {
            trace.Add("Method Call: " + method.FullName);
            if ((method.FullName.Contains("Microsoft.PSharp.Machine.CreateMachine") ||
                method.FullName.Contains("Microsoft.PSharp.PSharpRuntime.CreateMachine")) &&
                !callStack.Peek().FullName.Contains(".Main"))
            {
                isCreateMachine = true;
                trace.Add("call: " + method + " " + isCreateMachine);
            }

            else if (method.FullName.Equals("Microsoft.PSharp.Machine.Do"))
            {
                isDoCalled = true;
                trace.Add("do called");
            }

            else if (method.FullName.Equals("Microsoft.PSharp.Machine.ExecuteCurrentStateOnEntry"))
                isEntryFuntionCalled = true;

            else if (method.FullName.Equals("Microsoft.PSharp.Machine.ExecuteCurrentStateOnExit"))
                isExitFuntionCalled = true;

            else if ((method.FullName.Equals("Microsoft.PSharp.Machine.Send") ||
                method.FullName.Equals("Microsoft.PSharp.PSharpRuntime.SendEvent")) &&
                !callStack.Peek().FullName.Contains(".Main"))
            {
                trace.Add("send: " + method.FullName);
                ThreadTrace obj = thTrace[thTrace.Count - 1];

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

                    ThreadTrace obj = thTrace[thTrace.Count - 1];
                    obj.accesses.Add(new ActionInstr(r.GetHashCode(), true));
                }
            }
            catch (Exception)
            {
                try
                {
                    Task tid = (Task)value;
                    trace.Add("Task created: " + tid.Id);
                    if(taskMethods.Count > 0)
                    {
                        Method m = taskMethods[taskMethods.Count - 1].Item1;
                        taskMethods[taskMethods.Count - 1] = new Tuple<Method, int>(m, tid.Id);
                        ThreadTrace obj = thTrace[thTrace.Count - 1];
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
            if (isDoCalled || isEntryFuntionCalled || isExitFuntionCalled)
            {
                /*if (!localIter.Equals(Environment.GetEnvironmentVariable("ITERATION")))
                {
                    //Console.WriteLine("iteration changed for {0} from {1} to {2}", threadIndex, localIter, Environment.GetEnvironmentVariable("ITERATION"));

                    if (thTrace.Count > 0 && !localIter.Equals("-1"))
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
                            bformatter.Serialize(stream, thTrace);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("EXCEPTION: " + ex);
                        }
                        stream.Close();
                    }

                    thTrace = new List<ThreadTrace>();
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

                trace.Add("call receiver: " + mc.GetType() + " " + mcID);

                isDoCalled = false;
                isEntryFuntionCalled = false;
                isExitFuntionCalled = false;

                isAction = true;
                ThreadTrace obj = new ThreadTrace(mcID);

                if (actionIds.ContainsKey(mcID))
                {
                    actionIds[mcID]++;
                    trace.Add("action id: " + actionIds[mcID]);
                }
                else
                {
                    actionIds.Add(mcID, 1);
                    trace.Add("action id: 1");
                }

                obj.set(actionIds[mcID]);
                thTrace.Add(obj);
            }
        }

        /// <summary>
        /// Regular instruction; <see cref="System.Reflection.Emit.OpCodes.Callvirt"/>
        /// </summary>
        /// <param name="method">callee before vtable lookup</param>
        public override void Callvirt(Method method)
        {
            trace.Add("Virtual call: " + method.FullName);
            if (method.FullName.Contains("System.Threading.Tasks.Task.Start"))
            {
                taskMethods.Add(new Tuple<Method, int>(callStack.Peek(), -1));
            }
        }

        /// <summary>
        /// This method is called after <see cref="Callvirt"/> to indicate the 
        /// actual type of the receiver object.
        /// </summary>
        /// <remarks>
        /// This method is only called when the receiver was not null,
        /// and an exception is thrown otherwise.
        /// When the receiver is a boxed value, 
        /// <paramref name="type"/> is the value type.
        /// When a constrained call is performed, 
        /// the type is the type pointed to by the receiver pointer.
        /// When the receiver is a remote object, <paramref name="type"/>
        /// might not be a class but an interface.
        /// </remarks>
        /// <param name="codeLabel">code label</param>
        /// <param name="type">actual receiver type</param>
        public override void CallvirtType(TypeEx type, int codeLabel)
        {

        }

        #endregion

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
