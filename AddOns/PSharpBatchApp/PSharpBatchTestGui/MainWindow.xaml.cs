using Microsoft.Win32;
using PSharpBatchTestCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PSharpBatchTestGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string JobId;

        private PSharpBatchConfig config;

        private ControlWriter controlWriter;

        private bool DeleteBlobAfterComplete;

        private bool DeleteJobAfterComplete;

        private TextWriter originalOut;

        public MainWindow()
        {
            InitializeComponent();
            originalOut = Console.Out;
            controlWriter = new ControlWriter(LoggerFunction);
            Console.SetOut(controlWriter);

            this.Closing += onWindowClosing;
        }

        /// <summary>
        /// To prevent charecters in Number of nodes entry
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NumberTextbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Load config from a user chosen path
        /// </summary>
        /// <param name="path"></param>
        public void LoadConfig()
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = UIConstants.FileExtensionFilter;
            fileDialog.ShowDialog();
            var path = fileDialog.FileName;
            if (string.IsNullOrEmpty(path)) { return; }
            config = PSharpBatchConfig.LoadFromXML(path);
            PSharpOperations.ParseConfig(config);
            ApplyConfig();
        }

        /// <summary>
        /// Applying loaded config to the UI Elements
        /// </summary>
        public void ApplyConfig()
        {
            BatchAccountKeyTextbox.Text = config.BatchAccountKey;
            BatchAccountNameTextbox.Text = config.BatchAccountName;
            BatchAccountUrlTextbox.Text = config.BatchAccountUrl;
            SASExpiryHoursTextbox.Text = config.BlobContainerSasExpiryHours.ToString();
            JobIDTextbox.Text = config.JobDefaultId;
            NumNodesTextbox.Text = config.NumberOfNodesInPool.ToString();
            OutputPathTextbox.Text = config.OutputFolderPath;
            PoolIDTextbox.Text = config.PoolId;
            PSharpBinariesTextbox.Text = config.PSharpBinariesFolderPath;
            PSharpTestCommandTextbox.Text = config.PSharpTestCommand;
            StorageAccountKeyTextbox.Text = config.StorageAccountKey;
            StorageAccountNameTextbox.Text = config.StorageAccountName;
            TaskIDTextbox.Text = config.TaskDefaultId;
            TaskWaitHoursTextbox.Text = config.TaskWaitHours.ToString();
        }

        /// <summary>
        /// Extracting config from the UI elements
        /// </summary>
        /// <returns></returns>
        public bool ExtractConfig()
        {
            if (!validateUIElements()) { return false; }
            try
            {
                config = new PSharpBatchConfig
                {
                    BatchAccountKey = BatchAccountKeyTextbox.Text,
                    BatchAccountName = BatchAccountNameTextbox.Text,
                    BatchAccountUrl = BatchAccountUrlTextbox.Text,
                    BlobContainerSasExpiryHours = int.Parse(SASExpiryHoursTextbox.Text),
                    JobDefaultId = JobIDTextbox.Text,
                    NumberOfNodesInPool = int.Parse(NumNodesTextbox.Text),
                    OutputFolderPath = OutputPathTextbox.Text,
                    PoolId = PoolIDTextbox.Text,
                    PSharpBinariesFolderPath = PSharpBinariesTextbox.Text,
                    PSharpTestCommand = PSharpTestCommandTextbox.Text,
                    StorageAccountKey = StorageAccountKeyTextbox.Text,
                    StorageAccountName = StorageAccountNameTextbox.Text,
                    TaskDefaultId = TaskIDTextbox.Text,
                    TaskWaitHours = int.Parse(TaskWaitHoursTextbox.Text)
                };

                PSharpBatchTestCommon.PSharpOperations.ParseConfig(config);
            }
            catch(Exception e)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates the UI Elements
        /// </summary>
        /// <returns></returns>
        public bool validateUIElements()
        {
            int temp;
            uint utemp;
            if (string.IsNullOrEmpty(BatchAccountKeyTextbox.Text))
            {
                return false;
            }
            if (string.IsNullOrEmpty(BatchAccountNameTextbox.Text))
            {
                return false;
            }
            if (string.IsNullOrEmpty(BatchAccountUrlTextbox.Text))
            {
                return false;
            }
            if (string.IsNullOrEmpty(SASExpiryHoursTextbox.Text) || !int.TryParse(SASExpiryHoursTextbox.Text, out temp))
            {
                return false;
            }
            if (string.IsNullOrEmpty(JobIDTextbox.Text))
            {
                return false;
            }
            if (string.IsNullOrEmpty(NumNodesTextbox.Text) || !uint.TryParse(NumNodesTextbox.Text, out utemp))
            {
                return false;
            }
            if (string.IsNullOrEmpty(OutputPathTextbox.Text))
            {
                return false;
            }
            if (string.IsNullOrEmpty(PoolIDTextbox.Text))
            {
                return false;
            }
            if (string.IsNullOrEmpty(PSharpBinariesTextbox.Text))
            {
                return false;
            }
            if (string.IsNullOrEmpty(PSharpTestCommandTextbox.Text))
            {
                return false;
            }
            if (string.IsNullOrEmpty(StorageAccountKeyTextbox.Text))
            {
                return false;
            }
            if (string.IsNullOrEmpty(StorageAccountNameTextbox.Text))
            {
                return false;
            }
            if (string.IsNullOrEmpty(TaskIDTextbox.Text))
            {
                return false;
            }
            if (string.IsNullOrEmpty(TaskWaitHoursTextbox.Text) || !uint.TryParse(TaskWaitHoursTextbox.Text, out utemp))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Clears all the UI elements
        /// </summary>
        public void ClearUI()
        {
            BatchAccountKeyTextbox.Text = string.Empty;
            BatchAccountNameTextbox.Text = string.Empty;
            BatchAccountUrlTextbox.Text = string.Empty;
            SASExpiryHoursTextbox.Text = string.Empty;
            JobIDTextbox.Text = string.Empty;
            NumNodesTextbox.Text = string.Empty;
            OutputPathTextbox.Text = string.Empty;
            PoolIDTextbox.Text = string.Empty;
            PSharpBinariesTextbox.Text = string.Empty;
            PSharpTestCommandTextbox.Text = string.Empty;
            StorageAccountKeyTextbox.Text = string.Empty;
            StorageAccountNameTextbox.Text = string.Empty;
            TaskIDTextbox.Text = string.Empty;
            TaskWaitHoursTextbox.Text = string.Empty;
            LogTextBox.Text = string.Empty;
        }

        /// <summary>
        /// Save config file to local disk
        /// </summary>
        /// <param name="path"></param>
        public void SaveConfig()
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = UIConstants.FileExtensionFilter;
            fileDialog.AddExtension = true;
            fileDialog.ShowDialog();
            var path = fileDialog.FileName;
            if (string.IsNullOrEmpty(path)) { return; }
            if (null == config)
            {
                return;
            }
            if (!ExtractConfig())
            {
                MessageBox.Show(UIConstants.IncorrectParameterDuringSave);
                return;
            }
            //Save this config file
            config.SaveAsXML(path);
        }


        /// <summary>
        /// Action when Clear button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearUI();
        }


        /// <summary>
        /// Action when Run button is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {

            ClearLog();

            RunButton.IsEnabled = false;

            Console.WriteLine("Extracting Configurations");
            var isExtractSuccess = ExtractConfig();
            if (!isExtractSuccess || null == config || !config.Validate())
            {
                MessageBox.Show(UIConstants.IncorrectParameterDuringRun);
            }

            DeleteBlobAfterComplete = DeleteBlobAfterCompleteCheckbox.IsChecked ?? false;
            DeleteJobAfterComplete = DeleteJobAfterCompleteCheckbox.IsChecked ?? false;

            Console.WriteLine("Running application");
            var task = Task.Run(() =>
            {

                try
                {
                    Run().Wait();
                }
                catch (AggregateException ae)
                {
                    Console.WriteLine();
                    Console.WriteLine(ae.StackTrace);
                    Console.WriteLine(ae.Message);
                    Console.WriteLine();
                }

            });

            Task[] tasks = { task };
            Task.Factory.ContinueWhenAll(tasks, _ => {
                this.Dispatcher.Invoke(() =>
                {
                    RunButton.IsEnabled = true;
                    Console.SetOut(originalOut);
                });
                
            });
            
            //Console.SetOut(originalOut);
        }

        /// <summary>
        /// Log function which prints in the log textbox in UI
        /// </summary>
        /// <param name="logText"></param>
        public void LoggerFunction(string logText)
        {
            this.Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText(logText);
                LogTextBox.ScrollToEnd();
            });
        }

        /// <summary>
        /// Clears the log textbox in the UI
        /// </summary>
        public void ClearLog()
        {
            this.Dispatcher.Invoke(() =>
            {
                LogTextBox.Text = string.Empty;
            });
        }

        /// <summary>
        /// Prepping and running the batch testing
        /// </summary>
        /// <returns></returns>
        private async Task Run()
        {
            //Creating BatchOperations
            BatchOperations batchOperations = new BatchOperations(config.BatchAccountName, config.BatchAccountKey, config.BatchAccountUrl);

            //Creating BlobOperations
            BlobOperations blobOperations = new BlobOperations(config.StorageAccountName, config.StorageAccountKey, config.BlobContainerSasExpiryHours);


            //Pool operations
            if (!(await batchOperations.CheckIfPoolExists(config.PoolId)))
            {
                //Checking num nodes
                if (config.NumberOfNodesInPool > 10)
                {
                    MessageBoxResult result = MessageBox.Show(string.Format(UIConstants.NodeCreationConfirmationFormat, config.NumberOfNodesInPool), UIConstants.NodeCreationConfirmationDialogHeading, MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if(!(result == MessageBoxResult.Yes))
                    {
                        return;
                    }
                }
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
                       NodeStartCommand: PSharpBatchTestCommon.Constants.PSharpDefaultNodeStartCommand
                    );
            }

            //Job Details
            string jobManagerFilePath = /*typeof(PSharpBatchJobManager.Program).Assembly.Location;*/  @".\PSharpBatchJobManager\PSharpBatchJobManager.exe";   // Data files for Job Manager Task
            string jobTimeStamp = PSharpBatchTestCommon.Constants.GetTimeStamp();
            JobId = config.JobDefaultId + jobTimeStamp;

            //Task Details
            var testApplicationName = System.IO.Path.GetFileName(config.TestApplicationPath);

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
                    NumberOfTasks: config.NumberOfTasks,
                    IterationsPerTask: config.IterationsPerTask,
                    commandFlags: config.CommandFlags
                );


            //Monitor tasks
            await batchOperations.MonitorTasks
                (
                    jobId: JobId,
                    timeout: TimeSpan.FromHours(config.TaskWaitHours)
                );

            await blobOperations.DownloadOutputFiles(config.OutputFolderPath);

            //All task completed
            if (DeleteJobAfterComplete)
            {
                await batchOperations.DeleteJobAsync(JobId);
            }

            if (DeleteBlobAfterComplete)
            {
                await blobOperations.DeleteInputContainer();
                await blobOperations.DeleteJobManagerContainer();
                await blobOperations.DeleteOutputContainer();
            }
        }



        /// <summary>
        /// Prevent pasting of charecters in numeric textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NumberTextbox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            if (regex.IsMatch(e.DataObject.GetData(typeof(string)).ToString()))
            {
                e.CancelCommand();
            }
        }

        private void onWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Console.SetOut(originalOut);
        }

        private void LoadConfigCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            LoadConfig();
        }

        private void SaveConfigCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveConfig();
        }
    }

    public static class CustomCommands
    {
        public static readonly RoutedUICommand LoadConfig = new RoutedUICommand
                (
                        "Load Config",
                        "LoadConfig",
                        typeof(CustomCommands),
                        new InputGestureCollection()
                        {
                                        new KeyGesture(Key.L, ModifierKeys.Control)
                        }
                );

        public static readonly RoutedUICommand SaveConfig = new RoutedUICommand
                (
                        "Save Config",
                        "SaveConfig",
                        typeof(CustomCommands),
                        new InputGestureCollection()
                        {
                                        new KeyGesture(Key.S, ModifierKeys.Control)
                        }
                );

    }


    /// <summary>
    /// Class to redirect Console output to a custom log function
    /// </summary>
    public class ControlWriter : TextWriter
    {
        //private TextBlock textbox;

        /// <summary>
        /// Log function delegate
        /// </summary>
        /// <param name="logText"></param>
        public delegate void AppendLog(string logText);

        AppendLog logFunc;

        public ControlWriter(AppendLog logFunction)
        {
            logFunc = logFunction;
        }

        public override void Write(char value)
        {
            logFunc(""+value);
        }

        public override void Write(string value)
        {
            logFunc(value);
        }

        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }
    }
}
