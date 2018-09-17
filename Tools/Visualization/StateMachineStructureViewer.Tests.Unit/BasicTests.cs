using Microsoft.PSharp.PSharpStateMachineStructureViewer;
using System;
using System.Xml.Linq;
using Xunit;
using Microsoft.PSharp;
using System.Text;

namespace Microsoft.PSharp.StateMachineStructureViewer.Tests.Unit
{

    using MV = MachineVertex;
    using SV = StateVertex;
    using GT = GotoTransition;
    using IN = ContainsLink;

    public class BasicTests
    {
        [Fact]
        public void CrossNamespaceResolutionTest()
        {

            StateDiagramViewer.ResetResolutionHelper();
            string prog = @"
namespace ns1{
    event e11;
}
namespace ns2{
    event e21;
}
using ns1;
namespace ns3{
    event e31;
	machine m1{
		event el1;
		start state init
		{
            on el1 goto sl1; 
			on e11 goto s11; // 'using' ns1
            on ns2.e21 goto s21; 
            on e31 goto s31; 
		}

        state sl1{ }
		state s11{ }
		state s21{ }
		state s31{ }
	}
}";

            // Get DGML but remove first line.
            string dgml = StateDiagramViewer.GetDgmlForProgram(prog).Split(Environment.NewLine.ToCharArray(), 2)[1];
            DgmlParser dgmlParser = new DgmlParser();

            StateMachineGraph G = dgmlParser.ParseDgml(XDocument.Parse(dgml));

            StateMachineGraph expectedGraph = new StateMachineGraph(new Vertex[]{
                    new MV("ns3.m1", new Edge[]{
                        new IN(null, "ns3.m1.init"), new IN(null, "ns3.m1.sl1"),
                        new IN(null, "ns3.m1.s11"), new IN(null, "ns3.m1.s21"), new IN(null, "ns3.m1.s31"),
                    }),
                    new SV("ns3.m1.init", new Edge[] {
                        new GT("ns1.e11", "ns3.m1.s11"), new GT("ns2.e21", "ns3.m1.s21"),
                        new GT("ns3.e31", "ns3.m1.s31"), new GT("ns3.m1.el1", "ns3.m1.sl1"),
                    }),

                    new SV("ns3.m1.s11", new Edge[] { }),
                    new SV("ns3.m1.s21", new Edge[] { }),
                    new SV("ns3.m1.s31", new Edge[] { }),
                    new SV("ns3.m1.sl1", new Edge[] { }),
                });
            Assert.True(expectedGraph.DeepCheckEquality(G));
        }


        [Fact]
        public void TestStateGroup()
        {
            StateDiagramViewer.ResetResolutionHelper();
            string prog = @"
namespace ns1{
	machine m1{
		event e1;
		event e2;
		event e3;
		start state init
		{
			on e1 goto sg1.s1;
			entry{ raise(e1); }
		}

		group sg1
		{
			state s1{on e2 goto s2; on e3 goto s3;}
			state s2{ }
		}

		state s1{ on e2 goto s2; }

		state s2{ }
		state s3{ }

	}
}";

            // Get DGML but remove first line.
            string dgml = StateDiagramViewer.GetDgmlForProgram(prog).Split(Environment.NewLine.ToCharArray(), 2)[1];
            DgmlParser dgmlParser = new DgmlParser();

            StateMachineGraph G = dgmlParser.ParseDgml(XDocument.Parse(dgml));
            
            StateMachineGraph expectedGraph = new StateMachineGraph(new Vertex[]
            {
            new MV("ns1.m1", new Edge[]{
                new IN(null, "ns1.m1.init"), new IN(null, "ns1.m1.sg1.s1"), new IN(null, "ns1.m1.sg1.s2") ,
                new IN(null, "ns1.m1.s1"), new IN(null, "ns1.m1.s2"), new IN(null, "ns1.m1.s3")
            }),
            new SV("ns1.m1.init", new Edge[]{
                new GT("ns1.m1.e1", "ns1.m1.sg1.s1")
            }),
            new SV("ns1.m1.sg1.s1", new Edge[] {
                new GT("ns1.m1.e2", "ns1.m1.sg1.s2"), new GT("ns1.m1.e3", "ns1.m1.s3")
            }),
            new SV("ns1.m1.sg1.s2", new Edge[] { } ),
            new SV("ns1.m1.s1", new Edge[] { new GT("ns1.m1.e2", "ns1.m1.s2") } ),
            new SV("ns1.m1.s2", new Edge[] { } ),
            new SV("ns1.m1.s3", new Edge[] { } )
            });

            Assert.True(expectedGraph.DeepCheckEquality(G));
        }

