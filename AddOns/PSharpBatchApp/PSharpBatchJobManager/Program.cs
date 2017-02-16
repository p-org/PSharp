using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PSharpBatchJobManager
{
    public class Program
    {

        // Batch account credentials
        private static string BatchAccountName;
        private static string BatchAccountKey;
        private static string BatchAccountUrl;

        private static string JobId;
        private static string JobManagerName;

        private static string OutputContainerSasUrl;

        private static BatchClient batchClient;
        private static CloudJob Job;

        private const int TaskCompletionWaitPeriod = 30000;   //Every 30 seconds
        private const int TaskWaitPeriod = 5000; //5 seconds
        private const string LogFileName = "JobManagerLog.txt";
        private static TimeSpan timeoutTimeSpan = TimeSpan.FromHours(2);
        private static bool timeoutFlag = false;

        static int Main(string[] args)
        {
            if (args.Count() < 6) { Log("args error"); return -1; }

            //Job Started
            Log("");

            BatchAccountName = args[0];
            BatchAccountKey = args[1];
            BatchAccountUrl = args[2];
            JobId = args[3];
            JobManagerName = args[4];
            OutputContainerSasUrl = args[5];

            try
            {
                //We call the async main so we can await on many async calls
                MainAsync().Wait();
            }
            catch (AggregateException ae)
            {
                Console.WriteLine();
                Console.WriteLine(ae.StackTrace);
                Console.WriteLine();
                Log(ae.ToString());
                Log(ae.StackTrace);

                if (ae.InnerException != null)
                {
                    Log(ae.InnerException.StackTrace);
                    Log(ae.InnerException.ToString());
                }
                Log(ae.StackTrace);
                return -2;
            }

            
            return 0;
        }

        private static async Task MainAsync()
        {

            try
            {
                //Timeout Code
                Timer timeoutTimer = new Timer(timerCallBack, null, timeoutTimeSpan, Timeout.InfiniteTimeSpan);

                Log("Connecting to Batch");
                Log(BatchAccountName);
                Log(BatchAccountKey);
                Log(BatchAccountUrl);
                //Connecting to Batch
                var credentials = new BatchSharedKeyCredentials(BatchAccountUrl, BatchAccountName, BatchAccountKey);
                batchClient = BatchClient.Open(credentials);

                Log("Batch Connected");
                Log("Getting Job");

                //Getting Job
                Job = await batchClient.JobOperations.GetJobAsync(JobId);
                //Job = batchClient.JobOperations.ListJobs().First();

                Log("Getting Tasks");

                //Wait till tasks are added
                while (true)
                {
                    if(Job.ListTasks().Count() < 2)
                    {
                        Thread.Sleep(TaskWaitPeriod);
                        continue;
                    }
                    break;
                }

                //Getting all the Tasks in the Job
                var tasks = Job.ListTasks().ToList();
                tasks = tasks.Where(t => !t.Id.Equals(JobManagerName)).ToList();

                Log("Number of tasks: " + tasks.Count);

                //Periodically check on all jobs
                while (true)
                {

                    Log("Loop Started");

                    try
                    {
                        bool flag = false;
                        foreach (var task in tasks)
                        {
                            await task.RefreshAsync();
                            Log("Entering task: " + task.Id);
                            if (task.Id.Equals(JobManagerName)) { continue; }
                            if (task.State == TaskState.Completed)
                            {
                                Log("[" + task.Id + "] task completed");
                                if (task.ExecutionInformation.SchedulingError != null)
                                {
                                    Log("[" + task.Id + "] task scheduling error : Reactivating");
                                    //Scheduling error : Restart task
                                    //await task.ReactivateAsync();
                                    //flag = true;
                                }
                                else if (task.ExecutionInformation.ExitCode != 0)
                                {
                                    Log("[" + task.Id + "] task error, terminating all tasks");
                                    //Task retured with error : Stop all other task
                                    await TerminateAllTaks(tasks.ToList());
                                    flag = false;
                                    break;
                                }
                            }
                            else
                            {
                                Log(task.Id+" state: "+task.State);
                                flag = true;
                            }
                        }

                        if (!flag || timeoutFlag) { break; }
                    }
                    catch (Exception e)
                    {
                        Log("");
                        Log(e.ToString());
                        Log(e.StackTrace);
                        Log(e.Message);
                        if (null != e.InnerException)
                        {
                            Log(e.InnerException.ToString());
                            Log(e.InnerException.StackTrace);
                        }
                    }
                    Log("Sleeping for " + TaskCompletionWaitPeriod + " milliseconds");
                    Thread.Sleep(TaskCompletionWaitPeriod);
                }

                Log("Loop Ended");

                try
                {

                    //Upload all the files
                    foreach (var task in tasks)
                    {
                        try
                        {
                            await task.RefreshAsync();
                            Log("Entering task: " + task.Id);
                            if (task.Id.Equals(JobManagerName)) { continue; }
                            var files = task.ListNodeFiles(recursive: true).ToList();
                            if (null == files) { continue; }
                            //Upload these files to the central container and add a appinsights event
                            foreach (var file in files)
                            {
                                if (file.IsDirectory ?? false) { continue; }
                                if (file.Name.ToLower().Contains("wd") && !(file.Name.ToLower().Contains(".exe") || file.Name.ToLower().Contains(".dll")))
                                {
                                    //upload this file to container
                                    var fileName = task.Id + "_" + file.Name.Split('\\').Last();
                                    var fileFullPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
                                    Log(string.Format("Uploading output file for [{0}]. File Path: [{1}].", task.Id, fileFullPath));
                                    using (FileStream fileStream = new FileStream(fileFullPath, FileMode.Create))
                                    {
                                        await file.CopyToStreamAsync(fileStream);
                                        fileStream.Close();
                                    }
                                    UploadFileToContainer(fileFullPath, OutputContainerSasUrl);
                                }
                            }
                        }
                        catch(Exception e)
                        {
                            Log(e.ToString());
                            Log(e.StackTrace);
                        }
                    }
                }
                catch(Exception e)
                {
                    Log(e.StackTrace);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private static void timerCallBack(object sender)
        {
            Log("Timer Call Back");
            timeoutFlag = true;
        }

        private static async Task TerminateAllTaks(List<CloudTask> tasks)
        {
            foreach(var task in tasks)
            {
                await task.RefreshAsync();
                if(task.State == TaskState.Completed) { continue; }
                await task.TerminateAsync();
            }
        }

        private static void Log(string logString)
        {
            try
            {
                File.AppendAllText(LogFileName, logString + "\n");
            }
            catch(Exception exp)
            {

            }
        }

        /// <summary>
        /// Uploads the specified file to the container represented by the specified
        /// container shared access signature (SAS).
        /// </summary>
        /// <param name="filePath">The path of the file to upload to the Storage container.</param>
        /// <param name="containerSas">The shared access signature granting write access to the specified container.</param>
        private static void UploadFileToContainer(string filePath, string containerSas)
        {
            Log("Uploading to container : " + filePath);
            string blobName = Path.GetFileName(filePath);

            // Obtain a reference to the container using the SAS URI.
            CloudBlobContainer container = new CloudBlobContainer(new Uri(containerSas));

            // Upload the file (as a new blob) to the container
            try
            {
                CloudBlockBlob blob = container.GetBlockBlobReference(blobName);
                blob.UploadFromFile(filePath);

                Log("Write operation succeeded for SAS URL " + containerSas);
            }
            catch (StorageException e)
            {

                Log("Write operation failed for SAS URL " + containerSas);
                Log("Additional error information: " + e.Message);

                // Indicate that a failure has occurred so that when the Batch service sets the
                // CloudTask.ExecutionInformation.ExitCode for the task that executed this application,
                // it properly indicates that there was a problem with the task.
                Environment.ExitCode = -1;
            }
        }
    }
}
