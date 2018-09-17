using Microsoft.PSharp.PSharpStateMachineStructureViewer;
using System;
using Xunit;
using System.Xml.Linq;
namespace Microsoft.PSharp.StateMachineStructureViewer.Tests.Unit
{
    using MV = MachineVertex;
    using SV = StateVertex;
    using GT = GotoTransition;
    using IN = ContainsLink;

    /* 
     * Set of meta tests to make sure we're not interpreting the output wrong
     */
    public class DgmlParserTests
    {
        [Fact]
        public void TestDgmlParserStateGroup()
        {
            DgmlParser dgmlParser = new DgmlParser();
            string dgml =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<DirectedGraph xmlns=""http://schemas.microsoft.com/vs/2009/dgml"">
  <Nodes>
	<Node Id=""ns1.m1"" Label=""m1"" Category=""Machine""/>
	<Node Id=""ns1.m1.init"" Category=""State"" Label=""init""/>
	<Node Id=""ns1.m1.sg1.s1"" Category=""State"" Label=""s1""/>
	<Node Id=""ns1.m1.sg1.s2"" Category=""State"" Label=""s2""/>
	<Node Id=""ns1.m1.s1"" Category=""State"" Label=""s1""/>
	<Node Id=""ns1.m1.s2"" Category=""State"" Label=""s2""/>
	<Node Id=""ns1.m1.s3"" Category=""State"" Label=""s2""/>
  </Nodes>
  <Links>
    <Link Source=""ns1.m1"" Target=""ns1.m1.init"" Category=""Contains""/>
	<Link Source=""ns1.m1"" Target=""ns1.m1.sg1.s1"" Category=""Contains""/>
	<Link Source=""ns1.m1"" Target=""ns1.m1.sg1.s2"" Category=""Contains""/>
	<Link Source=""ns1.m1"" Target=""ns1.m1.s1"" Category=""Contains""/>
	<Link Source=""ns1.m1"" Target=""ns1.m1.s2"" Category=""Contains""/>
	<Link Source=""ns1.m1"" Target=""ns1.m1.s3"" Category=""Contains""/>
	
	<Link Source=""ns1.m1.init"" Event=""ns1.m1.e1"" Label=""e1""  Target=""ns1.m1.sg1.s1"" Category=""GotoTransition""/>
	<Link Source=""ns1.m1.sg1.s1"" Event=""ns1.m1.e2"" Label=""e2""  Target=""ns1.m1.sg1.s2"" Category=""GotoTransition""/>
	<Link Source=""ns1.m1.sg1.s1"" Event=""ns1.m1.e3"" Label=""e3""  Target=""ns1.m1.s3"" Category=""GotoTransition""/>
	<Link Source=""ns1.m1.s1"" Event=""ns1.m1.e2"" Label=""e2"" Target=""ns1.m1.s2"" Category=""GotoTransition""/>
  </Links>
</DirectedGraph>";
            // Program looks something like this:
            /*using System;
            namespace DgmlWriterTests.StateGroupTest
            {
                machine m1
                {
                event e1;
                event e2;
                event e3;
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

                    state s1{ on e2 goto s2; }

                    state s2{ }
                    state s3{ }

                }
            }";*/

            StateMachineGraph G = dgmlParser.ParseDgml(XDocument.Parse(dgml));
            string dumpstr1 = G.DumpString();
            Console.WriteLine(dumpstr1);


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

            string dumpstr2 = expectedGraph.DumpString();
            Console.WriteLine(dumpstr2);

            Assert.True(expectedGraph.DeepCheckEquality(G));
        }
    }
}
