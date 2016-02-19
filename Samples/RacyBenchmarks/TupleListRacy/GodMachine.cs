using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TupleListRacy
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
            Send(sId, new SubMachine.evt(new Tuple<List<int>>(intList)));

            intList.Add(7);
        }
        #endregion
    }
}
