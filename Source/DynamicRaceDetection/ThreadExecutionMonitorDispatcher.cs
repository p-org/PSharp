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

using Microsoft.ExtendedReflection.Collections;
using Microsoft.ExtendedReflection.Logging;
using Microsoft.ExtendedReflection.Metadata;
using Microsoft.ExtendedReflection.Monitoring;
using Microsoft.ExtendedReflection.Utilities.Safe.Diagnostics;

using Microsoft.PSharp.DynamicRaceDetection.CallsOnly;

namespace Microsoft.PSharp.DynamicRaceDetection.AllCallbacks
{
    /// <summary>
    /// Ignores all callbacks, except method/constructor calls and corresponding returns.
    /// </summary>
    /// <remarks>
    /// Cheap brother of InstructionInterpreter.
    /// </remarks>
    internal class ThreadExecutionMonitorDispatcher : Microsoft.ExtendedReflection.Monitoring.ThreadExecutionMonitorEmpty
    {
        // one of these per thread!
        readonly int threadIndex;
        readonly IThreadMonitor callMonitor;
        readonly IEventLog log;

        SafeList<String> trace = new SafeList<string>();
        List<ThreadTrace> thTrace = new List<ThreadTrace>();

        SafeStack<Method> callStack;
        private bool isDoCalled = false;
        private bool isEntryFuntionCalled = false;
        private bool isExitFuntionCalled = false;
        private bool isAction = false;
        private bool recordRW = false;
        private String currentAction;
        private int currentMachineId;
        private bool isCreateMachine = false;

        static Dictionary<int, int> actionIds = new Dictionary<int, int>();
        static Dictionary<int, int> sendIds = new Dictionary<int, int>();

        /// <summary>
        /// Constructor
        /// </summary>
        public ThreadExecutionMonitorDispatcher(IEventLog log, int threadIndex, IThreadMonitor callMonitor)
            : base(threadIndex)
        {
            SafeDebug.AssertNotNull(callMonitor, "callMonitor");

            this.log = log;
            this.threadIndex = threadIndex;
            this.callMonitor = callMonitor;

            //myTrace = new Trace.MyTrace();
            trace = new SafeList<string>();
            callStack = new SafeStack<Method>();
        }

        ~ThreadExecutionMonitorDispatcher()
        {
            /*Console.WriteLine("\nDestructor called " + threadIndex);
            foreach (var item in trace)
            {
                Console.WriteLine(item);
            }
            foreach (var item in thTrace)
            {
                Console.WriteLine("check: " + item.machineID + " " + item.actionName + " " + item.actionID);
                Console.WriteLine("memory accesses");
                foreach(var it in item.accesses)
                {
                    if (it.isSend)
                        Console.WriteLine("send");
                    else
                        Console.WriteLine(it.location + " " + " " + it.objectHash + " " + it.write);
                }
            }*/

            if (thTrace.Count > 0)
            {
                string path = "thTrace_" + threadIndex + ".osl";
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
        }

        [System.Diagnostics.Conditional("DEBUG")]
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
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses.Add(new ActionInstr(false, location, objH, objO, GetSourceLocation(location)));
                trace.Add("load: " + objH + " " + objO + " " + callStack.Peek());
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
                trace.Add("loaded value by ref: " + value.GetType());
                /*ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";ref: " + value;*/
                //trace.Add("loaded value: " + value.GetType());
                // thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }
        }

        public override void LoadedValueObject(object value)
        {
            if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                trace.Add("loaded value object: " + value.GetType());
                /*ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";obj: " + value;*/
                //trace.Add("loaded value: " + value.GetType());
                // thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }
        }

        public override void LoadedValuePtr(UIntPtr value, TypeEx pointerType)
        {
            if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                trace.Add("loaded value pointer: " + value.GetType() + " " + pointerType.GetType());
                /*ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";ptr: " + value;*/
                //trace.Add("loaded value: " + value.GetType());
                // thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }
        }

