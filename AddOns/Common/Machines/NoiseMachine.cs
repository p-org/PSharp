//-----------------------------------------------------------------------
// <copyright file="NoiseMachine.cs">
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

namespace Microsoft.PSharp.Common
{
    /// <summary>
    /// An experimental noise machine.
    /// </summary>
    public class NoiseMachine : Machine
    {
        /// <summary>
        /// A configure event.
        /// </summary>
        public class Configure : Event
        {
            /// <summary>
            /// Sender.
            /// </summary>
            public MachineId Sender;

            /// <summary>
            /// Duration.
            /// </summary>
            public int Duration;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="sender">MachineId</param>
            /// <param name="duration">Duration</param>
            public Configure(MachineId sender, int duration)
                : base()
            {
                this.Sender = sender;
                this.Duration = duration;
            }
        }

        /// <summary>
        /// A done event.
        /// </summary>
        public class Done : Event { }

        /// <summary>
        /// A noise event.
        /// </summary>
        private class NoiseEvent : Event { }

        private MachineId Sender;
        private int Duration;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : MachineState { }

        private void InitOnEntry()
        {
            this.Sender = (this.ReceivedEvent as Configure).Sender;
            this.Duration = (this.ReceivedEvent as Configure).Duration;
            this.Goto(typeof(Active));
        }

        [OnEntry(nameof(ActiveOnEntry))]
        [OnEventGotoState(typeof(NoiseEvent), typeof(Active))]
        private class Active : MachineState { }

        private void ActiveOnEntry()
        {
            this.Send(this.Id, new NoiseEvent());
            this.Duration--;

            if (this.Duration <= 0)
            {
                this.Send(this.Sender, new Done());
                this.Raise(new Halt());
            }
        }
    }
}
