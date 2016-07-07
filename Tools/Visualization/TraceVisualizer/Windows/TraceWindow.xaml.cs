using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Text;
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

using Microsoft.PSharp.TestingServices.Tracing.Error;

namespace Microsoft.PSharp.Visualization
{
    /// <summary>
    /// Interaction logic for TraceWindow.xaml
    /// </summary>
    public partial class TraceWindow : Window
    {
        #region fields

        /// <summary>
        /// The bug trace.
        /// </summary>
        private BugTrace BugTrace;

        #endregion

        #region constructor

        public TraceWindow()
        {
            this.WindowState = WindowState.Maximized;
            InitializeComponent();
        }

        #endregion

        #region actions
        
        private void MenuItem_Load_Click(object sender, RoutedEventArgs e)
        {
            this.BugTrace = IO.LoadTrace();
            if (this.BugTrace != null)
            {
                var traceList = this.GetObservableTrace();
                this.BugTraceView.ItemsSource = traceList;
            }
        }

        private void Row_CurrentCellChanged(object sender, EventArgs e)
        {
            //MessageBox.Show((string)this.BugTraceView.CurrentColumn.Header);

            // Restores the trace view style.
            this.RestoreTraceViewStyle();

            if (this.BugTraceView.CurrentColumn.Header is string &&
                this.BugTraceView.CurrentCell.Item is BugTraceObject)
            {
                string header = (string)this.BugTraceView.CurrentColumn.Header;
                BugTraceObject item = (BugTraceObject)this.BugTraceView.CurrentCell.Item;

                if (header.Equals("Type"))
                {
                    for (int i = 0; i < this.BugTraceView.Items.Count; i++)
                    {
                        DataGridRow row = (DataGridRow)this.BugTraceView.ItemContainerGenerator.ContainerFromIndex(i);
                        TextBlock cellContent = this.BugTraceView.Columns[0].GetCellContent(row) as TextBlock;
                        if (cellContent != null && cellContent.Text != null &&
                            !cellContent.Text.Equals(item.Type))
                        {
                            row.Background = new SolidColorBrush(Colors.LightGray);
                        }
                    }
                }
            }
        }

        private void Row_MouseLeave(object sender, EventArgs e)
        {
            // Restores the trace view style.
            this.RestoreTraceViewStyle();
        }

        #endregion

        #region utility methods

        /// <summary>
        /// Returns an observable bug trace.
        /// </summary>
        /// <returns>ObservableCollection</returns>
        private ObservableCollection<BugTraceObject> GetObservableTrace()
        {
            var traceList = new ObservableCollection<BugTraceObject>();
            foreach (var traceStep in this.BugTrace)
            {
                string type = null;
                string e = "";
                if (traceStep.Type == BugTraceStepType.CreateMachine)
                {
                    type = "Create";
                }
                else if (traceStep.Type == BugTraceStepType.SendEvent)
                {
                    type = "Send";
                    e = traceStep.Event;
                }

                string senderMachine = $"{traceStep.Machine}({traceStep.MachineId})";
                string targetMachine = $"{traceStep.TargetMachine}({traceStep.TargetMachineId})";

                traceList.Add(new BugTraceObject()
                {
                    Type = type,
                    SenderMachine = senderMachine,
                    Event = e,
                    TargetMachine = targetMachine
                });
            }

            return traceList;
        }

        /// <summary>
        /// Restores the style of the trace view.
        /// </summary>
        private void RestoreTraceViewStyle()
        {
            for (int i = 0; i < this.BugTraceView.Items.Count; i++)
            {
                DataGridRow row = (DataGridRow)this.BugTraceView.ItemContainerGenerator.ContainerFromIndex(i);
                TextBlock cellContent = this.BugTraceView.Columns[0].GetCellContent(row) as TextBlock;
                if (cellContent != null && cellContent.Text != null)
                {
                    if (cellContent.Text.Equals("Create"))
                    {
                        row.Background = new SolidColorBrush(Colors.Aqua);
                    }
                    else if (cellContent.Text.Equals("Send"))
                    {
                        row.Background = new SolidColorBrush(Colors.GreenYellow);
                    }
                }
            }
        }

        #endregion
    }
}
