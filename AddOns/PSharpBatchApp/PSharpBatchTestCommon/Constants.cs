using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSharpBatchTestCommon
{
    public class Constants
    {

        //Storage Constants
        public static string StorageConnectionStringFormat = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}";
        public static string NodeContainerNameFormat = "application{0}"; //{0}:PoolID.
        public static string InputContainerNameFormat = "input{0}{1}"; //{0}:PoolID. {1}:JobID
        public static string OutputContainerNameFormat = "output{0}{1}"; //{0}:PoolID. {1}:JobID
        public static string JobManagerContainerNameFormat = "jobmanager{0}{1}"; //{0}:PoolID. {1}:JobID
        //public static int BlobContainerSasExpiryHours = 10;


        //Batch Constats : includes Pools, Nodes, Jobs and Tasks
        //public static int MaxIterationPerTask = 1000;

        //Command Constants
        public const string PSharpDefaultNodeStartCommand = "cmd /c (robocopy %AZ_BATCH_TASK_WORKING_DIR% %AZ_BATCH_NODE_SHARED_DIR%) ^& IF %ERRORLEVEL% LEQ 1 exit 0";
        public const string PSharpDefaultTaskCommandLine = "cmd /c %AZ_BATCH_NODE_SHARED_DIR%\\PSharpTester.exe /test:RaceTest.exe 1>out.txt 2>&1";
        public const string PSharpTaskCommandFormat = "cmd /c %AZ_BATCH_NODE_SHARED_DIR%\\PSharpTester.exe /test:{0} /i:{1} 1>out.txt 2>&1";
        public const string PSharpTaskCommandFormatWithFlags = "cmd /c %AZ_BATCH_NODE_SHARED_DIR%\\PSharpTester.exe /test:{0} /i:{1} {2} 1>out.txt 2>&1"; //{0}: Test application, {1}: number of iterations, {2}: Flags

        //Util Methods
        public static string GetTimeStamp()
        {
            //All time will be in UTC
            return DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        }
    }
}
