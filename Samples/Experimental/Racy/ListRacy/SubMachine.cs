using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace ListRacy
{
    class SubMachine : Machine
    {
        #region events
        public class evt : Event
        {
            public List<int> lst;

            public evt(List<int> lst)
            {
                this.lst = lst;
            }
        }
        #endregion

        #region fields
        #endregion

        #region states
        [Start]
        [OnEventDoAction(typeof(evt), nameof(OnEvt))]
        private class Init : MachineState { }
        #endregion

        #region actions
        private void OnEvt()
        {
            List<int> receivedList = (this.ReceivedEvent as evt).lst;
            for(int i = 0; i < receivedList.Count; i++)
            {
                Console.WriteLine(receivedList[i]);
            }
        }
        #endregion
    }
}
