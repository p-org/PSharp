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
            var traceList = this.GetObservableTrace();
            this.BugTraceList.ItemsSource = traceList;
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

                traceList.Add(new BugTraceObject()
                {
                    Type = type,
                    Machine = traceStep.Machine,
                    MachineId = traceStep.MachineId,
                    TargetMachine = traceStep.TargetMachine,
                    TargetMachineId = traceStep.TargetMachineId,
                    Event = e
                });
            }

            return traceList;
        }

        #endregion
    }
}
