//-----------------------------------------------------------------------
// <copyright file="GotoStateTransition.cs">
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

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Defines a goto state transition.
    /// </summary>
    internal sealed class GotoStateTransition 
    {
        /// <summary>
        /// Target state.
        /// </summary>
        public Type TargetState;

        /// <summary>
        /// An optional lambda function, which can execute after
        /// the default OnExit function of the exiting state.
        /// </summary>
        public string Lambda;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GotoStateTransition(Type TargetState, string Lambda)
        {
            this.TargetState = TargetState;
            this.Lambda = Lambda;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GotoStateTransition(Type TargetState)
        {
            this.TargetState = TargetState;
            this.Lambda = null;
        }
    }
}