        public override void LoadedValueTypedReference(TypedReference typedReference)
        {
            if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp"))
            {
                trace.Add("loaded value typed reference: ");
                /*ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";typ_ref: ";*/
                //trace.Add("loaded value: " + value.GetType());
                // thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }
        }

        [DebuggerNonUserCodeAttribute]
        public override void Store(UIntPtr location, uint size, int codeLabel, bool is_volatile)
        {
            UIntPtr objH, objO;
            ObjectTracking.GetObjectHandle(location, out objH, out objO);

            if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && objH != null)
            {
                //trace.Add("got object handle: " + objH + " offset: " + objO);
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses.Add(new ActionInstr(true, location, objH, objO, GetSourceLocation(location)));
                trace.Add("store: " + objH + " " + objO + " " + callStack.Peek() + " " + (objH == UIntPtr.Zero));
            }
        }

        public override void StoredValue<T>(T value)
        {
            /*if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";val: " + value.ToString() + " = " + value.GetHashCode();
                //trace.Add("stored value: " + value + " " + value.GetHashCode());
                //thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }*/
        }

        public override void StoredValueByRef<T>(ref T value)
        {
            /*if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";ref: " + value.ToString() + " = " + value.GetHashCode();
                //trace.Add("stored value: " + value + " " + value.GetHashCode());
                //thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }*/
        }

        public override void StoredValueObject(object value)
        {
            /*if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";obj: " + value.ToString() + " = " + value.GetHashCode();
                //trace.Add("stored value: " + value + " " + value.GetHashCode());
                //thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }*/
        }

        public override void StoredValuePtr(UIntPtr value, TypeEx pointerType)
        {
            /*if (recordRW && !callStack.Peek().FullName.Contains("Microsoft.PSharp") && value != null)
            {
                ThreadTrace obj = thTrace[thTrace.Count - 1];
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";ptr: " + value.ToString() + " = " + value.GetHashCode();
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
                obj.accesses[obj.accesses.Count - 1].srcLocation += ";" + value.GetType() + " = " + value.ToString();
                //trace.Add("stored value: " + value + " " + value.GetHashCode());
                //thTrace[thTrace.Count - 1].accesses[thTrace[thTrace.Count - 1].accesses.Count - 1].set(value.GetHashCode());
            }
        }*/

        [DebuggerNonUserCodeAttribute]
        public override void AfterNewobjObject(object newObject)
        {
            trace.Add("new object: " + newObject.GetType() + " " + newObject.GetHashCode());
        }

        public override void AfterNewobj<T>(T newValue)
        {
            trace.Add("new struct? " + newValue.GetType() + " " + newValue.GetHashCode());
        }

        /// <summary>
        /// At start of method body.
        /// </summary>
        /// <remarks>Only one to push on callstack.</remarks>
        public override bool EnterMethod(Method method)
        {
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
            if ((method.FullName.Contains("Microsoft.PSharp.Machine.CreateMachine") ||
                method.FullName.Contains("Microsoft.PSharp.PSharpRuntime.CreateMachine")) &&
                !callStack.Peek().FullName.Contains(".Main"))
            {
                isCreateMachine = true;
                trace.Add("call: " + method + " " + isCreateMachine);
            }

            else if (method.FullName.Equals("Microsoft.PSharp.Machine.Do"))
                isDoCalled = true;

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
                    trace.Add("call result object: " + isCreateMachine + " " + r.GetHashCode().ToString());
                    isCreateMachine = false;

                    ThreadTrace obj = thTrace[thTrace.Count - 1];
                    obj.accesses.Add(new ActionInstr(r.GetHashCode(), true));
                }
            }
            catch (Exception)
            {

            }
        }

        public override void CallReceiver(object receiver)
        {
            if (isDoCalled || isEntryFuntionCalled || isExitFuntionCalled)
            {
                Machine mc = (Machine)receiver;
                int mcID = mc.GetHashCode();

                this.currentMachineId = mcID;

                trace.Add("call receiver: " + mc.GetType() + " " + mcID);

                isDoCalled = isDoCalled & false;
                isEntryFuntionCalled = isEntryFuntionCalled & false;
                isExitFuntionCalled = isExitFuntionCalled & false;

                isAction = true;
                ThreadTrace obj = new ThreadTrace(mcID);

                if (actionIds.ContainsKey(mcID))
                {
                    actionIds[mcID]++;
                }
                else
                {
                    actionIds.Add(mcID, 1);
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
    }
}

