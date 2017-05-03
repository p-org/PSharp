//-----------------------------------------------------------------------
// <copyright file="SharedDictionaryMachine.cs">
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

namespace Microsoft.PSharp.TestingServices
{
    internal sealed class SharedDictionaryMachine<TKey, TValue> : Machine 
    {
        Dictionary<TKey, TValue> dictionary;

        [Start]
        [OnEntry(nameof(Initialize))]
        [OnEventDoAction(typeof(SharedDictionaryEvent), nameof(ProcessEvent))]
        class Init : MachineState { }

        void Initialize()
        {
            var e = (this.ReceivedEvent as SharedDictionaryEvent);

            if (e == null)
            {
                dictionary = new Dictionary<TKey, TValue>();
                return;
            }

            if (e.op == SharedDictionaryEvent.SharedDictionaryOp.INIT && e.comparer != null)
            {
                dictionary = new Dictionary<TKey, TValue>(e.comparer as IEqualityComparer<TKey>);
            }
            else
            {
                throw new ArgumentException("Incorrect arguments provided to SharedDictionary");
            }
        }


        void ProcessEvent()
        {
            var e = this.ReceivedEvent as SharedDictionaryEvent;
            switch (e.op)
            {
                case SharedDictionaryEvent.SharedDictionaryOp.TRYADD:
                    if (dictionary.ContainsKey((TKey)e.key))
                    {
                        this.Send(e.sender, new SharedDictionaryResponseEvent<bool>(false));
                    }
                    else
                    {
                        dictionary[(TKey)e.key] = (TValue)e.value;
                        this.Send(e.sender, new SharedDictionaryResponseEvent<bool>(true));
                    }
                    break;
                case SharedDictionaryEvent.SharedDictionaryOp.TRYUPDATE:
                    if (!dictionary.ContainsKey((TKey)e.key))
                    {
                        this.Send(e.sender, new SharedDictionaryResponseEvent<bool>(false));
                    }
                    else
                    {
                        var currentValue = dictionary[(TKey)e.key];
                        if (currentValue.Equals((TValue)e.comparisonValue))
                        {
                            dictionary[(TKey)e.key] = (TValue)e.value;
                            this.Send(e.sender, new SharedDictionaryResponseEvent<bool>(true));
                        }
                        else
                        {
                            this.Send(e.sender, new SharedDictionaryResponseEvent<bool>(false));
                        }
                    }
                    break;
                case SharedDictionaryEvent.SharedDictionaryOp.GET:
                    this.Send(e.sender, new SharedDictionaryResponseEvent<TValue>(dictionary[(TKey)e.key]));
                    break;
                case SharedDictionaryEvent.SharedDictionaryOp.SET:
                    dictionary[(TKey)e.key] = (TValue)e.value;
                    break;
                case SharedDictionaryEvent.SharedDictionaryOp.COUNT:
                    this.Send(e.sender, new SharedDictionaryResponseEvent<int>(dictionary.Count));
                    break;
                case SharedDictionaryEvent.SharedDictionaryOp.TRYREMOVE:
                    if (dictionary.ContainsKey((TKey)e.key))
                    {
                        var value = dictionary[(TKey)e.key];
                        dictionary.Remove((TKey)e.key);
                        this.Send(e.sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(true, value)));
                    }
                    else
                    {
                        this.Send(e.sender, new SharedDictionaryResponseEvent<Tuple<bool, TValue>>(Tuple.Create(false, default(TValue))));
                    }
                    break;
            }

        }
    }
}
