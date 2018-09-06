// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Raft.PSharpLanguage
{
    public class Log
    {
        public readonly int Term;
        public readonly int Command;

        public Log(int term, int command)
        {
            this.Term = term;
            this.Command = command;
        }
    }
}
