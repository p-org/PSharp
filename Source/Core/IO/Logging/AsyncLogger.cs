using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.PSharp.IO
{
    internal sealed class AsyncLogger : StateMachineLogger
    {

        BufferBlock<Tuple<string, bool>> queue;
        public volatile bool IsRunning;
        public readonly TextWriter Null;
        TextWriter writer;


        public AsyncLogger(TextWriter t)
        {
            queue = new BufferBlock<Tuple<string, bool>>();
            this.IsRunning = true;
            Task.Run(async () => {
                while (this.IsRunning)
                {
                    var current = await queue.ReceiveAsync();
                    ProcessItem(current);
                }
            });
            writer = t == null ? TextWriter.Null : t ;
        }

        private void ProcessItem(Tuple<string, bool> current)
        {
            var message = current.Item1;
            if (current.Item2 == false)
            {
                writer.Write(message);
            }
            else
            {
                writer.WriteLine(message);
            }
        }

        public override void Dispose()
        {
            this.IsRunning = false;
            
            // flush the queue
            while (queue.Count > 0)
            {
                ProcessItem(queue.Receive());
            }

            // mark the queue as complete
            queue.Complete();
            
        }

        public override void Write(string value)
        {
            queue.Post(new Tuple<string,bool>(value, false));
        }

        public override void Write(string format, params object[] args)
        {
            var value = IO.Utilities.Format(format, args);
            queue.Post(new Tuple<string, bool>(value, false));
        }

        public override void WriteLine(string value)
        {
            queue.Post(new Tuple<string, bool>(value, true));
        }

        public override void WriteLine(string format, params object[] args)
        {
            var value = IO.Utilities.Format(format, args);
            queue.Post(new Tuple<string, bool>(value, true));
        }
    }
}
