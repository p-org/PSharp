// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Microsoft.PSharp.Visualization
{
    /// <summary>
    /// Interaction logic for VisualizeWindow.xaml
    /// </summary>
    public partial class VisualizeWindow : Window
    {
        public VisualizeWindow()
        {
            this.WindowState = WindowState.Maximized;
            InitializeComponent();
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            IO.LoadTrace();
        }
    }
}