        [Fact]
        public void TestMachineStateInheritance()
        {
            StateDiagramViewer.ResetResolutionHelper();
            string prog = @"
namespace ns1
{
    machine bm1
    {
        event e1;
        event e2;
        event e3;
        start state binit
        {
            on e1 goto s1;
            entry{ raise(e1); }
        }
        state s1
        {
            on e2 goto s2;
            entry { raise(s2); }
        }
        state s2 { 
            on e3 goto s3;
        }
        state s3{}
    }
    machine dm1 : bm1
    {
        event e1;
        start state dinit {
            on e1 goto s1;  // Transitition to inherited state
        }
        state s2{ 
            on e3 goto s4;  // Override e3 handler
        }
        state s4{}
    }
}
";
            string dgml = StateDiagramViewer.GetDgmlForProgram(prog).Split(Environment.NewLine.ToCharArray(), 2)[1];
            DgmlParser dgmlParser = new DgmlParser();

            StateMachineGraph G = dgmlParser.ParseDgml(XDocument.Parse(dgml));
            
            StateMachineGraph expectedGraph = new StateMachineGraph(new Vertex[]{
                new MV("ns1.bm1", new Edge[]{
                    new IN(null, "ns1.bm1.binit"), new IN(null, "ns1.bm1.s1"),
                        new IN(null, "ns1.bm1.s2"), new IN(null, "ns1.bm1.s3"),
                }),
                new MV("ns1.dm1", new Edge[]{
                    new IN(null, "ns1.dm1.dinit"), new IN(null, "ns1.dm1.s2"), new IN(null, "ns1.dm1.s4"),
                    new IN(null, "ns1.dm1>ns1.bm1.binit"), new IN(null, "ns1.dm1>ns1.bm1.s1"),
                        new IN(null, "ns1.dm1>ns1.bm1.s2"), new IN(null, "ns1.dm1>ns1.bm1.s3")
                }),
                // bm1 states
                new SV("ns1.bm1.binit", new Edge[]{
                    new GT("ns1.bm1.e1", "ns1.bm1.s1")
                }),
                new SV("ns1.bm1.s1", new Edge[]{
                    new GT("ns1.bm1.e2", "ns1.bm1.s2")
                }),
                new SV("ns1.bm1.s2", new Edge[]{
                    new GT("ns1.bm1.e3", "ns1.bm1.s3")
                }),
                new SV("ns1.bm1.s3", new Edge[]{}),
                // dm1 states
                new SV("ns1.dm1.dinit", new Edge[]{
                    new GT("ns1.dm1.e1", "ns1.dm1>ns1.bm1.s1")
                }),
                new SV("ns1.dm1.s4", new Edge[]{}),
                new SV("ns1.dm1.s2", new Edge[]{
                    new GT("ns1.bm1.e3", "ns1.dm1.s4")
                }),
                // States inherited by dm1
                new SV("ns1.dm1>ns1.bm1.binit", new Edge[]{
                    new GT("ns1.bm1.e1", "ns1.dm1>ns1.bm1.s1")
                }),
                new SV("ns1.dm1>ns1.bm1.s1", new Edge[]{
                    new GT("ns1.bm1.e2", "ns1.dm1>ns1.bm1.s2")
                }),
                new SV("ns1.dm1>ns1.bm1.s2", new Edge[]{
                    new GT("ns1.bm1.e3", "ns1.dm1>ns1.bm1.s3")
                }),
                new SV("ns1.dm1>ns1.bm1.s3", new Edge[]{}),
            });
            
            Assert.True(expectedGraph.DeepCheckEquality(G));
        }
    }
}
