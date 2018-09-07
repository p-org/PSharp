// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Xunit;

namespace Microsoft.PSharp.SharedObjects.Tests.Unit
{
    public class SharedDictionaryMockTest2 : BaseTest
    {
        class M : Machine
        {
            [Start]
            [OnEntry(nameof(EntryInit))]
            class Init : MachineState { }

            void EntryInit()
            {
                var counter = SharedDictionary.Create<int, string>(this.Id.Runtime);

                counter.TryAdd(1, "M");

                // key not present; will throw an exception
                var v = counter[2]; 
            }
        }

        [Fact]
        public void TestDictionaryException()
        {
            var config = Configuration.Create().WithNumberOfIterations(50);
            
            var test = new Action<IPSharpRuntime>((r) => {
                r.CreateMachine(typeof(M));
            });

            base.AssertFailed(config, test, 1);
        }
    }
}
