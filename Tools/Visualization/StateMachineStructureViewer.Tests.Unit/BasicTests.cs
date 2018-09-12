using Microsoft.PSharp.PSharpStateMachineStructureViewer;
using System;
using System.Xml.Linq;
using Xunit;
using Microsoft.PSharp;

namespace StateMachineStructureViewer.Tests.Unit
{

    using MV = MachineVertex;
    using SV = StateVertex;
    using GT = GotoTransition;
    using IN = ContainsLink;

    public class BasicTests
    {

        [Fact]
        public void TestStateGroup()
        {
            string prog = @"using System;
            namespace DgmlWriterTests.StateGroupTest
            {
                machine m1
                {
                event e1;
                event e2;
                event e2nosg;
                    start state Init
                    {
                        on e1 goto sg1.s1;
                        entry{ raise(e1); }
                    }

                    group sg1
                    {
                        state s1{on e2 goto s2; on e3 goto s3;}
                        state s2{ }
                    }

                    state s1{ on e2 goto s2 }

                    state s2{ }
                    state s3{ }

                }
            }";

            string dgml = StateDiagramViewer.GetDgmlForProgram(prog);
            DgmlParser dgmlParser = new DgmlParser();
             

            StateMachineGraph G = dgmlParser.parseDgml(XDocument.Parse(dgml));
            string dumpstr1 = G.DumpString();
            Console.WriteLine(dumpstr1);


            StateMachineGraph expectedGraph = new StateMachineGraph(new Vertex[]
            {
            new MV("m1", new Edge[]{
                new IN(null, "init"), new IN(null, "sg1.s1"), new IN(null, "sg1.s2") ,
                new IN(null, "s1"), new IN(null, "s2"), new IN(null, "s3")
            }),
            new SV("init", new Edge[]{
                new GT("e1", "sg1.s1")
            }),
            new SV("sg1.s1", new Edge[] {
                new GT("e2", "sg1.s2"), new GT("e3", "s3")
            }),
            new SV("sg1.s2", new Edge[] { } ),
            new SV("s1", new Edge[] { new GT("e2", "s2") } ),
            new SV("s2", new Edge[] { } ),
            new SV("s3", new Edge[] { } )
            });

            string dumpstr2 = expectedGraph.DumpString();
            Console.WriteLine(dumpstr2);

            Assert.True(expectedGraph.DeepCheckEquality(G));
        }

        [Fact]
        public void TestMachineStateInheritance()
        {
            Assert.True(false);
        }
    }
}
