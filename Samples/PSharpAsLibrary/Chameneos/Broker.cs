using System;
using System.Collections.Generic;
using Microsoft.PSharp;

namespace Chameneos
{
    internal class Broker : Machine
    {
        internal class Config : Event
        {
            public int Input;

            public Config(int input)
                : base()
            {
                this.Input = input;
            }
        }

        internal class GotoSecondRound : Event
        {
            public int TotalRendezvous;

            public GotoSecondRound(int totalRendezvous)
                : base()
            {
                this.TotalRendezvous = totalRendezvous;
            }
        }

        internal class Hook : Event
        {
            public MachineId Creature;
            public Colour Colour;

            public Hook(MachineId creature, Colour colour)
                : base()
            {
                this.Creature = creature;
                this.Colour = colour;
            }
        }

        List<MachineId> Creatures;

        int TotalRendezvous;
        MachineId FirstHooker;
        Colour FirstColour;

        int TotalCreatures;
        int TotalStoppedCreatures;
        int Round;

        int TR;
        int TT;

        List<List<Colour>> Groups;

        int Counter;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.TR = (this.ReceivedEvent as Config).Input;
            this.TotalRendezvous = this.TR;
            this.Round = 1;

            this.Groups = new List<List<Colour>>();
            this.Groups.Add(new List<Colour> {
                Colour.Blue, Colour.Red, Colour.Yellow });
            this.Groups.Add(new List<Colour> {
                Colour.Blue, Colour.Red, Colour.Yellow,
                Colour.Red, Colour.Yellow, Colour.Blue,
                Colour.Red, Colour.Yellow, Colour.Red,
                Colour.Blue});
            
            this.Goto(typeof(Booting));
        }

        [OnEntry(nameof(BootingOnEntry))]
        [OnEventGotoState(typeof(GotoSecondRound), typeof(SecondRound))]
        [OnEventDoAction(typeof(Hook), nameof(DoHook))]
        [OnEventDoAction(typeof(Chameneos.GetCountAck), nameof(ProcessPostRoundResults))]
        [OnEventDoAction(typeof(Chameneos.GetStringAck), nameof(HandleGetStringAck))]
        [OnEventDoAction(typeof(Chameneos.GetNumberAck), nameof(HandleGetNumberAck))]
        class Booting : MachineState { }

        void BootingOnEntry()
        {
            this.TT = 0;
            this.Counter = 0;

            this.Creatures = new List<MachineId>();
            this.TotalCreatures = this.Groups[0].Count;
            this.TotalStoppedCreatures = 0;

            for (int idx = 0; idx < this.Groups[0].Count; idx++)
            {
                var creature = this.CreateMachine(typeof(Chameneos),
                    new Chameneos.Config(idx, this.Id, this.Groups[0][idx]));
                this.Creatures.Add(creature);
                this.Send(creature, new Chameneos.Start());
            }
        }

        [OnEntry(nameof(SecondRoundOnEntry))]
        [OnEventDoAction(typeof(Hook), nameof(DoHook))]
        [OnEventDoAction(typeof(Chameneos.GetCountAck), nameof(ProcessPostRoundResults))]
        [OnEventDoAction(typeof(Chameneos.GetStringAck), nameof(HandleGetStringAck))]
        [OnEventDoAction(typeof(Chameneos.GetNumberAck), nameof(HandleGetNumberAck))]
        class SecondRound : MachineState { }

        void SecondRoundOnEntry()
        {
            this.TotalRendezvous = (this.ReceivedEvent as GotoSecondRound).TotalRendezvous;
            this.Creatures = new List<MachineId>();
            this.TotalCreatures = this.Groups[1].Count;
            this.TotalStoppedCreatures = 0;

            for (int idx = 0; idx < this.Groups[1].Count; idx++)
            {
                var creature = this.CreateMachine(typeof(Chameneos),
                    new Chameneos.Config(idx + 10, this.Id, this.Groups[1][idx]));
                this.Creatures.Add(creature);
                this.Send(creature, new Chameneos.Start());
            }
        }

        void DoHook()
        {
            var creature = (this.ReceivedEvent as Hook).Creature;
            var colour = (this.ReceivedEvent as Hook).Colour;

            if (this.TotalRendezvous == 0)
            {
                this.TotalStoppedCreatures++;
                this.DoPostRoundProcessing();
                return;
            }

            if (this.FirstHooker == null)
            {
                this.FirstHooker = creature;
                this.FirstColour = colour;
            }
            else
            {
                this.Send(this.FirstHooker, new Hook(creature, colour));
                this.Send(creature, new Hook(this.FirstHooker, this.FirstColour));
                this.FirstHooker = null;
                this.TotalRendezvous = this.TotalRendezvous - 1;
            }
        }

        void DoPostRoundProcessing()
        {
            if (this.TotalCreatures == this.TotalStoppedCreatures)
            {
                foreach (var creature in this.Creatures)
                {
                    this.Send(creature, new Chameneos.GetString());
                    this.Send(creature, new Chameneos.GetCount());
                    this.Send(creature, new Chameneos.GetNumber(this.TT));
                }
            }
            else
            {
                foreach (var c in this.Creatures)
                {
                    this.Send(c, new Halt());
                }

                this.Send(this.Id, new Halt());
            }
        }

        void ProcessPostRoundResults()
        {
            this.TT = this.TT + (this.ReceivedEvent as Chameneos.GetCountAck).Count;

            this.Counter = this.Counter + 1;

            if (this.Counter == this.Creatures.Count)
            {
                if (this.Round == 1)
                {
                    foreach (var c in this.Creatures)
                    {
                        this.Send(c, new Halt());
                    }

                    this.Round = 2;
                    this.TT = 0;
                    this.Counter = 0;
                    this.Raise(new GotoSecondRound(this.TR));
                }
            }
        }

        void HandleGetStringAck()
        {
            var str = (this.ReceivedEvent as Chameneos.GetStringAck).Text;
        }

        void HandleGetNumberAck()
        {
            var num = (this.ReceivedEvent as Chameneos.GetNumberAck).Number;
        }
    }
}
