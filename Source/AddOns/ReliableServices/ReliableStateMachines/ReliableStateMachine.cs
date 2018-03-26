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
        protected IRsmHost Host;

        internal override void OnStatePush(string nextState)
        {
            
        }
    }
}
