using Newtonsoft.Json;
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

        static void Main(string[] args)
        {
            if (args.Count() < 1)
            {
                Console.WriteLine("No config file path given.");
                Console.Read();
                return;
            }
            try
            {
                //Get args
                string configFilePath = args[0];
                configFilePath = Path.GetFullPath(configFilePath);
                using (FileStream configStream = new FileStream(configFilePath, FileMode.Open))
                {
                    config = PSharpBatchConfig.XMLDeserialize(configStream);
                    configStream.Close();
                }

                //We call the async main so we can await on many async calls
                MainAsync().Wait();
            }
            catch (AggregateException ae)
            {
                Console.WriteLine();
                Console.WriteLine(ae.StackTrace);
                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static async Task MainAsync()
        {

            //Creating BatchOperations
            BatchOperations batchOperations = new BatchOperations(config.BatchAccountName, config.BatchAccountKey, config.BatchAccountUrl);

            //Creating BlobOperations
            BlobOperations blobOperations = new BlobOperations(config.StorageAccountName, config.StorageAccountKey, config.BlobContainerSasExpiryHours);


            //Pool operations
            if (!(await batchOperations.CheckIfPoolExists(config.PoolId)))
            {
                //Upload the application and the dependencies to azure storage and get the resource objects.
                var nodeFiles = await blobOperations.UploadNodeFiles(config.PSharpBinariesFolderPath, config.PoolId);

                //Creating the pool
                await batchOperations.CreatePoolIfNotExistAsync
                    (
                       poolId: config.PoolId,
                       resourceFiles: nodeFiles,
                       numberOfNodes: config.NumberOfNodesInPool,
                       OSFamily: "5",
                       VirtualMachineSize: "small",
                       NodeStartCommand: Constants.PSharpDefaultNodeStartCommand
                    );
            }

            //Job Details
            string jobManagerFilePath = /*typeof(PSharpBatchJobManager.Program).Assembly.Location;*/  @".\PSharpBatchJobManager\PSharpBatchJobManager.exe";   // Data files for Job Manager Task
            string jobTimeStamp = Constants.GetTimeStamp();
            JobId = config.JobDefaultId + jobTimeStamp;

            //Task Details
            var testApplicationName = Path.GetFileName(config.TestApplicationPath);

            //Uploading the data files to azure storage and get the resource objects.
            var inputFiles = await blobOperations.UploadInputFiles(config.TestApplicationPath, config.PoolId, JobId);

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

            //Adding tasks
            await batchOperations.AddTaskWithIterations
                (
                    jobId: JobId,
                    taskIDPrefix: config.TaskDefaultId,
                    inputFiles: inputFiles,
                    testFileName: testApplicationName,
                    iterations: config.TotalIterations,
                    maxIterationsPerTask : config.MaxIterationPerTask
                );


            //Monitor tasks
            await batchOperations.MonitorTasks
                (
                    jobId: JobId,
                    timeout: TimeSpan.FromHours(config.TaskWaitHours)
                );

            await blobOperations.DownloadOutputFiles(config.OutputFolderPath);

            //All task completed
            Console.WriteLine();
            Console.Write("Delete job? [yes] no: ");
            string response = Console.ReadLine().ToLower();
            if (response == "y" || response == "yes")
            {
                await batchOperations.DeleteJobAsync(JobId);
            }
            Console.WriteLine();
            Console.Write("Delete Containers? [yes] no: ");
            response = Console.ReadLine().ToLower();
            if (response == "y" || response == "yes")
            {
                await blobOperations.DeleteInputContainer();
                await blobOperations.DeleteJobManagerContainer();
                await blobOperations.DeleteOutputContainer();
            }
        }
        
    }
}
