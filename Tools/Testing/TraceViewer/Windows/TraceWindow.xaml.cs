//-----------------------------------------------------------------------
// <copyright file="TraceWindow.xaml.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

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

        /// <summary>
        /// The open command.
        /// </summary>
        public RoutedCommand OpenCommand;

        /// <summary>
        /// The search command.
        /// </summary>
        public RoutedCommand SearchCommand;

        /// <summary>
        /// Map from search query to rows the
        /// query is satisfied.
        /// </summary>
        private Dictionary<string, Queue<int>> SearchQueryCache;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public TraceWindow()
        {
            this.SearchQueryCache = new Dictionary<string, Queue<int>>();

            this.WindowState = WindowState.Maximized;

            InitializeComponent();

            this.OpenCommand = new RoutedCommand();
            this.SearchCommand = new RoutedCommand();

            this.OpenCommand.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            this.SearchCommand.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control));

            base.CommandBindings.Add(new CommandBinding(this.OpenCommand, MenuItem_Open_Trace_Click));
            base.CommandBindings.Add(new CommandBinding(this.SearchCommand, MenuItem_Search_Focus));

            this.SearchTextBox.Search += SearchTextBox_Search;
        }

        #endregion

        #region actions

        private void MenuItem_Open_Trace_Click(object sender, RoutedEventArgs e)
        {
            this.BugTraceView.ItemsSource = null;
            this.BugTraceView.Items.Refresh();

            this.BugTrace = IO.LoadTrace();
            if (this.BugTrace != null)
            {
                var traceList = this.GetObservableTrace();
                this.BugTraceView.ItemsSource = traceList;
                this.BugTraceView.Items.Refresh();
            }
        }

        private void MenuItem_Close_Trace_Click(object sender, RoutedEventArgs e)
        {
            this.BugTraceView.ItemsSource = null;
            this.BugTraceView.Items.Refresh();
        }

        /// <summary>
        /// Focuses on the search text box.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">RoutedEventArgs</param>
        private void MenuItem_Search_Focus(object sender, RoutedEventArgs e)
        {
            this.SearchTextBox.Focus();
        }

        /// <summary>
        /// Performs the specified search in the trace view.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">RoutedEventArgs</param>
        private void SearchTextBox_Search(object sender, RoutedEventArgs e)
        {
            // If this is an empty query, clean up and return.
            if (this.SearchTextBox.Text.Length == 0)
            {
                this.RestoreTraceViewStyle();
                this.SearchQueryCache.Clear();
                return;
            }

            // Checks if this is a new search query.
            if (!this.SearchQueryCache.ContainsKey(this.SearchTextBox.Text))
            {
                this.RestoreTraceViewStyle();
                this.SearchQueryCache.Clear();
                this.SearchQueryCache.Add(this.SearchTextBox.Text, new Queue<int>());

                // Caches all rows that satisfy the search query, and disables
                // all remaining rows.
                for (int i = 0; i < this.BugTraceView.Items.Count; i++)
                {
                    DataGridRow row = (DataGridRow)this.BugTraceView.ItemContainerGenerator.ContainerFromIndex(i);

                    bool containsSearch = false;
                    for (int j = 1; j < this.BugTraceView.Columns.Count; j++)
                    {
                        TextBlock cellContent = this.BugTraceView.Columns[j].GetCellContent(row) as TextBlock;
                        if (cellContent.Text.Contains(this.SearchTextBox.Text))
                        {
                            containsSearch = true;
                            break;
                        }
                    }

                    if (containsSearch)
                    {
                        this.SearchQueryCache[this.SearchTextBox.Text].Enqueue(i);
                    }
                    else
                    {
                        this.FadeRow(row);
                    }
                }
            }

            // Restores the style of all rows related to the search query.
            foreach (var rowId in this.SearchQueryCache[this.SearchTextBox.Text])
            {
                DataGridRow row = (DataGridRow)this.BugTraceView.ItemContainerGenerator.ContainerFromIndex(rowId);
                this.RestoreRow(row);
            }

            if (this.SearchQueryCache[this.SearchTextBox.Text].Count > 0)
            {
                int rowId = this.SearchQueryCache[this.SearchTextBox.Text].Dequeue();
                this.SearchQueryCache[this.SearchTextBox.Text].Enqueue(rowId);

                DataGridRow row = (DataGridRow)this.BugTraceView.ItemContainerGenerator.ContainerFromIndex(rowId);
                row.Foreground = new SolidColorBrush(Colors.White);
                row.Background = new SolidColorBrush(Colors.Black);

                object item = this.BugTraceView.Items[rowId];
                this.BugTraceView.SelectedItem = item;
                this.BugTraceView.ScrollIntoView(item);
            }
        }

        private void TraceView_CurrentCellChanged(object sender, EventArgs e)
        {
            this.RestoreTraceViewStyle();

            if (this.BugTraceView.CurrentColumn != null &&
                this.BugTraceView.CurrentCell != null &&
                this.BugTraceView.CurrentColumn.Header is string &&
                this.BugTraceView.CurrentCell.Item is BugTraceObject)
            {
                string header = (string)this.BugTraceView.CurrentColumn.Header;
                BugTraceObject item = (BugTraceObject)this.BugTraceView.CurrentCell.Item;

                for (int i = 0; i < this.BugTraceView.Items.Count; i++)
                {
                    DataGridRow row = (DataGridRow)this.BugTraceView.ItemContainerGenerator.ContainerFromIndex(i);
                    TextBlock typeCellContent = this.BugTraceView.Columns[1].GetCellContent(row) as TextBlock;
                    TextBlock machineCellContent = this.BugTraceView.Columns[2].GetCellContent(row) as TextBlock;
                    TextBlock actionCellContent = this.BugTraceView.Columns[3].GetCellContent(row) as TextBlock;
                    TextBlock targetCellContent = this.BugTraceView.Columns[4].GetCellContent(row) as TextBlock;
                    
                    if (header.Equals("Type") &&
                        !typeCellContent.Text.Equals(item.Type))
                    {
                        this.FadeRow(row);
                    }
                    else if (header.Equals("Source Machine") &&
                        !machineCellContent.Text.Equals(item.SenderMachine))
                    {
                        this.FadeRow(row);
                    }
                    else if (header.Equals("Action") &&
                        !actionCellContent.Text.Equals(item.Action))
                    {
                        this.FadeRow(row);
                    }
                    else if (header.Equals("Target Machine") &&
                        !targetCellContent.Text.Equals(item.TargetMachine))
                    {
                        this.FadeRow(row);
                    }
                    else
                    {
                        this.RestoreRow(row);
                    }
                }
            }
        }

        private void TraceView_MouseDoubleClick(object sender, EventArgs e)
        {
            this.RestoreTraceViewStyle();

            if (this.BugTraceView.CurrentColumn != null &&
                this.BugTraceView.CurrentCell != null &&
                this.BugTraceView.CurrentColumn.Header is string &&
                this.BugTraceView.CurrentCell.Item is BugTraceObject)
            {
                string header = (string)this.BugTraceView.CurrentColumn.Header;
                BugTraceObject item = (BugTraceObject)this.BugTraceView.CurrentCell.Item;

                for (int i = 0; i < this.BugTraceView.Items.Count; i++)
                {
                    DataGridRow row = (DataGridRow)this.BugTraceView.ItemContainerGenerator.ContainerFromIndex(i);
                    TextBlock typeCellContent = this.BugTraceView.Columns[1].GetCellContent(row) as TextBlock;
                    TextBlock machineCellContent = this.BugTraceView.Columns[2].GetCellContent(row) as TextBlock;
                    TextBlock actionCellContent = this.BugTraceView.Columns[3].GetCellContent(row) as TextBlock;
                    TextBlock targetCellContent = this.BugTraceView.Columns[4].GetCellContent(row) as TextBlock;
                    
                    if (header.Equals("Type") &&
                        !typeCellContent.Text.Equals(item.Type))
                    {
                        row.Visibility = Visibility.Collapsed;
                    }
                    else if (header.Equals("Source Machine") &&
                        !machineCellContent.Text.Equals(item.SenderMachine))
                    {
                        row.Visibility = Visibility.Collapsed;
                    }
                    else if (header.Equals("Action") &&
                        !actionCellContent.Text.Equals(item.Action))
                    {
                        row.Visibility = Visibility.Collapsed;
                    }
                    else if (header.Equals("Target Machine") &&
                        !targetCellContent.Text.Equals(item.TargetMachine))
                    {
                        row.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void TraceView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is ScrollViewer)
            {
                this.RestoreTraceViewStyle();
            }
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
                string type = $"{traceStep.Type}";
                string action = "";

                if (traceStep.Type == BugTraceStepType.CreateMachine)
                {
                    action = "Created new machine.";
                }
                else if (traceStep.Type == BugTraceStepType.CreateMonitor)
                {
                    action = "Created new monitor.";
                }
                else if (traceStep.Type == BugTraceStepType.SendEvent)
                {
                    action = $"Sent event '{traceStep.Event}'.";
                }
                else if (traceStep.Type == BugTraceStepType.RaiseEvent)
                {
                    action = $"Raised event '{traceStep.Event}'.";
                }
                else if (traceStep.Type == BugTraceStepType.InvokeAction)
                {
                    action = $"Invoked action '{traceStep.InvokedAction}'.";
                }
                else if (traceStep.Type == BugTraceStepType.RandomChoice)
                {
                    action = $"Nondeterministically chose '{traceStep.RandomChoice}'.";
                }

                string senderMachine = "";
                if (traceStep.MachineId > 0)
                {
                    senderMachine = $"{traceStep.Machine}({traceStep.MachineId})";
                }
                else if (traceStep.Type == BugTraceStepType.CreateMachine ||
                    traceStep.Type == BugTraceStepType.CreateMonitor ||
                    traceStep.Type == BugTraceStepType.SendEvent ||
                    traceStep.Type == BugTraceStepType.RandomChoice)
                {
                    senderMachine = $"[Environment]";
                }

                string targetMachine = "";
                if (traceStep.TargetMachineId > 0)
                {
                    targetMachine = $"{traceStep.TargetMachine}({traceStep.TargetMachineId})";
                }
                else if (traceStep.Type == BugTraceStepType.SendEvent)
                {
                    targetMachine = $"[Environment]";
                }
                else if (traceStep.Type == BugTraceStepType.RaiseEvent ||
                    traceStep.Type == BugTraceStepType.InvokeAction ||
                    traceStep.Type == BugTraceStepType.RandomChoice)
                {
                    targetMachine = $"[Self]";
                }

                traceList.Add(new BugTraceObject()
                {
                    Id = traceList.Count,
                    Type = type,
                    SenderMachine = senderMachine,
                    Action = action,
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
                this.RestoreRow(row);
            }
        }

        /// <summary>
        /// Restores the specified row to its defalt style.
        /// </summary>
        /// <param name="row">DataGridRow</param>
        private void RestoreRow(DataGridRow row)
        {
            TextBlock cellContent = this.BugTraceView.Columns[1].GetCellContent(row) as TextBlock;
            if (cellContent != null && cellContent.Text != null)
            {
                row.Visibility = Visibility.Visible;
            }

            if (cellContent.Text.Equals("CreateMachine"))
            {
                row.Foreground = new SolidColorBrush(Colors.White);
                row.Background = new SolidColorBrush(Colors.BlueViolet);
            }
            else if (cellContent.Text.Equals("CreateMonitor"))
            {
                row.Foreground = new SolidColorBrush(Colors.White);
                row.Background = new SolidColorBrush(Colors.DarkSlateGray);
            }
            else if (cellContent.Text.Equals("SendEvent"))
            {
                row.Foreground = new SolidColorBrush(Colors.White);
                row.Background = new SolidColorBrush(Colors.RoyalBlue);
            }
            else if (cellContent.Text.Equals("RaiseEvent"))
            {
                row.Foreground = new SolidColorBrush(Colors.Black);
                row.Background = new SolidColorBrush(Colors.GreenYellow);
            }
            else if (cellContent.Text.Equals("InvokeAction"))
            {
                row.Foreground = new SolidColorBrush(Colors.Black);
                row.Background = new SolidColorBrush(Colors.BurlyWood);
            }
            else if (cellContent.Text.Equals("RandomChoice"))
            {
                row.Foreground = new SolidColorBrush(Colors.Black);
                row.Background = new SolidColorBrush(Colors.Orange);
            }
        }

        /// <summary>
        /// Fades the specified row.
        /// </summary>
        /// <param name="row">DataGridRow</param>
        private void FadeRow(DataGridRow row)
        {
            row.Foreground = new SolidColorBrush(Colors.Silver);
            row.Background = new SolidColorBrush(Colors.WhiteSmoke);
        }

        #endregion
    }
}
