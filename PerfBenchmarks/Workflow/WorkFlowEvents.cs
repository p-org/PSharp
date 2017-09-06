using Microsoft.PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workflow
{
    class WorkFlowEvents
    {
        internal class WorkFlowCompletionEvent : Event
        {
            public readonly string message;
            public readonly long total;
            public WorkFlowCompletionEvent(string msg, long total = 0)
            {
                this.message = msg;
                this.total = total;
            }
        }

        internal class WorkFlowWorkEvent : Event
        {
            public long count { get; private set; }

            public WorkFlowWorkEvent(long count)
            {
                this.count = count;
            }

            public override string ToString()
            {
                return count.ToString();
            }
        }

        internal class WorkFlowStartEvent : Event
        {

        }
    }
}
