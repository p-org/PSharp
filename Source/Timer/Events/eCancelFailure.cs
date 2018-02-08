using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Timer
{
	/// <summary>
	/// Event signalling a timer cancellation, where the timer has already sent a timeout.
	/// </summary>
	public class eCancelFailure : Event
	{
	}
}
