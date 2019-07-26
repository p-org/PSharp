using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.Scheduling.Strategies;

namespace PartialOrderSlicingClient
{
    /// <summary>
    /// There is one within the P# project. Use that for the time being
    /// </summary>
    public class NotProgramModelReplayStrategy : ISchedulingStrategy
    {


        public void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            throw new NotImplementedException("");
        }

        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            throw new NotImplementedException();
        }

        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            throw new NotImplementedException();
        }

        public string GetDescription()
        {
            throw new NotImplementedException();
        }

        public bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            throw new NotImplementedException();
        }

        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            throw new NotImplementedException();
        }

        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            throw new NotImplementedException();
        }

        public int GetScheduledSteps()
        {
            throw new NotImplementedException();
        }

        public bool HasReachedMaxSchedulingSteps()
        {
            throw new NotImplementedException();
        }

        public bool IsFair()
        {
            throw new NotImplementedException();
        }

        public bool PrepareForNextIteration()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
