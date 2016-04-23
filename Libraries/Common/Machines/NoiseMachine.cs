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
    public class NoiseMachine : Machine
    {
        public class ConfigureEvent : Event
        {
            public MachineId Sender;
            public int Duration;

            public ConfigureEvent(MachineId sender, int duration)
                : base()
            {
                this.Sender = sender;
                this.Duration = duration;
            }
        }

        public class Done : Event { }
        private class NoiseEvent : Event { }

        private MachineId Sender;
        private int Duration;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        private class Init : MachineState { }

        private void InitOnEntry()
        {
            this.Sender = (this.ReceivedEvent as ConfigureEvent).Sender;
            this.Duration = (this.ReceivedEvent as ConfigureEvent).Duration;
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
