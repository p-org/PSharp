//-----------------------------------------------------------------------
// <copyright file="Exceptions.cs">
//      Copyright (c) 2015 Pantazis Deligiannis (p.deligiannis@imperial.ac.uk)
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
    /// Implements a P# generic exception.
    /// </summary>
    internal sealed class PSharpGenericException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message">Message</param>
        public PSharpGenericException(string message)
            : base(message)
        {

        }
    }

    /// <summary>
    /// This exception is thrown whenever the Return() statement
    /// is executed to pop a state from the call state stack.
    /// </summary>
    internal sealed class ReturnUsedException : Exception
    {
        /// <summary>
        /// State from which Return() was used.
        /// </summary>
        internal MachineState ReturningState;

        /// <summary>
        /// Default constructor of the ReturnUsedException class.
        /// </summary>
        /// <param name="s">State</param>
        public ReturnUsedException(MachineState s)
        {
            this.ReturningState = s;
        }
    }
}
