using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Creator
{
    public class TPLTest
    {
        Task[] Tasks;

        public void Start(int numOfTasks)
        {
            this.Tasks = new Task[numOfTasks];
            for (int idx = 0; idx < numOfTasks; idx++)
            {
                var task = new Task(() => { Execute(); });
                task.Start();
                this.Tasks[idx] = task;
            }

            Task.WaitAll(Tasks);
        }

        private void Execute()
        {
            Thread.SpinWait(50);
        }
    }
}
