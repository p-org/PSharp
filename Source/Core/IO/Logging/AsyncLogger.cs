//-----------------------------------------------------------------------
// <copyright file="AsyncLogger.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.PSharp.IO
{
    /// <summary>
    /// The logger maintains a queue of log items,
    /// and dequeues and writes them to a text writer
    /// in a background thread
    /// </summary>
    internal sealed class AsyncLogger : StateMachineLogger
    {

        /// <summary>
        /// Messages are tagged with this enum. The tag says how to log them
        /// Write(Line) => Use the Write(Line) method of the underlying Writer
        /// Wake => This is an internal message generated when the logger is being
        /// disposed - it wakes up the dequeuing thread if it is suspended at an await
        /// and causes it to flush all remaining messages
        /// </summary>
        enum ProcessingOption { Write, WriteLine, Wake };

        /// <summary>
        /// An item in the queue is the string to be logged,
        /// and a ProcessingOption that tells what to do with the message         
        /// </summary>
        BufferBlock<Tuple<string, ProcessingOption>> queue;

        TaskCompletionSource<bool> finished;
        public volatile bool IsRunning;        
        TextWriter writer;

        
        public AsyncLogger(TextWriter t)
        {
            queue = new BufferBlock<Tuple<string, ProcessingOption>>();
            this.IsRunning = true;
            writer = t ?? TextWriter.Null;
            finished = new TaskCompletionSource<bool>();

            Task.Run(async () => {
                while (this.IsRunning)
                {
                    var current = await queue.ReceiveAsync().ConfigureAwait(false);
                    ProcessItem(current);
                }

                // flush the queue
                Tuple<string, ProcessingOption> message;
                while (queue.TryReceive(out message))
                {                    
                    ProcessItem(message);
                }
                
                finished.SetResult(true);
            });            
        }

        private void ProcessItem(Tuple<string, ProcessingOption> current)
        {
            var message = current.Item1;
            switch (current.Item2)
            {
                case ProcessingOption.Write:     this.writer.Write(message);
                                                 break;
                case ProcessingOption.WriteLine: this.writer.WriteLine(message);
                                                 break;
                case ProcessingOption.Wake:      break;    
            }            
        }

        public override void Dispose()
        {
            
            this.IsRunning = false;

            // The dequeueing task might be suspended at the await.
            // this message will wake it up and cause it to flush
            queue.Post(new Tuple<string, ProcessingOption>("", ProcessingOption.Wake));
            
            finished.Task.Wait();

            // mark the queue as complete
            queue.Complete();            
        }

        public override void Write(string value)
        {
            queue.Post(new Tuple<string,ProcessingOption>(value, ProcessingOption.Write));
        }

        public override void Write(string format, params object[] args)
        {
            var value = IO.Utilities.Format(format, args);
            queue.Post(new Tuple<string, ProcessingOption>(value, ProcessingOption.Write));
        }

        public override void WriteLine(string value)
        {
            queue.Post(new Tuple<string, ProcessingOption>(value, ProcessingOption.WriteLine));
        }

        public override void WriteLine(string format, params object[] args)
        {
            var value = IO.Utilities.Format(format, args);
            queue.Post(new Tuple<string, ProcessingOption>(value, ProcessingOption.WriteLine));
        }
    }
}
