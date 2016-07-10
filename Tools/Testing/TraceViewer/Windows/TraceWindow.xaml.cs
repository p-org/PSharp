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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
        /// The observable bug trace.
        /// </summary>
        private ObservableCollection<BugTraceObject> ObservableBugTrace;

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

        /// <summary>
        /// Checks if the trace view is currently collapsed.
        /// </summary>
        private bool IsTraceViewCollapsed;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public TraceWindow()
        {
            this.SearchQueryCache = new Dictionary<string, Queue<int>>();
            this.IsTraceViewCollapsed = false;

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
            this.BugTrace = IO.LoadTrace();
            if (this.BugTrace != null)
            {
                this.ObservableBugTrace = this.GetObservableTrace();
                this.BugTraceView.ItemsSource = this.ObservableBugTrace;
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

                // Caches all row ids that satisfy the search query.
                foreach (var item in this.ObservableBugTrace)
                {
                    if (item.Type.Contains(this.SearchTextBox.Text) ||
                        item.Action.Contains(this.SearchTextBox.Text) ||
                        item.Action.Contains(this.SearchTextBox.Text) ||
                        item.TargetMachine.Contains(this.SearchTextBox.Text))
                    {
                        var rowId = this.ObservableBugTrace.IndexOf(item);
                        this.SearchQueryCache[this.SearchTextBox.Text].Enqueue(rowId);
                    }
                }
            }

            // Updates the rows based on the search query.
            for (int i = 0; i < this.BugTraceView.Items.Count; i++)
            {
                DataGridRow row = (DataGridRow)this.BugTraceView.ItemContainerGenerator.ContainerFromIndex(i);
                if (row == null) continue;
                this.UpdateRow(row);
            }

            if (this.SearchQueryCache[this.SearchTextBox.Text].Count > 0)
            {
                int rowId = this.SearchQueryCache[this.SearchTextBox.Text].Dequeue();
                this.SearchQueryCache[this.SearchTextBox.Text].Enqueue(rowId);

                this.BugTraceView.SelectedIndex = rowId;
                this.BugTraceView.ScrollIntoView(this.BugTraceView.SelectedItem);

                DataGridRow row = (DataGridRow)this.BugTraceView.ItemContainerGenerator.ContainerFromIndex(rowId);
                this.SelectRow(row);
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
                    if (row == null) continue;
                    this.UpdateRow(row);
                }
            }
        }

        private void MenuItem_Collapse_Click(object sender, EventArgs e)
        {
            if (this.IsTraceViewCollapsed)
            {
                this.MenuItem__Collapse.Header = "_Collapse";
                this.RestoreTraceViewStyle();
                return;
            }

            this.MenuItem__Collapse.Header = "_Expand";
            this.IsTraceViewCollapsed = true;

            BugTraceObject selectedItem = null;
            string selectedHeader = null;

            if (this.BugTraceView.CurrentCell != null &&
                this.BugTraceView.CurrentCell.Item is BugTraceObject)
            {
                selectedItem = (BugTraceObject)this.BugTraceView.CurrentCell.Item;
                selectedHeader = (string)this.BugTraceView.CurrentCell.Column.Header;
            }

            if (selectedItem != null && selectedHeader != null)
            {
                for (int i = 0; i < this.BugTraceView.Items.Count; i++)
                {
                    DataGridRow row = (DataGridRow)this.BugTraceView.ItemContainerGenerator.ContainerFromIndex(i);
                    if (row == null) continue;

                    BugTraceObject item = (BugTraceObject)row.DataContext;

                    if ((selectedHeader.Equals("Type") &&
                        !item.Type.Equals(selectedItem.Type)) ||
                        (selectedHeader.Equals("Machine") &&
                        !item.Machine.Equals(selectedItem.Machine)) ||
                        (selectedHeader.Equals("State") &&
                        !item.MachineState.Equals(selectedItem.MachineState)) ||
                        (selectedHeader.Equals("Action") &&
                        !item.Action.Equals(selectedItem.Action)) ||
                        (selectedHeader.Equals("Target Machine") &&
                        !item.TargetMachine.Equals(selectedItem.TargetMachine)))
                    {
                        this.CollapseRow(row);
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

        private void TraceView_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            this.UpdateRow(e.Row);
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
                    action = $"Sent event '{traceStep.EventInfo.EventName}'.";
                }
                else if (traceStep.Type == BugTraceStepType.DequeueEvent)
                {
                    action = $"Dequeued event '{traceStep.EventInfo.EventName}'.";
                }
                else if (traceStep.Type == BugTraceStepType.RaiseEvent)
                {
                    action = $"Raised event '{traceStep.EventInfo.EventName}'.";
                }
                else if (traceStep.Type == BugTraceStepType.GotoState)
                {
                    action = $"Transitions to state '{traceStep.MachineState}'.";
                }
                else if (traceStep.Type == BugTraceStepType.InvokeAction)
                {
                    action = $"Invoked action '{traceStep.InvokedAction}'.";
                }
                else if (traceStep.Type == BugTraceStepType.WaitToReceive)
                {
                    action = $"Waiting to receive events:{traceStep.ExtraInfo}.";
                }
                else if (traceStep.Type == BugTraceStepType.ReceiveEvent)
                {
                    action = $"Received events '{traceStep.EventInfo.EventName}' and unblocked.";
                }
                else if (traceStep.Type == BugTraceStepType.RandomChoice)
                {
                    action = $"Nondeterministically chose '{traceStep.RandomChoice}'.";
                }
                else if (traceStep.Type == BugTraceStepType.Halt)
                {
                    action = $"Machine has halted.";
                }

                string machine = "";
                if (traceStep.Machine != null)
                {
                    machine = $"{traceStep.Machine}";
                }
                else if (traceStep.Type == BugTraceStepType.CreateMachine ||
                    traceStep.Type == BugTraceStepType.CreateMonitor ||
                    traceStep.Type == BugTraceStepType.SendEvent ||
                    traceStep.Type == BugTraceStepType.RandomChoice)
                {
                    machine = $"[Environment]";
                }

                string machineState = "";
                if (traceStep.MachineState != null)
                {
                    machineState = traceStep.MachineState;
                }
                else
                {
                    machineState = "[None]";
                }

                string targetMachine = "";
                if (traceStep.TargetMachine != null)
                {
                    targetMachine = $"{traceStep.TargetMachine}";
                }
                else if (traceStep.Type == BugTraceStepType.SendEvent)
                {
                    targetMachine = $"[Environment]";
                }
                else if (traceStep.Type == BugTraceStepType.DequeueEvent ||
                    traceStep.Type == BugTraceStepType.RaiseEvent ||
                    traceStep.Type == BugTraceStepType.GotoState ||
                    traceStep.Type == BugTraceStepType.InvokeAction ||
                    traceStep.Type == BugTraceStepType.WaitToReceive ||
                    traceStep.Type == BugTraceStepType.ReceiveEvent ||
                    traceStep.Type == BugTraceStepType.RandomChoice ||
                    traceStep.Type == BugTraceStepType.Halt)
                {
                    targetMachine = $"[Self]";
                }

                traceList.Add(new BugTraceObject()
                {
                    Id = traceList.Count,
                    Type = type,
                    Machine = machine,
                    MachineState = machineState,
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
                if (row == null) continue;
                this.RestoreRow(row);
            }

            if (this.IsTraceViewCollapsed)
            {
                this.MenuItem__Collapse.Header = "_Collapse";
                this.IsTraceViewCollapsed = false;
            }
        }

        /// <summary>
        /// Updates the specified row.
        /// </summary>
        /// <param name="row">DataGridRow</param>
        private void UpdateRow(DataGridRow row)
        {
            BugTraceObject loadingItem = (BugTraceObject)row.DataContext;
            BugTraceObject selectedItem = null;
            string selectedHeader = null;

            if (this.BugTraceView.CurrentCell != null &&
                this.BugTraceView.CurrentCell.Item is BugTraceObject)
            {
                selectedItem = (BugTraceObject)this.BugTraceView.CurrentCell.Item;
                selectedHeader = (string)this.BugTraceView.CurrentCell.Column.Header;
            }

            if (this.SearchTextBox.Text.Length > 0 &&
                this.SearchQueryCache.ContainsKey(this.SearchTextBox.Text))
            {
                int rowId = row.GetIndex();
                if (this.SearchQueryCache[this.SearchTextBox.Text].Contains(rowId))
                {
                    this.RestoreRow(row);
                }
                else
                {
                    this.FadeRow(row);
                }
            }
            else if (selectedItem != null && selectedHeader != null &&
                ((selectedHeader.Equals("Type") &&
                !loadingItem.Type.Equals(selectedItem.Type)) ||
                (selectedHeader.Equals("Machine") &&
                !loadingItem.Machine.Equals(selectedItem.Machine)) ||
                (selectedHeader.Equals("State") &&
                !loadingItem.MachineState.Equals(selectedItem.MachineState)) ||
                (selectedHeader.Equals("Action") &&
                !loadingItem.Action.Equals(selectedItem.Action)) ||
                (selectedHeader.Equals("Target Machine") &&
                !loadingItem.TargetMachine.Equals(selectedItem.TargetMachine))))
            {
                if (this.IsTraceViewCollapsed)
                {
                    this.CollapseRow(row);
                }
                else
                {
                    this.FadeRow(row);
                }
            }
            else
            {
                this.RestoreRow(row);
            }
        }

        /// <summary>
        /// Restores the specified row to its defalt style.
        /// </summary>
        /// <param name="row">DataGridRow</param>
        private void RestoreRow(DataGridRow row)
        {
            BugTraceObject context = row.DataContext as BugTraceObject;
            
            if (context.Type.Equals("CreateMachine"))
            {
                row.Foreground = new SolidColorBrush(Colors.White);
                row.Background = new SolidColorBrush(Colors.Teal);
            }
            else if (context.Type.Equals("CreateMonitor"))
            {
                row.Foreground = new SolidColorBrush(Colors.White);
                row.Background = new SolidColorBrush(Colors.DarkSlateGray);
            }
            else if (context.Type.Equals("SendEvent"))
            {
                row.Foreground = new SolidColorBrush(Colors.White);
                row.Background = new SolidColorBrush(Colors.RoyalBlue);
            }
            else if (context.Type.Equals("DequeueEvent"))
            {
                row.Foreground = new SolidColorBrush(Colors.White);
                row.Background = new SolidColorBrush(Colors.SteelBlue);
            }
            else if (context.Type.Equals("RaiseEvent"))
            {
                row.Foreground = new SolidColorBrush(Colors.Black);
                row.Background = new SolidColorBrush(Colors.LightSalmon);
            }
            else if (context.Type.Equals("GotoState"))
            {
                row.Foreground = new SolidColorBrush(Colors.Black);
                row.Background = new SolidColorBrush(Colors.Khaki);
            }
            else if (context.Type.Equals("InvokeAction"))
            {
                row.Foreground = new SolidColorBrush(Colors.Black);
                row.Background = new SolidColorBrush(Colors.BurlyWood);
            }
            else if (context.Type.Equals("WaitToReceive"))
            {
                row.Foreground = new SolidColorBrush(Colors.White);
                row.Background = new SolidColorBrush(Colors.DarkOrchid);
            }
            else if (context.Type.Equals("ReceiveEvent"))
            {
                row.Foreground = new SolidColorBrush(Colors.White);
                row.Background = new SolidColorBrush(Colors.DarkOrchid);
            }
            else if (context.Type.Equals("RandomChoice"))
            {
                row.Foreground = new SolidColorBrush(Colors.White);
                row.Background = new SolidColorBrush(Colors.BlueViolet);
            }
            else if (context.Type.Equals("Halt"))
            {
                row.Foreground = new SolidColorBrush(Colors.White);
                row.Background = new SolidColorBrush(Colors.DimGray);
            }

            row.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Selects the specified row.
        /// </summary>
        /// <param name="row">DataGridRow</param>
        private void SelectRow(DataGridRow row)
        {
            row.Foreground = new SolidColorBrush(Colors.White);
            row.Background = new SolidColorBrush(Colors.Black);
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

        /// <summary>
        /// Collapses the specified row.
        /// </summary>
        /// <param name="row">DataGridRow</param>
        private void CollapseRow(DataGridRow row)
        {
            row.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}
