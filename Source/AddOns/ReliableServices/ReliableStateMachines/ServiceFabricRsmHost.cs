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
    /// <summary>
    /// Object hosting an RSM
    /// </summary>
    public sealed class ServiceFabricRsmHost : RsmHost
    {
        /// <summary>
        /// Persistent current state (stack)
        /// </summary>
        private IReliableDictionary<int, string> StateStackStore;

        /// <summary>
        /// Inbox
        /// </summary>
        private IReliableConcurrentQueue<Event> InputQueue;

        /// <summary>
        /// For creating unique RsmIds
        /// </summary>
        private ServiceFabricRsmIdFactory IdFactory;

        /// <summary>
        /// Has the machine halted
        /// </summary>
        private bool MachineHalted;

        /// <summary>
        /// Machine failed with an exception
        /// </summary>
        private Exception MachineFailureException;

        private ServiceFabricRsmHost(IReliableStateManager stateManager, ServiceFabricRsmId id, ServiceFabricRsmIdFactory factory)
            : base(stateManager)
        {
            this.Id = id;
            this.IdFactory = factory;

            MachineHalted = false;
            MachineFailureException = null;
        }

        private async Task Initialize(Type machineType, RsmInitEvent ev)
        {
            InputQueue = await StateManager.GetOrAddAsync<IReliableConcurrentQueue<Event>>(GetInputQueueName(this.Id));
            StateStackStore = await StateManager.GetOrAddAsync<IReliableDictionary<int, string>>(string.Format("StateStackStore.{0}", this.Id.Name));

            Runtime = PSharpRuntime.Create();

            // TODO: retry policy
            while (true)
            {
                try
                {
                    using (CurrentTransaction = StateManager.CreateTransaction())
                    {
                        await InitializationTransaction(machineType, ev);
                        await PersistStateStack();
                        await CurrentTransaction.CommitAsync();
                    }
                    break;
                }
                catch (Exception ex) when (ex is TimeoutException || ex is System.Fabric.TransactionFaultedException)
                {
                    MachineFailureException = null;
                    MachineHalted = false;
                    // retry
                    await Task.Delay(100);
                    continue;
                }
            }

            RunMachine();
        }

        private void RunMachine()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await EventHandlerLoop();
                    if (MachineHalted)
                    {
                        return;
                    }
                    await Task.Delay(100);
                }
            });
        }

        private async Task InitializationTransaction(Type machineType, RsmInitEvent ev)
        {
            var stack = new List<string>();

            var cnt = await StateStackStore.GetCountAsync(CurrentTransaction);
            if (cnt != 0)
            {
                for (int i = 0; i < cnt; i++)
                {
                    var s = await StateStackStore.TryGetValueAsync(CurrentTransaction, i);
                    stack.Add(s.Value);
                }

                this.Mid = await Runtime.CreateMachineAndExecute(machineType, new ResumeEvent(stack, new RsmInitEvent(this)));
            }
            else
            {
                this.Mid = await Runtime.CreateMachineAndExecute(machineType, ev);
            }

            if (MachineFailureException != null &&
                (MachineFailureException is TimeoutException || MachineFailureException is System.Fabric.TransactionFaultedException))
            {
                throw MachineFailureException;
            }

        }

        private async Task EventHandlerLoop()
        {
            var machineRestartRequired = false;

            // TODO: retry policy
            while (!MachineHalted)
            {
                try
                {
                    using (CurrentTransaction = StateManager.CreateTransaction())
                    {
                        if(machineRestartRequired)
                        {
                            machineRestartRequired = false;
                            await InitializationTransaction(HostedMachineType, null);
                        }

                        var ret1 = await EventHandler();
                        var ret2 = await PersistStateStack();

                        if (ret1 || ret2)
                        {
                            await CurrentTransaction.CommitAsync();
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                catch (Exception ex) when (ex is TimeoutException || ex is System.Fabric.TransactionFaultedException)
                {
                    MachineFailureException = null;
                    MachineHalted = false;
                    machineRestartRequired = true;
                    // retry
                    await Task.Delay(100);
                    continue;
                }
            }
        }

        private async Task<bool> EventHandler()
        {
            var cv = await InputQueue.TryDequeueAsync(CurrentTransaction);
            if (!cv.HasValue)
            {
                return false;
            }

            var ev = cv.Value;
            await Runtime.SendEventAndExecute(Mid, ev);

            if (MachineFailureException != null &&
                (MachineFailureException is TimeoutException || MachineFailureException is System.Fabric.TransactionFaultedException))
            {
                throw MachineFailureException;
            }

            return true;
        }

        private async Task<bool> PersistStateStack()
        {
            if (StackChanges.PopDepth == 0 && StackChanges.PushedSuffix.Count == 0)
            {
                return false;
            }

            var cnt = (int) await StateStackStore.GetCountAsync(CurrentTransaction);
            for (int i = cnt - 1; i > cnt - 1 - StackChanges.PopDepth; i--)
            {
                await StateStackStore.TryRemoveAsync(CurrentTransaction, i);
            }

            for (int i = 0; i < StackChanges.PushedSuffix.Count; i++)
            {
                await StateStackStore.AddAsync(CurrentTransaction, i + (cnt - StackChanges.PopDepth), 
                    StackChanges.PushedSuffix[i]);
            }

            return true;
        }

        public override Task<IRsmId> ReliableCreateMachine<T>(RsmInitEvent startingEvent)
        {
            throw new NotImplementedException();
        }

        public override Task<IRsmId> ReliableCreateMachine<T>(RsmInitEvent startingEvent, string partitionName)
        {
            throw new NotImplementedException();
        }

        public override Task ReliableSend(IRsmId target, Event e)
        {
            throw new NotImplementedException();
        }

        internal override void NotifyFailure(Exception ex, string methodName)
        {
            throw new NotImplementedException();
        }

        internal override void NotifyHalt()
        {
            throw new NotImplementedException();
        }

        static string GetInputQueueName(IRsmId id)
        {
            return string.Format("InputQueue.{0}", id.Name);
        }
    }
}
