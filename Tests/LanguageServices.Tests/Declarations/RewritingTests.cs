// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.LanguageServices.Parsing;

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class RewritingTests
    {
        #region failure tests

        [Fact]
        public void TestDuplicateStatesAndJump()
        {
            var test = @"
namespace Foo {
public event e;
machine M {
start state S1 { }
state S2 { 
  on e do { jump(S1); }
}
state S2 { 
  on e do { jump(S1); }
}
}
}";

            var exception_hit = false;
            try
            {
                LanguageTestUtilities.RunRewriter(test);
            }
            catch (RewritingException ex)
            {
                exception_hit = true;
                Assert.Equal(ex.Message.Replace(Environment.NewLine, string.Empty), 
                    "Multiple declarations of the state 'S2'" + 
                    "File: Program.psharp" +
                    "Lines: 5 and 8");
            }

            Assert.True(exception_hit, "expected exception was not hit");
        }

        #endregion
    }
}
