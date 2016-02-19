using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace TicketBoxOfficeRacy
{
    class BoxOfficeMachine : Machine
    {
        #region events
        public class eBuyAtSeat : Event { }

        public class eBuyContiguous : Event { }
        #endregion

        #region classes
        public class TicketBoxOffice
        {
            public readonly int RowsInVenue;
            public readonly int SeatsPerRow;

            public bool[,] _seatSoldStates;

            public TicketBoxOffice(int rowsInVenue, int seatsPerRow)
            {
                RowsInVenue = rowsInVenue;
                SeatsPerRow = seatsPerRow;
                _seatSoldStates = new bool[RowsInVenue, SeatsPerRow];
            }
        }

        public class Ticket
        {

            public Ticket(int row, int seat)
            {
                Row = row;
                Seat = seat;
            }

            public int Row;
            public int Seat;

        }
        #endregion

        #region fields
        private Ticket t0, t1, t2, t3, t4;
        #endregion

        #region states
        [Start]
        [OnEventDoAction(typeof(eBuyAtSeat), nameof(OnBuyAtSeat))]
        [OnEventDoAction(typeof(eBuyContiguous), nameof(OnBuyContiguous))]
        private class Init : MachineState { }
        #endregion

        #region actions
        private void OnBuyAtSeat()
        {
            Console.WriteLine("But tickets at seat");
            var boxOffice = new TicketBoxOffice(4, 8);
            t0 = t1 = t2 = t3 = t4 = null;
            MachineId m1 = CreateMachine(typeof(TicketMachine));
            Send(m1, new TicketMachine.eBuyTicket(0, 3, boxOffice, t0));

            MachineId m2= CreateMachine(typeof(TicketMachine));
            Send(m2, new TicketMachine.eBuyTicket(1, 5, boxOffice, t1));

            MachineId m3 = CreateMachine(typeof(TicketMachine));
            Send(m3, new TicketMachine.eBuyTicket(2, 2, boxOffice, t2));

            MachineId m4 = CreateMachine(typeof(TicketMachine));
            Send(m4, new TicketMachine.eBuyTicket(2, 6, boxOffice, t3));

            MachineId m5 = CreateMachine(typeof(TicketMachine));
            Send(m5, new TicketMachine.eBuyTicket(2, 2, boxOffice, t4));

            /*Assert(t0 != null, "t0 is null");
            Assert(t1 != null, "t1 is null");
            Assert(t3 != null, "t3 is null");

            Assert(t2 != null || t4 != null, "At least one of t2/t4 should've been successful.");
            Assert(t2 == null || t4 == null, "At least one of t2/t4 should've been unsuccessful.");
            Console.WriteLine("asserts successful");*/
        }

        private void OnBuyContiguous()
        {
            Console.WriteLine("But tickets contiguously");
            var boxOffice = new TicketBoxOffice(4, 8);

            MachineId m1 = CreateMachine(typeof(TicketMachine));
            Send(m1, new TicketMachine.eBuyTicket(0, 3, boxOffice, t0));

            MachineId m2 = CreateMachine(typeof(TicketMachine));
            Send(m2, new TicketMachine.eBuyTicket(1, 5, boxOffice, t1));

            MachineId m3 = CreateMachine(typeof(TicketMachine));
            Send(m3, new TicketMachine.eBuyTicket(2, 2, boxOffice, t2));

            MachineId m4 = CreateMachine(typeof(TicketMachine));
            Send(m4, new TicketMachine.eBuyTicket(2, 6, boxOffice, t3));

            /*Assert(t0 != null, "t0 is null");
            Assert(t1 != null, "t1 is null");
            Assert(t1 != null, "t1 is null");
            Assert(t3 != null, "t3 is null");
             Assert.IsNull(boxOffice.BuyTicketAtSeat(2, 6));
            */

            MachineId mc1 = CreateMachine(typeof(TicketMachine));
            List<Ticket> tl = new List<Ticket>();
            Send(mc1, new TicketMachine.eBuyContiguous(1, boxOffice, tl));
            Assert(1 == tl.Count);

            MachineId mc2 = CreateMachine(typeof(TicketMachine));
            List<Ticket> tl2 = new List<Ticket>();
            Send(mc2, new TicketMachine.eBuyContiguous(3, boxOffice, tl2));
            Assert(3 == tl2.Count);

            MachineId mc3 = CreateMachine(typeof(TicketMachine));
            List<Ticket> tl3 = new List<Ticket>();
            Send(mc3, new TicketMachine.eBuyContiguous(8, boxOffice, tl3));
            Assert(8 == tl3.Count);
        }
        #endregion
    }
}