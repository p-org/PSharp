//-----------------------------------------------------------------------
// <copyright file="InitTimer.cs">
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

namespace Microsoft.PSharp.Timer
{
	/// <summary>
	/// Event fired by a client machine to initialize a timer.
	/// </summary>
	public class InitTimer : Event
	{
		MachineId client;   // machine id of the client
		int period;			// periodicity of the timeout events

		/// <summary>
		/// Constructor to register the client, and set the periodicity of timeout events.
		/// </summary>
		/// <param name="client"> The machine id of the client. </param>
		/// <param name="period"> The desired periodicity of timeouts. Default is 100ms. This parameter is ignored in the models. </param>
		public InitTimer(MachineId client, int period) : base()
		{
			this.client = client;
			this.period = period;
		}

		/// <summary>
		/// Constructor to register the client, and set the periodicity to the default value of 100ms.
		/// </summary>
		/// <param name="client"></param>
		public InitTimer(MachineId client) : base()
		{
			this.client = client;
			this.period = 100;
		}

		/// <summary>
		/// Returns the client machine id.
		/// </summary>
		/// <returns></returns>
		public MachineId getClientId()
		{
			return this.client;
		}

		/// <summary>
		/// Get the intended periodicity.
		/// </summary>
		/// <returns></returns>
		public int getPeriod()
		{
			return this.period;
		}
	}
}
