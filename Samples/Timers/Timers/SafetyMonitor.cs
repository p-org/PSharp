using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace Timers
{
	class SafetyMonitor : Monitor
	{
		#region events

		public class NotifyCancelSuccess : Event { }
		public class NotifyCancelFailure : Event { }
		public class NotifyTimeoutReceived : Event { }
		#endregion

		#region fields
		private bool IsCancelSuccess;
		private bool IsCancelFailed;
		private bool IsTimeoutSent;

		#endregion

		#region states

		[Start]
		[OnEventDoAction(typeof(NotifyTimeoutReceived), nameof(HandleTimeout))]
		internal sealed class Active : MonitorState { }

		#endregion

		#region handlers
		private void HandleTimeout()
		{
			this.Assert(!IsTimeoutSent && !IsCancelSuccess && !IsCancelFailed);
			this.IsTimeoutSent = true;
		}

		private void HandleCancelSuccess()
		{
			this.Assert(!IsTimeoutSent && !IsCancelSuccess && !(IsCancelFailed));
			this.IsCancelSuccess = true;
		}
		#endregion
	}
}
