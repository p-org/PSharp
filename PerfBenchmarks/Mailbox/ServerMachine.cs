using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mailbox.MailerMachine;

namespace Mailbox
{
    internal class ServerMachine : Machine
    {
        /// <summary>
        /// The number of messages received so far
        /// </summary>
        long total = 0L;

        /// <summary>
        /// The number of messages we expect to receive
        /// </summary>
        long TotalMessageCount;

        /// <summary>
        /// Is set to true when the number of messages we
        /// expect to receive is equal to the number of
        /// messages we have received
        /// </summary>
        TaskCompletionSource<bool> hasCompleted;


        internal class Config : Event
        {            
            public long TotalMessageCount;
            public TaskCompletionSource<bool> hasCompleted;
            public Config(long MessageCount, TaskCompletionSource<bool> hasCompleted)
            {            
                this.TotalMessageCount = MessageCount;
                this.hasCompleted = hasCompleted;
            }
        }

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(Ping), nameof(Process))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            var e = this.ReceivedEvent as Config;
            this.TotalMessageCount = e.TotalMessageCount;
            this.hasCompleted = e.hasCompleted;
        }

        void Process()
        {
            this.total++;
            if (this.total % 100000 == 0)
            {
                Console.WriteLine("Processed {0} messages", this.total);
            }
            if (this.total == this.TotalMessageCount)
            {
                this.hasCompleted.SetResult(true);
            }
        }
        
    }
}
