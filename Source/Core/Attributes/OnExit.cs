//-----------------------------------------------------------------------
// <copyright file="OnExit.cs">
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

namespace Microsoft.PSharp
{
    /// <summary>
    /// Attribute for declaring what action to perform
    /// when exiting a machine state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OnExit : Attribute
    {
        /// <summary>
        /// Action name.
        /// </summary>
        internal string Action;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="actionName">Action name</param>
        public OnExit(string actionName)
        {
            this.Action = actionName;
        }
    }
}
