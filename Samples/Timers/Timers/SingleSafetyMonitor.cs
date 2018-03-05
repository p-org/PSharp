using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp;

namespace Timers
{
	class SingleSafetyMonitor : Monitor
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
		[OnEntry(nameof(InitializeMonitor))]
		[OnEventDoAction(typeof(NotifyTimeoutReceived), nameof(HandleTimeout))]
		[OnEventDoAction(typeof(NotifyCancelSuccess), nameof(HandleCancelSuccess))]
		[OnEventDoAction(typeof(NotifyCancelFailure), nameof(HandleCancelFailure))]
		internal sealed class Active : MonitorState { }

		#endregion

		#region handlers
		private void InitializeMonitor()
		{
			IsTimeoutSent = false;
			IsCancelSuccess = false;
			IsCancelFailed = false;
		}
		private void HandleTimeout()
		{
			this.Assert(!IsTimeoutSent && !IsCancelSuccess && !IsCancelFailed);
			IsTimeoutSent = true;
		}

		private void HandleCancelSuccess()
		{
			this.Assert(!IsTimeoutSent && !IsCancelSuccess && !(IsCancelFailed));
			IsCancelSuccess = true;
		}

		private void HandleCancelFailure()
		{
			this.Assert(IsTimeoutSent && !IsCancelSuccess && !IsCancelFailed);
			IsCancelFailed = true;
		}
		#endregion
	}
}
