// ------------------------------------------------------------------------------------------------

using System.Diagnostics;

using Microsoft.Build.Framework;
using Microsoft.PSharp.IO;

namespace Microsoft.PSharp
{
    public class RewriterAsSeparateProcess : ITask
    {
        public IBuildEngine BuildEngine { get; set; }

        public ITaskHost HostObject { get; set; }

#pragma warning disable CA1819 // Properties should not return arrays
        public ITaskItem[] InputFiles { get; set; }

        [Output]
        public ITaskItem[] OutputFiles { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        public bool Execute()
        {
            string processInputString = string.Empty;
            for (int i = 0; i < this.InputFiles.Length; i++)
            {
                processInputString += this.InputFiles[i].ItemSpec;
                processInputString += " ";
                processInputString += this.OutputFiles[i].ItemSpec;
                if (i + 1 < this.InputFiles.Length)
                {
                    processInputString += " ";
                }
            }

            var process = new Process();
            var processStartInfo = new ProcessStartInfo(this.GetType().Assembly.Location, processInputString);
            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            process.StartInfo = processStartInfo;
            process.Start();
            process.WaitForExit();
            return true;
        }
    }
}
