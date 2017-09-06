using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mailbox
{
    class MailerMachine : Machine
    {
        internal class Config : Event
        {
            /// <summary>
            /// The number of messages this mailer needs to send
            /// </summary>
            public uint MessageCount;

            /// <summary>
            /// The server the messages need to be sent to
            /// </summary>
            public MachineId Server;

            public Config(MachineId Server, uint MessageCount)
            {
                this.Server = Server;
                this.MessageCount = MessageCount;
            }
        }

        internal class Ping : Event {   }

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(Config), nameof(InitOnEntry))]
        class Init : MachineState { }

        /// <summary>
        /// On initialization, the Config event sent by the environment
        /// tells us the number of messages we need to send, and who to 
        /// send them to
        /// </summary>
        void InitOnEntry()
        {            
            var e = this.ReceivedEvent as Config;
            for(int i = 0; i < e.MessageCount; i++)
            {
                this.Send(e.Server, new Ping());
            }
            Console.WriteLine("{0} is done sending sending messages", this.Id);
        }
    }
}
