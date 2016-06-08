using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Chameneos
{
    internal class Chameneos : Machine
    {
        internal class Config : Event
        {
            public int Id;
            public MachineId Broker;
            public Colour Colour;

            public Config(int id, MachineId broker, Colour colour)
                : base()
            {
                this.Id = id;
                this.Broker = broker;
                this.Colour = colour;
            }
        }
        
        internal class GetNumber : Event
        {
            public int Number;

            public GetNumber(int num)
                : base()
            {
                this.Number = num;
            }
        }

        internal class GetNumberAck : Event
        {
            public string Number;

            public GetNumberAck(string num)
                : base()
            {
                this.Number = num;
            }
        }

        internal class GetStringAck : Event
        {
            public string Text;

            public GetStringAck(string text)
                : base()
            {
                this.Text = text;
            }
        }

        internal class GetCountAck : Event
        {
            public int Count;

            public GetCountAck(int count)
                : base()
            {
                this.Count = count;
            }
        }

        internal class Start : Event { }
        internal class GetString : Event { }
        internal class GetCount : Event { }

        int CreatureId;

        MachineId Broker;

        Colour Colour;

        int MyHooks;

        int SelfHooks;

        [Microsoft.PSharp.Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventGotoState(typeof(Start), typeof(Active))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.CreatureId = (this.ReceivedEvent as Config).Id;
            this.Broker = (this.ReceivedEvent as Config).Broker;
            this.Colour = (this.ReceivedEvent as Config).Colour;

            this.MyHooks = 0;
            this.SelfHooks = 0;
        }

        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventGotoState(typeof(Start), typeof(Active))]
        [OnEventDoAction(typeof(Broker.Hook), nameof(DoHook))]
        [OnEventDoAction(typeof(Chameneos.GetNumber), nameof(HandleGetNumber))]
        [OnEventDoAction(typeof(Chameneos.GetCount), nameof(HandleGetCount))]
        [OnEventDoAction(typeof(Chameneos.GetString), nameof(HandleGetString))]
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            this.Send(this.Broker, new Broker.Hook(this.Id, this.Colour));
        }

        void DoHook()
        {
            var creature = (this.ReceivedEvent as Broker.Hook).Creature;
            var colour = (this.ReceivedEvent as Broker.Hook).Colour;

            this.Colour = this.DoCompliment(this.Colour, colour);

            if (this.Id.Equals(creature))
            {
                this.SelfHooks = this.SelfHooks + 1;
            }

            this.MyHooks = this.MyHooks + 1;

            this.Raise(new Chameneos.Start());
        }

        void HandleGetNumber()
        {
            var num = (this.ReceivedEvent as Chameneos.GetNumber).Number;
            var str = this.CreateNumber(num);
            this.Send(this.Broker, new GetNumberAck(str));
        }

        void HandleGetCount()
        {
            this.Send(this.Broker, new GetCountAck(this.MyHooks));
        }

        void HandleGetString()
        {
            var str = MyHooks.ToString() + this.CreateNumber(this.SelfHooks);
            this.Send(this.Broker, new GetStringAck(str));
        }

        string CreateNumber(int n)
        {
            string str = "";
            string nStr = n.ToString();

            for (int i = 0; i < nStr.Length; i++)
            {
                str = str + " ";
                str = str + nStr[i];
            }

            return str;
        }

        Colour DoCompliment(Colour c1, Colour c2)
        {
            if (c1 == Colour.Blue)
            {
                if (c1 == Colour.Blue)
                {
                    return Colour.Blue;
                }
                else if (c1 == Colour.Red)
                {
                    return Colour.Yellow;
                }
                else
                {
                    return Colour.Red;
                }
            }
            else if (c1 == Colour.Red)
            {
                if (c1 == Colour.Blue)
                {
                    return Colour.Yellow;
                }
                else if (c1 == Colour.Red)
                {
                    return Colour.Red;
                }
                else
                {
                    return Colour.Blue;
                }
            }
            else
            {
                if (c1 == Colour.Blue)
                {
                    return Colour.Red;
                }
                else if (c1 == Colour.Red)
                {
                    return Colour.Blue;
                }
                else
                {
                    return Colour.Yellow;
                }
            }
        }
    }
}
