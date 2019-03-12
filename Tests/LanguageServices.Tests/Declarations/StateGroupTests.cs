// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Xunit;

namespace Microsoft.PSharp.LanguageServices.Tests
{
    public class StateGroupTests
    {
        #region correct tests

        [Fact]
        public void TestMachineStateGroupDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
start state S1 { }
group G { state S2 { } }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        [Microsoft.PSharp.Start]
        class S1 : MachineState
        {
        }

        class G : StateGroup
        {
            public class S2 : MachineState
            {
            }
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineEntryDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
group G {
start state S
{
entry{}
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEntry(nameof(psharp_G_S_on_entry_action))]
            public class S : MachineState
            {
            }
        }

        protected void psharp_G_S_on_entry_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineExitDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
group G 
{
start state S
{
exit{}
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnExit(nameof(psharp_G_S_on_exit_action))]
            public class S : MachineState
            {
            }
        }

        protected void psharp_G_S_on_exit_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineEntryAndExitDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S
{
entry {}
exit {}
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEntry(nameof(psharp_G_S_on_entry_action))]
            [OnExit(nameof(psharp_G_S_on_exit_action))]
            public class S : MachineState
            {
            }
        }

        protected void psharp_G_S_on_entry_action()
        {}

        protected void psharp_G_S_on_exit_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineOnEventGotoStateDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S1
{
on e goto S2;
}
}
state S2
{
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class S2 : MachineState
        {
        }

        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEventGotoState(typeof(e), typeof(S2))]
            public class S1 : MachineState
            {
            }
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineOnEventGotoStateDeclaration2()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S1
{
on e1 goto S2;
on e2 goto S3;
}
state S2 {}
state S3 {}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEventGotoState(typeof(e1), typeof(S2))]
            [OnEventGotoState(typeof(e2), typeof(S3))]
            public class S1 : MachineState
            {
            }

            public class S2 : MachineState
            {
            }

            public class S3 : MachineState
            {
            }
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineOnEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S1
{
on e goto S2 with {}
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEventGotoState(typeof(e), typeof(S2), nameof(psharp_G_S1_e_action))]
            public class S1 : MachineState
            {
            }
        }

        protected void psharp_G_S1_e_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineOnSimpleGenericEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S1
{
on e<int> goto S2 with {}
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEventGotoState(typeof(e<int>), typeof(S2), nameof(psharp_G_S1_e_type_0_action))]
            public class S1 : MachineState
            {
            }
        }

        protected void psharp_G_S1_e_type_0_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineOnEventDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S1
{
on e do Bar;
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEventDoAction(typeof(e), nameof(Bar))]
            public class S1 : MachineState
            {
            }
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineOnEventDoActionDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S1
{
on e do {}
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEventDoAction(typeof(e), nameof(psharp_G_S1_e_action))]
            public class S1 : MachineState
            {
            }
        }

        protected void psharp_G_S1_e_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineOnSimpleGenericEventDoActionDeclarationWithBody()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S1
{
on e<int> do {}
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEventDoAction(typeof(e<int>), nameof(psharp_G_S1_e_type_0_action))]
            public class S1 : MachineState
            {
            }
        }

        protected void psharp_G_S1_e_type_0_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineOnEventGotoStateAndDoActionDeclaration()
        {
            var test = @"
namespace Foo {
machine M {
group G
{
start state S1
{
on e1 goto S2;
on e2 do Bar;
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEventGotoState(typeof(e1), typeof(S2))]
            [OnEventDoAction(typeof(e2), nameof(Bar))]
            public class S1 : MachineState
            {
            }
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineNestedGroup()
        {
            var test = @"
namespace Foo {
machine M {
group G1 {
group G2 {
start state S1 { entry { jump(S1); } }
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G1 : StateGroup
        {
            public class G2 : StateGroup
            {
                [Microsoft.PSharp.Start]
                [OnEntry(nameof(psharp_G1_G2_S1_on_entry_action))]
                public class S1 : MachineState
                {
                }
            }
        }

        protected void psharp_G1_G2_S1_on_entry_action()
        { this.Goto<G1.G2.S1>(); }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineNestedGroup2()
        {
            var test = @"
namespace Foo {
machine M {
group G1 {
group G2 {
group G3 {
start state S1 { entry { jump(S1); } }
}
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G1 : StateGroup
        {
            public class G2 : StateGroup
            {
                public class G3 : StateGroup
                {
                    [Microsoft.PSharp.Start]
                    [OnEntry(nameof(psharp_G1_G2_G3_S1_on_entry_action))]
                    public class S1 : MachineState
                    {
                    }
                }
            }
        }

        protected void psharp_G1_G2_G3_S1_on_entry_action()
        { this.Goto<G1.G2.G3.S1>(); }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineMultipleGroups()
        {
            var test = @"
namespace Foo {
machine M {
group G1 {
  start state S1 { entry { jump(S1); } }
}
group G2 {
  state S1 { entry { jump(S1); } }
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G1 : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEntry(nameof(psharp_G1_S1_on_entry_action))]
            public class S1 : MachineState
            {
            }
        }

        class G2 : StateGroup
        {
            [OnEntry(nameof(psharp_G2_S1_on_entry_action))]
            public class S1 : MachineState
            {
            }
        }

        protected void psharp_G1_S1_on_entry_action()
        { this.Goto<G1.S1>(); }

        protected void psharp_G2_S1_on_entry_action()
        { this.Goto<G2.S1>(); }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineMultipleGroups2()
        {
            var test = @"
namespace Foo {
machine M {
group G1 {
  start state S1 { entry { jump(S2); } }
  state S2 { entry { jump(S1); } }
}
group G2 {
  state S1 { entry { jump(S2); } }
  state S2 { entry { jump(S1); } }
  state S3 { entry { jump(G1.S1); } }
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G1 : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEntry(nameof(psharp_G1_S1_on_entry_action))]
            public class S1 : MachineState
            {
            }

            [OnEntry(nameof(psharp_G1_S2_on_entry_action))]
            public class S2 : MachineState
            {
            }
        }

        class G2 : StateGroup
        {
            [OnEntry(nameof(psharp_G2_S1_on_entry_action))]
            public class S1 : MachineState
            {
            }

            [OnEntry(nameof(psharp_G2_S2_on_entry_action))]
            public class S2 : MachineState
            {
            }

            [OnEntry(nameof(psharp_G2_S3_on_entry_action))]
            public class S3 : MachineState
            {
            }
        }

        protected void psharp_G1_S1_on_entry_action()
        { this.Goto<G1.S2>(); }

        protected void psharp_G1_S2_on_entry_action()
        { this.Goto<G1.S1>(); }

        protected void psharp_G2_S1_on_entry_action()
        { this.Goto<G2.S2>(); }

        protected void psharp_G2_S2_on_entry_action()
        { this.Goto<G2.S1>(); }

        protected void psharp_G2_S3_on_entry_action()
        { this.Goto<G1.S1>(); }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMachineNestedGroups()
        {
            var test = @"
namespace Foo {
machine M {
group G1 {
  start state S1 { entry { jump(G3.S2); } }
  group G3 {
    state S2 { entry { jump(S1); } }
    state S3 { entry { jump(S2); } }
  }
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Machine
    {
        class G1 : StateGroup
        {
            public class G3 : StateGroup
            {
                [OnEntry(nameof(psharp_G1_G3_S2_on_entry_action))]
                public class S2 : MachineState
                {
                }

                [OnEntry(nameof(psharp_G1_G3_S3_on_entry_action))]
                public class S3 : MachineState
                {
                }
            }

            [Microsoft.PSharp.Start]
            [OnEntry(nameof(psharp_G1_S1_on_entry_action))]
            public class S1 : MachineState
            {
            }
        }

        protected void psharp_G1_S1_on_entry_action()
        { this.Goto<G1.G3.S2>(); }

        protected void psharp_G1_G3_S2_on_entry_action()
        { this.Goto<G1.S1>(); }

        protected void psharp_G1_G3_S3_on_entry_action()
        { this.Goto<G1.G3.S2>(); }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorStateGroupDeclaration()
        {
            var test = @"
namespace Foo {
monitor M {
start state S1 { }
group G { state S2 { } }
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        [Microsoft.PSharp.Start]
        class S1 : MonitorState
        {
        }

        class G : StateGroup
        {
            public class S2 : MonitorState
            {
            }
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorEntryDeclaration()
        {
            var test = @"
namespace Foo {
monitor M {
group G {
start state S
{
entry{}
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEntry(nameof(psharp_G_S_on_entry_action))]
            public class S : MonitorState
            {
            }
        }

        protected void psharp_G_S_on_entry_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorExitDeclaration()
        {
            var test = @"
namespace Foo {
monitor M {
group G 
{
start state S
{
exit{}
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnExit(nameof(psharp_G_S_on_exit_action))]
            public class S : MonitorState
            {
            }
        }

        protected void psharp_G_S_on_exit_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorEntryAndExitDeclaration()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
start state S
{
entry {}
exit {}
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEntry(nameof(psharp_G_S_on_entry_action))]
            [OnExit(nameof(psharp_G_S_on_exit_action))]
            public class S : MonitorState
            {
            }
        }

        protected void psharp_G_S_on_entry_action()
        {}

        protected void psharp_G_S_on_exit_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorOnEventGotoStateDeclaration()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
start state S1
{
on e goto S2;
}
}
state S2
{
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        class S2 : MonitorState
        {
        }

        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEventGotoState(typeof(e), typeof(S2))]
            public class S1 : MonitorState
            {
            }
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorOnEventGotoStateDeclaration2()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
start state S1
{
on e1 goto S2;
on e2 goto S3;
}
state S2 {}
state S3 {}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEventGotoState(typeof(e1), typeof(S2))]
            [OnEventGotoState(typeof(e2), typeof(S3))]
            public class S1 : MonitorState
            {
            }

            public class S2 : MonitorState
            {
            }

            public class S3 : MonitorState
            {
            }
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorOnEventGotoStateDeclarationWithBody()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
start state S1
{
on e goto S2 with {}
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEventGotoState(typeof(e), typeof(S2), nameof(psharp_G_S1_e_action))]
            public class S1 : MonitorState
            {
            }
        }

        protected void psharp_G_S1_e_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorOnEventDoActionDeclaration()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
start state S1
{
on e do Bar;
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEventDoAction(typeof(e), nameof(Bar))]
            public class S1 : MonitorState
            {
            }
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorOnEventDoActionDeclarationWithBody()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
start state S1
{
on e do {}
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEventDoAction(typeof(e), nameof(psharp_G_S1_e_action))]
            public class S1 : MonitorState
            {
            }
        }

        protected void psharp_G_S1_e_action()
        {}
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorOnEventGotoStateAndDoActionDeclaration()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
start state S1
{
on e1 goto S2;
on e2 do Bar;
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        class G : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEventGotoState(typeof(e1), typeof(S2))]
            [OnEventDoAction(typeof(e2), nameof(Bar))]
            public class S1 : MonitorState
            {
            }
        }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorNestedGroup()
        {
            var test = @"
namespace Foo {
monitor M {
group G1 {
group G2 {
start state S1 { entry { jump(S1); } }
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        class G1 : StateGroup
        {
            public class G2 : StateGroup
            {
                [Microsoft.PSharp.Start]
                [OnEntry(nameof(psharp_G1_G2_S1_on_entry_action))]
                public class S1 : MonitorState
                {
                }
            }
        }

        protected void psharp_G1_G2_S1_on_entry_action()
        { this.Goto<G1.G2.S1>(); }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorNestedGroup2()
        {
            var test = @"
namespace Foo {
monitor M {
group G1 {
group G2 {
group G3 {
start state S1 { entry { jump(S1); } }
}
}
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        class G1 : StateGroup
        {
            public class G2 : StateGroup
            {
                public class G3 : StateGroup
                {
                    [Microsoft.PSharp.Start]
                    [OnEntry(nameof(psharp_G1_G2_G3_S1_on_entry_action))]
                    public class S1 : MonitorState
                    {
                    }
                }
            }
        }

        protected void psharp_G1_G2_G3_S1_on_entry_action()
        { this.Goto<G1.G2.G3.S1>(); }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorMultipleGroups()
        {
            var test = @"
namespace Foo {
monitor M {
group G1 {
  start state S1 { entry { jump(S1); } }
}
group G2 {
  state S1 { entry { jump(S1); } }
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        class G1 : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEntry(nameof(psharp_G1_S1_on_entry_action))]
            public class S1 : MonitorState
            {
            }
        }

        class G2 : StateGroup
        {
            [OnEntry(nameof(psharp_G2_S1_on_entry_action))]
            public class S1 : MonitorState
            {
            }
        }

        protected void psharp_G1_S1_on_entry_action()
        { this.Goto<G1.S1>(); }

        protected void psharp_G2_S1_on_entry_action()
        { this.Goto<G2.S1>(); }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorMultipleGroups2()
        {
            var test = @"
namespace Foo {
monitor M {
group G1 {
  start state S1 { entry { jump(S2); } }
  state S2 { entry { jump(S1); } }
}
group G2 {
  state S1 { entry { jump(S2); } }
  state S2 { entry { jump(S1); } }
  state S3 { entry { jump(G1.S1); } }
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        class G1 : StateGroup
        {
            [Microsoft.PSharp.Start]
            [OnEntry(nameof(psharp_G1_S1_on_entry_action))]
            public class S1 : MonitorState
            {
            }

            [OnEntry(nameof(psharp_G1_S2_on_entry_action))]
            public class S2 : MonitorState
            {
            }
        }

        class G2 : StateGroup
        {
            [OnEntry(nameof(psharp_G2_S1_on_entry_action))]
            public class S1 : MonitorState
            {
            }

            [OnEntry(nameof(psharp_G2_S2_on_entry_action))]
            public class S2 : MonitorState
            {
            }

            [OnEntry(nameof(psharp_G2_S3_on_entry_action))]
            public class S3 : MonitorState
            {
            }
        }

        protected void psharp_G1_S1_on_entry_action()
        { this.Goto<G1.S2>(); }

        protected void psharp_G1_S2_on_entry_action()
        { this.Goto<G1.S1>(); }

        protected void psharp_G2_S1_on_entry_action()
        { this.Goto<G2.S2>(); }

        protected void psharp_G2_S2_on_entry_action()
        { this.Goto<G2.S1>(); }

        protected void psharp_G2_S3_on_entry_action()
        { this.Goto<G1.S1>(); }
    }
}";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        [Fact]
        public void TestMonitorNestedGroups()
        {
            var test = @"
namespace Foo {
monitor M {
group G1 {
  start state S1 { entry { jump(G3.S2); } }
  group G3 {
    state S2 { entry { jump(S1); } }
    state S3 { entry { jump(S2); } }
  }
}
}
}";
            var expected = @"
using Microsoft.PSharp;

namespace Foo
{
    class M : Monitor
    {
        class G1 : StateGroup
        {
            public class G3 : StateGroup
            {
                [OnEntry(nameof(psharp_G1_G3_S2_on_entry_action))]
                public class S2 : MonitorState
                {
                }

                [OnEntry(nameof(psharp_G1_G3_S3_on_entry_action))]
                public class S3 : MonitorState
                {
                }
            }

            [Microsoft.PSharp.Start]
            [OnEntry(nameof(psharp_G1_S1_on_entry_action))]
            public class S1 : MonitorState
            {
            }
        }

        protected void psharp_G1_S1_on_entry_action()
        { this.Goto<G1.G3.S2>(); }

        protected void psharp_G1_G3_S2_on_entry_action()
        { this.Goto<G1.S1>(); }

        protected void psharp_G1_G3_S3_on_entry_action()
        { this.Goto<G1.G3.S2>(); }
    }
}
";
            LanguageTestUtilities.AssertRewritten(expected, test);
        }

        #endregion

        #region failure tests

        [Fact]
        public void TestMachineStateDeclarationWithMoreThanOneEntry()
        {
            var test = @"
namespace Foo {
machine M {
group G {
start state S
{
entry {}
entry{}
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Duplicate entry declaration.", test);
        }

        [Fact]
        public void TestMachineStateDeclarationWithMoreThanOneExit()
        {
            var test = @"
namespace Foo {
machine M {
group G {
start state S
{
exit{}
exit {}
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Duplicate exit declaration.", test);
        }

        [Fact]
        public void TestMachineEntryDeclarationWithUnexpectedIdentifier()
        {
            var test = @"
namespace Foo {
machine M {
group G {
start state S
{
entry Bar {}
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"{\".", test);
        }

        [Fact]
        public void TestMachineOnEventGotoStateDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
group G {
start state S1
{
on e goto S2
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \";\".", test);
        }

        [Fact]
        public void TestMachineOnEventGotoStateDeclarationWithoutState()
        {
            var test = @"
namespace Foo {
machine M {
group G 
{
start state S1
{
on e goto;
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected state identifier.", test);
        }

        [Fact]
        public void TestMachineOnEventDoActionDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
machine M {
start state S1
{
on e do Bar
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \";\".", test);
        }

        [Fact]
        public void TestMachineOnEventDoActionDeclarationWithoutAction()
        {
            var test = @"
namespace Foo {
machine M {
group G {
start state S1
{
on e do;
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected async keyword, action identifier, or opening curly bracket.", test);
        }

        [Fact]
        public void TestMachineOnEventDoActionDeclarationWithoutAsyncAction()
        {
            var test = @"
namespace Foo {
machine M {
group G {
start state S1
{
on e do async;
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected action identifier or opening curly bracket.", test);
        }

        [Fact]
        public void TestMachineOnEventDeclarationWithoutHandler()
        {
            var test = @"
namespace Foo {
machine M {
group G {
start state S1
{
on e;
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"do\", \"goto\" or \"push\".", test);
        }

        [Fact]
        public void TestMachineIgnoreEventDeclarationWithoutComma()
        {
            var test = @"
namespace Foo {
machine M {
group G {
start state S
{
ignore e1 e2;
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \",\".", test);
        }

        [Fact]
        public void TestMachineIgnoreEventDeclarationWithExtraComma()
        {
            var test = @"
namespace Foo {
machine M {
group G {
start state S
{
ignore e1,e2,;
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [Fact]
        public void TestMachineDeferEventDeclarationWithoutComma()
        {
            var test = @"
namespace Foo {
machine M {
group G {
start state S
{
defer e1 e2;
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \",\".", test);
        }

        [Fact]
        public void TestMachineDeferEventDeclarationWithExtraComma()
        {
            var test = @"
namespace Foo {
machine M {
group G {
start state S
{
defer e1,e2,;
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [Fact]
        public void TestMachineGroupInsideState()
        {
            var test = @"
namespace Foo {
machine M {
start state S
{
group G { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Unexpected token.", test);
        }

        [Fact]
        public void TestMachineEmptyGroup()
        {
            var test = @"
namespace Foo {
machine M {
group G { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A state group must declare at least one state.", test);
        }

        [Fact]
        public void TestMachineEmptyNestedGroup()
        {
            var test = @"
namespace Foo {
machine M {
group G {
group G2 { }
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A state group must declare at least one state.", test);
        }

        [Fact]
        public void TestMachineMethodInsideGroup()
        {
            var test = @"
namespace Foo {
machine M {
group G {
void Bar() { }
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Unexpected token 'void'.", test);
        }

        [Fact]
        public void TestMachineStartGroup()
        {
            var test = @"
namespace Foo {
machine M {
start group G { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine state group cannot be marked start.", test);
        }

        [Fact]
        public void TestMachineColdGroup()
        {
            var test = @"
namespace Foo {
machine M {
cold group G { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine state group cannot be cold.", test);
        }

        [Fact]
        public void TestMachineHotGroup()
        {
            var test = @"
namespace Foo {
machine M {
hot group G { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine state group cannot be hot.", test);
        }

        [Fact]
        public void TestMachineGroupName()
        {
            var test = @"
namespace Foo {
machine M {
group G.G2 { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"{\".", test);
        }

        [Fact]
        public void TestMonitorStateDeclarationWithMoreThanOneEntry()
        {
            var test = @"
namespace Foo {
monitor M {
group G {
start state S
{
entry {}
entry{}
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Duplicate entry declaration.", test);
        }

        [Fact]
        public void TestMonitorStateDeclarationWithMoreThanOneExit()
        {
            var test = @"
namespace Foo {
monitor M {
group G {
start state S
{
exit{}
exit {}
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Duplicate exit declaration.", test);
        }

        [Fact]
        public void TestMonitorEntryDeclarationWithUnexpectedIdentifier()
        {
            var test = @"
namespace Foo {
monitor M {
group G {
start state S
{
entry Bar {}
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"{\".", test);
        }

        [Fact]
        public void TestMonitorOnEventGotoStateDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
monitor M {
group G {
start state S1
{
on e goto S2
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \";\".", test);
        }

        [Fact]
        public void TestMonitorOnEventGotoStateDeclarationWithoutState()
        {
            var test = @"
namespace Foo {
monitor M {
group G 
{
start state S1
{
on e goto;
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected state identifier.", test);
        }

        [Fact]
        public void TestMonitorOnEventDoActionDeclarationWithoutSemicolon()
        {
            var test = @"
namespace Foo {
monitor M {
start state S1
{
on e do Bar
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \";\".", test);
        }

        [Fact]
        public void TestMonitorOnEventDoActionDeclarationWithoutAction()
        {
            var test = @"
namespace Foo {
monitor M {
group G {
start state S1
{
on e do;
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected async keyword, action identifier, or opening curly bracket.", test);
        }

        [Fact]
        public void TestMonitorOnEventDeclarationWithoutHandler()
        {
            var test = @"
namespace Foo {
monitor M {
group G {
start state S1
{
on e;
}
}
}
}";

            LanguageTestUtilities.AssertFailedTestLog("Expected \"do\", \"goto\" or \"push\".", test);
        }

        [Fact]
        public void TestMonitorIgnoreEventDeclarationWithoutComma()
        {
            var test = @"
namespace Foo {
monitor M {
group G {
start state S
{
ignore e1 e2;
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \",\".", test);
        }

        [Fact]
        public void TestMonitorIgnoreEventDeclarationWithExtraComma()
        {
            var test = @"
namespace Foo {
monitor M {
group G {
start state S
{
ignore e1,e2,;
}
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected event identifier.", test);
        }

        [Fact]
        public void TestMonitorGroupInsideState()
        {
            var test = @"
namespace Foo {
monitor M {
start state S
{
group G { }
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Unexpected token.", test);
        }

        [Fact]
        public void TestMonitorEmptyGroup()
        {
            var test = @"
namespace Foo {
monitor M {
group G { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A state group must declare at least one state.", test);
        }

        [Fact]
        public void TestMonitorEmptyNestedGroup()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
group G2 { }
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A state group must declare at least one state.", test);
        }

        [Fact]
        public void TestMonitorMethodInsideGroup()
        {
            var test = @"
namespace Foo {
monitor M {
group G
{
void Bar() { }
}
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Unexpected token 'void'.", test);
        }

        [Fact]
        public void TestMonitorStartGroup()
        {
            var test = @"
namespace Foo {
monitor M {
start group G { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine state group cannot be marked start.", test);
        }

        [Fact]
        public void TestMonitorColdGroup()
        {
            var test = @"
namespace Foo {
monitor M {
cold group G { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine state group cannot be cold.", test);
        }

        [Fact]
        public void TestMonitorHotGroup()
        {
            var test = @"
namespace Foo {
monitor M {
hot group G { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("A machine state group cannot be hot.", test);
        }

        [Fact]
        public void TestMonitorGroupName()
        {
            var test = @"
namespace Foo {
monitor M {
group G.G2 { }
}
}";
            LanguageTestUtilities.AssertFailedTestLog("Expected \"{\".", test);
        }

        #endregion
    }
}
