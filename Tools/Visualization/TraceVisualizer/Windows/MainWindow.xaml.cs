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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region fields

        /// <summary>
        /// The bug trace.
        /// </summary>
        private BugTrace Trace;

        #endregion

        #region constructor

        public MainWindow()
        {
            this.WindowState = WindowState.Maximized;
            InitializeComponent();
        }

        #endregion

        #region actions

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            this.Trace = IO.LoadTrace();
            var traceList = new ObservableCollection<BugTraceStep>();
            foreach (var step in this.Trace)
            {
                trace.Add(new BugTraceStep() { step });
            }
        }

        #endregion
    }
}
