using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.ReliableServices
{
    class BugFindingRsmHostMachine : Machine
    {
        BugFindingRsmHost Host;
        Type machineType;
        RsmInitEvent initEvent;
        MachineId HostedMachineId;

        private async Task EventHandlerLoop()
        {
            var machineRestartRequired = false;

            while (true)
            {
                try
                {
                    var writeTx = false;
                    var dequeued = true;

                    var tx = Host.StateManager.CreateTransaction();
                    Host.SetTransaction(tx);
                    Host.SetReliableRegisterTx();

                    using (tx)
                    {
                        if (machineRestartRequired)
                        {
                            machineRestartRequired = false;
                            //await InitializationTransaction(machineType, ev);
                            //await PersistStateStack();
                            writeTx = true;
                        }
                        else
                        {
                            await EventHandler();
                            //var stackChanged = await PersistStateStack();
                            writeTx = (dequeued || stackChanged);
                        }

                        if (writeTx)
                        {
                            await tx.CommitAsync();
                        }
                    }

                    Host.StackChanges = new StackDelta();
                    await ExecutePendingWork();
                    break;
                }
                catch (Exception ex) when (ex is TimeoutException || ex is System.Fabric.TransactionFaultedException)
                {
                    Host.MachineFailureException = null;
                    Host.MachineHalted = false;
                    machineRestartRequired = true;

                    Host.StackChanges = new StackDelta();
                    Host.PendingMachineCreations.Clear();
                }
            }

        }

        /// <summary>
        /// Returns true if dequeued
        /// </summary>
        /// <returns></returns>
        private async Task EventHandler()
        {
            await this.Id.Runtime.SendEventAndExecute(HostedMachineId, this.ReceivedEvent);

            if (Host.MachineFailureException != null &&
                (Host.MachineFailureException is TimeoutException || Host.MachineFailureException is System.Fabric.TransactionFaultedException))
            {
                throw Host.MachineFailureException;
            }
        }
    }


    class BugFindingRsmHostMachineInitEvent : Event
    {

    }
}
