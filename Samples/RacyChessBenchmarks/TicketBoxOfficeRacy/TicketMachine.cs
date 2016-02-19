using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace TicketBoxOfficeRacy
{
    class TicketMachine : Machine
    {
        #region events
        public class eBuyTicket : Event
        {
            public int row;
            public int col;
            public BoxOfficeMachine.TicketBoxOffice tbf;
            public BoxOfficeMachine.Ticket tckt;

            public eBuyTicket(int row, int col, BoxOfficeMachine.TicketBoxOffice tbf, BoxOfficeMachine.Ticket tckt)
            {
                this.row = row;
                this.col = col;
                this.tbf = tbf;
                this.tckt = tckt;
            }
        }

        public class eBuyContiguous : Event
        {
            public int number;
            public BoxOfficeMachine.TicketBoxOffice tbf;
            public List<BoxOfficeMachine.Ticket> tckts;

            public eBuyContiguous(int number, BoxOfficeMachine.TicketBoxOffice tbf, List<BoxOfficeMachine.Ticket> tckts)
            {
                this.number = number;
                this.tbf = tbf;
                this.tckts = tckts;
            }
        }
        #endregion

        #region fields
        #endregion

        #region states
        [Start]
        [OnEventDoAction(typeof(eBuyTicket), nameof(OnBuyTicket))]
        [OnEventDoAction(typeof(eBuyContiguous), nameof(OnBuyContiguous))]
        private class Init : MachineState { }
        #endregion

        #region actions
        private void OnBuyTicket()
        {
            int row = (this.ReceivedEvent as eBuyTicket).row;
            int seat = (this.ReceivedEvent as eBuyTicket).col;
            BoxOfficeMachine.TicketBoxOffice tbf = (this.ReceivedEvent as eBuyTicket).tbf;
            BoxOfficeMachine.Ticket ticket = (this.ReceivedEvent as eBuyTicket).tckt;

            if (tbf._seatSoldStates[row, seat])
                ticket = null;

            tbf._seatSoldStates[row, seat] = true;
            ticket = new BoxOfficeMachine.Ticket(row, seat);
        }

        private void OnBuyContiguous()
        {
            int count = (this.ReceivedEvent as eBuyContiguous).number;
            BoxOfficeMachine.TicketBoxOffice tbf = (this.ReceivedEvent as eBuyContiguous).tbf;
            List<BoxOfficeMachine.Ticket> tckts = (this.ReceivedEvent as eBuyContiguous).tckts;

            if (count <= 0)
                throw new ArgumentOutOfRangeException("count", count, "Must be greater than zero.");

            List<BoxOfficeMachine.Ticket> tickets = new List<BoxOfficeMachine.Ticket>();

            // Iterate thru each seat as a potential start to the contiguous seats in the theatre (row, seat)
            for (int row = 0; row < tbf.RowsInVenue; ++row)
            {
                for (int startSeat = 0; startSeat < tbf.SeatsPerRow; ++startSeat)
                {
                    // Is there potentially enough seats left in row from here?
                    if (!tbf._seatSoldStates[row, startSeat] && (tbf.SeatsPerRow - startSeat) >= count)
                    {
                        // Collect seats from the current seat up till count
                        // We're assuming first that 'count' seats are available starting with this seat
                        for (int seatOffset = 0; seatOffset < count; seatOffset++)
                        {
                            int seat = startSeat + seatOffset;
                            if (tbf._seatSoldStates[row, seat])
                            {
                                // there isn't enough contiguous seats from the start seat.
                                tickets.Clear();
                                break;
                            }
                            tickets.Add(new BoxOfficeMachine.Ticket(row, seat));
                        }

                        // If we've collected all the seats we need, then commit the sale
                        if (tickets.Count > 0)
                        {
                            Assert(tickets.Count == count);
                            // Mark the seat as being sold
                            foreach (var t in tickets)
                                tbf._seatSoldStates[t.Row, t.Seat] = true;
                            tckts = tickets;
                        }
                    }
                }
            }

            Assert(tickets.Count == 0, "If we got here, then we shouldn't have found any seats because the number of contiguous seats wasn't found.");
            tckts = tickets;
        }
        #endregion
    }
}
