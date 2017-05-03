//-----------------------------------------------------------------------
// <copyright file="SharedDictionaryEvent.cs">
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
    internal class SharedDictionaryEvent : Event
    {
        internal enum SharedDictionaryOp { INIT, GET, SET, TRYADD, TRYUPDATE, TRYREMOVE, COUNT };

        public SharedDictionaryOp op { get; private set; }

        public object key { get; private set; }

        public object value { get; private set; }

        public object comparisonValue { get; private set; }

        public MachineId sender { get; private set; }

        public object comparer { get; private set; }

        SharedDictionaryEvent(SharedDictionaryOp op, object key, object value, object comparisonValue, MachineId sender, object comparer)
        {
            this.op = op;
            this.key = key;
            this.value = value;
            this.comparisonValue = comparisonValue;
            this.sender = sender;
            this.comparer = comparer;
        }

        public static SharedDictionaryEvent InitEvent(object comparer)
        {
            return new SharedDictionaryEvent(SharedDictionaryOp.INIT, null, null, null, null, comparer);
        }

        public static SharedDictionaryEvent TryAddEvent(object key, object value, MachineId sender)
        {
            return new SharedDictionaryEvent(SharedDictionaryOp.TRYADD, key, value, null, sender, null);
        }

        public static SharedDictionaryEvent TryUpdateEvent(object key, object value, object comparisonValue, MachineId sender)
        {
            return new SharedDictionaryEvent(SharedDictionaryOp.TRYUPDATE, key, value, comparisonValue, sender, null);
        }

        public static SharedDictionaryEvent GetEvent(object key, MachineId sender)
        {
            return new SharedDictionaryEvent(SharedDictionaryOp.GET, key, null, null, sender, null);
        }

        public static SharedDictionaryEvent SetEvent(object key, object value)
        {
            return new SharedDictionaryEvent(SharedDictionaryOp.SET, key, value, null, null, null);
        }

        public static SharedDictionaryEvent CountEvent(MachineId sender)
        {
            return new SharedDictionaryEvent(SharedDictionaryOp.COUNT, null, null, null, sender, null);
        }

        public static SharedDictionaryEvent TryRemoveEvent(object key, MachineId sender)
        {
            return new SharedDictionaryEvent(SharedDictionaryOp.TRYREMOVE, key, null, null, sender, null);
        }

    }
}
