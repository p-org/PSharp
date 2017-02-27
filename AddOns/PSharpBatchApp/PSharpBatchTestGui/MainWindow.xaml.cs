using Microsoft.Win32;
using PSharpBatchTestCommon;
using System;
using System.Collections.Generic;
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
        private PSharpBatchConfig mConfig;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// To prevent charecters in Number of nodes entry
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NumNodesTextbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        //Load Config
        public void LoadConfig(string path)
        {
            mConfig = PSharpBatchConfig.LoadFromXML(path);
            PSharpOperations.ParseConfig(mConfig);
            ApplyConfig();
        }

        public void ApplyConfig()
        {
            BatchAccountKeyTextbox.Text = mConfig.BatchAccountKey;
            BatchAccountNameTextbox.Text = mConfig.BatchAccountName;
            BatchAccountUrlTextbox.Text = mConfig.BatchAccountUrl;
            SASExpiryHoursTextbox.Text = mConfig.BlobContainerSasExpiryHours.ToString();
            JobIDTextbox.Text = mConfig.JobDefaultId;
            NumNodesTextbox.Text = mConfig.NumberOfNodesInPool.ToString();
            OutputPathTextbox.Text = mConfig.OutputFolderPath;
            PoolIDTextbox.Text = mConfig.PoolId;
            PSharpBinariesTextbox.Text = mConfig.PSharpBinariesFolderPath;
            PSharpTestCommandTextbox.Text = mConfig.PSharpTestCommand;
            StorageAccountKeyTextbox.Text = mConfig.StorageAccountKey;
            StorageAccountNameTextbox.Text = mConfig.StorageAccountName;
            TaskIDTextbox.Text = mConfig.TaskDefaultId;
            TaskWaitHoursTextbox.Text= mConfig.TaskWaitHours.ToString();
        }

        public void ExtractConfig()
        {
            mConfig = new PSharpBatchConfig
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

            PSharpBatchTestCommon.PSharpOperations.ParseConfig(mConfig);
        }

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
        }

        public void SaveConfig(string path)
        {
            if(null == mConfig)
            {
                return;
            }
            ExtractConfig();
            //Save this config file
            mConfig.SaveAsXML(path);
        }

        private void LoadConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "CONFIG|*.config";
            fileDialog.ShowDialog();
            var path = fileDialog.FileName;
            if (string.IsNullOrEmpty(path)) { return; }
            LoadConfig(path);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearUI();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            ExtractConfig();
        }

        private void Run()
        {

        }
    }
}
