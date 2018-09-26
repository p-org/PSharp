// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.PSharp;

namespace Chord
{
    public class Finger
    {
        public int Start;
        public int End;
        public MachineId Node;

        public Finger(int start, int end, MachineId node)
        {
            this.Start = start;
            this.End = end;
            this.Node = node;
        }
    }
}
