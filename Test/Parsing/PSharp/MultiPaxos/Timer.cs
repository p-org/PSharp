using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp;

namespace MultiPaxos
{
    internal class Timer : ISendable
    {
        private MachineId Id;
        private MachineId Target;
        private int Timeout;

        void ISendable.Create(MachineId mid, object payload)
        {
            this.Id = mid;
            this.Target = (payload as Object[])[0] as MachineId;
            this.Timeout = (int)(payload as Object[])[1];
        }

        void ISendable.Send(Event e)
        {
            if (e.GetType() == typeof(startTimer))
            {
                this.Handle();
            }
        }

        private void Handle()
        {
            int i = 0;
            while (i < this.Timeout)
            {
                i++;
            }

            Runtime.SendEvent(this.Target, new timeout(this.Id));
        }
    }
}
