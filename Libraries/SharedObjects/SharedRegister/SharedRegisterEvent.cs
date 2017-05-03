//-----------------------------------------------------------------------
// <copyright file="SharedRegisterEvent.cs">
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp
{
    internal class SharedRegisterEvent: Event 
    {
        internal enum SharedRegisterOp { GET, SET, UPDATE };

        public SharedRegisterOp op { get; private set; }

        public object value { get; private set; }

        public object func { get; private set; }

        public MachineId sender { get; private set; }

        SharedRegisterEvent(SharedRegisterOp op, object value, object func, MachineId sender)
        {
            this.op = op;
            this.value = value;
            this.func = func;
            this.sender = sender;
        }

        public static SharedRegisterEvent UpdateEvent(object func, MachineId sender)
        {
            return new SharedRegisterEvent(SharedRegisterOp.UPDATE, null, func, sender);
        }

        public static SharedRegisterEvent SetEvent(object value)
        {
            return new SharedRegisterEvent(SharedRegisterOp.SET, value, null, null);
        }

        public static SharedRegisterEvent GetEvent(MachineId sender)
        {
            return new SharedRegisterEvent(SharedRegisterOp.GET, null, null, sender);
        }

    }
}
