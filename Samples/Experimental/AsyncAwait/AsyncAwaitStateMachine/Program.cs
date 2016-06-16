using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncAwaitStateMachine
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncClass obj = new AsyncClass();
            obj.caller();
        }
    }

    class AsyncClass
    {
        public void caller()
        {
            Task<int> resultTask = this.fooAsync(20);
            Console.WriteLine("Inside caller");
            resultTask.Wait();
        }

        public /*async*/ Task<int> fooAsync(int x)
        {
            StateMachine stateMachine = new StateMachine(x);
            stateMachine.MoveNext();
            return stateMachine.resultTask.Task;
        }
    }
}
