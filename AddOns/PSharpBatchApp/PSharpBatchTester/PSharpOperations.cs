using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSharpBatchTester
{
    class PSharpOperations
    {
        public static void ParseConfig(PSharpBatchConfig config)
        {
            /*
             *Args to parse
             * /parallel : number of tasks
             * /i : iterations per task
             * All the other args should be kept and the above ones should be removed.
             */

            var modifiedCommand = config.PSharpTestCommand;
            var flags = new StringBuilder();
            var wordlist = config.PSharpTestCommand.Split(' ');
            foreach (var word in wordlist)
            {
                if (word.Length == 0)
                {
                    continue;
                }
                else if (word.StartsWith("/parallel:"))
                {
                    //contains parallel
                    config.NumberOfTasks = int.Parse(word.Substring("/parallel:".Length));
                }
                else if (word.StartsWith("/i:"))
                {
                    //contains parallel
                    config.IterationsPerTask = int.Parse(word.Substring("/i:".Length));
                }
                else if (word.StartsWith("/test:"))
                {
                    config.TestApplicationPath = word.Substring("/test:".Length);
                }
                else if (word.Contains("PSharpTester.exe"))
                {
                    //Do nothing
                }
                else
                {
                    //Just directly add to the flag string
                    flags.Append(word);
                    flags.Append(" ");
                }
            }

            config.CommandFlags = flags.ToString();

        }
    }
}
