//-----------------------------------------------------------------------
// <copyright file="CachedAction.cs">
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
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp
{
    internal class CachedAction
    {
        internal readonly MethodInfo MethodInfo;
        private Action Action;
        private Func<Task> TaskFunc;

        internal bool IsAsync => this.TaskFunc != null;

        internal CachedAction(MethodInfo methodInfo, BaseMachine machine)
        {
            this.MethodInfo = methodInfo;

            // MethodInfo.Invoke catches the exception to wrap it in a TargetInvocationException.
            // This unwinds the stack before Machine.ExecuteAction's exception filter is invoked,
            // so call through the delegate instead (which is also much faster than Invoke).
            if (methodInfo.ReturnType == typeof(void))
            {
                this.Action = (Action)Delegate.CreateDelegate(typeof(Action), machine, methodInfo);
            }
            else
            {
                this.TaskFunc = (Func<Task>)Delegate.CreateDelegate(typeof(Func<Task>), machine, methodInfo);
            }
        }

        internal void Execute()
        {
            this.Action();
        }

        internal async Task ExecuteAsync()
        {
            await this.TaskFunc();
        }
    }
}
