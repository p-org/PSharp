using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ListRacy
{
    class GodMachine : Machine
    {
        #region events
        #endregion

        #region fields
        private List<int> intList;
        #endregion

        #region states
        [Start]
        [OnEntry(nameof(OnInit))]
        private class Init : MachineState { }
        #endregion

        #region actions
        private void OnInit()
        {
            intList = new List<int>();
            intList.Add(8);

            MachineId sId = CreateMachine(typeof(SubMachine));
            Send(sId, new SubMachine.evt(intList));

            Console.WriteLine("************ Adding to list *****************");
            intList.Add(7);
            Console.WriteLine("************ Added to list *****************");
        }
        #endregion
    }
}
