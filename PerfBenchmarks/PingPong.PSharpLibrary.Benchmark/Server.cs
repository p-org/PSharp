using Microsoft.PSharp;


namespace PingPong.PSharpLibrary
{
    internal class Server : Machine
    {
        /// <summary>
        /// Event declaration of a 'Pong' event that does not contain any payload.
        /// </summary>
        internal class Pong : Event { }

        internal class Ack : Event { }

        public MachineId client;

        [Start]
        [OnEventDoAction(typeof(Messages.Started), nameof(Respond))]
        [OnEventDoAction(typeof(Messages.Msg), nameof(Respond))]
        [OnEventDoAction(typeof(Client.Register), nameof(Register))]
        /// </summary>
        class Active : MachineState { }

        void Respond()
        {            
            this.Send(client, this.ReceivedEvent);
        }

        void Register()
        {
            var e = ReceivedEvent as Client.Register;
            this.client = e.Client;
            this.Send(e.Client, new Ack());            
        }

    }
}