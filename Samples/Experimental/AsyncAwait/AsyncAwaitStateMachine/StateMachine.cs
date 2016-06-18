using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncAwaitStateMachine
{
    internal class StateMachine
    {
        int x;
        int state;
        public TaskCompletionSource<int> resultTask;
        Task<int> currentTaskToAwait;

        public StateMachine(int x)
        {
            this.x = x;
            state = -1;
            resultTask = new TaskCompletionSource<int>();
        }

        public void MoveNext()
        {
            switch (state)
            {
                case -1:
                    Console.WriteLine("foo called");
                    Task<int> task = Task.Run(() => x * x);
                    state = 0;
                    currentTaskToAwait = task;
                    currentTaskToAwait.ContinueWith(_ => this.MoveNext());
                    break;
                case 0:
                    state = -2;
                    int res = currentTaskToAwait.Result;
                    Console.WriteLine("got result: " + res);
                    resultTask.SetResult(res);
                    break;

            }
        }
    }
}
