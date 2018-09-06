﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp;

namespace TwoPhaseCommit.PSharpLibrary
{
    internal class PendingWriteRequest
    {
        public MachineId Client;
        public int SeqNum;
        public int Idx;
        public int Val;

        public PendingWriteRequest(int seqNum, int idx, int val)
        {
            this.SeqNum = seqNum;
            this.Idx = idx;
            this.Val = val;
        }

        public PendingWriteRequest(MachineId client, int idx, int val)
        {
            this.Client = client;
            this.Idx = idx;
            this.Val = val;
        }
    }
}
