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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace outphasing_pa_test_equipment_controller
    {
    /// <summary>
    /// Interaction logic for VisaDebugger.xaml
    /// </summary>
    public partial class VisaDebugger : UserControl
        {
        public list_visa_devices_dialogue.VisaDevice device { get; set; }
        public VisaDebugger()
            {
            InitializeComponent();
            }

        private void WriteButton_Click(object sender, RoutedEventArgs e)
            {
            var command = WriteTextBox.Text;
            device.connection.RawIO.Write(command);
            }

        private void QueryButton_Click(object sender, RoutedEventArgs e)
            {
            var query = WriteTextBox.Text;
            ReadTextBlock.Text = device.ReadString(query);
            }
        }
    }