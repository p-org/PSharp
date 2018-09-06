﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PSharp.Timers;
using Microsoft.PSharp;
using Xunit;

namespace Microsoft.PSharp.TestingServices.Tests.Unit
{
	public class TimerLivenessTest : BaseTest
	{
		#region events
		class TimeoutReceivedEvent : Event { }
		#endregion

		#region machines/monitors

		class Client : TimedMachine
		{
			#region fields
			TimerId tid;
			object payload = new object();
			#endregion

			#region states
			[Start]
			[OnEntry(nameof(Initialize))]
			[OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
			private class Init : MachineState { }

			#endregion

			#region handlers
			private void Initialize()
			{
				tid = StartTimer(payload, 10, false);
			}

			private void HandleTimeout()
			{
				this.Monitor<LivenessMonitor>(new TimeoutReceivedEvent());
			}
			#endregion
		}

		class LivenessMonitor : Monitor
		{
			[Start]
			[Hot]
			[OnEventGotoState(typeof(TimeoutReceivedEvent), typeof(TimeoutReceived))]
			class NoTimeoutReceived : MonitorState { }

			[Cold]
			class TimeoutReceived : MonitorState { }
		}

		#endregion

		#region test

		[Fact]
		public void PeriodicLivenessTest()
		{
			var config = base.GetConfiguration();
			config.LivenessTemperatureThreshold = 150;
			config.MaxSchedulingSteps = 300;
			config.SchedulingIterations = 1000;

			var test = new Action<PSharpRuntime>((r) => {
				r.RegisterMonitor(typeof(LivenessMonitor));
				r.CreateMachine(typeof(Client));
			});

			base.AssertSucceeded(config, test);
		}

		#endregion
	}
}
