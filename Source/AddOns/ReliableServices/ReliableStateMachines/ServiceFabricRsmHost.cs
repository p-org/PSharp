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
        private IReliableConcurrentQueue<EventInfo> InputQueue;

        /// <summary>
        /// For creating unique RsmIds
        /// </summary>
        private ServiceFabricRsmIdFactory IdFactory;

        private ServiceFabricRsmHost(IReliableStateManager stateManager, ServiceFabricRsmIdFactory factory)
            : base(stateManager)
        {
            this.IdFactory = factory;
        }

        private async Task Initialize(ServiceFabricRsmId id, Type machineType, RsmInitEvent ev)
        {
            this.Id = id;

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

    }
}
