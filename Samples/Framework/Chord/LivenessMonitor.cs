﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp;

namespace Chord
{
    internal class LivenessMonitor : Monitor
    {
        #region events

        public class NotifyClientRequest : Event
        {
            public int Key;

            public NotifyClientRequest(int key)
                : base()
            {
                this.Key = key;
            }
        }

        public class NotifyClientResponse : Event
        {
            public int Key;

            public NotifyClientResponse(int key)
                : base()
            {
                this.Key = key;
            }
        }

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MonitorState { }

        void InitOnEntry()
        {
            this.Goto<Responded>();
        }

        [Cold]
        [OnEventGotoState(typeof(NotifyClientRequest), typeof(Requested))]
        class Responded : MonitorState { }

        [Hot]
        [OnEventGotoState(typeof(NotifyClientResponse), typeof(Responded))]
        class Requested : MonitorState { }

        #endregion
    }
}