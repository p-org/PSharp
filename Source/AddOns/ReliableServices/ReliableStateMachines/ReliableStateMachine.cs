using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace Microsoft.PSharp.ReliableServices
{
    public abstract class ReliableStateMachine : Machine
    {
        /// <summary>
        /// RSM Host
        /// </summary>
        protected RsmHost Host { get; private set; }

        /// <summary>
        /// Currently executing transaction
        /// </summary>
        protected ITransaction CurrentTransaction
        {
            get
            {
                return Host.CurrentTransaction;
            }
        }

        /// <summary>
        /// For initializing a machine, on creation or restart
        /// </summary>
        /// <returns></returns>
        protected abstract Task OnActivate();

        /// <summary>
        /// Initializes the RSM
        /// </summary>
        /// <returns></returns>
        [MachineConstructor]
        async Task RsmInitialization()
        {
            var re = this.ReceivedEvent as RsmInitEvent;
            this.Assert(re != null, "Internal error in RSM initialization");
            this.Host = re.Host;

            await OnActivate();
        }

        #region RSM User API

        /// <summary>
        /// Reliable (persistent, failover resistant) identifier of this machine
        /// </summary>
        protected IRsmId ReliableId
        {
            get
            {
                return this.Host.Id;
            }
        }

        /// <summary>
        /// Creates an RSM in the local partition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Unique identifier for the machine</returns>
        protected Task<IRsmId> ReliableCreateMachine<T>() where T : ReliableStateMachine
        {
            return Host.ReliableCreateMachine<T>(new RsmInitEvent());
        }

        /// <summary>
        /// Creates an RSM in the local partition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="startingEvent">Starting event for the machine</param>
        /// <returns>Unique identifier for the machine</returns>
        protected Task<IRsmId> ReliableCreateMachine<T>(RsmInitEvent startingEvent) where T : ReliableStateMachine
        {
            return Host.ReliableCreateMachine<T>(startingEvent);
        }

        /// <summary>
        /// Creates an RSM in the specified partition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="partitionName">Partition name</param>
        /// <returns>Unique identifier for the machine</returns>
        protected Task<IRsmId> ReliableCreateMachine<T>(string partitionName) where T : ReliableStateMachine
        {
            return Host.ReliableCreateMachine<T>(new RsmInitEvent(), partitionName);
        }

        /// <summary>
        /// Creates an RSM in the specified partition
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="startingEvent">Starting event for the machine</param>
        /// <param name="partitionName">Partition name</param>
        /// <returns>Unique identifier for the machine</returns>
        protected Task<IRsmId> ReliableCreateMachine<T>(RsmInitEvent startingEvent, string partitionName) where T : ReliableStateMachine
        {
            return Host.ReliableCreateMachine<T>(startingEvent, partitionName);
        }

        /// <summary>
        /// Sends an event to an RSM
        /// </summary>
        /// <param name="target">Target RSM identifier</param>
        /// <param name="e">Event</param>
        /// <returns></returns>
        protected Task ReliableSend(IRsmId target, Event e)
        {
            return Host.ReliableSend(target, e);
        }

        /// <summary>
        /// Starts a periodic timer
        /// </summary>
        /// <param name="name">Name of the timer</param>
        /// <param name="period">Periodic interval (ms)</param>
        /// <returns></returns>
        protected Task StartTimer(string name, int period)
        {
            return Host.StartTimer(name, period);
        }

        /// <summary>
        /// Stops a timer
        /// </summary>
        /// <param name="name">Name of the timer</param>
        /// <returns></returns>
        protected Task StopTimer(string name)
        {
            return Host.StopTimer(name);
        }

        #endregion

        #region Internal callbacks

        /// <summary>
        /// Invokes user callback when a machine throws an exception.
        /// </summary>
        /// <param name="ex">The exception thrown by the machine</param>
        /// <param name="methodName">The handler (outermost) that threw the exception</param>
        /// <returns>False if the exception should continue to get thrown, true if it was handled in this method</returns>
        internal override bool OnExceptionHandler(string methodName, Exception ex)
        {
            if (ex is ExecutionCanceledException)
            {
                // internal exception, used by PsharpTester
                return false;
            }

            this.Logger.OnMachineExceptionThrown(this.Id, CurrentStateName, methodName, ex);
            Host.NotifyFailure(ex, methodName);
            OnExceptionRequestedGracefulHalt = true;
            return false;
        }

        /// <summary>
        /// Invokes user callback when a machine receives an event it cannot handle
        /// </summary>
        /// <param name="ex">The exception thrown by the machine</param>
        /// <param name="methodName">The handler (outermost) that threw the exception</param>
        /// <returns>False if the exception should continue to get thrown, true if the machine should gracefully halt</returns>
        internal override bool OnUnhandledEventExceptionHandler(string methodName, UnhandledEventException ex)
        {
            this.Logger.OnMachineExceptionThrown(this.Id, ex.CurrentStateName, methodName, ex);
            Host.NotifyFailure(ex, methodName);
            OnExceptionRequestedGracefulHalt = true;
            return true;
        }

        /// <summary>
        /// Notify state push
        /// </summary>
        /// <param name="nextState"></param>
        internal override void OnStatePush(string nextState)
        {
            Host?.NotifyStatePush(nextState);
        }

        /// <summary>
        /// Notify state pop
        /// </summary>
        internal override void OnStatePop()
        {
            Host?.NotifyStatePop();
        }

        #endregion

        protected override void OnHalt()
        {
            Host.NotifyHalt();
            base.OnHalt();
        }
    }
}
