// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp;

namespace ChainReplication
{
    public class SentLog
    {
        public int NextSeqId;
        public MachineId Client;
        public int Key;
        public int Value;

        public SentLog(int nextSeqId, MachineId client, int key, int val)
        {
            this.NextSeqId = nextSeqId;
            this.Client = client;
            this.Key = key;
            this.Value = val;
        }
    }
}
