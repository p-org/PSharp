using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using PSharpBatchTestCommon;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSharpBatchTester
{
    class Program
    {

        private static string JobId;

        //Config
        private static PSharpBatchConfig config;
        private static PSharpBatchAuthConfig authConfig;

        private static string LogSession;

        static void Main(string[] args)
        {
            if (args.Count() < 2)
            {
                if (args.Count() < 1)
                {
                    Console.WriteLine("No Config file path given");
                }
                Console.WriteLine("No auth config file path given.");
                return;
            }

            LogSession = Guid.NewGuid().ToString();

            try
            {
                //Get args
                string configFilePath = Path.GetFullPath(args[0]);
                config = PSharpBatchConfig.LoadFromXML(configFilePath);
                //PSharpOperations.ParseConfig(config);

                string authConfigFilePath = Path.GetFullPath(args[1]);
                authConfig = PSharpBatchAuthConfig.LoadFromXML(authConfigFilePath);

                //If it contains 3 args, then get the location of the test application
                if(args.Count() >= 3)
                {
                    if (!string.IsNullOrEmpty(args[3]))
                    {
                        config.PSharpBinariesFolderPath = args[3];
                    }
                }

                //We call the async main so we can await on many async calls
                MainAsync().Wait();
            }
            catch (AggregateException ae)
            {
                #region ExceptionLog
                Dictionary<string, string> errorProp = new Dictionary<string, string>();
                errorProp.Add("StackTrace", ae.StackTrace);
                errorProp.Add("Message", ae.Message);
                errorProp.Add("InnerStackTrace", ae.InnerException.StackTrace);
                errorProp.Add("InnerMessage", ae.InnerException.Message);
                Logger.LogEvents("Exception", errorProp); 
                #endregion

                Console.WriteLine();
                Console.WriteLine(ae.StackTrace);
                Console.WriteLine(ae.Message);
                if(ae.InnerException != null)
                {
                    Console.WriteLine(ae.InnerException.StackTrace);
                    Console.WriteLine(ae.InnerException.Message);
                }
                Console.WriteLine();
            }

            Console.WriteLine();
        }

        private static async Task MainAsync()
        {
            #region StartEventLog
            Dictionary<string, string> startEventProp = new Dictionary<string, string>();
            startEventProp.Add("MachineId", System.Environment.MachineName);
            startEventProp.Add("UserName", System.Environment.UserName);
            startEventProp.Add("Session", LogSession);
            Logger.LogEvents("BatchTestStart", startEventProp); 
            #endregion

            //Creating BatchOperations
            BatchOperations batchOperations = new BatchOperations(authConfig.BatchAccountName, authConfig.BatchAccountKey, authConfig.BatchAccountUrl);

            //Creating BlobOperations
            BlobOperations blobOperations = new BlobOperations(authConfig.StorageAccountName, authConfig.StorageAccountKey, config.BlobContainerSasExpiryHours);


            //Pool operations
            if (!(await batchOperations.CheckIfPoolExists(config.PoolId)))
            {

                //Upload the application and the dependencies to azure storage and get the resource objects.
                var nodeFiles = await blobOperations.UploadNodeFiles(config.PSharpBinariesFolderPath, config.PoolId);

                #region NodeUploadLog
                //Logging node files upload
                var nodeFilesEventName = "UploadNodeFiles";
                Dictionary<string, string> nodeFilesProp = new Dictionary<string, string>();
                nodeFilesProp.Add("MachineId", System.Environment.MachineName);
                nodeFilesProp.Add("UserName", System.Environment.UserName);
                nodeFilesProp.Add("NumberOfFiles", nodeFiles.Count.ToString());
                nodeFilesProp.Add("Session", LogSession);
                Logger.LogEvents(nodeFilesEventName, nodeFilesProp); 
                #endregion

                //Creating the pool
                await batchOperations.CreatePoolIfNotExistAsync
                    (
                       poolId: config.PoolId,
                       resourceFiles: nodeFiles,
                       numberOfNodes: config.NumberOfNodesInPool,
                       OSFamily: config.NodeOsFamily,
                       VirtualMachineSize: config.NodeVirtualMachineSize,
                       NodeStartCommand: PSharpBatchTestCommon.Constants.PSharpDefaultNodeStartCommand
                    );

                #region CreatePoolLog
                //Create Pool Log
                Dictionary<string, string> CreatePoolProp = new Dictionary<string, string>();
                CreatePoolProp.Add("MachineId", System.Environment.MachineName);
                CreatePoolProp.Add("UserName", System.Environment.UserName);
                CreatePoolProp.Add("PoolName", config.PoolId);
                CreatePoolProp.Add("OSFamily", config.NodeOsFamily);
                CreatePoolProp.Add("NumberOfNodes", config.NumberOfNodesInPool.ToString());
                CreatePoolProp.Add("VirtualMachineSize", config.NodeVirtualMachineSize);
                CreatePoolProp.Add("NodeStartCommand", PSharpBatchTestCommon.Constants.PSharpDefaultNodeStartCommand);
                CreatePoolProp.Add("Session", LogSession);
                Logger.LogEvents("PoolCreate", CreatePoolProp); 
                #endregion
            }

            string executingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            //Job Details
            string jobManagerFilePath = /*typeof(PSharpBatchJobManager.Program).Assembly.Location;*/ Path.Combine(executingDirectory, @".\PSharpBatchJobManager\PSharpBatchJobManager.exe");   // Data files for Job Manager Task
            string jobTimeStamp = PSharpBatchTestCommon.Constants.GetTimeStamp();
            JobId = config.JobDefaultId + jobTimeStamp;

            //Task Details
            //var testApplicationName = Path.GetFileName(config.TestApplicationPath);

            //Uploading the data files to azure storage and get the resource objects.
            //var inputFiles = await blobOperations.UploadInputFiles(config.TestApplicationPath, config.PoolId, JobId);
            var inputFiles = await blobOperations.UploadInputFilesFromCommandEntities(config.CommandEntities, config.PoolId, JobId);

            //Uploading JobManager Files
            var jobManagerFiles = await blobOperations.UploadJobManagerFiles(jobManagerFilePath, config.PoolId, JobId);

            await blobOperations.CreateOutputContainer(config.PoolId, JobId);
            var outputContainerSasUrl = blobOperations.GetOutputContainerSasUrl();

            //Creating the job
            await batchOperations.CreateJobAsync
                (
                    jobId: JobId,
                    poolId: config.PoolId,
                    resourceFiles: jobManagerFiles,
                    outputContainerSasUrl: outputContainerSasUrl
                );

            #region CreateJobLog
            //Create Job Log
            Dictionary<string, string> CreateJobProp = new Dictionary<string, string>();
            CreateJobProp.Add("MachineId", System.Environment.MachineName);
            CreateJobProp.Add("UserName", System.Environment.UserName);
            CreateJobProp.Add("JobId", JobId);
            CreateJobProp.Add("ResourceFileCount", jobManagerFiles.Count.ToString());
            CreateJobProp.Add("PoolId", config.PoolId);
            CreateJobProp.Add("OutputContainerSAS", outputContainerSasUrl);
            CreateJobProp.Add("Session", LogSession);
            Logger.LogEvents("CreateJob", CreateJobProp); 
            #endregion

            //Adding tasks
            await batchOperations.AddTasksFromCommandEntities
                (
                    jobId: JobId,
                    taskIDPrefix: config.TaskDefaultId,
                    inputFiles: inputFiles,
                    CommandEntities: config.CommandEntities
                );

            #region AddTaskLog
            //Add Task Log
            Dictionary<string, string> AddTaskProp = new Dictionary<string, string>();
            AddTaskProp.Add("MachineId", System.Environment.MachineName);
            AddTaskProp.Add("UserName", System.Environment.UserName);
            AddTaskProp.Add("PoolName", config.PoolId);
            AddTaskProp.Add("JobId", JobId);
            AddTaskProp.Add("TaskIdPrefix", config.TaskDefaultId);
            AddTaskProp.Add("Commands", string.Join("\n\n", config.CommandEntities));
            AddTaskProp.Add("Session", LogSession);
            Logger.LogEvents("AddTask", AddTaskProp); 
            #endregion

            #region OldTaskAddition
            //await batchOperations.AddTaskWithIterations
            //    (
            //        jobId: JobId,
            //        taskIDPrefix: config.TaskDefaultId,
            //        inputFiles: inputFiles,
            //        testFileName: testApplicationName,
            //        NumberOfTasks: config.NumberOfTasks,
            //        IterationsPerTask : config.IterationsPerTask,
            //        commandFlags : config.CommandFlags
            //    ); 
            #endregion


            //Monitor tasks
            var taskResult = await batchOperations.MonitorTasks
                (
                    jobId: JobId,
                    timeout: TimeSpan.FromHours(config.TaskWaitHours)
                );

            #region TaskCompleteLog
            //Task Complete Log
            Dictionary<string, string> TaskCompleteProp = new Dictionary<string, string>();
            TaskCompleteProp.Add("MachineId", System.Environment.MachineName);
            TaskCompleteProp.Add("UserName", System.Environment.UserName);
            TaskCompleteProp.Add("PoolName", config.PoolId);
            TaskCompleteProp.Add("JobId", JobId);
            TaskCompleteProp.Add("SuccessStatus", taskResult ? "Success" : "Fail");
            TaskCompleteProp.Add("Session", LogSession);
            Logger.LogEvents("TaskComplete", TaskCompleteProp);
            #endregion

            //Flush Log
            Logger.FlushLogs();

            await blobOperations.DownloadOutputFiles(config.OutputFolderPath);

            //All task completed
            Console.WriteLine();
            //Console.Write("Delete job? [yes] no: ");
            //string response = Console.ReadLine().ToLower();
            if (/*response == "y" || response == "yes"*/ config.DeleteJobAfterDone)
            {
                await batchOperations.DeleteJobAsync(JobId);
            }
            Console.WriteLine();
            //Console.Write("Delete Containers? [yes] no: ");
            //response = Console.ReadLine().ToLower();
            if (/*response == "y" || response == "yes"*/config.DeleteContainerAfterDone)
            {
                await blobOperations.DeleteInputContainer();
                await blobOperations.DeleteJobManagerContainer();
                await blobOperations.DeleteOutputContainer();
            }
        }
        
    }
}
